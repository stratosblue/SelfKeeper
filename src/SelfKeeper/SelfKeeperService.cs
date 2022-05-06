using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SelfKeeper;

internal sealed class SelfKeeperService
{
    private readonly ProcessStartInfo _baseProcessStartInfo;
    private readonly KeepSelfFeatureFlag _features;
    private readonly ILogger? _logger;
    private readonly KeepSelfHostOptions _options;
    private volatile bool _isShutdownRequested = false;

    public SelfKeeperService(KeepSelfHostOptions options, ProcessStartInfo baseProcessStartInfo)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _baseProcessStartInfo = baseProcessStartInfo ?? throw new ArgumentNullException(nameof(baseProcessStartInfo));

        _features = options.Features;

        _logger = options.Logger;

        _baseProcessStartInfo.ArgumentList.Add(SelfKeeperEnvironment.KeepSelfChildProcessCommandArgumentName);
        _baseProcessStartInfo.ArgumentList.Add("KeepSelfChildProcessOptionsValue"); //占位
    }

    public int? Run()
    {
        Process? childProcess = null;

        void HandlePosixSignal(PosixSignalContext context)
        {
            _logger?.Debug("Occurs PosixSignal {PosixSignal}.", context.Signal);
            _isShutdownRequested = true;

            try
            {
                SendPosixSignal(childProcess, context.Signal);
            }
            catch (Exception ex)
            {
                _logger?.Warn("Send posix signal to process {ProcessId} error. {ExceptionMessage}", childProcess?.Id.ToString() ?? string.Empty, ex.Message);
            }

            context.Cancel = true;
        }

        //忽略终止，等待子进程退出
        using var signalRegistrationSIGINT = PosixSignalRegistration.Create(PosixSignal.SIGINT, HandlePosixSignal);
        using var signalRegistrationSIGQUIT = PosixSignalRegistration.Create(PosixSignal.SIGQUIT, HandlePosixSignal);
        using var signalRegistrationSIGTERM = PosixSignalRegistration.Create(PosixSignal.SIGTERM, HandlePosixSignal);

        CheckForceGC(KeepSelfFeatureFlag.ForceGCBeforeRunKeepService);

        while (!_isShutdownRequested)
        {
            var sessionId = SelfKeeperEnvironment.GenerateSessionId();

            IDisposable? processKillSignalMonitor = _features.Contains(KeepSelfFeatureFlag.DisableForceKillByHost)
                                                        ? null
                                                        : ChildProcessKillSignalMonitor.Create(Environment.ProcessId, sessionId, waitSuccess => ProcessKillSignalCallback(childProcess, waitSuccess));

            try
            {
                childProcess = Process.Start(GetProcessStartInfo(sessionId)) ?? throw new InvalidOperationException($"Start child process fail. {nameof(Process)}.{nameof(Process.Start)} returned null.");

                if (_isShutdownRequested)
                {
                    try
                    {
                        childProcess.Kill(true);
                        return childProcess.ExitCode;
                    }
                    catch (Exception ex)
                    {
                        _logger?.Warn("Shutdown has requested. Host force kill child process {processId} fail. {exceptionMessage}", childProcess.Id, ex.Message);
                    }

                    return null;
                }

                _logger?.Debug("Process {ProcessId} for session {SessionId} was started.", childProcess.Id, sessionId);

                childProcess.WaitForExit();
            }
            catch (Exception ex)
            {
                if (_isShutdownRequested)
                {
                    _logger?.Debug("Start and wait child process fail. And shutdown has requested. {ExceptionMessage}", _options.StartFailRetryDelay.TotalSeconds, ex.Message);
                    return null;
                }

                _logger?.Error("Start and wait child process fail. Retry after {StartFailRetryDelay} seconds. {ExceptionMessage}", _options.StartFailRetryDelay.TotalSeconds, ex.Message);
                Thread.Sleep(_options.StartFailRetryDelay);
                continue;
            }
            finally
            {
                processKillSignalMonitor?.Dispose();
            }

            if (_isShutdownRequested)
            {
                return childProcess.ExitCode;
            }

            _logger?.Warn("Child process \"{ChildProcessId}\" for session \"{SessionId}\" exited with code \"{ChildProcessExitCode}\". A new process is about to start after {RestartDelay} seconds.", childProcess.Id, sessionId, childProcess.ExitCode, _options.RestartDelay.TotalSeconds);

            childProcess = null;

            CheckForceGC(KeepSelfFeatureFlag.ForceGCAfterChildProcessExited);

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
        var childProcessOptions = new KeepSelfChildProcessOptions(sessionId)
        {
            ParentProcessId = Environment.ProcessId,
            Features = _features,
        };

        _baseProcessStartInfo.ArgumentList.RemoveAt(_baseProcessStartInfo.ArgumentList.Count - 1);
        _baseProcessStartInfo.ArgumentList.Add(childProcessOptions.ToCommandLineArgumentValue());

        return _baseProcessStartInfo;
    }

    private void ProcessKillSignalCallback(Process? childProcess, bool waitSuccess)
    {
        if (childProcess is not Process process)
        {
            return;
        }

        if (waitSuccess)
        {
            _logger?.Warn("Signal for force kill by host received. Force kill child process {ProcessId}.", process.Id);
        }
        else if (_isShutdownRequested)
        {
            return;
        }
        else
        {
            _logger?.Warn("Process kill signal wait fail. The process may have exited. Try to force kill the child process {ProcessId}.", process.Id);
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
                _logger?.Warn("Host force kill child process {ProcessId} fail. {ExceptionMessage}", process.Id, ex.Message);
            }
        }
    }

    #endregion base
}
