using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Mathematics;

namespace AV.AnimationOneOff
{
    // ===================================================================================
    // LAYER B: LOGIC (Primitives Only, Stateless)
    // ===================================================================================

    /// <summary>
    /// Stateless, pure logic layer for Action Animations.
    /// </summary>
    [BurstCompile]
    public static class ActionAnimationLogic
    {
        /// <summary>
        /// Calculates the blend weight for the action clip based on a trapezoidal time curve.
        /// </summary>
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CalculateBlendState(
            in float elapsedTimeSeconds,
            in float totalDurationSeconds,
            in float fadeInSeconds,
            in float fadeOutSeconds,
            out float calculatedWeight,
            out bool isActionFinished)
        {
            // 1. Check Completion
            if (elapsedTimeSeconds >= totalDurationSeconds)
            {
                calculatedWeight = 0f;
                isActionFinished = true;
                return;
            }

            isActionFinished = false;

            // 2. Calculate Fade In (0 -> 1)
            float weightIn = (fadeInSeconds > 0f)
                ? math.saturate(elapsedTimeSeconds / fadeInSeconds)
                : 1f;

            // 3. Calculate Fade Out (1 -> 0)
            float timeRemaining = totalDurationSeconds - elapsedTimeSeconds;
            float weightOut = (fadeOutSeconds > 0f)
                ? math.saturate(timeRemaining / fadeOutSeconds)
                : 1f;

            // 4. Combine (trapezoidal blend)
            calculatedWeight = weightIn * weightOut;
        }

        /// <summary>
        /// Increments the internal timer.
        /// </summary>
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void TickTimer(
            in float deltaTimeSeconds,
            ref float elapsedTimeSeconds)
        {
            elapsedTimeSeconds += deltaTimeSeconds;
        }

        /// <summary>
        /// Calculates the fade-out weight for the previous action during a crossfade.
        /// </summary>
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CalculateLinearFadeOut(
            in float elapsedTimeSeconds,
            in float durationSeconds,
            out float weight,
            out bool isFinished)
        {
            if (elapsedTimeSeconds >= durationSeconds)
            {
                weight = 0f;
                isFinished = true;
                return;
            }

            // Inverse Lerp: 1.0 at start, 0.0 at end
            weight = 1f - math.saturate(elapsedTimeSeconds / durationSeconds);
            isFinished = false;
        }

        /// <summary>
        /// Calculates locomotion weight based on action weights.
        /// </summary>
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CalculateLocomotionWeight(in float currentActionWeight, in float previousActionWeight)
        {
            float totalActionInfluence = math.saturate(currentActionWeight + previousActionWeight);
            return 1f - totalActionInfluence;
        }
    }
}