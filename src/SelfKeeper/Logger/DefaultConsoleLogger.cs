using System.Text.RegularExpressions;

namespace SelfKeeper;

/// <summary>
/// 默认的控制台Logger
/// </summary>
public sealed class DefaultConsoleLogger : ISelfKeeperLogger
{
    private static readonly Regex s_messageTemplateReplaceRegex = new("{.+?}", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private readonly string _logPrefix;

    /// <inheritdoc cref="DefaultConsoleLogger"/>
    public DefaultConsoleLogger(string loggerName)
    {
        _logPrefix = $"{loggerName} [{Environment.ProcessId}] ";
    }

    /// <inheritdoc/>
    public void Debug(string message, params object[] args)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(FormatMessage(message, args));
        Console.ResetColor();
    }

    /// <inheritdoc/>
    public void Error(string message, params object[] args)
    {
        Console.ForegroundColor = ConsoleColor.DarkRed;
        Console.Error.WriteLine(FormatMessage(message, args));
        Console.ResetColor();
    }

    /// <inheritdoc/>
    public void Info(string message, params object[] args)
    {
        Console.ResetColor();
        Console.WriteLine(FormatMessage(message, args));
    }

    /// <inheritdoc/>
    public void Warn(string message, params object[] args)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(FormatMessage(message, args));
        Console.ResetColor();
    }

    private string FormatMessage(string message, params object[] args)
    {
        if (message.Contains('{'))
        {
            int index = 0;
            message = s_messageTemplateReplaceRegex.Replace(message, match => args[index++].ToString() ?? string.Empty, args.Length);
        }

        return $"{DateTime.Now:MM-dd HH:mm:ss.fff} {_logPrefix}{message}";
    }
}
