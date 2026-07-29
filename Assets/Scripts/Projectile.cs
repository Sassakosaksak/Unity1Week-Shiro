using UnityEngine;

namespace Shiro
{
    public sealed class Projectile
    {
        public readonly GameObject Visual;
        public readonly GameObject Warning;
        public Vector2 Position;
        public Vector2 Velocity;
        public Vector2 FallVelocity;
        public Vector2 FallStartPosition;
        public float Radius;
        public float Damage;
        public float ReflectCooldown;
        public float WarningTimer;
        public bool Reflected;
        public ProjectilePhase Phase;

        public Projectile(GameObject visual, GameObject warning, Vector2 position, Vector2 velocity, Vector2 fallStartPosition, Vector2 fallVelocity, float radius, float damage)
        {
            Visual = visual;
            Warning = warning;
            Position = position;
            Velocity = velocity;
            FallStartPosition = fallStartPosition;
            FallVelocity = fallVelocity;
            Radius = radius;
            Damage = damage;
            ReflectCooldown = 0f;
            WarningTimer = 0f;
            Reflected = false;
            Phase = ProjectilePhase.Warning;
        }

        public void Tick(float dt)
        {
            Position += Velocity * dt;
            ReflectCooldown = Mathf.Max(0f, ReflectCooldown - dt);
            Visual.transform.position = Position;

            if (Warning != null)
            {
                WarningTimer += dt;
                var pulse = 0.6f + Mathf.PingPong(WarningTimer * 3.8f, 0.4f);
                Warning.transform.localScale = Vector3.one * pulse;
            }

            var spin = Reflected ? -620f : Phase == ProjectilePhase.Warning ? 360f : 240f;
            Visual.transform.Rotate(0f, 0f, spin * dt);
        }

        public void BeginFall()
        {
            Phase = ProjectilePhase.Falling;
            Position = FallStartPosition;
            Velocity = FallVelocity;
            Visual.transform.position = Position;
            if (Warning != null)
            {
                Object.Destroy(Warning);
            }
        }
    }

    public enum ProjectilePhase
    {
        Warning,
        Falling
    }
}
