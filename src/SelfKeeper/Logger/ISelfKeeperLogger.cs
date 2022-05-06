namespace SelfKeeper;

/// <summary>
/// SelfKeeper内部使用的Logger
/// </summary>
public interface ISelfKeeperLogger
{
    /// <summary>
    /// 记录 Debug 等级的日志
    /// </summary>
    /// <param name="message"></param>
    /// <param name="arg"></param>
    void Debug(string message, params object[] arg);

    /// <summary>
    /// 记录 Error 等级的日志
    /// </summary>
    /// <param name="message"></param>
    /// <param name="arg"></param>
    void Error(string message, params object[] arg);

    /// <summary>
    /// 记录 Info 等级的日志
    /// </summary>
    /// <param name="message"></param>
    /// <param name="arg"></param>
    void Info(string message, params object[] arg);

    /// <summary>
    /// 记录 Warn 等级的日志
    /// </summary>
    /// <param name="message"></param>
    /// <param name="arg"></param>
    void Warn(string message, params object[] arg);
}
