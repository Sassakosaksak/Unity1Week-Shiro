using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class GameRewindService : MonoBehaviour
{
    private readonly List<HeroSnapshot> heroSnapshots = new List<HeroSnapshot>();
    private readonly List<TrapSnapshot> trapSnapshots = new List<TrapSnapshot>();
    private float rewindDuration;
    private Ease rewindEase;
    private Sequence rewindSequence;

    public void Initialize(float configuredRewindDuration, Ease configuredRewindEase)
    {
        rewindDuration = Mathf.Max(0f, configuredRewindDuration);
        rewindEase = configuredRewindEase;
    }

    public void CaptureSnapshot()
    {
        heroSnapshots.Clear();
        trapSnapshots.Clear();

        foreach (HeroController hero in FindObjectsByType<HeroController>(FindObjectsSortMode.None))
        {
            heroSnapshots.Add(new HeroSnapshot(hero));
        }

        foreach (TrapBase trap in FindObjectsByType<TrapBase>(FindObjectsSortMode.None))
        {
            trapSnapshots.Add(new TrapSnapshot(trap));
        }
    }

    public bool TryBeginRewind(Action started, Action completed)
    {
        if ((rewindSequence != null && rewindSequence.IsActive()) || heroSnapshots.Count == 0)
        {
            return false;
        }

        started?.Invoke();
        rewindSequence = DOTween.Sequence();
        RestoreHeroes();
        RestoreTraps();

        if (rewindSequence.Duration() <= 0f)
        {
            rewindSequence.Kill();
            rewindSequence = null;
            CompleteHeroRestore();
            completed?.Invoke();
            return true;
        }

        rewindSequence.OnComplete(() =>
        {
            rewindSequence = null;
            CompleteHeroRestore();
            completed?.Invoke();
        });
        return true;
    }

    public void Cancel()
    {
        rewindSequence?.Kill();
        rewindSequence = null;
    }

    private void RestoreHeroes()
    {
        foreach (HeroSnapshot snapshot in heroSnapshots)
        {
            if (snapshot.Hero == null)
            {
                continue;
            }

            snapshot.Hero.RestoreForRewind(snapshot.Hp);
            AppendTransformRewind(snapshot.Hero.transform, snapshot.Position, snapshot.Rotation, snapshot.Scale);
        }
    }

    private void RestoreTraps()
    {
        foreach (TrapSnapshot snapshot in trapSnapshots)
        {
            if (snapshot.Trap == null)
            {
                continue;
            }

            snapshot.Trap.RestoreForRewind();
            AppendTransformRewind(snapshot.Trap.transform, snapshot.Position, snapshot.Rotation, snapshot.Scale);
        }
    }

    private void CompleteHeroRestore()
    {
        foreach (HeroSnapshot snapshot in heroSnapshots)
        {
            snapshot.Hero?.CompleteRewindRestore();
        }
    }

    private void AppendTransformRewind(Transform target, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        if (rewindDuration <= 0f)
        {
            target.SetPositionAndRotation(position, rotation);
            target.localScale = scale;
            return;
        }

        rewindSequence.Join(target.DOMove(position, rewindDuration).SetEase(rewindEase));
        rewindSequence.Join(target.DORotateQuaternion(rotation, rewindDuration).SetEase(rewindEase));
        rewindSequence.Join(target.DOScale(scale, rewindDuration).SetEase(rewindEase));
    }

    private readonly struct HeroSnapshot
    {
        public readonly HeroController Hero;
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;
        public readonly Vector3 Scale;
        public readonly int Hp;

        public HeroSnapshot(HeroController hero)
        {
            Hero = hero;
            Position = hero.transform.position;
            Rotation = hero.transform.rotation;
            Scale = hero.transform.localScale;
            Hp = hero.CurrentHp;
        }
    }

    private readonly struct TrapSnapshot
    {
        public readonly TrapBase Trap;
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;
        public readonly Vector3 Scale;

        public TrapSnapshot(TrapBase trap)
        {
            Trap = trap;
            Position = trap.transform.position;
            Rotation = trap.transform.rotation;
            Scale = trap.transform.localScale;
        }
    }
}
