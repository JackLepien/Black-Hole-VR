using UnityEngine;

namespace BlackHoleMotion
{
    /// <summary>
    /// Makes the black holes orbit the player on different axes while slowly spiralling in,
    /// until they all sit on the player's position.
    /// Put this on the object that tracks the player (CenterOnPlayer). Positions are driven in that
    /// object's local space, so the player tracking keeps working. Runtime only: nothing is saved to the scene.
    /// </summary>
    public class BlackHoleConverge : MonoBehaviour
    {
        [Header("Black holes")]
        [Tooltip("Leave empty to use all direct children of this object.")]
        [SerializeField] private Transform[] holes;
        [Tooltip("Local-space point the holes converge on. (0,0,0) is the tracked player position.")]
        [SerializeField] private Vector3 focusOffset = Vector3.zero;

        [Header("Convergence")]
        [Tooltip("Seconds before the holes start moving in.")]
        [SerializeField] private float startDelay = 5f;
        [Tooltip("Seconds from the start of the approach until they overlap on the player.")]
        [SerializeField] private float convergeDuration = 180f;
        [Tooltip("Distance (metres) the holes end up from the player. 0 = exactly on the player.")]
        [SerializeField] private float finalDistance = 0f;
        [Tooltip("Shape of the approach over time. 0..1 in, 0..1 out (distance covered).")]
        [SerializeField] private AnimationCurve approach = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Orbit")]
        [Tooltip("Orbit speed range in degrees per second; each hole gets its own value and direction.")]
        [SerializeField] private float minOrbitSpeed = 4f;
        [SerializeField] private float maxOrbitSpeed = 14f;
        [Tooltip("Changes which axes and speeds the holes get.")]
        [SerializeField] private int seed = 7;

        private struct State
        {
            public Transform Hole;
            public Vector3 StartDirection;
            public float StartRadius;
            public Vector3 Axis;
            public float Speed;
            public float Angle;
        }

        private State[] _states;
        private float _elapsed;

        /// <summary>True once every hole has reached its final distance from the player.</summary>
        public bool HasConverged { get; private set; }

        private void Start()
        {
            Initialize();
        }

        /// <summary>Restart the approach from the holes' current starting positions.</summary>
        [ContextMenu("Restart")]
        public void Restart()
        {
            if (_states != null)
            {
                foreach (var s in _states)
                {
                    if (s.Hole != null)
                    {
                        s.Hole.localPosition = focusOffset + s.StartDirection * s.StartRadius;
                    }
                }
            }
            Initialize();
        }

        private void Initialize()
        {
            if (holes == null || holes.Length == 0)
            {
                holes = new Transform[transform.childCount];
                for (int i = 0; i < holes.Length; i++)
                {
                    holes[i] = transform.GetChild(i);
                }
            }

            var rng = new System.Random(seed);
            _states = new State[holes.Length];
            for (int i = 0; i < holes.Length; i++)
            {
                var hole = holes[i];
                Vector3 p0 = hole.localPosition - focusOffset;
                float r0 = p0.magnitude;
                Vector3 dir = r0 > 1e-4f ? p0 / r0 : Vector3.forward;

                // Axis perpendicular to the hole's direction, so it travels a circle around the player.
                Vector3 random = new Vector3(Next(rng) * 2f - 1f, Next(rng) * 2f - 1f, Next(rng) * 2f - 1f);
                Vector3 axis = Vector3.Cross(dir, random);
                if (axis.sqrMagnitude < 1e-4f)
                {
                    axis = Vector3.Cross(dir, Vector3.up);
                }
                axis.Normalize();

                float speed = Mathf.Lerp(minOrbitSpeed, maxOrbitSpeed, Next(rng)) * (Next(rng) < 0.5f ? -1f : 1f);

                _states[i] = new State
                {
                    Hole = hole,
                    StartDirection = dir,
                    StartRadius = r0,
                    Axis = axis,
                    Speed = speed,
                    Angle = 0f
                };
            }
            _elapsed = 0f;
            HasConverged = false;
        }

        private void Update()
        {
            if (_states == null)
            {
                return;
            }

            _elapsed += Time.deltaTime;
            float u = convergeDuration > 0f
                ? Mathf.Clamp01((_elapsed - startDelay) / convergeDuration)
                : 1f;
            float k = approach.Evaluate(u);

            for (int i = 0; i < _states.Length; i++)
            {
                ref State s = ref _states[i];
                if (s.Hole == null)
                {
                    continue;
                }

                s.Angle += s.Speed * Time.deltaTime;
                float radius = Mathf.Lerp(s.StartRadius, finalDistance, k);
                Vector3 offset = Quaternion.AngleAxis(s.Angle, s.Axis) * s.StartDirection * radius;
                s.Hole.localPosition = focusOffset + offset;
            }

            // All holes share the same progress value, so they arrive together
            if (u >= 1f)
            {
                HasConverged = true;
            }
        }

        private static float Next(System.Random rng) => (float)rng.NextDouble();
    }
}
