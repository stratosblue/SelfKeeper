using System.Text;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SelfKeeper.Test;

[TestClass]
public class KeepSelfWorkerProcessOptionsTest
{
    [TestMethod]
    public void NoExceptionForInvalidValue()
    {
        Check(null);
        Check(string.Empty);
        Check(" ");
        Check("\t");
        Check("\n");
        Check("KeepSelfWorkerProcessOptionsTest");

        static void Check(string? value)
        {
            Assert.IsFalse(KeepSelfWorkerProcessOptions.TryParseFromCommandLineArgumentValue(value!, out var options));
            Assert.IsNull(options);
        }
    }

    [TestMethod]
    public void ShouldEqualAfterParse()
    {
        var originOptions = new KeepSelfWorkerProcessOptions((uint)Random.Shared.Next())
        {
            Features = KeepSelfFeatureFlag.ExitWhenHostExited | KeepSelfFeatureFlag.ForceGCAfterWorkerProcessExited,
            ParentProcessId = Random.Shared.Next(),
        };
        var value = originOptions.ToCommandLineArgumentValue();

        Assert.IsTrue(KeepSelfWorkerProcessOptions.TryParseFromCommandLineArgumentValue(value, out var newOptions));
        Assert.IsNotNull(newOptions);

        Assert.AreEqual(originOptions.SessionId, newOptions.SessionId);
        Assert.AreEqual(originOptions.Features, newOptions.Features);
        Assert.AreEqual(originOptions.ParentProcessId, newOptions.ParentProcessId);
    }
}
