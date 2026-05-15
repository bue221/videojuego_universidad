using LightChasePrototype;
using NUnit.Framework;

public class LightChaseMathTests
{
    [Test]
    public void ComputeLightSignatureRange_GrowsFastWithBrightness()
    {
        var range = LightChaseMath.ComputeLightSignatureRange(2f, 1.85f, 20f);

        Assert.AreEqual(7.4f, range);
    }

    [Test]
    public void NormalizeBrightness_StaysWithinZeroToOne()
    {
        var normalized = LightChaseMath.NormalizeBrightness(2f, 0.75f, 3f);

        Assert.That(normalized, Is.InRange(0f, 1f));
    }
}
