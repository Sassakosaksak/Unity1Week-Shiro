using System;
using UnityEngine;

public class HeroController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField, Range(1, 5)] private int maxHp = 3;

    private GameController gameController;
    private int currentHp;
    private bool isStopped;

    public int MaxHp => maxHp;
    public int CurrentHp => currentHp;

    public event Action<int, int> HealthChanged;

    private void Awake()
    {
        currentHp = maxHp;
    }

    private void Start()
    {
        gameController = GameController.Instance;

        if (gameController == null)
        {
            Debug.LogWarning("GameController was not found in the scene.", this);
        }
    }

    private void Update()
    {
        if (isStopped)
        {
            return;
        }

        transform.position += Vector3.right * moveSpeed * Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isStopped)
        {
            return;
        }

        if (other.CompareTag("Goal"))
        {
            ShowDefeat();
            return;
        }

        TrapBase trap = other.GetComponentInParent<TrapBase>();
        if (trap != null)
        {
            trap.OnHeroHit(this);
        }
    }

    public void TakeDamage(int damage)
    {
        if (isStopped || damage <= 0)
        {
            return;
        }

        currentHp = Mathf.Max(0, currentHp - damage);
        HealthChanged?.Invoke(currentHp, maxHp);

        if (currentHp <= 0)
        {
            Stop();
            ShowSuccess();
        }
    }

    private void OnValidate()
    {
        maxHp = Mathf.Clamp(maxHp, 1, 5);
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);
    }

    private void ShowDefeat()
    {
        if (isStopped)
        {
            return;
        }

        Stop();
        if (gameController != null)
        {
            gameController.ShowFailure();
        }
    }

    private void ShowSuccess()
    {
        if (gameController != null)
        {
            gameController.ShowSuccess();
        }
    }

    private void Stop()
    {
        isStopped = true;
    }
}
