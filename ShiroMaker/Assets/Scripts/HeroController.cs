using UnityEngine;

public class HeroController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 1f;

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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Goal"))
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
        Debug.Log("Defeat");
    }
}
