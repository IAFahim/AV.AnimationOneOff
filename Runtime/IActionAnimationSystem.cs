namespace AV.AnimationOneOff
{
    // ===================================================================================
    // LAYER D: CONTRACT
    // ===================================================================================
    
    public interface IActionAnimationSystem
    {
        ActionAnimationResult TryPlayAction(UnityEngine.AnimationClip clip, float fadeInSeconds, float fadeOutSeconds);
        bool IsActionPlaying { get; }
        event System.Action<UnityEngine.AnimationClip> OnActionCompleted;
    }
}
