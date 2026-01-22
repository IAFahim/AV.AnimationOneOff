using Unity.Burst;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;

namespace AV.AnimationOneOff
{
    /// <summary>
    /// Data-Oriented Animation Job for reading curve values.
    /// Pure DOD: no external state, no side effects.
    /// </summary>
    [BurstCompile]
    public struct ReadActionCurveJob : IAnimationJob
    {
        [ReadOnly] public NativeArray<PropertyStreamHandle> CurveHandles;
        [WriteOnly] public NativeArray<float> OutputValues;

        [BurstCompile]
        public void ProcessAnimation(AnimationStream stream)
        {
            for (var i = 0; i < CurveHandles.Length; i++)
            {
                OutputValues[i] = CurveHandles[i].GetFloat(stream);
            }
        }

        public void ProcessRootMotion(AnimationStream stream)
        {
            // Intentionally empty
        }
    }
}