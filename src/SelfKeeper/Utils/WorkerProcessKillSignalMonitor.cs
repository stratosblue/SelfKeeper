namespace SelfKeeper;

/// <summary>
/// 工作进程关闭信号监控器
/// </summary>
internal sealed class WorkerProcessKillSignalMonitor : IDisposable
{
    public delegate void WorkerProcessKillSignalCallback(bool waitSuccess);

    private readonly WorkerProcessKillSignalCallback _callback;
    private readonly string _mutexName;
    private volatile bool _isDisposed;
    private int _isStarted = 0;
    private volatile Mutex? _mutex = null;

    public WorkerProcessKillSignalMonitor(string mutexName, WorkerProcessKillSignalCallback callback)
    {
        _mutexName = mutexName ?? throw new ArgumentNullException(nameof(mutexName));
        _callback = callback ?? throw new ArgumentNullException(nameof(callback));
    }

    public static IDisposable Create(int processId, uint sessionId, WorkerProcessKillSignalCallback callback)
    {
        var sessionName = SelfKeeperEnvironment.CreateSessionName(processId, sessionId);

        var monitor = new WorkerProcessKillSignalMonitor($"Global\\{sessionName}", callback);

        monitor.Start();

        return monitor;
    }

    public void Dispose()
    {
        _isDisposed = true;
        _mutex?.Dispose();
    }

    private void Start()
    {
        if (Interlocked.Increment(ref _isStarted) != 1)
        {
            throw new InvalidOperationException("Already started.");
        }

        Task.Factory.StartNew(() =>
        {
            Mutex? mutex = null;
            while (!_isDisposed
                   && !Mutex.TryOpenExisting(_mutexName, out mutex))
            {
                //TODO make it configurable
                Thread.Sleep(100);
            }

            _mutex = mutex;

            if (_isDisposed
                || mutex is null)
            {
                mutex?.Dispose();
                return;
            }

            bool received = false;
            bool success = true;
            try
            {
                received = mutex.WaitOne();
            }
            catch
            {
                // 忽略所有异常
                received = true;
                success = false;
            }

            if (received)
            {
                _callback(success);
            }
        }, TaskCreationOptions.LongRunning);
    }
}
