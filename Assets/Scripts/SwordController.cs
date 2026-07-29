using UnityEngine;

namespace Shiro
{
    public sealed class SwordController : MonoBehaviour
    {
        [Header("Grip")]
        [SerializeField] private float mouseImpulse = 0.62f;
        [SerializeField] private float cursorPull = 7.2f;
        [SerializeField] private float farPullBonus = 5.5f;
        [SerializeField] private float gripDamping = 2.7f;
        [SerializeField] private float maxGripSpeed = 77.5f;
        [SerializeField] private float reversalInputLoss = 0.62f;
        [SerializeField] private Vector2 minBounds = new Vector2(-7.4f, -4.0f);
        [SerializeField] private Vector2 maxBounds = new Vector2(7.4f, 1.9f);

        [Header("Blade")]
        [SerializeField] private float bladeLength = 2.8f;
        [SerializeField] private float bladeWidth = 0.26f;
        [SerializeField] private float torqueFromSideMove = 132f;
        [SerializeField] private float bigSwingTorqueBonus = 1.65f;
        [SerializeField] private float circleTorqueBonus = 2200f;
        [SerializeField] private float circleRadiusMin = 0.85f;
        [SerializeField] private float circleRadiusMax = 3.6f;
        [SerializeField] private float circleSpeedThreshold = 300f;
        [SerializeField] private float angularDamping = 0.8f;
        [SerializeField] private float maxAngularSpeed = 32400f;

        [Header("Sweep")]
        [SerializeField] private float sweepSpeedThreshold = 4.2f;
        [SerializeField] private float sweepUpSpeedThreshold = 1.1f;
        [SerializeField] private float sweepStartSideX = 2.05f;
        [SerializeField] private float sweepStanceAreaWidth = 1.34f;
        [SerializeField] private float sweepGuideY = -1.65f;
        [SerializeField] private float sweepGuideHalfWidth = 3.65f;
        [SerializeField] private float sweepGuideLift = 0.9f;
        [SerializeField] private float sweepArcLiftRequired = 0.45f;
        [SerializeField] private float sweepHoldTime = 0.22f;
        [SerializeField] private float sweepStabilizeTorque = 185f;
        [SerializeField] private float sweepSpinDamping = 14f;
        [SerializeField] private float sweepCircleSuppression = 1f;
        [SerializeField] private float sweepFinishSpin = 2700f;
        [SerializeField] private float sweepNoDampingDuration = 0.5f;

        private Camera gameCamera;
        private Transform grip;
        private Transform blade;
        private Transform tipMarker;
        private Transform cursorMarker;
        private Transform sweepEffectRoot;
        private SpriteRenderer[] sweepEffectRenderers;
        private Transform sweepGuideRoot;
        private SpriteRenderer[] sweepGuideRenderers;
        private SpriteRenderer leftSweepAreaRenderer;
        private SpriteRenderer rightSweepAreaRenderer;
        private Vector2 gripPosition;
        private Vector2 previousGripPosition;
        private Vector2 gripVelocity;
        private Vector2 targetPosition;
        private Vector2 previousTargetPosition;
        private Vector2 previousMouseVelocity;
        private Vector2 previousCursorOffset;
        private float swingCharge;
        private float circleCharge;
        private float circleDirection;
        private float sweepCharge;
        private float sweepMemory;
        private float sweepStartY;
        private float sweepPeakY;
        private float sweepDirection;
        private bool sweepGestureActive;
        private bool sweepAngleLocked;
        private bool sweepFinishSpinApplied;
        private bool sweepCompleted;
        private float noAngularDampingTimer;
        private float angle;
        private float angularVelocity;

        public Vector2 GripPosition => gripPosition;
        public Vector2 GripVelocity => gripVelocity;
        public float Angle => angle;
        public float AngularVelocityRadians => angularVelocity * Mathf.Deg2Rad;
        public float BladeLength => bladeLength;
        public float BladeWidth => bladeWidth;
        public Vector2 BladeDirection => new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
        public Vector2 BladeNormal => new Vector2(-BladeDirection.y, BladeDirection.x);
        public Vector2 TipPosition => gripPosition + BladeDirection * bladeLength;
        public float SwingCharge => swingCharge;
        public float SweepCharge => sweepCharge;

        public void Initialize(Camera camera)
        {
            gameCamera = camera;
            gripPosition = new Vector2(-0.7f, -2.65f);
            previousGripPosition = gripPosition;
            targetPosition = gripPosition;
            previousTargetPosition = gripPosition;
            previousMouseVelocity = Vector2.zero;
            previousCursorOffset = Vector2.right;
            angle = 70f;

            grip = VisualFactory.SpriteObject("Grip", new Color(0.28f, 0.18f, 0.12f), new Vector2(0.42f, 0.42f), gripPosition, transform, 10).transform;
            blade = VisualFactory.SpriteObject("Blade", new Color(0.82f, 0.9f, 1f), new Vector2(bladeLength, bladeWidth), Vector3.zero, transform, 9).transform;
            tipMarker = VisualFactory.SpriteObject("Tip", new Color(0.58f, 0.92f, 1f), new Vector2(0.28f, 0.36f), Vector3.zero, transform, 11).transform;
            cursorMarker = VisualFactory.SpriteObject("Cursor Pull", new Color(1f, 1f, 1f, 0.28f), new Vector2(0.18f, 0.18f), gripPosition, transform, 8).transform;
            CreateSweepEffect();
            CreateSweepAreas();
            RefreshVisuals();
        }

        private void Update()
        {
            var dt = Mathf.Max(Time.deltaTime, 0.0001f);
            targetPosition = GetMouseWorldPosition();
            targetPosition.x = Mathf.Clamp(targetPosition.x, minBounds.x, maxBounds.x);
            targetPosition.y = Mathf.Clamp(targetPosition.y, minBounds.y, maxBounds.y);
            var mouseVelocity = Vector2.ClampMagnitude((targetPosition - previousTargetPosition) / dt, 28f);
            previousTargetPosition = targetPosition;

            previousGripPosition = gripPosition;
            var toCursor = targetPosition - gripPosition;
            var distance = toCursor.magnitude;
            var pullStrength = cursorPull + Mathf.SmoothStep(0f, farPullBonus, Mathf.InverseLerp(0.35f, 3.8f, distance));
            var pullToCursor = toCursor * pullStrength;
            var reversal = previousMouseVelocity.sqrMagnitude > 0.01f && Vector2.Dot(previousMouseVelocity.normalized, mouseVelocity.normalized) < -0.45f;
            var inputLoss = reversal && mouseVelocity.magnitude > 12f ? reversalInputLoss : 1f;
            var plannedMotion = mouseVelocity * mouseImpulse * inputLoss;
            gripVelocity += (plannedMotion + pullToCursor) * dt;
            gripVelocity *= Mathf.Exp(-gripDamping * dt);
            gripVelocity = Vector2.ClampMagnitude(gripVelocity, maxGripSpeed);
            gripPosition += gripVelocity * dt;
            previousMouseVelocity = mouseVelocity;
            gripPosition.x = Mathf.Clamp(gripPosition.x, minBounds.x, maxBounds.x);
            gripPosition.y = Mathf.Clamp(gripPosition.y, minBounds.y, maxBounds.y);

            var side = Vector2.Dot(gripVelocity, BladeNormal);
            var sideMove = Mathf.Sign(side) * Mathf.Max(0f, Mathf.Abs(side) - 0.75f);
            var distanceCharge = Mathf.InverseLerp(0.65f, 3.8f, distance);
            var speedCharge = Mathf.InverseLerp(3.5f, 18f, gripVelocity.magnitude);
            var targetSwingCharge = distanceCharge * speedCharge;
            swingCharge = Mathf.Lerp(swingCharge, targetSwingCharge, 1f - Mathf.Exp(-9f * dt));

            var cursorOffset = targetPosition - gripPosition;
            var orbitSpeed = 0f;
            if (previousCursorOffset.sqrMagnitude > 0.01f && cursorOffset.sqrMagnitude > 0.01f)
            {
                orbitSpeed = Vector2.SignedAngle(previousCursorOffset, cursorOffset) / dt;
            }

            var steadyRadius = distance >= circleRadiusMin && distance <= circleRadiusMax;
            var lateralMotion = Mathf.Abs(gripVelocity.x) >= sweepSpeedThreshold;
            var upwardArcStart = gripVelocity.y >= sweepUpSpeedThreshold || targetPosition.y > gripPosition.y + 0.4f;
            var leftStance = gripPosition.x <= minBounds.x + sweepStanceAreaWidth;
            var rightStance = gripPosition.x >= maxBounds.x - sweepStanceAreaWidth;
            var cursorInLeftArea = targetPosition.x <= minBounds.x + sweepStanceAreaWidth;
            var cursorInRightArea = targetPosition.x >= maxBounds.x - sweepStanceAreaWidth;
            var startsRightSweep = leftStance && cursorInRightArea && upwardArcStart;
            var startsLeftSweep = rightStance && cursorInLeftArea && upwardArcStart;
            if (!sweepGestureActive && (startsRightSweep || startsLeftSweep))
            {
                sweepGestureActive = true;
                sweepDirection = startsRightSweep ? 1f : -1f;
                sweepStartY = gripPosition.y;
                sweepPeakY = gripPosition.y;
                sweepAngleLocked = true;
                sweepFinishSpinApplied = false;
                sweepCompleted = false;
            }

            if (sweepGestureActive)
            {
                var movingCorrectly = Mathf.Sign(gripVelocity.x) == Mathf.Sign(sweepDirection) && lateralMotion;
                var fellTooFar = gripPosition.y < sweepStartY - 0.9f;
                if (!movingCorrectly || fellTooFar)
                {
                    sweepGestureActive = false;
                    sweepAngleLocked = false;
                    sweepMemory = 0f;
                }
                else
                {
                    sweepPeakY = Mathf.Max(sweepPeakY, gripPosition.y);
                    var reachedOppositeStance = sweepDirection > 0f
                        ? gripPosition.x >= maxBounds.x - sweepStanceAreaWidth
                        : gripPosition.x <= minBounds.x + sweepStanceAreaWidth;
                    var liftedEnough = sweepPeakY - sweepStartY >= sweepArcLiftRequired;
                    if (reachedOppositeStance && sweepAngleLocked && liftedEnough)
                    {
                        sweepGestureActive = false;
                        sweepAngleLocked = false;
                        sweepMemory = sweepHoldTime;
                        sweepCompleted = true;
                    }
                }
            }

            sweepMemory = Mathf.Max(0f, sweepMemory - dt);
            var sweepActive = sweepAngleLocked || sweepMemory > 0f;
            sweepCharge = Mathf.MoveTowards(sweepCharge, sweepActive ? 1f : 0f, dt * (sweepActive ? 14f : 7f));

            var steadyCircle = steadyRadius && Mathf.Abs(orbitSpeed) >= circleSpeedThreshold && sweepCharge < 0.08f;
            if (steadyCircle)
            {
                circleDirection = Mathf.Sign(orbitSpeed);
                circleCharge = Mathf.MoveTowards(circleCharge, 1f, dt * 6.5f);
            }
            else
            {
                circleCharge = Mathf.MoveTowards(circleCharge, 0f, dt * 2.2f);
            }

            previousCursorOffset = cursorOffset;

            var bigSwingBoost = 1f + swingCharge * bigSwingTorqueBonus;
            var circleTorque = circleDirection * circleTorqueBonus * circleCharge * circleCharge * (1f - sweepCharge * sweepCircleSuppression);
            var desiredSweepAngle = sweepDirection >= 0f ? 120f : 60f;
            var sweepDelta = Mathf.DeltaAngle(angle, desiredSweepAngle);
            var sweepTorque = sweepDelta * sweepStabilizeTorque * sweepCharge;
            var sideTorque = sideMove * torqueFromSideMove * bigSwingBoost + circleTorque + sweepTorque;

            angularVelocity += sideTorque * dt;
            if (sweepAngleLocked)
            {
                angularVelocity *= Mathf.Exp(-sweepSpinDamping * sweepCharge * dt);
            }
            if (sweepAngleLocked)
            {
                angle = desiredSweepAngle;
                angularVelocity = 0f;
            }

            if (sweepCompleted && !sweepGestureActive && sweepMemory > 0f && !sweepFinishSpinApplied)
            {
                angularVelocity += -sweepDirection * sweepFinishSpin;
                sweepFinishSpinApplied = true;
                sweepCompleted = false;
                noAngularDampingTimer = sweepNoDampingDuration;
            }
            angularVelocity = Mathf.Clamp(angularVelocity, -maxAngularSpeed, maxAngularSpeed);
            if (noAngularDampingTimer > 0f)
            {
                noAngularDampingTimer = Mathf.Max(0f, noAngularDampingTimer - dt);
            }
            else
            {
                angularVelocity *= Mathf.Exp(-angularDamping * dt);
            }
            angle += angularVelocity * dt;

            RefreshVisuals();
        }

        public SwordHit SampleHit(Vector2 point)
        {
            var local = point - gripPosition;
            var along = Mathf.Clamp(Vector2.Dot(local, BladeDirection), 0f, bladeLength);
            var closest = gripPosition + BladeDirection * along;
            var distance = Vector2.Distance(point, closest);
            var leverage = along / bladeLength;
            var spinVelocity = BladeNormal * (AngularVelocityRadians * along);
            var pointVelocity = gripVelocity + spinVelocity;

            return new SwordHit(closest, distance, leverage, pointVelocity);
        }

        public void ApplyPowerUp(float lengthMultiplier, float controlMultiplier)
        {
            bladeLength = 2.8f * lengthMultiplier;
            mouseImpulse = 0.62f * controlMultiplier;
            cursorPull = 7.2f * controlMultiplier;
            gripDamping = 2.7f * controlMultiplier;
            angularDamping = 1.2f * controlMultiplier;
            RefreshVisuals();
        }

        public void ApplyImpact(Vector2 hitPoint, Vector2 incomingVelocity, float force)
        {
            var lever = hitPoint - gripPosition;
            var spinSign = Mathf.Sign(lever.x * incomingVelocity.y - lever.y * incomingVelocity.x);
            angularVelocity += spinSign * force;
            gripVelocity += incomingVelocity.normalized * force * 0.08f;
        }

        public void DampenSpin(float dampingMultiplier)
        {
            angularVelocity *= dampingMultiplier;
        }

        private Vector2 GetMouseWorldPosition()
        {
            var mouse = Input.mousePosition;
            mouse.z = -gameCamera.transform.position.z;
            return gameCamera.ScreenToWorldPoint(mouse);
        }

        private void RefreshVisuals()
        {
            var direction = BladeDirection;
            grip.position = gripPosition;
            cursorMarker.position = targetPosition;
            blade.position = gripPosition + direction * (bladeLength * 0.5f);
            blade.rotation = Quaternion.Euler(0f, 0f, angle);
            blade.localScale = new Vector3(bladeLength, bladeWidth, 1f);
            tipMarker.position = TipPosition;
            tipMarker.rotation = Quaternion.Euler(0f, 0f, angle);
            RefreshSweepEffect();
            RefreshSweepGuide();
            RefreshSweepAreas();
        }

        private void CreateSweepAreas()
        {
            var areaHeight = maxBounds.y - minBounds.y;
            var areaWidth = sweepStanceAreaWidth;
            var areaY = (minBounds.y + maxBounds.y) * 0.5f;
            var leftArea = VisualFactory.SpriteObject("Left Sweep Stance Area", new Color(0.2f, 0.75f, 1f, 0.08f), new Vector2(areaWidth, areaHeight), new Vector3(minBounds.x + areaWidth * 0.5f, areaY, 0f), transform, -2);
            var rightArea = VisualFactory.SpriteObject("Right Sweep Stance Area", new Color(0.2f, 0.75f, 1f, 0.08f), new Vector2(areaWidth, areaHeight), new Vector3(maxBounds.x - areaWidth * 0.5f, areaY, 0f), transform, -2);
            leftSweepAreaRenderer = leftArea.GetComponent<SpriteRenderer>();
            rightSweepAreaRenderer = rightArea.GetComponent<SpriteRenderer>();
        }

        private void RefreshSweepAreas()
        {
            if (leftSweepAreaRenderer == null || rightSweepAreaRenderer == null)
            {
                return;
            }

            var leftActive = gripPosition.x <= minBounds.x + sweepStanceAreaWidth;
            var rightActive = gripPosition.x >= maxBounds.x - sweepStanceAreaWidth;
            leftSweepAreaRenderer.color = new Color(0.2f, 0.75f, 1f, leftActive ? 0.22f : 0.08f);
            rightSweepAreaRenderer.color = new Color(0.2f, 0.75f, 1f, rightActive ? 0.22f : 0.08f);
        }

        private void CreateSweepEffect()
        {
            sweepEffectRoot = new GameObject("Sweep Effect").transform;
            sweepEffectRoot.SetParent(transform);
            sweepEffectRenderers = new SpriteRenderer[7];

            for (var i = 0; i < sweepEffectRenderers.Length; i++)
            {
                var segment = VisualFactory.SpriteObject("Sweep Arc", new Color(0.45f, 0.9f, 1f, 0f), new Vector2(0.82f, 0.08f), gripPosition, sweepEffectRoot, 12);
                sweepEffectRenderers[i] = segment.GetComponent<SpriteRenderer>();
            }

            sweepGuideRoot = new GameObject("Sweep Guide").transform;
            sweepGuideRoot.SetParent(transform);
            sweepGuideRenderers = new SpriteRenderer[9];

            for (var i = 0; i < sweepGuideRenderers.Length; i++)
            {
                var segment = VisualFactory.SpriteObject("Sweep Guide Segment", new Color(0.35f, 0.7f, 1f, 0.12f), new Vector2(0.42f, 0.035f), gripPosition, sweepGuideRoot, 6);
                sweepGuideRenderers[i] = segment.GetComponent<SpriteRenderer>();
            }
        }

        private void RefreshSweepEffect()
        {
            if (sweepEffectRenderers == null)
            {
                return;
            }

            var intensity = Mathf.SmoothStep(0f, 1f, sweepCharge);
            var facingRight = sweepDirection >= 0f;
            var centerAngle = facingRight ? 24f : 156f;
            var arcWidth = Mathf.Lerp(18f, 48f, intensity);
            var radius = Mathf.Lerp(bladeLength * 0.45f, bladeLength * 0.9f, intensity);

            sweepEffectRoot.position = gripPosition;
            for (var i = 0; i < sweepEffectRenderers.Length; i++)
            {
                var t = sweepEffectRenderers.Length == 1 ? 0.5f : i / (float)(sweepEffectRenderers.Length - 1);
                var arcAngle = centerAngle + Mathf.Lerp(-arcWidth, arcWidth, t);
                var direction = new Vector2(Mathf.Cos(arcAngle * Mathf.Deg2Rad), Mathf.Sin(arcAngle * Mathf.Deg2Rad));
                var renderer = sweepEffectRenderers[i];
                var mirroredT = facingRight ? t : 1f - t;
                renderer.transform.position = gripPosition + direction * radius + Vector2.up * 0.25f;
                renderer.transform.rotation = Quaternion.Euler(0f, 0f, facingRight ? 8f : 172f);
                renderer.transform.localScale = new Vector3(Mathf.Lerp(0.45f, 1.55f, intensity) * (0.7f + mirroredT * 0.65f), Mathf.Lerp(0.025f, 0.1f, intensity), 1f);
                renderer.color = new Color(0.48f, 0.94f, 1f, intensity * Mathf.Lerp(0.12f, 0.56f, mirroredT));
            }
        }

        private void RefreshSweepGuide()
        {
            if (sweepGuideRenderers == null)
            {
                return;
            }

            var guideCenter = new Vector2(0f, sweepGuideY);
            var guideWidth = sweepGuideHalfWidth;
            var pulse = 0.08f + sweepCharge * 0.22f;

            sweepGuideRoot.position = guideCenter;
            for (var i = 0; i < sweepGuideRenderers.Length; i++)
            {
                var t = sweepGuideRenderers.Length == 1 ? 0.5f : i / (float)(sweepGuideRenderers.Length - 1);
                var x = Mathf.Lerp(-guideWidth, guideWidth, t);
                var y = Mathf.Sin(t * Mathf.PI) * sweepGuideLift;
                var renderer = sweepGuideRenderers[i];
                renderer.transform.position = guideCenter + new Vector2(x, y);
                renderer.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(18f, -18f, t));
                renderer.transform.localScale = new Vector3(0.42f + pulse, 0.035f + pulse * 0.18f, 1f);
                renderer.color = new Color(0.35f, 0.75f, 1f, 0.1f + sweepCharge * 0.24f);
            }
        }

    }

    public readonly struct SwordHit
    {
        public readonly Vector2 ClosestPoint;
        public readonly float Distance;
        public readonly float Leverage;
        public readonly Vector2 PointVelocity;

        public SwordHit(Vector2 closestPoint, float distance, float leverage, Vector2 pointVelocity)
        {
            ClosestPoint = closestPoint;
            Distance = distance;
            Leverage = leverage;
            PointVelocity = pointVelocity;
        }
    }
}
