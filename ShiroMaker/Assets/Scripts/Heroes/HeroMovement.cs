using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class HeroMovement : MonoBehaviour
{
    private const float DefaultHeroSeparation = 0.3f;

    private Collider2D bodyCollider;
    private Rigidbody2D bodyRigidbody;

    public Collider2D BodyCollider => bodyCollider;
    public bool IsMoving { get; private set; }

    public void Initialize(Collider2D configuredCollider, Rigidbody2D configuredRigidbody)
    {
        bodyCollider = configuredCollider != null ? configuredCollider : GetComponent<Collider2D>();
        bodyRigidbody = configuredRigidbody != null ? configuredRigidbody : GetComponent<Rigidbody2D>();
    }

    public void SetMoving(bool value)
    {
        IsMoving = value;
    }

    public void SetBodyPhysicsEnabled(bool enabled)
    {
        if (bodyRigidbody != null)
        {
            bodyRigidbody.simulated = enabled;
        }
    }

    public bool IsBodyOverlappingBounds(Bounds detectionBounds)
    {
        if (bodyCollider == null || !bodyCollider.enabled)
        {
            return false;
        }

        if (bodyCollider is BoxCollider2D boxCollider)
        {
            return GetBoxColliderBounds(boxCollider).Intersects(detectionBounds);
        }

        return bodyCollider.bounds.Intersects(detectionBounds);
    }

    public bool CanMoveInDirection(Vector3 direction, HeroController owner, IReadOnlyList<HeroController> activeHeroes)
    {
        if (direction.x == 0f)
        {
            return true;
        }

        return Mathf.Abs(GetSafeMovement(
            new Vector3(Mathf.Sign(direction.x) * 0.01f, 0f, 0f), owner, activeHeroes).x) > 0f;
    }

    public void Move(Vector3 requestedMovement, HeroController owner, IReadOnlyList<HeroController> activeHeroes)
    {
        transform.position += GetSafeMovement(requestedMovement, owner, activeHeroes);
    }

    private Vector3 GetSafeMovement(Vector3 requestedMovement, HeroController owner, IReadOnlyList<HeroController> activeHeroes)
    {
        if (bodyCollider == null || requestedMovement.x == 0f)
        {
            return requestedMovement;
        }

        Bounds ownBounds = bodyCollider.bounds;
        float safeMovementX = requestedMovement.x;

        for (int i = 0; i < activeHeroes.Count; i++)
        {
            HeroController otherHero = activeHeroes[i];
            if (otherHero == null || otherHero == owner || otherHero.IsDead)
            {
                continue;
            }

            Collider2D otherCollider = otherHero.BodyCollider;
            if (otherCollider == null || !otherCollider.enabled || !IsVerticallyOverlapping(ownBounds, otherCollider.bounds))
            {
                continue;
            }

            if (requestedMovement.x > 0f && otherCollider.bounds.min.x >= ownBounds.max.x)
            {
                float maximumMovement = otherCollider.bounds.min.x - DefaultHeroSeparation - ownBounds.max.x;
                safeMovementX = Mathf.Min(safeMovementX, Mathf.Max(0f, maximumMovement));
            }
            else if (requestedMovement.x < 0f && otherCollider.bounds.max.x <= ownBounds.min.x)
            {
                float maximumMovement = otherCollider.bounds.max.x + DefaultHeroSeparation - ownBounds.min.x;
                safeMovementX = Mathf.Max(safeMovementX, Mathf.Min(0f, maximumMovement));
            }
        }

        return new Vector3(safeMovementX, requestedMovement.y, requestedMovement.z);
    }

    private static Bounds GetBoxColliderBounds(BoxCollider2D boxCollider)
    {
        Transform colliderTransform = boxCollider.transform;
        Vector2 halfSize = boxCollider.size * 0.5f;
        Vector3 right = colliderTransform.TransformVector(Vector3.right);
        Vector3 up = colliderTransform.TransformVector(Vector3.up);
        Vector2 extents = new Vector2(
            Mathf.Abs(right.x) * halfSize.x + Mathf.Abs(up.x) * halfSize.y,
            Mathf.Abs(right.y) * halfSize.x + Mathf.Abs(up.y) * halfSize.y);
        Vector3 center = colliderTransform.TransformPoint(boxCollider.offset);
        return new Bounds(center, extents * 2f);
    }

    private static bool IsVerticallyOverlapping(Bounds first, Bounds second)
    {
        return first.min.y < second.max.y && first.max.y > second.min.y;
    }
}
