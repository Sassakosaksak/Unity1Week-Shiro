using UnityEngine;

public class PriestMagicEffectRelay : MonoBehaviour
{
    [SerializeField] private AudioClip attackClip;
    [SerializeField] private AudioClip healClip;
    [SerializeField] private AudioClip raiseClip;
    [SerializeField, Range(0f, 1f)] private float volume = 0.7f;

    private PriestHeroBehavior source;

    public void Initialize(PriestHeroBehavior priest)
    {
        source = priest;
    }

    // Called by the Attack Effect's final Animation Event.
    public void Defeat()
    {
        source?.OnAttackEffectCompleted(this);
    }

    public void PlayAttackSound()
    {
        Play(attackClip);
    }

    public void PlayHealSound()
    {
        Play(healClip);
    }

    public void PlayRaiseSound()
    {
        Play(raiseClip);
    }

    private void Play(AudioClip clip)
    {
        SEController.Instance?.Play(clip, volume);
    }
}
