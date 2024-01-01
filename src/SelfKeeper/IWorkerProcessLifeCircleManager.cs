using System.Diagnostics;

namespace SelfKeeper;

/// <summary>
/// 工作进程生命周期管理器
/// </summary>
public interface IWorkerProcessLifeCircleManager
{
    /// <summary>
    /// 工作进程退出时
    /// </summary>
    /// <param name="process"></param>
    /// <returns></returns>
    Process OnExited(Process process);

    /// <summary>
    /// 工作进程启动时
    /// </summary>
    /// <param name="processStartInfo"></param>
    /// <param name="startDelegate"></param>
    /// <returns></returns>
    Process OnStarting(ProcessStartInfo processStartInfo, Func<ProcessStartInfo, Process> startDelegate);
}

/// <inheritdoc cref="IWorkerProcessLifeCircleManager"/>
public abstract class WorkerProcessLifeCircleManager : IWorkerProcessLifeCircleManager
{
    /// <inheritdoc/>
    public virtual Process OnExited(Process process)
    {
        return process;
    }

    /// <inheritdoc/>
    public virtual Process OnStarting(ProcessStartInfo processStartInfo, Func<ProcessStartInfo, Process> startDelegate)
    {
        return startDelegate(processStartInfo);
    }
}
