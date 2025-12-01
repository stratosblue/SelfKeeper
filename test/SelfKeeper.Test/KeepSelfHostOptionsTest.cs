namespace SelfKeeper.Test;

[TestClass]
public class KeepSelfHostOptionsTest
{
    [TestMethod]
    public void FlagFastAddRemove()
    {
        var options = new KeepSelfHostOptions();

        for (int i = 0; i < 31; i++)
        {
            AddRemoveTest2(options, (KeepSelfFeatureFlag)(1 << i), (KeepSelfFeatureFlag)(1 << i + 1));
        }

        options.Features = KeepSelfFeatureFlag.None;

        for (int i = 0; i < 31; i++)
        {
            AddRemoveTest2(options, (KeepSelfFeatureFlag)(1 << i), (KeepSelfFeatureFlag)(1 << i + 1));
        }

        void AddRemoveTest2(KeepSelfHostOptions options, KeepSelfFeatureFlag flag1, KeepSelfFeatureFlag flag2)
        {
            AddRemoveTest(options, flag1);
            AddRemoveTest(options, flag2);
            AddRemoveTest(options, flag1 | flag2);
        }

        void AddRemoveTest(KeepSelfHostOptions options, KeepSelfFeatureFlag flag)
        {
            var origin = options.Features;
            var isContains = options.Features.Contains(flag);

            if (isContains)
            {
                options.RemoveFlag(flag);
                Assert.IsFalse(options.Features.Contains(flag));
                options.AddFlag(flag);
                Assert.IsTrue(options.Features.Contains(flag));
            }
            else
            {
                options.AddFlag(flag);
                Assert.IsTrue(options.Features.Contains(flag));
                options.RemoveFlag(flag);
                Assert.IsFalse(options.Features.Contains(flag));
            }

            Assert.AreEqual(origin, options.Features);
        }
    }
}
