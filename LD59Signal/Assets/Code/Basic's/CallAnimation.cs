using UnityEngine;

public class CallAnimation : MonoBehaviour
{
    [SerializeField] private AnimationClip animationClip;
    [SerializeField] private Animator animator;

    public void PlayAnimation()
    {
        animator.Play(animationClip.name);
    }
}
