using DG.Tweening;
using UnityEngine;
using UnityEngine.Tilemaps;

public class RollingRockTrap : TrapBase
{
    private enum RockState
    {
        Waiting = 0,
        Appearing = 1,
        Rolling = 2,
        Breaking = 3
    }

    [SerializeField] private Collider2D damageCollider;
    [SerializeField] private SpriteRenderer rockRenderer;
    [SerializeField] private Rigidbody2D rockBody;
    [SerializeField] private LayerMask additionalBlockingLayers;
    [SerializeField, Min(0f)] private float appearDuration = 0.5f;
    [SerializeField, Min(0f)] private float rollingSpeed = 3f;
    [SerializeField, Min(0.01f)] private float breakBlinkInterval = 0.08f;
    [SerializeField, Min(1)] private int breakBlinkCount = 3;

    private static readonly int AppearHash = Animator.StringToHash("Appear");
    private static readonly int RollingHash = Animator.StringToHash("Rolling");
    private static readonly int BreakHash = Animator.StringToHash("Break");
    private Color defaultColor = Color.white;
    private Vector3 initialPosition;
    private float appearRemainingTime;
    private RockState state;
    private Tween breakTween;

    public bool IsRolling => state == RockState.Rolling;

    public void CaptureInitialPosition()
    {
        initialPosition = transform.position;
    }

    protected override void Awake()
    {
        base.Awake();
        initialPosition = transform.position;

        if (damageCollider == null)
        {
            damageCollider = GetComponent<Collider2D>();
        }

        if (rockRenderer == null)
        {
            rockRenderer = GetComponent<SpriteRenderer>();
        }

        if (rockBody == null)
        {
            rockBody = GetComponent<Rigidbody2D>();
        }

        if (rockRenderer != null)
        {
            defaultColor = rockRenderer.color;
        }

        SetDamageColliderActive(false);
    }

    private void Update()
    {
        if (!CanRun)
        {
            return;
        }

        if (state == RockState.Appearing)
        {
            appearRemainingTime -= Time.deltaTime;
            if (appearRemainingTime <= 0f)
            {
                StartRolling();
            }

            return;
        }
    }

    private void FixedUpdate()
    {
        if (CanRun && state == RockState.Rolling)
        {
            MoveLeft();
        }
    }

    protected override void OnDestroy()
    {
        breakTween?.Kill();
        base.OnDestroy();
    }

    public bool Activate()
    {
        if (!CanRun || state != RockState.Waiting)
        {
            return false;
        }

        state = RockState.Appearing;
        appearRemainingTime = appearDuration;
        SetDamageColliderActive(false);
        TrapAnimator?.SetTrigger(AppearHash);
        return true;
    }

    public override void RestoreForRewind()
    {
        gameObject.SetActive(true);
        breakTween?.Kill();
        breakTween = null;
        transform.position = initialPosition;
        if (rockBody != null)
        {
            rockBody.position = initialPosition;
            rockBody.linearVelocity = Vector2.zero;
            rockBody.angularVelocity = 0f;
        }

        state = RockState.Waiting;
        appearRemainingTime = 0f;
        SetDamageColliderActive(false);

        if (rockRenderer != null)
        {
            rockRenderer.color = defaultColor;
        }

        base.RestoreForRewind();
    }

    public override void OnHeroHit(HeroController hero)
    {
        if (hero != null && hero.TryGetComponent(out WarriorHeroBehavior warrior))
        {
            warrior.StartRockAttack(this);
            return;
        }

        base.OnHeroHit(hero);
    }

    public void BreakFromWarrior()
    {
        if (state == RockState.Rolling)
        {
            BeginBreaking();
        }
    }

    private void StartRolling()
    {
        state = RockState.Rolling;
        SetDamageColliderActive(true);
        TrapAnimator?.SetTrigger(RollingHash);
    }

    private void MoveLeft()
    {
        if (rollingSpeed <= 0f || rockBody == null)
        {
            return;
        }

        rockBody.MovePosition(rockBody.position + Vector2.left * (rollingSpeed * Time.fixedDeltaTime));
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (state == RockState.Rolling && IsBlockingCollider(other))
        {
            BeginBreaking();
        }
    }

    private bool IsBlockingCollider(Collider2D other)
    {
        if (other == null)
        {
            return false;
        }

        if (other.GetComponent<TilemapCollider2D>() != null)
        {
            return true;
        }

        return additionalBlockingLayers.value != 0
            && (additionalBlockingLayers.value & (1 << other.gameObject.layer)) != 0;
    }

    private void BeginBreaking()
    {
        if (state == RockState.Breaking)
        {
            return;
        }

        state = RockState.Breaking;
        SetDamageColliderActive(false);
        TrapAnimator?.SetTrigger(BreakHash);
        TrapAnimator?.Play(BreakHash, 0, 0f);

        if (rockRenderer == null)
        {
            gameObject.SetActive(false);
            return;
        }

        breakTween?.Kill();
        rockRenderer.color = defaultColor;
        breakTween = rockRenderer
            .DOFade(0f, breakBlinkInterval)
            .SetLoops(breakBlinkCount * 2, LoopType.Yoyo)
            .OnComplete(() => gameObject.SetActive(false));
    }

    private void SetDamageColliderActive(bool isActive)
    {
        if (damageCollider != null)
        {
            damageCollider.enabled = isActive;
        }
    }
}
