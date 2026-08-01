using UnityEngine;

public class HeroSEController : MonoBehaviour
{
    [SerializeField] private HeroController hero;
    [SerializeField] private AudioClip footstepClip;
    [SerializeField, Range(0f, 1f)] private float footstepVolume = 0.5f;
    [SerializeField, Min(0.01f)] private float footstepInterval = 0.5f;
    [SerializeField] private AudioClip attackClip;
    [SerializeField, Range(0f, 1f)] private float attackVolume = 0.7f;
    [SerializeField] private AudioClip hurtClip;
    [SerializeField, Range(0f, 1f)] private float hurtVolume = 0.7f;

    private float footstepElapsedTime;

    private void Awake()
    {
        if (hero == null)
        {
            hero = GetComponent<HeroController>();
        }
    }

    private void Update()
    {
        if (hero == null || !hero.IsMoving)
        {
            footstepElapsedTime = 0f;
            return;
        }

        footstepElapsedTime += Time.deltaTime;
        if (footstepElapsedTime < footstepInterval)
        {
            return;
        }

        footstepElapsedTime -= footstepInterval;
        SEController.Instance?.Play(footstepClip, footstepVolume);
    }

    public void PlayAttack()
    {
        SEController.Instance?.Play(attackClip, attackVolume);
    }

    public void PlayHurt()
    {
        SEController.Instance?.Play(hurtClip, hurtVolume);
    }
}
