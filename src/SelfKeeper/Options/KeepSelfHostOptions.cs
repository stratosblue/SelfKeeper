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
    public ISelfKeeperLogger? Logger { get; set; } = new DefaultConsoleLogger("KeepSelf");

    /// <summary>
    /// 进程退出后的重启延时
    /// </summary>
    public TimeSpan RestartDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// 启动失败的重试延时
    /// </summary>
    public TimeSpan StartFailRetryDelay { get; set; } = TimeSpan.FromSeconds(3);

    #region EnvironmentVariableNames

    private string _noKeepSelfEnvironmentVariableName = SelfKeeperEnvironment.DefaultEnvironmentVariableNameNoKeepSelf;

    /// <summary>
    /// 环境变量名称 - 不启用 KeepSelf (默认: <see cref="SelfKeeperEnvironment.DefaultEnvironmentVariableNameNoKeepSelf"/>)
    /// </summary>
    public string NoKeepSelfEnvironmentVariableName { get => _noKeepSelfEnvironmentVariableName; set => SetCommandArgumentName(ref _noKeepSelfEnvironmentVariableName, value); }

    #endregion EnvironmentVariableNames

    #region CommandArgumentNames

    private string _workerProcessOptionsCommandArgumentName = SelfKeeperEnvironment.DefaultCommandArgumentNameWorkerProcessOptions;
    private string _noKeepSelfCommandArgumentName = SelfKeeperEnvironment.DefaultCommandArgumentNameNoKeepSelf;

    /// <summary>
    /// 命令行参数名称 - 工作进程选项 (默认: <see cref="SelfKeeperEnvironment.DefaultCommandArgumentNameWorkerProcessOptions"/>)
    /// </summary>
    public string WorkerProcessOptionsCommandArgumentName { get => _workerProcessOptionsCommandArgumentName; set => SetCommandArgumentName(ref _workerProcessOptionsCommandArgumentName, value); }

    /// <summary>
    /// 命令行参数名称 - 不启用 KeepSelf (默认: <see cref="SelfKeeperEnvironment.DefaultCommandArgumentNameNoKeepSelf"/>)
    /// </summary>
    public string NoKeepSelfCommandArgumentName { get => _noKeepSelfCommandArgumentName; set => SetCommandArgumentName(ref _noKeepSelfCommandArgumentName, value); }

    private static void SetCommandArgumentName(ref string target, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"“{nameof(value)}”不能为 null 或空白。", nameof(value));
        }
        target = value;
    }

    #endregion CommandArgumentNames

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
