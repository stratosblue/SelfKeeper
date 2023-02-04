namespace SelfKeeper;

/// <summary>
/// 特性标识
/// </summary>
[Flags]
public enum KeepSelfFeatureFlag
{
    /// <summary>
    /// 没有标志
    /// </summary>
    None = 0,

    /// <summary>
    /// 当主进程退出时，工作进程也退出
    /// </summary>
    ExitWhenHostExited = 1,

    /// <summary>
    /// 已附加调试器时不进行处理
    /// </summary>
    SkipWhenDebuggerAttached = 1 << 1,

    /// <summary>
    /// 在运行保持服务前，触发强制GC
    /// </summary>
    ForceGCBeforeRunKeepService = 1 << 2,

    /// <summary>
    /// 工作进程退出时，触发强制GC
    /// </summary>
    ForceGCAfterWorkerProcessExited = 1 << 3,

    /// <summary>
    /// 禁用主进程强制关闭功能
    /// </summary>
    DisableForceKillByHost = 1 << 4,
}

/// <summary>
/// <see cref="KeepSelfFeatureFlag"/> 的相关拓展
/// </summary>
public static class KeepSelfFeatureFlagExtensions
{
    /// <summary>
    /// 检查是否包含指定的特性
    /// </summary>
    /// <param name="source"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    public static bool Contains(this KeepSelfFeatureFlag source, KeepSelfFeatureFlag target)
    {
        return (source & target) == target;
    }
}
