using System.Diagnostics;

namespace SelfKeeper;

internal static class ProcessStartInfoUtil
{
    private static ProcessStartInfo CreateProcessStartInfo(string fileName, string[] commandLineArgs)
    {
        var processStartInfo = new ProcessStartInfo(fileName)
        {
            //HACK 默认关闭子进程标准输入/输出/异常流的重定向，避免缓冲区满导致的程序阻塞
            RedirectStandardError = false,
            RedirectStandardInput = false,
            RedirectStandardOutput = false,

            UseShellExecute = false,
            WorkingDirectory = Environment.CurrentDirectory,
        };

        foreach (var item in commandLineArgs)
        {
            processStartInfo.ArgumentList.Add(item);
        }

        return processStartInfo;
    }

    /// <summary>
    /// 复制一个当前进程的启动信息
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static ProcessStartInfo CloneCurrentProcessStartInfo()
    {
        var fileName = Process.GetCurrentProcess().MainModule?.FileName;

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new InvalidOperationException("Can not get current process file path. ");
        }

        var commandLineArgs = Environment.GetCommandLineArgs();

        if (commandLineArgs.Length > 0)
        {
            //去除不必要的启动参数
            //HACK 启动参数在复杂情况下可能有问题，但目前好像运行良好，先这样吧
            var fileNameInCommandLine = Path.GetFileName(commandLineArgs[0]);
            if (fileNameInCommandLine == Path.GetFileName(fileName))
            {
                commandLineArgs = commandLineArgs.Skip(1).ToArray();
            }
        }

        return CreateProcessStartInfo(fileName, commandLineArgs);
    }
}
