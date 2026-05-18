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

    [Test]
    public void ComputeWarningLevel_ActivatesBeforeDetectionAndGetsStrongerCloser()
    {
        var farWarning = LightChaseMath.ComputeWarningLevel(14f, 8f, 4f);
        var edgeWarning = LightChaseMath.ComputeWarningLevel(11f, 8f, 4f);
        var closeWarning = LightChaseMath.ComputeWarningLevel(6f, 8f, 4f);

        Assert.That(farWarning, Is.EqualTo(0f));
        Assert.That(edgeWarning, Is.GreaterThan(0f));
        Assert.That(closeWarning, Is.GreaterThan(edgeWarning));
    }
}
