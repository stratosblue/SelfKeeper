using System.Text.RegularExpressions;

namespace SelfKeeper;

internal class InternalConsoleLogger : ISelfKeeperLogger
{
    public void Debug(string message, params object[] arg)
    {
        System.Diagnostics.Debug.WriteLine(FormatMessage(message, arg));
    }

    public void Error(string message, params object[] arg)
    {
        Console.Error.WriteLine(FormatMessage(message, arg));
    }

    public void Info(string message, params object[] arg)
    {
        Console.WriteLine(FormatMessage(message, arg));
    }

    public void Warn(string message, params object[] arg)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(FormatMessage(message, arg));
        Console.ResetColor();
    }

    private static string FormatMessage(string message, params object[] arg)
    {
        if (!message.Contains('{'))
        {
            return message;
        }

        int index = 0;
        var messageTemplate = Regex.Replace(message, "{.+?}", match => $"{{{index++}}}");
        return $"[{Environment.ProcessId}] " + string.Format(messageTemplate, arg);
    }
}
