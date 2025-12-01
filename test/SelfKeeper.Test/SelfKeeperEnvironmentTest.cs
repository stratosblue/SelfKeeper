namespace SelfKeeper.Test;

[TestClass]
public class SelfKeeperEnvironmentTest
{
    [TestMethod]
    public void NoneParentProcessId()
    {
        Assert.IsNull(SelfKeeperEnvironment.ParentProcessId);
    }

    [TestMethod]
    public void ThrowIfWithOutInit()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => SelfKeeperEnvironment.IsWorkerProcess);
        Assert.ThrowsExactly<InvalidOperationException>(() => SelfKeeperEnvironment.SessionId);
        Assert.ThrowsExactly<InvalidOperationException>(() => SelfKeeperEnvironment.RequestKillCurrentProcess());
    }
}
