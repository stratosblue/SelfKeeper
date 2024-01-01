using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SelfKeeper;

internal sealed class SelfKeeperService
{
    private readonly ProcessStartInfo _baseProcessStartInfo;

    private readonly KeepSelfFeatureFlag _features;

    private readonly ILogger? _logger;

    private readonly KeepSelfHostOptions _options;

    private readonly IWorkerProcessLifeCircleManager? _workerProcessLifeCircleManager;

    private volatile bool _isShutdownRequested = false;

    public SelfKeeperService(KeepSelfHostOptions options, ProcessStartInfo baseProcessStartInfo)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _baseProcessStartInfo = baseProcessStartInfo ?? throw new ArgumentNullException(nameof(baseProcessStartInfo));

        _features = options.Features;

        _logger = options.Logger;

        _workerProcessLifeCircleManager = options.WorkerProcessLifeCircleManager;

        _baseProcessStartInfo.ArgumentList.Add(options.WorkerProcessOptionsCommandArgumentName);
        _baseProcessStartInfo.ArgumentList.Add("KeepSelfWorkerProcessOptionsValue"); //占位
    }

    public int? Run()
    {
        Process? workerProcess = null;

        void HandlePosixSignal(PosixSignalContext context)
        {
            _logger?.Debug("Occurs PosixSignal {PosixSignal}.", context.Signal);
            _isShutdownRequested = true;

            try
            {
                SendPosixSignal(workerProcess, context.Signal);
            }
            catch (Exception ex)
            {
                _logger?.Warn("Send posix signal to process {ProcessId} error. {ExceptionMessage}", workerProcess?.Id.ToString() ?? string.Empty, ex.Message);
            }

            //忽略终止，等待工作进程退出
            context.Cancel = true;
        }

        using var signalRegistrationSIGINT = PosixSignalRegistration.Create(PosixSignal.SIGINT, HandlePosixSignal);
        using var signalRegistrationSIGQUIT = PosixSignalRegistration.Create(PosixSignal.SIGQUIT, HandlePosixSignal);
        using var signalRegistrationSIGTERM = PosixSignalRegistration.Create(PosixSignal.SIGTERM, HandlePosixSignal);

        CheckForceGC(KeepSelfFeatureFlag.ForceGCBeforeRunKeepService);

        Func<ProcessStartInfo, Process> startWorkerProcessDelegate = static processStartInfo => Process.Start(processStartInfo) ?? throw new InvalidOperationException($"Start worker process fail. {nameof(Process)}.{nameof(Process.Start)} returned null.");

        if (_workerProcessLifeCircleManager is not null)
        {
            var defaultWorkerProcessDelegate = startWorkerProcessDelegate;
            startWorkerProcessDelegate = processStartInfo => _workerProcessLifeCircleManager.OnStarting(processStartInfo, defaultWorkerProcessDelegate);
        }

        while (!_isShutdownRequested)
        {
            var sessionId = SelfKeeperEnvironment.GenerateSessionId();

            IDisposable? processKillSignalMonitor = _features.Contains(KeepSelfFeatureFlag.DisableForceKillByHost)
                                                        ? null
                                                        : WorkerProcessKillSignalMonitor.Create(Environment.ProcessId, sessionId, waitSuccess => ProcessKillSignalCallback(workerProcess, waitSuccess));

            try
            {
                workerProcess = startWorkerProcessDelegate(GetProcessStartInfo(sessionId));

                if (_isShutdownRequested)
                {
                    try
                    {
                        workerProcess.Kill(true);

                        workerProcess = _workerProcessLifeCircleManager?.OnExited(workerProcess) ?? workerProcess;

                        return workerProcess.ExitCode;
                    }
                    catch (Exception ex)
                    {
                        _logger?.Warn("Shutdown has requested. Host force kill worker process {ProcessId} fail. {ExceptionMessage}", workerProcess.Id, ex.Message);
                    }

                    return null;
                }

                _logger?.Debug("Worker process {ProcessId} for session {SessionId} was started.", workerProcess.Id, sessionId);

                workerProcess.WaitForExit();

                workerProcess = _workerProcessLifeCircleManager?.OnExited(workerProcess) ?? workerProcess;

                var exitCode = workerProcess.ExitCode;
                if (_options.ExcludeRestartExitCodes?.Contains(exitCode) == true)
                {
                    //认为程序正常退出
                    _logger?.Info("Worker process \"{WorkerProcessId}\" for session \"{SessionId}\" exited with code \"{WorkerProcessExitCode}\". This exit code will not restart.", workerProcess.Id, sessionId, workerProcess.ExitCode);
                    return exitCode;
                }
            }
            catch (Exception ex)
            {
                if (_isShutdownRequested)
                {
                    _logger?.Debug("Start and wait worker process fail. And shutdown has requested. {ExceptionMessage}", _options.StartFailRetryDelay.TotalSeconds, ex.Message);
                    return null;
                }

                _logger?.Error("Start and wait worker process fail. Retry after {StartFailRetryDelay} seconds. {ExceptionMessage}", _options.StartFailRetryDelay.TotalSeconds, ex.Message);
                Thread.Sleep(_options.StartFailRetryDelay);
                continue;
            }
            finally
            {
                processKillSignalMonitor?.Dispose();
            }

            if (_isShutdownRequested)
            {
                return workerProcess.ExitCode;
            }

            _logger?.Warn("Worker process \"{WorkerProcessId}\" for session \"{SessionId}\" exited with code \"{WorkerProcessExitCode}\". A new process is about to start after {RestartDelay} seconds.", workerProcess.Id, sessionId, workerProcess.ExitCode, _options.RestartDelay.TotalSeconds);

            workerProcess = null;

            CheckForceGC(KeepSelfFeatureFlag.ForceGCAfterWorkerProcessExited);

            Thread.Sleep(_options.RestartDelay);
        }

        return null;
    }

    #region base

    private static void SendPosixSignal(Process? process, PosixSignal signal)
    {
        if (OperatingSystem.IsLinux()
            && process?.Id is int processId)
        {
            LinuxSignalUtil.KillWithSignal(processId, signal);
        }
    }

    private void CheckForceGC(KeepSelfFeatureFlag flag)
    {
        if (_features.Contains(flag))
        {
            GC.Collect();
        }
    }

    private ProcessStartInfo GetProcessStartInfo(uint sessionId)
    {
        var workerProcessOptions = new KeepSelfWorkerProcessOptions(sessionId)
        {
            ParentProcessId = Environment.ProcessId,
            Features = _features,
        };

        _baseProcessStartInfo.ArgumentList.RemoveAt(_baseProcessStartInfo.ArgumentList.Count - 1);
        _baseProcessStartInfo.ArgumentList.Add(workerProcessOptions.ToCommandLineArgumentValue());

        return _baseProcessStartInfo;
    }

    private void ProcessKillSignalCallback(Process? workerProcess, bool waitSuccess)
    {
        if (workerProcess is not Process process)
        {
            return;
        }

        if (waitSuccess)
        {
            _logger?.Warn("Signal for force kill by host received. Force kill worker process {ProcessId}.", process.Id);
        }
        else if (_isShutdownRequested)
        {
            return;
        }
        else
        {
            _logger?.Warn("Process kill signal wait fail. The process may have exited. Try to force kill the worker process {ProcessId}.", process.Id);
        }

        ForceKill(process);

        void ForceKill(Process process)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(true);
                }
            }
            catch (Exception ex)
            {
                _logger?.Warn("Host force kill worker process {ProcessId} fail. {ExceptionMessage}", process.Id, ex.Message);
            }
        }
    }

    #endregion base
}
