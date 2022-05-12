global using ILogger = SelfKeeper.ISelfKeeperLogger;

namespace SelfKeeper;

/// <summary>
/// SelfKeeper环境信息
/// </summary>
public static class SelfKeeperEnvironment
{
    /// <summary>
    /// 默认命令行参数名称 - 工作进程选项
    /// </summary>
    public const string DefaultCommandArgumentNameWorkerProcessOptions = "--Keep-Self";

    /// <summary>
    /// 默认命令行参数名称 - 不启用 KeepSelf
    /// </summary>
    public const string DefaultCommandArgumentNameNoKeepSelf = "--No-Keep-Self";

    /// <summary>
    /// 默认环境变量名称 - 不启用 KeepSelf
    /// </summary>
    public const string DefaultEnvironmentVariableNameNoKeepSelf = "NoKeepSelf";

    private static uint s_increaseSessionId;
    private static bool? s_isWorkerProcess;
    private static int s_isInitiated = 0;
    private static int? s_parentProcessId;
    private static uint? s_sessionId;

    /// <summary>
    /// 是否为工作进程
    /// </summary>
    public static bool IsWorkerProcess
    {
        get => s_isWorkerProcess ?? throw new InvalidOperationException("SelfKeeper not initialization successful yet.");
        internal set => s_isWorkerProcess = value;
    }

    /// <summary>
    /// 父进程ID，当前为工作进程时才会有具体的值
    /// </summary>
    public static int? ParentProcessId { get => s_parentProcessId; internal set => s_parentProcessId = value; }

    /// <summary>
    /// SessionId，当前为工作进程时才会有具体的值
    /// </summary>
    public static uint? SessionId
    {
        get => s_isInitiated > 0
                 ? IsWorkerProcess
                     ? s_sessionId ?? throw new InvalidOperationException("SelfKeeper not initialization successful yet.")
                     : null
                 : throw new InvalidOperationException("SelfKeeper not initialization successful yet.");
        internal set => s_sessionId = value;
    }

    internal static string CreateSessionName(int hostProcessId, uint sessionId)
    {
        //SelfKeeperSessionName
        return $"_SKSN:{hostProcessId}_{sessionId}";
    }

    /// <summary>
    /// 生成SessionId
    /// </summary>
    /// <returns></returns>
    internal static uint GenerateSessionId()
    {
        var result = Interlocked.Increment(ref s_increaseSessionId);
        Interlocked.CompareExchange(ref s_increaseSessionId, uint.MaxValue / 2, 0);
        return result;
    }

    /// <summary>
    /// 设置初始化状态
    /// </summary>
    /// <returns></returns>
    internal static void SetInitializationState()
    {
        if (Interlocked.Increment(ref s_isInitiated) != 1)
        {
            throw new InvalidOperationException("Do not call the method multiple times in a process.");
        }
    }

    #region HostKill

    private static volatile ManualResetEvent? s_hostKillEvent;

    /// <summary>
    /// 请求主进程关闭当前进程（仅当当前进程为工作进程，且未开启 <see cref="KeepSelfFeatureFlag.DisableForceKillByHost"/> 时有效）
    /// </summary>
    /// <returns>请求是否成功</returns>
    public static bool RequestKillCurrentProcess()
    {
        if (!IsWorkerProcess
            || s_hostKillEvent is not ManualResetEvent hostKillEvent)
        {
            return false;
        }

        try
        {
            hostKillEvent.Set();

            Interlocked.Exchange(ref s_hostKillEvent, null)?.Dispose();

            return true;
        }
        catch
        {
            // 忽略所有异常
            return false;
        }
    }

    internal static void SetupTheHostKillMutex(KeepSelfWorkerProcessOptions options)
    {
        if (!IsWorkerProcess)
        {
            throw new InvalidOperationException("Only can call this method in worker process.");
        }

        var hostKillEvent = new ManualResetEvent(false);

        if (Interlocked.CompareExchange(ref s_hostKillEvent, hostKillEvent, null) is not null)
        {
            hostKillEvent.Dispose();
            throw new InvalidOperationException("Do not call the method multiple times in a process.");
        }

        var sessionName = CreateSessionName(options.ParentProcessId, options.SessionId);

        using var mutexInitEvent = new ManualResetEvent(false);

        Exception? mutexInitException = null;

        Task.Factory.StartNew(() =>
        {
            Mutex mutex;
            try
            {
                mutex = new Mutex(true, $"Global\\{sessionName}", out var createNew);

                if (!createNew)
                {
                    mutex.Dispose();
                    mutexInitException = new InvalidOperationException($"Create mutex fail. The name of mutex Global\\{sessionName} existed.");
                    return;
                }
            }
            catch (Exception ex)
            {
                mutexInitException = ex;
                return;
            }
            finally
            {
                mutexInitEvent.Set();
            }

            using (mutex)
            using (hostKillEvent)
            {
                hostKillEvent.WaitOne();

                try
                {
                    mutex.ReleaseMutex();
                }
                catch
                {
                    // 忽略所有异常
                }
            }
        }, TaskCreationOptions.LongRunning);

        mutexInitEvent.WaitOne();

        if (mutexInitException is not null)
        {
            throw mutexInitException;
        }
    }

    #endregion HostKill
}
