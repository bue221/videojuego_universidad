using UnityEngine;

namespace LightChasePrototype
{
    [CreateAssetMenu(fileName = "PlayerAvatarEntry", menuName = "LightChase/Player Avatar Entry")]
    public class PlayerAvatarEntry : ScriptableObject
    {
        [Tooltip("Unique key for this avatar (e.g. 'andres'). Must be unique across all entries.")]
        public string avatarId;

        [Tooltip("Human-readable display name (e.g. 'Andres').")]
        public string displayName;

        [TextArea(2, 4)]
        [Tooltip("Short description shown in avatar selection UI.")]
        public string description;

        [Tooltip("Path relative to a Resources folder (e.g. 'PlayerAvatars/PlayerAndres').")]
        public string resourcePath;

        [Tooltip("10 footstep audio clips, matching StarterAssets ThirdPersonController layout.")]
        public AudioClip[] footstepClips = new AudioClip[10];

        [Tooltip("Audio clip played when the player lands after a jump.")]
        public AudioClip landingClip;

        [Tooltip("Optional ambient foley AudioClip for the avatar.")]
        public AudioClip foleyClip;
    }
}
