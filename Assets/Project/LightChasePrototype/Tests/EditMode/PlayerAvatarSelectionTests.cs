using LightChasePrototype;
using NUnit.Framework;

public class PlayerAvatarSelectionTests
{
    [SetUp]
    public void SetUp()
    {
        PlayerAvatarSelection.SelectAvatar(PlayerAvatarSelection.ArmatureAvatarId);
    }

    [Test]
    public void SelectedAvatar_DefaultsToArmature()
    {
        Assert.That(PlayerAvatarSelection.SelectedAvatarId, Is.EqualTo(PlayerAvatarSelection.ArmatureAvatarId));
        Assert.That(PlayerAvatarSelection.SelectedAvatar.DisplayName, Is.EqualTo("Humano"));
    }

    [Test]
    public void SelectAvatar_SwitchesToCapsuleWhenRequested()
    {
        PlayerAvatarSelection.SelectAvatar(PlayerAvatarSelection.CapsuleAvatarId);

        Assert.That(PlayerAvatarSelection.SelectedAvatarId, Is.EqualTo(PlayerAvatarSelection.CapsuleAvatarId));
        Assert.That(PlayerAvatarSelection.SelectedAvatar.DisplayName, Is.EqualTo("Andres"));
    }

    [Test]
    public void GetAvatar_InvalidIdFallsBackToArmature()
    {
        var avatar = PlayerAvatarSelection.GetAvatar("desconocido");

        Assert.That(avatar.Id, Is.EqualTo(PlayerAvatarSelection.ArmatureAvatarId));
    }
}
