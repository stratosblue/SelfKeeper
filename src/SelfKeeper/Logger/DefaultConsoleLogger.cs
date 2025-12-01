using System.Text.RegularExpressions;

namespace SelfKeeper;

/// <summary>
/// 默认的控制台Logger
/// </summary>
/// <inheritdoc cref="DefaultConsoleLogger"/>
public sealed partial class DefaultConsoleLogger(string loggerName) : ISelfKeeperLogger
{
    #region Private 字段

    private static readonly Regex s_messageTemplateReplaceRegex = GetMessageTemplateReplaceRegex();

    private readonly string _logPrefix = $"{loggerName} [{Environment.ProcessId}] ";

    #endregion Private 字段

    #region Public 方法

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

    #endregion Public 方法

    #region Private 方法

    [GeneratedRegex("{.+?}", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex GetMessageTemplateReplaceRegex();

    private string FormatMessage(string message, params object[] args)
    {
        if (message.Contains('{'))
        {
            int index = 0;
            message = s_messageTemplateReplaceRegex.Replace(message, match => args[index++].ToString() ?? string.Empty, args.Length);
        }

        return $"{DateTime.Now:MM-dd HH:mm:ss.fff} {_logPrefix}{message}";
    }

    #endregion Private 方法
}
