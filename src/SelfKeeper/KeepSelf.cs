using System.Diagnostics;

namespace SelfKeeper;

#pragma warning disable CS1572 // XML 注释中有 param 标记，但是没有该名称的参数

/// <summary>
/// 监控自身
/// </summary>
public static class KeepSelf
{
    /// <inheritdoc cref="Handle(string[], KeepSelfHostOptions?)"/>
    public static void Handle(string[] args, Action<KeepSelfHostOptions> setupAction)
    {
        var hostOptions = new KeepSelfHostOptions();
        setupAction(hostOptions);

        Handle(args, hostOptions);
    }

    /// <summary>
    /// 接管自身监控逻辑<para/>
    /// 如果当前为主进程，则启动工作进程，对其进行监控及重启。并在接收到关闭信号后关闭工作进程，以工作进程的 <see cref="Process.ExitCode"/> 调用 <see cref="Environment.Exit(int)"/> 退出当前进程<para/>
    /// 如果当前为工作进程，则不进行处理，并立即返回
    /// </summary>
    /// <param name="args">程序启动参数</param>
    /// <param name="hostOptions">选项</param>
    /// <param name="setupAction">配置选项的委托</param>
    public static void Handle(string[] args, KeepSelfHostOptions? hostOptions = null)
    {
        if (TryHandle(args, out var workerProcessExitCode, hostOptions))
        {
            Environment.Exit(workerProcessExitCode ?? 0);
        }
    }

    /// <summary>
    /// 尝试接管自身监控逻辑<para/>
    /// 如果当前为主进程，则启动工作进程，对其进行监控及重启。并在接收到关闭信号后关闭工作进程<para/>
    /// 如果当前为工作进程，则不进行处理，并立即返回
    /// </summary>
    /// <param name="args">程序启动参数</param>
    /// <param name="workerProcessExitCode">工作进程的 <see cref="Process.ExitCode"/>。当前为主进程时才可能不为 null。</param>
    /// <param name="hostOptions">选项</param>
    /// <returns>当前为主进程时，返回true，否则返回false</returns>
    /// <exception cref="ArgumentException"></exception>
    public static bool TryHandle(string[] args, out int? workerProcessExitCode, KeepSelfHostOptions? hostOptions = null)
    {
        SelfKeeperEnvironment.SetInitializationState();

        hostOptions ??= new();

        if (CheckIsNoKeepSelfOn(args, hostOptions))
        {
            SelfKeeperEnvironment.IsWorkerProcess = false;

            workerProcessExitCode = null;
            return false;
        }

        var logger = hostOptions.Logger;

        var index = args.TakeWhile(m => !string.Equals(hostOptions.WorkerProcessOptionsCommandArgumentName, m, StringComparison.OrdinalIgnoreCase)).Count();
        if (index < args.Length - 1) //当前为工作进程
        {
            var keepSelfOptionValue = args[index + 1];

            if (!KeepSelfWorkerProcessOptions.TryParseFromCommandLineArgumentValue(keepSelfOptionValue, out var options))
            {
                throw new ArgumentException($"Incorrect value \"{keepSelfOptionValue}\" for \"{hostOptions.WorkerProcessOptionsCommandArgumentName}\".");
            }

            SelfKeeperEnvironment.IsWorkerProcess = true;
            SelfKeeperEnvironment.ParentProcessId = options.ParentProcessId;
            SelfKeeperEnvironment.SessionId = options.SessionId;

            logger?.Debug("Current process is worker process.", Environment.ProcessId);

            if (options.Features.Contains(KeepSelfFeatureFlag.ExitWhenHostExited))
            {
                logger?.Debug($"Feature \"{nameof(KeepSelfFeatureFlag.ExitWhenHostExited)}\" enabled. The current process will exit when host process exited.");
                WaitParentProcessExit(options.ParentProcessId, hostOptions.Logger);
            }

            if (!options.Features.Contains(KeepSelfFeatureFlag.DisableForceKillByHost))
            {
                logger?.Debug($"Feature \"{nameof(KeepSelfFeatureFlag.DisableForceKillByHost)}\" disabled. The current process can request force kill by host process.");
                SelfKeeperEnvironment.SetupTheHostKillMutex(options);
            }

            workerProcessExitCode = null;

            return false;
        }

        SelfKeeperEnvironment.IsWorkerProcess = false;

        logger?.Debug("Current process is host process.", Environment.ProcessId);

        var selfKeeperService = new SelfKeeperService(options: hostOptions, baseProcessStartInfo: ProcessStartInfoUtil.CloneCurrentProcessStartInfo());

        workerProcessExitCode = selfKeeperService.Run();

        return true;
    }

    #region Base

    /// <summary>
    /// 检查是否开启了 - NoKeepSelf
    /// </summary>
    /// <param name="args"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    private static bool CheckIsNoKeepSelfOn(string[] args, KeepSelfHostOptions options)
    {
        if (Debugger.IsAttached
            && options.Features.Contains(KeepSelfFeatureFlag.SkipWhenDebuggerAttached))
        {
            options.Logger?.Debug("KeepSelf disabled by debugger attached.");
            return true;
        }
        else if (args.Any(m => string.Equals(options.NoKeepSelfCommandArgumentName, m, StringComparison.OrdinalIgnoreCase)))
        {
            options.Logger?.Debug("KeepSelf disabled by command line argument.");
            return true;
        }
        else if (EnvironmentUtil.IsSwitchOn(options.NoKeepSelfEnvironmentVariableName))
        {
            options.Logger?.Debug("KeepSelf disabled by environment variable.");
            return true;
        }

        return false;
    }

    private static void WaitParentProcessExit(int parentProcessId, ILogger? logger)
    {
        Process process;
        try
        {
            process = Process.GetProcessById(parentProcessId);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"KeepSelf can not found parent process by id - [{parentProcessId}]", ex);
        }

        _ = process.WaitForExitAsync()
                   .ContinueWith(_ =>
                   {
                       logger?.Error($"Parent process \"{parentProcessId}\" exited with code \"{process.ExitCode}\". Current process will shutdown now.", parentProcessId, process.ExitCode);

                       //TODO graceful shutdown ?

                       Environment.Exit(1);
                   });
    }

    #endregion Base
}
