using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace AV.AnimationOneOff
{
    // ===================================================================================
    // LAYER A: DATA & DEFINITIONS
    // ===================================================================================

    public enum ActionAnimationResult
    {
        Success,
        InvalidInput,
        GraphUninitialized
    }

    /// <summary>
    /// Pure data container for the Action Animation System.
    /// Manages the PlayableGraph and blending state for One-Off Actions.
    /// </summary>
    [Serializable]
    public struct ActionAnimationState
    {
        // -- Graph References --
        public PlayableGraph Graph;
        public AnimationScriptPlayable CurveReaderPlayable;
        public AnimationMixerPlayable MainMixerPlayable;

        // Slot 1: The Active/Incoming Action
        public AnimationClipPlayable CurrentActionPlayable;

        // Slot 2: The Outgoing Action (Fading out)
        public AnimationClipPlayable PreviousActionPlayable;

        // -- Logic State (Current Action) --
        public bool IsActionPlaying;
        public float ElapsedTimeSeconds;
        public float TotalDurationSeconds;
        public float FadeInDurationSeconds;
        public float FadeOutDurationSeconds;

        // -- Logic State (Crossfade) --
        public bool IsCrossfading;
        public float CrossfadeElapsedSeconds;
        public float CrossfadeDurationSeconds;

        // -- Native Arrays --
        public NativeArray<PropertyStreamHandle> CurveHandles;
        public NativeArray<float> CurveValues;

        // -- Constants --
        public const int LOCOMOTION_PORT_INDEX = 0;
        public const int ACTION_PORT_INDEX = 1;
        public const int PREVIOUS_ACTION_PORT_INDEX = 2;

        public readonly bool IsGraphValid => Graph.IsValid();
        
        // ⭐️ DEBUG CARD: Compact, Visual, Fun.
        public override string ToString()
        {
            if (!Graph.IsValid()) return "[ACTION] ❌ Graph Invalid";
            
            var status = IsActionPlaying ? "⚡ PLAYING" : "💤 IDLE";
            var fade = IsCrossfading ? " (Crossfading)" : "";
            var time = IsActionPlaying ? $"{ElapsedTimeSeconds:F2}/{TotalDurationSeconds:F2}s" : "--";
            
            return $"[{status}{fade}] Time: {time}";
        }
    }
}