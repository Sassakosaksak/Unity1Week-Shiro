using System.Collections.Generic;
using UnityEngine;

namespace Shiro
{
    public sealed class GameController : MonoBehaviour
    {
        [Header("Stage")]
        [SerializeField] private float castleHealthMax = 120f;
        [SerializeField] private float playerHealthMax = 1000f;
        [SerializeField] private float playerDamagePerShot = 5f;
        [SerializeField] private float defenseLineY = -4.1f;
        [SerializeField] private float castleHitY = 2.05f;
        [SerializeField] private float warningExitY = 5.7f;

        [Header("Cannon")]
        [SerializeField] private float shotIntervalMin = 0.28f;
        [SerializeField] private float shotIntervalMax = 0.55f;
        [SerializeField] private int burstMin = 1;
        [SerializeField] private int burstMax = 2;
        [SerializeField] private float projectileSpeedMin = 1.23f;
        [SerializeField] private float projectileSpeedMax = 1.73f;

        [Header("Reflection")]
        [SerializeField] private float baseReflectSpeed = 4.3f;
        [SerializeField] private float swingReflectScale = 0.34f;
        [SerializeField] private float weakSwingThreshold = 2.2f;
        [SerializeField] private float strongSwingThreshold = 6.4f;
        [SerializeField] private float tipDamageBonus = 1.8f;
        [SerializeField] private float magicPerReflect = 13f;
        [SerializeField] private float strongHitStopDuration = 0.065f;
        [SerializeField] private float strongHitStopScale = 0.12f;

        private readonly List<Projectile> projectiles = new List<Projectile>();
        private readonly Vector2[] cannonPositions =
        {
            new Vector2(-2.35f, 2.75f),
            new Vector2(0f, 2.9f),
            new Vector2(2.35f, 2.75f)
        };

        private Camera gameCamera;
        private SwordController sword;
        private GameObject castle;
        private GameObject castleFlash;
        private float castleHealth;
        private float playerHealth;
        private float nextShotAt;
        private float elapsed;
        private float magic;
        private float hitStopTimer;
        private float savedTimeScale = 1f;
        private float comboTimer;
        private int combo;
        private int maxCombo;
        private int perfectReflects;
        private float maxReflectSpeed;
        private bool finished;
        private string resultText = string.Empty;

        private void Awake()
        {
            Random.InitState(System.DateTime.Now.Millisecond);
            SetupCamera();
            SetupStage();
            StartGame();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                StartGame();
            }

            if (finished)
            {
                return;
            }

            TickHitStop();
            elapsed += Time.deltaTime;
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0f)
            {
                combo = 0;
            }

            if (Input.GetKeyDown(KeyCode.Space) && magic >= 100f)
            {
                FireMagicCannon();
            }

            if (Time.time >= nextShotAt)
            {
                SpawnBurst();
                ScheduleNextShot();
            }

            TickProjectiles();
            CheckEndState();
        }

        private void OnGUI()
        {
            GUI.color = Color.white;
            GUI.Label(new Rect(18, 16, 360, 30), $"Castle HP: {Mathf.CeilToInt(castleHealth)} / {castleHealthMax}");
            GUI.Label(new Rect(18, 42, 360, 30), $"Your HP: {Mathf.CeilToInt(playerHealth)} / {playerHealthMax}  Magic: {Mathf.FloorToInt(magic)}%  Combo: {combo}");
            GUI.Label(new Rect(18, 68, 500, 30), $"Tip hits: {perfectReflects}  Max speed: {maxReflectSpeed:0.0}  Time: {elapsed:0.0}");
            GUI.Label(new Rect(18, 94, 620, 30), $"Sword angular speed: {sword.AngularVelocityRadians * Mathf.Rad2Deg:0} deg/s");
            GUI.Label(new Rect(18, 120, 620, 30), "Mouse movement sets the sword trajectory. Reflected shots hurt you if sent downward.");

            if (magic >= 100f && !finished)
            {
                GUI.Label(new Rect(Screen.width * 0.5f - 170f, 16, 420, 40), "MAGIC READY - Press Space for Magic Cannon");
            }

            if (finished)
            {
                var rect = new Rect(Screen.width * 0.5f - 190f, Screen.height * 0.5f - 70f, 420f, 160f);
                GUI.Box(rect, resultText + "\n\nPress R to retry");
            }
        }

        private void SetupCamera()
        {
            gameCamera = Camera.main;
            if (gameCamera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                gameCamera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
            }

            gameCamera.transform.position = new Vector3(0f, 0f, -10f);
            gameCamera.transform.rotation = Quaternion.identity;
            gameCamera.orthographic = true;
            gameCamera.orthographicSize = 5f;
            gameCamera.clearFlags = CameraClearFlags.SolidColor;
            gameCamera.nearClipPlane = 0.1f;
            gameCamera.farClipPlane = 20f;
            gameCamera.backgroundColor = new Color(0.06f, 0.08f, 0.12f);
        }

        private void SetupStage()
        {
            VisualFactory.SpriteObject("Sky", new Color(0.08f, 0.11f, 0.17f), new Vector2(20f, 10f), Vector3.zero, transform, -20);
            VisualFactory.SpriteObject("Ground", new Color(0.18f, 0.16f, 0.13f), new Vector2(20f, 1.0f), new Vector3(0f, -4.55f, 0f), transform, -10);
            VisualFactory.SpriteObject("Defense Line", new Color(0.2f, 0.55f, 0.82f, 0.75f), new Vector2(15.6f, 0.08f), new Vector3(0f, defenseLineY, 0f), transform, 4);

            castle = VisualFactory.SpriteObject("Enemy Castle", new Color(0.52f, 0.5f, 0.56f), new Vector2(6.2f, 1.75f), new Vector3(0f, 3.05f, 0f), transform, 1);
            castleFlash = VisualFactory.SpriteObject("Castle Damage Flash", new Color(1f, 0.65f, 0.35f, 0f), new Vector2(6.35f, 1.9f), castle.transform.position, transform, 2);
            VisualFactory.SpriteObject("Castle Gate", new Color(0.24f, 0.22f, 0.25f), new Vector2(1.1f, 1.15f), new Vector3(0f, 2.55f, 0f), transform, 3);
            VisualFactory.SpriteObject("Left Tower", new Color(0.46f, 0.46f, 0.52f), new Vector2(1.1f, 2.45f), new Vector3(-3.15f, 2.78f, 0f), transform, 2);
            VisualFactory.SpriteObject("Right Tower", new Color(0.46f, 0.46f, 0.52f), new Vector2(1.1f, 2.45f), new Vector3(3.15f, 2.78f, 0f), transform, 2);

            foreach (var cannonPosition in cannonPositions)
            {
                VisualFactory.SpriteObject("Cannon", new Color(0.22f, 0.21f, 0.24f), new Vector2(0.95f, 0.42f), cannonPosition, transform, 3);
            }

            var swordObject = new GameObject("Sword");
            swordObject.transform.SetParent(transform);
            sword = swordObject.AddComponent<SwordController>();
            sword.Initialize(gameCamera);
        }

        private void StartGame()
        {
            EndHitStop();
            foreach (var projectile in projectiles)
            {
                Destroy(projectile.Visual);
                if (projectile.Warning != null)
                {
                    Destroy(projectile.Warning);
                }
            }

            projectiles.Clear();
            castleHealth = castleHealthMax;
            playerHealth = playerHealthMax;
            elapsed = 0f;
            magic = 0f;
            combo = 0;
            maxCombo = 0;
            perfectReflects = 0;
            maxReflectSpeed = 0f;
            finished = false;
            resultText = string.Empty;
            VisualFactory.SetColor(castle, new Color(0.52f, 0.5f, 0.56f));
            ScheduleNextShot(0.3f);
        }

        private void ScheduleNextShot(float delay = -1f)
        {
            nextShotAt = Time.time + (delay >= 0f ? delay : Random.Range(shotIntervalMin, shotIntervalMax));
        }

        private void SpawnBurst()
        {
            var burstCount = Random.Range(burstMin, burstMax + 1);
            if (elapsed > 20f)
            {
                burstCount += 1;
            }
            if (elapsed > 45f)
            {
                burstCount += 1;
            }

            for (var i = 0; i < burstCount; i++)
            {
                SpawnProjectile(i * 0.08f);
            }
        }

        private void SpawnProjectile(float startDelay)
        {
            var cannonIndex = Random.Range(0, cannonPositions.Length);
            var origin = cannonPositions[cannonIndex];
            var fallTarget = new Vector2(Random.Range(-6.5f, 6.5f), Random.Range(-3.75f, -2.15f));
            var speed = Random.Range(projectileSpeedMin, projectileSpeedMax) + Mathf.Min(elapsed * 0.035f, 1.8f);
            var radius = Random.value < 0.15f ? 0.36f : 0.25f;
            var color = radius > 0.3f ? new Color(0.95f, 0.38f, 0.18f) : new Color(0.92f, 0.78f, 0.43f);
            var visual = VisualFactory.SpriteObject("Cannonball", color, Vector2.one * radius * 2f, origin, transform, 7);
            var warning = VisualFactory.SpriteObject("Landing Warning", new Color(1f, 0.25f, 0.18f, 0.42f), Vector2.one * 0.55f, fallTarget, transform, 5);
            var arcDirection = new Vector2(Random.Range(-0.35f, 0.35f), 1f).normalized;
            var fallStart = new Vector2(fallTarget.x + Random.Range(-0.55f, 0.55f), warningExitY);
            var fallVelocity = (fallTarget - fallStart).normalized * speed;
            var projectile = new Projectile(visual, warning, origin, arcDirection * speed, fallStart, fallVelocity, radius, 3f);
            projectile.WarningTimer = -startDelay;
            projectiles.Add(projectile);
        }

        private void TickProjectiles()
        {
            for (var i = projectiles.Count - 1; i >= 0; i--)
            {
                var projectile = projectiles[i];
                if (projectile.WarningTimer < 0f)
                {
                    projectile.WarningTimer += Time.deltaTime;
                    continue;
                }

                projectile.Tick(Time.deltaTime);

                if (projectile.Phase == ProjectilePhase.Warning && projectile.Position.y >= warningExitY)
                {
                    projectile.BeginFall();
                }

                if (!projectile.Reflected && projectile.Phase == ProjectilePhase.Falling)
                {
                    TryReflect(projectile);
                }

                if (projectile.Reflected && projectile.Position.y >= castleHitY && Mathf.Abs(projectile.Position.x) <= 4.2f)
                {
                    DamageCastle(projectile.Damage);
                    RemoveProjectile(i);
                    continue;
                }

                if (projectile.Position.y <= defenseLineY)
                {
                    playerHealth = Mathf.Max(0f, playerHealth - playerDamagePerShot);
                    combo = 0;
                    RemoveProjectile(i);
                    continue;
                }

                if (Mathf.Abs(projectile.Position.x) > 10.5f || Mathf.Abs(projectile.Position.y) > 6f)
                {
                    RemoveProjectile(i);
                }
            }
        }

        private void TryReflect(Projectile projectile)
        {
            if (projectile.ReflectCooldown > 0f)
            {
                return;
            }

            var hit = sword.SampleHit(projectile.Position);
            var hitRadius = projectile.Radius + sword.BladeWidth * 0.85f;
            if (hit.Distance > hitRadius)
            {
                return;
            }

            var normal = sword.BladeNormal;
            if (normal.y < 0f)
            {
                normal = -normal;
            }

            var liftPower = Vector2.Dot(hit.PointVelocity, normal);
            var bladeSpeed = Mathf.Max(0f, liftPower) + Mathf.Abs(sword.AngularVelocityRadians) * Mathf.Lerp(0.25f, 0.75f, hit.Leverage);
            if (liftPower <= 0f || bladeSpeed < weakSwingThreshold)
            {
                sword.DampenSpin(0.72f);
                projectile.Velocity = Vector2.Lerp(projectile.Velocity, new Vector2(projectile.Velocity.x * 0.7f, -Mathf.Abs(projectile.Velocity.y) * 1.08f), 0.45f);
                projectile.ReflectCooldown = 0.14f;
                combo = 0;
                var blockedRenderer = projectile.Visual.GetComponent<SpriteRenderer>();
                if (blockedRenderer != null)
                {
                    blockedRenderer.color = new Color(1f, 0.42f, 0.32f);
                }
                return;
            }

            var isStrongReflect = bladeSpeed >= strongSwingThreshold;
            var tipBonus = Mathf.Lerp(0.65f, tipDamageBonus, hit.Leverage);
            var reflectSpeed = baseReflectSpeed + liftPower * swingReflectScale + hit.Leverage * (isStrongReflect ? 3.2f : 1.1f) + sword.SwingCharge * 1.8f + sword.SweepCharge * 1.2f;
            var castleAim = (new Vector2(Mathf.Clamp(projectile.Position.x * -0.35f, -1.5f, 1.5f), 3.05f) - projectile.Position).normalized;
            var bladeAim = new Vector2(Mathf.Clamp(normal.x * 0.85f, -0.9f, 0.9f), 1f).normalized;
            var bladeAimWeight = isStrongReflect ? 0.55f : 0.35f;
            bladeAimWeight = Mathf.Lerp(bladeAimWeight, 0.18f, sword.SwingCharge);
            bladeAimWeight = Mathf.Lerp(bladeAimWeight, 0.1f, sword.SweepCharge);
            var aimAssist = Vector2.Lerp(castleAim, bladeAim, bladeAimWeight).normalized;
            var reflectedVelocity = Vector2.Reflect(projectile.Velocity, normal) * 0.32f + aimAssist * reflectSpeed + hit.PointVelocity * 0.1f;

            if (reflectedVelocity.y < 2f)
            {
                reflectedVelocity.y = Mathf.Abs(reflectedVelocity.y) + reflectSpeed;
            }

            projectile.Velocity = Vector2.ClampMagnitude(reflectedVelocity, isStrongReflect ? 15.5f : 10.5f);
            projectile.Damage = isStrongReflect ? Mathf.Lerp(5f, 7f, Mathf.InverseLerp(strongSwingThreshold, 13f, bladeSpeed)) * Mathf.Lerp(0.9f, 1f, hit.Leverage) : 3f;
            projectile.Reflected = true;
            projectile.ReflectCooldown = 0.25f;

            combo++;
            comboTimer = 2.2f;
            maxCombo = Mathf.Max(maxCombo, combo);
            maxReflectSpeed = Mathf.Max(maxReflectSpeed, projectile.Velocity.magnitude);

            var magicGain = magicPerReflect * Mathf.Lerp(isStrongReflect ? 1.15f : 0.45f, isStrongReflect ? 1.9f : 0.9f, hit.Leverage);
            magic = Mathf.Clamp(magic + magicGain, 0f, 100f);

            if (isStrongReflect && (hit.Leverage > 0.78f || liftPower > 8f))
            {
                perfectReflects++;
                magic = Mathf.Clamp(magic + 8f, 0f, 100f);
                StartHitStop(strongHitStopDuration);
            }

            var renderer = projectile.Visual.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = isStrongReflect ? new Color(0.62f, 0.95f, 1f) : new Color(1f, 0.95f, 0.55f);
            }
        }

        private void StartHitStop(float duration)
        {
            if (hitStopTimer <= 0f)
            {
                savedTimeScale = Time.timeScale;
            }

            hitStopTimer = Mathf.Max(hitStopTimer, duration);
            Time.timeScale = strongHitStopScale;
        }

        private void TickHitStop()
        {
            if (hitStopTimer <= 0f)
            {
                return;
            }

            hitStopTimer -= Time.unscaledDeltaTime;
            if (hitStopTimer <= 0f)
            {
                EndHitStop();
            }
        }

        private void EndHitStop()
        {
            hitStopTimer = 0f;
            Time.timeScale = savedTimeScale;
        }

        private void OnDisable()
        {
            EndHitStop();
        }

        private void FireMagicCannon()
        {
            magic = 0f;
            var bonusDamage = 30f + projectiles.Count * 7f + maxCombo * 1.5f;
            DamageCastle(bonusDamage);

            for (var i = projectiles.Count - 1; i >= 0; i--)
            {
                var projectile = projectiles[i];
                projectile.Reflected = true;
                projectile.Velocity = new Vector2(Random.Range(-1.2f, 1.2f), Random.Range(8.5f, 11.5f));
                projectile.Damage *= 1.4f;
                var renderer = projectile.Visual.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    renderer.color = new Color(0.55f, 0.75f, 1f);
                }
            }
        }

        private void DamageCastle(float damage)
        {
            castleHealth = Mathf.Max(0f, castleHealth - damage);
            var damageRatio = 1f - castleHealth / castleHealthMax;
            VisualFactory.SetColor(castle, Color.Lerp(new Color(0.52f, 0.5f, 0.56f), new Color(0.95f, 0.32f, 0.22f), damageRatio));
            VisualFactory.SetColor(castleFlash, new Color(1f, 0.65f, 0.35f, 0.32f));
            Invoke(nameof(ClearCastleFlash), 0.08f);
        }

        private void ClearCastleFlash()
        {
            VisualFactory.SetColor(castleFlash, new Color(1f, 0.65f, 0.35f, 0f));
        }

        private void RemoveProjectile(int index)
        {
            Destroy(projectiles[index].Visual);
            projectiles.RemoveAt(index);
        }

        private void CheckEndState()
        {
            if (castleHealth <= 0f)
            {
                finished = true;
                resultText = $"Castle Destroyed!\nTime {elapsed:0.0}s / Max Combo {maxCombo}\nPerfect Reflects {perfectReflects}";
            }
            else if (playerHealth <= 0f)
            {
                finished = true;
                resultText = $"Defense Broken\nCastle HP {Mathf.CeilToInt(castleHealth)}\nMax Combo {maxCombo}";
            }
        }
    }
}
