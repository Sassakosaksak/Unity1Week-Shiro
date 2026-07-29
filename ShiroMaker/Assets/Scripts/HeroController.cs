using UnityEngine;

public class HeroController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private GameController gameController;

    private bool isDefeated;

    private void Update()
    {
        if (isDefeated)
        {
            return;
        }

        transform.position += Vector3.right * moveSpeed * Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Goal"))
        {
            ShowDefeat();
        }
    }

    private void ShowDefeat()
    {
        if (isDefeated)
        {
            return;
        }

        isDefeated = true;
        if (gameController != null)
        {
            gameController.ShowFailure();
        }
    }
}
