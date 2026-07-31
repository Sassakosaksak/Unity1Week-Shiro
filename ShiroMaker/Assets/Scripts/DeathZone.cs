using UnityEngine;

public class DeathZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        HeroController hero = other.GetComponentInParent<HeroController>();
        if (hero == null)
        {
            return;
        }

        hero.Kill();
    }
}
