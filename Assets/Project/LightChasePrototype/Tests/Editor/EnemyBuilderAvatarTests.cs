using NUnit.Framework;
using UnityEngine;

public class EnemyBuilderAvatarTests
{
    [Test]
    public void BuildEnemyRoot_ProducesAnimatorWithValidAvatar()
    {
        EnemyBuilder.PrepareEnemyKindAssets(EnemyKindCatalog.GetAssets(EnemyBuilder.DefaultEnemyKind));
        var enemyRoot = EnemyBuilder.BuildEnemyRoot("EnemyAvatarTest", Vector3.zero);
        try
        {
            Assert.That(enemyRoot, Is.Not.Null);

            var animator = enemyRoot.GetComponentInChildren<Animator>(true);
            Assert.That(animator, Is.Not.Null, "BuildEnemyRoot must attach an Animator to the model");
            Assert.That(animator.runtimeAnimatorController, Is.Not.Null, "Animator must have a runtime controller");
            Assert.That(animator.avatar, Is.Not.Null, "Animator must have an avatar so generic clips can animate the rig");
            Assert.That(animator.avatar.isValid, Is.True, "Animator avatar must be valid");
        }
        finally
        {
            if (enemyRoot != null)
            {
                Object.DestroyImmediate(enemyRoot);
            }
        }
    }
}
