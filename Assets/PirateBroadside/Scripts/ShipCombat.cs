using System.Collections;
using UnityEngine;

namespace PirateBroadside
{
    public enum ShipTeam
    {
        Player,
        Pirate
    }

    public abstract class ShipBase : MonoBehaviour
    {
        private const float BroadsideCooldown = 4.2f;

        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float maxSpeed = 12f;
        [SerializeField] private float acceleration = 5.5f;
        [SerializeField] private float turnPower = 8f;

        protected Rigidbody Body { get; private set; }
        protected bool Sunk { get; private set; }

        private float health;
        private float throttle;
        private float steering;
        private float portReadyAt;
        private float starboardReadyAt;
        private float bobPhase;
        private Renderer[] shipRenderers;
        private Color[] baseColors;

        public ShipTeam Team { get; private set; }
        public float HealthNormalized => Mathf.Clamp01(health / maxHealth);
        public float SpeedKnots => Body == null ? 0f : Mathf.Abs(Vector3.Dot(Body.linearVelocity, transform.forward)) * 1.75f;
        public bool PortReady => Time.time >= portReadyAt && !Sunk;
        public bool StarboardReady => Time.time >= starboardReadyAt && !Sunk;
        public float PortReloadRemaining => Mathf.Max(0f, portReadyAt - Time.time);
        public float StarboardReloadRemaining => Mathf.Max(0f, starboardReadyAt - Time.time);

        public void Initialize(ShipTeam team, bool player)
        {
            Team = team;
            maxHealth = player ? 140f : 75f;
            maxSpeed = player ? 13.5f : 10.5f;
            turnPower = player ? 9.5f : 7.2f;
            health = maxHealth;
            bobPhase = Random.Range(0f, Mathf.PI * 2f);

            Body = GetComponent<Rigidbody>();
            Body.mass = player ? 1800f : 1450f;
            Body.linearDamping = 0.55f;
            Body.angularDamping = 2.4f;
            Body.centerOfMass = new Vector3(0f, -0.8f, 0f);
            Body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            Body.collisionDetectionMode = CollisionDetectionMode.Continuous;

            shipRenderers = GetComponentsInChildren<Renderer>();
            baseColors = new Color[shipRenderers.Length];
            for (var i = 0; i < shipRenderers.Length; i++)
            {
                baseColors[i] = shipRenderers[i].material.color;
            }
        }

        protected void SetHelm(float newThrottle, float newSteering)
        {
            throttle = Mathf.Clamp(newThrottle, -0.35f, 1f);
            steering = Mathf.Clamp(newSteering, -1f, 1f);
        }

        protected virtual void FixedUpdate()
        {
            if (Body == null || Sunk)
            {
                return;
            }

            var forwardSpeed = Vector3.Dot(Body.linearVelocity, transform.forward);
            var desiredSpeed = throttle * maxSpeed;
            Body.AddForce(transform.forward * ((desiredSpeed - forwardSpeed) * acceleration), ForceMode.Acceleration);

            var lateralSpeed = Vector3.Dot(Body.linearVelocity, transform.right);
            Body.AddForce(-transform.right * lateralSpeed * 2.2f, ForceMode.Acceleration);

            var steerAuthority = Mathf.Lerp(0.35f, 1f, Mathf.Clamp01(Mathf.Abs(forwardSpeed) / 7f));
            Body.AddTorque(Vector3.up * steering * turnPower * steerAuthority, ForceMode.Acceleration);

            var waveHeight = -0.05f
                + Mathf.Sin(Time.time * 1.15f + bobPhase + transform.position.x * 0.045f) * 0.16f
                + Mathf.Sin(Time.time * 0.82f + transform.position.z * 0.038f) * 0.12f;
            var lift = (waveHeight - transform.position.y) * 8f - Body.linearVelocity.y * 3.8f;
            Body.AddForce(Vector3.up * lift, ForceMode.Acceleration);

            if (transform.position.sqrMagnitude > 280f * 280f)
            {
                var home = new Vector3(-transform.position.x, 0f, -transform.position.z).normalized;
                Body.AddForce(home * 12f, ForceMode.Acceleration);
            }
        }

        protected bool FireBroadside(int side)
        {
            if (Sunk)
            {
                return false;
            }

            var readyAt = side < 0 ? portReadyAt : starboardReadyAt;
            if (Time.time < readyAt)
            {
                return false;
            }

            if (side < 0)
            {
                portReadyAt = Time.time + BroadsideCooldown;
            }
            else
            {
                starboardReadyAt = Time.time + BroadsideCooldown;
            }
            var sideVector = side < 0 ? -transform.right : transform.right;
            for (var i = 0; i < 4; i++)
            {
                var longitudinal = -2.7f + i * 1.8f;
                var muzzle = transform.position
                    + transform.forward * longitudinal
                    + sideVector * 3.15f
                    + Vector3.up * 0.85f;
                var spread = Random.Range(-2.5f, 2.5f);
                var direction = Quaternion.AngleAxis(spread, Vector3.up) * sideVector;
                Cannonball.Launch(muzzle, direction, Body.linearVelocity, Team);
                Effects.MuzzleFlash(muzzle, direction);
            }

            Effects.CannonSound(transform.position);
            Body.AddForceAtPosition(-sideVector * 120f, transform.position + Vector3.up, ForceMode.Impulse);
            if (this is PlayerShip && CameraRig.Instance != null)
            {
                CameraRig.Instance.Shake(0.22f);
            }

            return true;
        }

        public void TakeDamage(float amount, Vector3 hitPoint)
        {
            if (Sunk)
            {
                return;
            }

            health -= amount;
            Effects.HitBurst(hitPoint);
            StartCoroutine(HitFlash());
            if (health <= 0f)
            {
                Sink();
            }
        }

        private IEnumerator HitFlash()
        {
            for (var i = 0; i < shipRenderers.Length; i++)
            {
                if (shipRenderers[i] != null)
                {
                    shipRenderers[i].material.color = Color.Lerp(baseColors[i], Color.white, 0.72f);
                }
            }

            yield return new WaitForSeconds(0.10f);
            for (var i = 0; i < shipRenderers.Length; i++)
            {
                if (shipRenderers[i] != null)
                {
                    shipRenderers[i].material.color = baseColors[i];
                }
            }
        }

        private void Sink()
        {
            Sunk = true;
            health = 0f;
            Body.constraints = RigidbodyConstraints.None;
            Body.linearDamping = 1.4f;
            Body.angularDamping = 0.7f;
            Body.AddTorque(transform.forward * Random.Range(2200f, 3600f), ForceMode.Impulse);
            Body.AddForce(Vector3.down * 850f, ForceMode.Impulse);
            Effects.SinkingBurst(transform.position);
            OnSunk();
            Destroy(gameObject, 6f);
        }

        protected abstract void OnSunk();

        private void OnCollisionEnter(Collision collision)
        {
            if (!Sunk && collision.relativeVelocity.magnitude > 8f
                && collision.gameObject.GetComponentInParent<Obstacle>() != null)
            {
                TakeDamage(collision.relativeVelocity.magnitude * 1.4f, collision.GetContact(0).point);
            }
        }
    }

    public sealed class PlayerShip : ShipBase
    {
        private float throttle;

        private void Update()
        {
            if (Sunk || PirateGame.Instance == null || PirateGame.Instance.State != BattleState.Playing)
            {
                SetHelm(0f, 0f);
                return;
            }

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            {
                throttle = Mathf.MoveTowards(throttle, 1f, Time.deltaTime * 0.8f);
            }
            else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            {
                throttle = Mathf.MoveTowards(throttle, -0.25f, Time.deltaTime * 0.9f);
            }
            else
            {
                throttle = Mathf.MoveTowards(throttle, 0.42f, Time.deltaTime * 0.35f);
            }

            var steering = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                steering -= 1f;
            }
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                steering += 1f;
            }

            SetHelm(throttle, steering);

            if (Input.GetKeyDown(KeyCode.Q) || Input.GetMouseButtonDown(0))
            {
                FireBroadside(-1);
            }
            if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(1))
            {
                FireBroadside(1);
            }
        }

        protected override void OnSunk()
        {
            PirateGame.Instance?.NotifyPlayerSunk();
        }
    }

    public sealed class EnemyShip : ShipBase
    {
        private PlayerShip target;
        private float orbitDirection;
        private float thinkAt;
        private float aggression;

        public void SetTarget(PlayerShip player)
        {
            target = player;
            orbitDirection = Random.value > 0.5f ? 1f : -1f;
            aggression = Random.Range(0.88f, 1.08f);
        }

        private void Update()
        {
            if (Sunk || target == null || PirateGame.Instance == null || PirateGame.Instance.State != BattleState.Playing)
            {
                SetHelm(0f, 0f);
                return;
            }

            var toTarget = target.transform.position - transform.position;
            toTarget.y = 0f;
            var distance = toTarget.magnitude;
            var radial = toTarget.normalized;
            var tangent = Vector3.Cross(Vector3.up, radial) * orbitDirection;
            var rangeCorrection = Mathf.Clamp((distance - 58f) / 24f, -1f, 1f);
            var desiredDirection = (tangent + radial * rangeCorrection * 0.85f).normalized;
            var angle = Vector3.SignedAngle(transform.forward, desiredDirection, Vector3.up);
            SetHelm(distance > 22f ? aggression : 0.35f, Mathf.Clamp(angle / 35f, -1f, 1f));

            if (Time.time >= thinkAt && distance < 82f)
            {
                thinkAt = Time.time + Random.Range(0.22f, 0.38f);
                var rightAlignment = Vector3.Dot(transform.right, radial);
                var forwardAlignment = Mathf.Abs(Vector3.Dot(transform.forward, radial));
                if (forwardAlignment < 0.48f && Mathf.Abs(rightAlignment) > 0.72f)
                {
                    FireBroadside(rightAlignment > 0f ? 1 : -1);
                }
            }
        }

        protected override void OnSunk()
        {
            PirateGame.Instance?.NotifyEnemySunk(this);
        }
    }

    public sealed class Cannonball : MonoBehaviour
    {
        private ShipTeam team;
        private float bornAt;

        public static void Launch(Vector3 position, Vector3 direction, Vector3 inheritedVelocity, ShipTeam ownerTeam)
        {
            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "Cannonball";
            ball.transform.position = position;
            ball.transform.localScale = Vector3.one * 0.32f;
            ball.GetComponent<Renderer>().material = WorldMaterials.Cannonball;

            var body = ball.AddComponent<Rigidbody>();
            body.mass = 8f;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.linearVelocity = direction * 31f + inheritedVelocity + Vector3.up * 3.2f;
            body.useGravity = true;

            var cannonball = ball.AddComponent<Cannonball>();
            cannonball.team = ownerTeam;
            cannonball.bornAt = Time.time;
            Destroy(ball, 7f);
        }

        private void Update()
        {
            if (transform.position.y < -2.2f)
            {
                Effects.WaterSplash(transform.position);
                Destroy(gameObject);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (Time.time - bornAt < 0.08f)
            {
                return;
            }

            var ship = collision.gameObject.GetComponentInParent<ShipBase>();
            if (ship != null && ship.Team != team)
            {
                ship.TakeDamage(20f, collision.GetContact(0).point);
            }
            else
            {
                Effects.HitBurst(collision.GetContact(0).point);
            }

            Destroy(gameObject);
        }
    }
}
