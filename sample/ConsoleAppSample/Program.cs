using System.Runtime.InteropServices;

using SelfKeeper;

Log($"Hello, World! 【{Environment.OSVersion}】");

KeepSelf.Handle(args, options =>
{
    options.RemoveFlag(KeepSelfFeatureFlag.SkipWhenDebuggerAttached | KeepSelfFeatureFlag.DisableForceKillByHost);   //配置功能
    options.StartFailRetryDelay = TimeSpan.FromSeconds(1);  //配置启动失败的重试延时
    options.RestartDelay = TimeSpan.FromSeconds(1); //进程退出后的重启延时
    //options.Logger = null;  //配置日志记录器
    options.ChildProcessOptionsCommandArgumentName = "--child-process-options"; //自定义子进程选项的参数名
    options.NoKeepSelfCommandArgumentName = "--no-keep-self"; //自定义不启用 KeepSelf 的参数名
});

Log($"SelfKeeperEnvironment IsChildProcess: {SelfKeeperEnvironment.IsChildProcess}, SessionId: {SelfKeeperEnvironment.SessionId}");

Task.Run(() =>
{
    Thread.Sleep(10_000);
    Log($"SelfKeeperEnvironment.RequestKillCurrentProcess - {SelfKeeperEnvironment.RequestKillCurrentProcess()}");
});

PosixSignalRegistration.Create(PosixSignal.SIGTERM, s =>
{
    Log($"SIGTERM {Environment.ProcessId} {s}", ConsoleColor.Red);
    Thread.Sleep(15_000);
});

PosixSignalRegistration.Create(PosixSignal.SIGQUIT, s =>
{
    Log($"SIGQUIT {Environment.ProcessId} {s}", ConsoleColor.Red);
});

PosixSignalRegistration.Create(PosixSignal.SIGINT, s =>
{
    Log($"SIGINT {Environment.ProcessId} {s}", ConsoleColor.Red);
    Thread.Sleep(15_000);
});

Console.CancelKeyPress += (s, e) =>
{
    Log($"CancelKeyPress {Environment.ProcessId} {e}", ConsoleColor.Red);
};

AppDomain.CurrentDomain.ProcessExit += (s, e) =>
{
    Log($"AppDomain.CurrentDomain.ProcessExit {Environment.ProcessId} {e}", ConsoleColor.Red);
    Thread.Sleep(5_000);
};

Log("-----------");
Thread.Sleep(-1);
return 0;

static void Log(string message, ConsoleColor? color = null)
{
    if (color.HasValue)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[{Environment.ProcessId}] {message}");
        Console.ResetColor();
    }
    else
    {
        Console.WriteLine($"[{Environment.ProcessId}] {message}");
    }
}