namespace SelfKeeper;

/// <summary>
/// SelfKeeper的宿主程序选项
/// </summary>
public class KeepSelfHostOptions
{
    /// <summary>
    /// 特性标识
    /// </summary>
    public KeepSelfFeatureFlag Features { get; set; } = unchecked((KeepSelfFeatureFlag)0xFFFF_FFFF);

    /// <summary>
    /// Logger
    /// </summary>
    public ISelfKeeperLogger? Logger { get; set; } = new InternalConsoleLogger();

    /// <summary>
    /// 进程退出后的重启延时
    /// </summary>
    public TimeSpan RestartDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// 启动失败的重试延时
    /// </summary>
    public TimeSpan StartFailRetryDelay { get; set; } = TimeSpan.FromSeconds(3);

    /// <summary>
    /// 添加特性标识
    /// </summary>
    /// <param name="flag"></param>
    /// <returns>当前选项</returns>
    public KeepSelfHostOptions AddFlag(KeepSelfFeatureFlag flag)
    {
        Features |= flag;
        return this;
    }

    /// <summary>
    /// 移除特性标识
    /// </summary>
    /// <param name="flag"></param>
    /// <returns>当前选项</returns>
    public KeepSelfHostOptions RemoveFlag(KeepSelfFeatureFlag flag)
    {
        Features ^= flag;
        return this;
    }
}
