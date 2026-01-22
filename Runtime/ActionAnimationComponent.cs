using System;
using UnityEngine;

namespace AV.AnimationOneOff
{
    // ===================================================================================
    // LAYER D: BRIDGE & COMPONENT
    // ===================================================================================

    /// <summary>
    /// Component wrapper for the Action Animation System.
    /// Implements IActionAnimationSystem explicitly.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [DefaultExecutionOrder(-9999)]
    public sealed class ActionAnimationComponent : MonoBehaviour, IActionAnimationSystem
    {
        [SerializeField]
        private Animator targetAnimator;

        [Tooltip("Names of animation curves to expose to the graph.")]
        [SerializeField]
        private string[] exposedCurveNames = Array.Empty<string>();

        // Event for the interface
        public event Action<AnimationClip> OnActionCompleted;

        // The single source of truth
        [SerializeField] // serialized for debug inspection if needed
        private ActionAnimationState _state;
        
        private AnimationClip _currentClip; // Track for callback

        // -- Lifecycle --

        private void OnValidate()
        {
            targetAnimator ??= GetComponent<Animator>();
        }

        private void OnEnable()
        {
            // Fail Loud
            if (targetAnimator == null) 
            {
                Debug.LogError($"[ActionAnimationComponent] Missing Animator on {name}!");
                return;
            }

            var result = _state.TryInitializeGraph(targetAnimator, exposedCurveNames, $"{name}_ActionGraph");
            if (result != ActionAnimationResult.Success)
            {
                Debug.LogError($"[ActionAnimationComponent] Initialization Failed: {result}");
            }
        }

        private void OnDisable()
        {
            _state.Dispose();
        }

        private void Update()
        {
            bool wasPlaying = _state.IsActionPlaying;
            
            _state.TryUpdateState(Time.deltaTime);

            // Detect Edge: Playing -> Not Playing
            if (wasPlaying && !_state.IsActionPlaying)
            {
                OnActionCompleted?.Invoke(_currentClip);
                _currentClip = null;
            }
        }

        // -- Explicit Interface Implementation --

        bool IActionAnimationSystem.IsActionPlaying => _state.IsActionPlaying;

        ActionAnimationResult IActionAnimationSystem.TryPlayAction(AnimationClip clip, float fadeInSeconds, float fadeOutSeconds)
        {
            if (clip == null) return ActionAnimationResult.InvalidInput;

            _currentClip = clip;
            return _state.TryPlayAction(clip, fadeInSeconds, fadeOutSeconds);
        }

        // -- Editor Debugging --

        [ContextMenu("⚡ Debug: Log State")]
        private void DebugLogState()
        {
            Debug.Log(_state.ToString());
        }
    }
}