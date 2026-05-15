using UnityEngine;

namespace LightChasePrototype
{
    public class PrototypeLevelManager : MonoBehaviour
    {
        [SerializeField] private int starsRequiredToExit = 5;

        public int CollectedStars { get; private set; }
        public int StarsRequiredToExit => starsRequiredToExit;
        public bool ExitUnlocked => CollectedStars >= starsRequiredToExit;

        public void RegisterStarCollected()
        {
            CollectedStars++;
            Debug.Log($"Stars: {CollectedStars}/{starsRequiredToExit}");
        }

        public bool CanExit()
        {
            return ExitUnlocked;
        }
    }
}
