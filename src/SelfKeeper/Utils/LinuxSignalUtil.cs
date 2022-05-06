using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace SelfKeeper;

[SupportedOSPlatform("linux")]
internal static class LinuxSignalUtil
{
    //SEE https://stackoverflow.com/questions/41041730/net-core-app-how-to-send-sigterm-to-child-processes

    private static int GetSignalValue(PosixSignal posixSignal)
    {
        return posixSignal switch
        {
            PosixSignal.SIGTSTP => 20,
            PosixSignal.SIGTTOU => 22,
            PosixSignal.SIGTTIN => 21,
            PosixSignal.SIGWINCH => 28,
            PosixSignal.SIGCONT => 18,
            PosixSignal.SIGCHLD => 17,
            PosixSignal.SIGTERM => 15,
            PosixSignal.SIGQUIT => 3,
            PosixSignal.SIGINT => 2,
            PosixSignal.SIGHUP => 1,
            _ => throw new ArgumentException($"Unknown signal {posixSignal}.")
        };
    }

    [DllImport("libc", EntryPoint = "kill", SetLastError = false)]
    private static extern int Kill(int pid, int sig);

    public static void KillWithSignal(int processId, PosixSignal signal)
    {
        var result = Kill(processId, GetSignalValue(signal));
        if (result != 0)
        {
            throw new InvalidOperationException($"Kill returned value - {result}");
        }
    }
}
