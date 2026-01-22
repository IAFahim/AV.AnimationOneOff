using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace AV.AnimationOneOff
{
    // ===================================================================================
    // LAYER C: API (TryX Extensions)
    // ===================================================================================

    public static class ActionAnimationExtensions
    {
        // -- Initialization --

        public static ActionAnimationResult TryInitializeGraph(ref this ActionAnimationState state, Animator targetAnimator, string[] exposedCurveNames, string graphName)
        {
            if (targetAnimator == null) return ActionAnimationResult.InvalidInput;

            int curveCount = exposedCurveNames.Length;
            state.CurveHandles = new NativeArray<PropertyStreamHandle>(curveCount, Allocator.Persistent);
            state.CurveValues = new NativeArray<float>(curveCount, Allocator.Persistent);

            for (int i = 0; i < curveCount; i++)
            {
                state.CurveHandles[i] = targetAnimator.BindStreamProperty(targetAnimator.avatarRoot, typeof(Animator), exposedCurveNames[i]);
            }

            state.Graph = PlayableGraph.Create(graphName);
            state.Graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            var curveJob = new ReadActionCurveJob { CurveHandles = state.CurveHandles, OutputValues = state.CurveValues };
            state.CurveReaderPlayable = AnimationScriptPlayable.Create(state.Graph, curveJob, 1);

            // Mixer: Locomotion, Action, PreviousAction
            state.MainMixerPlayable = AnimationMixerPlayable.Create(state.Graph, 3);

            var output = AnimationPlayableOutput.Create(state.Graph, "FinalAnimatorOutput", targetAnimator);
            state.CurveReaderPlayable.ConnectInput(0, state.MainMixerPlayable, 0, 1.0f);
            output.SetSourcePlayable(state.CurveReaderPlayable);

            var locomotionPlayable = AnimatorControllerPlayable.Create(state.Graph, targetAnimator.runtimeAnimatorController);
            state.Graph.Connect(locomotionPlayable, 0, state.MainMixerPlayable, ActionAnimationState.LOCOMOTION_PORT_INDEX);
            state.MainMixerPlayable.SetInputWeight(ActionAnimationState.LOCOMOTION_PORT_INDEX, 1.0f);

            state.Graph.Play();

            return ActionAnimationResult.Success;
        }

        // -- Gameplay --

        public static ActionAnimationResult TryPlayAction(ref this ActionAnimationState state, AnimationClip clip, float fadeInSeconds, float fadeOutSeconds)
        {
            if (!state.Graph.IsValid()) return ActionAnimationResult.GraphUninitialized;
            if (clip == null) return ActionAnimationResult.InvalidInput;

            // 1. Handle Overlap (Shift Current to Previous)
            if (state.IsActionPlaying && state.CurrentActionPlayable.IsValid())
            {
                state.ShiftCurrentToPrevious(fadeInSeconds);
            }
            else
            {
                state.CleanupPreviousAction();
            }

            // 2. Setup New Action Data
            state.ElapsedTimeSeconds = 0f;
            state.TotalDurationSeconds = clip.length;
            state.FadeInDurationSeconds = fadeInSeconds;
            state.FadeOutDurationSeconds = fadeOutSeconds;
            state.IsActionPlaying = true;

            // 3. Create Playable
            state.CurrentActionPlayable = AnimationClipPlayable.Create(state.Graph, clip);
            state.CurrentActionPlayable.SetDuration(clip.length);
            state.CurrentActionPlayable.SetTime(0);

            // 4. Connect
            state.Graph.Connect(state.CurrentActionPlayable, 0, state.MainMixerPlayable, ActionAnimationState.ACTION_PORT_INDEX);
            state.MainMixerPlayable.SetInputWeight(ActionAnimationState.ACTION_PORT_INDEX, 0f); // Logic will ramp it up

            return ActionAnimationResult.Success;
        }

        public static ActionAnimationResult TryUpdateState(ref this ActionAnimationState state, float deltaTime)
        {
            if (!state.Graph.IsValid()) return ActionAnimationResult.GraphUninitialized;
            if (!state.IsActionPlaying && !state.IsCrossfading) return ActionAnimationResult.Success; // Idle is success

            float currentWeight = 0f;
            float previousWeight = 0f;
            bool currentFinished = false;

            // 1. Process Current Action
            if (state.IsActionPlaying)
            {
                ActionAnimationLogic.TickTimer(in deltaTime, ref state.ElapsedTimeSeconds);
                ActionAnimationLogic.CalculateBlendState(
                    in state.ElapsedTimeSeconds,
                    in state.TotalDurationSeconds,
                    in state.FadeInDurationSeconds,
                    in state.FadeOutDurationSeconds,
                    out currentWeight,
                    out currentFinished
                );
            }

            // 2. Process Previous Action (Crossfade)
            if (state.IsCrossfading)
            {
                state.CrossfadeElapsedSeconds += deltaTime;
                ActionAnimationLogic.CalculateLinearFadeOut(
                    in state.CrossfadeElapsedSeconds,
                    in state.CrossfadeDurationSeconds,
                    out previousWeight,
                    out bool prevFinished
                );

                if (prevFinished)
                {
                    state.CleanupPreviousAction();
                }
            }

            // 3. Apply Weights
            state.ApplyBlendWeights(currentWeight, previousWeight);

            // 4. Handle Finish
            if (currentFinished)
            {
                state.FinishCurrentAction();
                // We don't return "Finished" as a result code typically, unless requested.
                // But the caller might want to know if it *just* finished.
                // For now, return Success. The event can be handled by the caller observing state change?
                // The Logic function returned `currentFinished`.
                // We should probably expose `IsActionPlaying` state check.
            }

            return ActionAnimationResult.Success;
        }

        public static void Dispose(ref this ActionAnimationState state)
        {
            if (state.Graph.IsValid()) state.Graph.Destroy();
            if (state.CurveHandles.IsCreated) state.CurveHandles.Dispose();
            if (state.CurveValues.IsCreated) state.CurveValues.Dispose();
        }

        // -- Private Helpers --

        private static void ShiftCurrentToPrevious(ref this ActionAnimationState state, float crossfadeDuration)
        {
            state.CleanupPreviousAction();

            state.PreviousActionPlayable = state.CurrentActionPlayable;
            
            state.Graph.Disconnect(state.MainMixerPlayable, ActionAnimationState.ACTION_PORT_INDEX);
            state.Graph.Connect(state.PreviousActionPlayable, 0, state.MainMixerPlayable, ActionAnimationState.PREVIOUS_ACTION_PORT_INDEX);

            state.IsCrossfading = true;
            state.CrossfadeDurationSeconds = crossfadeDuration;
            state.CrossfadeElapsedSeconds = 0f;

            state.CurrentActionPlayable = default;
        }

        private static void CleanupPreviousAction(ref this ActionAnimationState state)
        {
            if (state.PreviousActionPlayable.IsValid())
            {
                state.Graph.Disconnect(state.MainMixerPlayable, ActionAnimationState.PREVIOUS_ACTION_PORT_INDEX);
                state.PreviousActionPlayable.Destroy();
            }
            state.MainMixerPlayable.SetInputWeight(ActionAnimationState.PREVIOUS_ACTION_PORT_INDEX, 0f);
            state.IsCrossfading = false;
        }

        private static void FinishCurrentAction(ref this ActionAnimationState state)
        {
            if (state.CurrentActionPlayable.IsValid())
            {
                state.Graph.Disconnect(state.MainMixerPlayable, ActionAnimationState.ACTION_PORT_INDEX);
                state.CurrentActionPlayable.Destroy();
            }
            state.MainMixerPlayable.SetInputWeight(ActionAnimationState.ACTION_PORT_INDEX, 0f);
            state.IsActionPlaying = false;

            // Recalculate locomotion
            state.ApplyBlendWeights(0f, state.IsCrossfading ? state.MainMixerPlayable.GetInputWeight(ActionAnimationState.PREVIOUS_ACTION_PORT_INDEX) : 0f);
        }

        private static void ApplyBlendWeights(ref this ActionAnimationState state, float currentWeight, float previousWeight)
        {
            state.MainMixerPlayable.SetInputWeight(ActionAnimationState.ACTION_PORT_INDEX, currentWeight);
            state.MainMixerPlayable.SetInputWeight(ActionAnimationState.PREVIOUS_ACTION_PORT_INDEX, previousWeight);

            float locomotionWeight = ActionAnimationLogic.CalculateLocomotionWeight(in currentWeight, in previousWeight);
            state.MainMixerPlayable.SetInputWeight(ActionAnimationState.LOCOMOTION_PORT_INDEX, locomotionWeight);
        }
    }
}