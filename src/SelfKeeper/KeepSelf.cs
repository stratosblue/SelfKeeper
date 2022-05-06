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
    /// 如果当前为主进程，则启动子进程，对其进行监控及重启。并在接收到关闭信号后关闭子进程，以子进程的 <see cref="Process.ExitCode"/> 调用 <see cref="Environment.Exit(int)"/> 退出当前进程<para/>
    /// 如果当前为子进程，则不进行处理，并立即返回
    /// </summary>
    /// <param name="args">程序启动参数</param>
    /// <param name="hostOptions">选项</param>
    /// <param name="setupAction">配置选项的委托</param>
    public static void Handle(string[] args, KeepSelfHostOptions? hostOptions = null)
    {
        if (TryHandle(args, out var childProcessExitCode, hostOptions))
        {
            Environment.Exit(childProcessExitCode ?? 0);
        }
    }

    /// <summary>
    /// 尝试接管自身监控逻辑<para/>
    /// 如果当前为主进程，则启动子进程，对其进行监控及重启。并在接收到关闭信号后关闭子进程<para/>
    /// 如果当前为子进程，则不进行处理，并立即返回
    /// </summary>
    /// <param name="args">程序启动参数</param>
    /// <param name="childProcessExitCode">子进程的 <see cref="Process.ExitCode"/>。当前为主进程时才可能不为 null。</param>
    /// <param name="hostOptions">选项</param>
    /// <returns>当前为主进程时，返回true，否则返回false</returns>
    /// <exception cref="ArgumentException"></exception>
    public static bool TryHandle(string[] args, out int? childProcessExitCode, KeepSelfHostOptions? hostOptions = null)
    {
        SelfKeeperEnvironment.SetInitializationState();

        hostOptions ??= new();

        if (Debugger.IsAttached
            && hostOptions.Features.Contains(KeepSelfFeatureFlag.SkipWhenDebuggerAttached))
        {
            SelfKeeperEnvironment.IsChildProcess = false;

            childProcessExitCode = null;
            return false;
        }

        var index = args.TakeWhile(m => !string.Equals(SelfKeeperEnvironment.KeepSelfChildProcessCommandArgumentName, m, StringComparison.Ordinal)).Count();
        if (index < args.Length - 1) //当前为子进程
        {
            var keepSelfOptionValue = args[index + 1];

            if (!KeepSelfChildProcessOptions.TryParseFromCommandLineArgumentValue(keepSelfOptionValue, out var options))
            {
                throw new ArgumentException($"Incorrect value \"{keepSelfOptionValue}\" for \"{SelfKeeperEnvironment.KeepSelfChildProcessCommandArgumentName}\".");
            }

            SelfKeeperEnvironment.IsChildProcess = true;
            SelfKeeperEnvironment.ParentProcessId = options.ParentProcessId;
            SelfKeeperEnvironment.SessionId = options.SessionId;

            if (options.Features.Contains(KeepSelfFeatureFlag.ExitWhenHostExited))
            {
                WaitParentProcessExit(options.ParentProcessId, hostOptions.Logger);
            }

            if (!options.Features.Contains(KeepSelfFeatureFlag.DisableForceKillByHost))
            {
                SelfKeeperEnvironment.SetupTheHostKillMutex(options);
            }

            childProcessExitCode = null;

            return false;
        }

        SelfKeeperEnvironment.IsChildProcess = false;

        var selfKeeperService = new SelfKeeperService(options: hostOptions, baseProcessStartInfo: ProcessStartInfoUtil.CloneCurrentProcessStartInfo());

        childProcessExitCode = selfKeeperService.Run();

        return true;
    }

    #region Base

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
