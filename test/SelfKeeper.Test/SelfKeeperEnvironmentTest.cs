using Microsoft.VisualStudio.TestTools.UnitTesting;

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
        Assert.ThrowsException<InvalidOperationException>(() => SelfKeeperEnvironment.IsWorkerProcess);
        Assert.ThrowsException<InvalidOperationException>(() => SelfKeeperEnvironment.SessionId);
        Assert.ThrowsException<InvalidOperationException>(() => SelfKeeperEnvironment.RequestKillCurrentProcess());
    }
}
