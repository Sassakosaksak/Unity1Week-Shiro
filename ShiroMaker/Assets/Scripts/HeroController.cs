using UnityEngine;

public class HeroController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 1f;

    private GameController gameController;
    private bool isStopped;

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

        if (other.TryGetComponent(out TrapBase trap))
        {
            Stop();
            trap.Activate(this);
            ShowSuccess();
        }
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
