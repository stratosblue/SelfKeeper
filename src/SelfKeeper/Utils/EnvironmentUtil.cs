namespace SelfKeeper;

internal static class EnvironmentUtil
{
    /// <summary>
    /// 检查开关是否启用
    /// </summary>
    /// <param name="switchVariableName"></param>
    /// <returns></returns>
    public static bool IsSwitchOn(string switchVariableName)
    {
        if (string.IsNullOrWhiteSpace(switchVariableName))
        {
            return false;
        }
        var value = Environment.GetEnvironmentVariable(switchVariableName);
        // If the value is null, empty, "0", or "false", then the switch is off
        return !string.IsNullOrWhiteSpace(value)
               && !string.Equals("0", value, StringComparison.Ordinal)
               && !string.Equals("false", value, StringComparison.OrdinalIgnoreCase);
    }
}
