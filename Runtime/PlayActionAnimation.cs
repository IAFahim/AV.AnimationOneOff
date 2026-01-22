using System;
using UnityEngine;

namespace AV.AnimationOneOff
{
    /// <summary>
    /// Consumer component for playing action animations.
    /// Uses the IActionAnimationSystem interface contract.
    /// </summary>
    public sealed class PlayActionAnimation : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField]
        private float fadeInDurationSeconds = 0.15f;

        [SerializeField]
        private float fadeOutDurationSeconds = 0.15f;

        [Header("References")]
        [SerializeField]
        private ActionAnimationComponent animationComponent; // Serialized reference for Inspector

        [Tooltip("Assign generic clip here (Attacks, Emotes, Reactions)")]
        [SerializeField]
        private AnimationClip testAnimationClip;

        /// <summary>
        /// Event raised when the test animation completes.
        /// </summary>
        public event Action OnTestAnimationCompleted;

        private IActionAnimationSystem _system;

        private void OnValidate()
        {
            animationComponent ??= GetComponentInParent<ActionAnimationComponent>();
        }

        private void Awake()
        {
            // Cache Interface
            if (animationComponent != null)
            {
                _system = animationComponent;
            }
            else
            {
                _system = GetComponentInParent<IActionAnimationSystem>();
            }
        }

        private void OnEnable()
        {
            if (_system != null)
            {
                _system.OnActionCompleted += HandleAnimationCompleted;
            }

            PlayTestClip();
        }

        private void OnDisable()
        {
            if (_system != null)
            {
                _system.OnActionCompleted -= HandleAnimationCompleted;
            }
        }

        [ContextMenu("⚡ Test: Play Clip")]
        public void DebugPlayTestClip()
        {
            if (!Application.isPlaying) return;
            PlayTestClip();
        }

        public void PlayClip(AnimationClip clip)
        {
            if (_system == null)
            {
                Debug.LogError($"[PlayActionAnimation] System not found on {name}");
                return;
            }

            var result = _system.TryPlayAction(clip, fadeInDurationSeconds, fadeOutDurationSeconds);
            if (result != ActionAnimationResult.Success)
            {
                Debug.LogWarning($"[PlayActionAnimation] Failed to play clip: {result}");
            }
        }

        private void PlayTestClip()
        {
            PlayClip(testAnimationClip);
        }

        private void HandleAnimationCompleted(AnimationClip clip)
        {
            if (clip == testAnimationClip)
            {
                OnTestAnimationCompleted?.Invoke();
            }
        }
    }
}