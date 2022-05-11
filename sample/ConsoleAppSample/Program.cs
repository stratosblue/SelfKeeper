﻿using System.Runtime.InteropServices;

using SelfKeeper;

var logger = new DefaultConsoleLogger("ConsoleAppSample");

logger.Info($"Hello, World! 【{Environment.OSVersion}】");

KeepSelf.Handle(args, options =>
{
    options.RemoveFlag(KeepSelfFeatureFlag.SkipWhenDebuggerAttached | KeepSelfFeatureFlag.DisableForceKillByHost);   //配置功能
    options.StartFailRetryDelay = TimeSpan.FromSeconds(1);  //配置启动失败的重试延时
    options.RestartDelay = TimeSpan.FromSeconds(1); //进程退出后的重启延时
    //options.Logger = null;  //配置日志记录器
    options.ChildProcessOptionsCommandArgumentName = "--child-process-options"; //自定义子进程选项的参数名
    options.NoKeepSelfCommandArgumentName = "--no-keep-self"; //自定义不启用 KeepSelf 的参数名
    options.NoKeepSelfEnvironmentVariableName = "NoWatchDog"; //自定义不启用 KeepSelf 的环境变量名
});

logger.Info($"SelfKeeperEnvironment IsChildProcess: {SelfKeeperEnvironment.IsChildProcess}, SessionId: {SelfKeeperEnvironment.SessionId}");

Task.Run(() =>
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
