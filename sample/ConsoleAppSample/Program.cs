using System.Runtime.InteropServices;

using SelfKeeper;

var logger = new DefaultConsoleLogger("ConsoleAppSample");

logger.Info($"Hello, World! 【{Environment.OSVersion}】");

KeepSelf.Handle(args, options =>
{
    options.RemoveFlag(KeepSelfFeatureFlag.SkipWhenDebuggerAttached | KeepSelfFeatureFlag.DisableForceKillByHost);   //配置功能
    options.StartFailRetryDelay = TimeSpan.FromSeconds(1);  //配置启动失败的重试延时
    options.RestartDelay = TimeSpan.FromSeconds(1); //进程退出后的重启延时
    //options.Logger = null;  //配置日志记录器
    options.WorkerProcessOptionsCommandArgumentName = "--worker-process-options"; //自定义工作进程选项的参数名
    options.NoKeepSelfCommandArgumentName = "--no-keep-self"; //自定义不启用 KeepSelf 的参数名
    options.NoKeepSelfEnvironmentVariableName = "NoWatchDog"; //自定义不启用 KeepSelf 的环境变量名
    options.ExcludeRestartExitCodes.Add(0); //添加工作进程执行成功的退出码，当工作进程退出码在该列表内时，不再进行重启
});

logger.Info($"SelfKeeperEnvironment IsWorkerProcess: {SelfKeeperEnvironment.IsWorkerProcess}, SessionId: {SelfKeeperEnvironment.SessionId}");

_ = Task.Run(() =>
{
    Thread.Sleep(10_000);
    logger.Info($"SelfKeeperEnvironment.RequestKillCurrentProcess - {SelfKeeperEnvironment.RequestKillCurrentProcess()}");
});

PosixSignalRegistration.Create(PosixSignal.SIGTERM, s =>
{
    logger.Warn($"SIGTERM {Environment.ProcessId} {s}");
    Thread.Sleep(15_000);
});

PosixSignalRegistration.Create(PosixSignal.SIGQUIT, s =>
{
    logger.Warn($"SIGQUIT {Environment.ProcessId} {s}");
});

PosixSignalRegistration.Create(PosixSignal.SIGINT, s =>
{
    logger.Warn($"SIGINT {Environment.ProcessId} {s}");
    Thread.Sleep(15_000);
});

Console.CancelKeyPress += (s, e) =>
{
    logger.Warn($"CancelKeyPress {Environment.ProcessId} {e}");
};

AppDomain.CurrentDomain.ProcessExit += (s, e) =>
{
    logger.Warn($"AppDomain.CurrentDomain.ProcessExit {Environment.ProcessId} {e}");
    Thread.Sleep(5_000);
};

logger.Info("-----------");
Thread.Sleep(-1);
return 0;
