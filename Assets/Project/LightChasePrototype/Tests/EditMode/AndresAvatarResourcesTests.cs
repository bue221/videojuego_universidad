using NUnit.Framework;
using UnityEngine;

public class AndresAvatarResourcesTests
{
    [Test]
    public void AndresPlayerPrefab_CanBeLoadedFromResourcesAsGameObject()
    {
        // Must match PlayerAvatarSelection option for the "Andres" avatar.
        const string path = "PlayerAvatars/PlayerAndres";
        var loaded = Resources.Load<GameObject>(path);

        Assert.That(loaded, Is.Not.Null, $"Expected Resources.Load<GameObject>(\"{path}\") to succeed. " +
                                        "If it fails, build the prefab via Tools > Prototype > Avatar > Build PlayerAndres Prefab.");
    }
}
