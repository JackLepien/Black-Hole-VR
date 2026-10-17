using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace StarInflowEffect
{
    /// <summary>
    /// Many small stars orbiting and spiralling into the black hole, then recycling.
    /// All motion runs in the shader (StarInflow/Stars); this component builds the star mesh,
    /// finds the (runtime-spawned) black hole and feeds the shader its position and size.
    /// Put it on an empty GameObject; rotate that object to tilt the stars' orbital disc
    /// (its Up axis is the disc axis).
    /// </summary>
    public class StarInflow : MonoBehaviour
    {
        [Header("Setup")]
        [Tooltip("Assigned automatically in the editor. Must reference StarInflow/Stars.")]
        [SerializeField] private Shader shader;
        [Tooltip("Optional. The MeshRenderer that uses the Black Hole shader. Left empty, it is found automatically at runtime, so spawned black holes work.")]
        [SerializeField] private Renderer blackHoleRenderer;

        [Header("Horizon size (metres)")]
        [Tooltip("Matches the graph: SpherePercentage x object scale. Turn off to use the override below.")]
        [SerializeField] private bool autoHorizonRadius = true;
        [SerializeField] private float horizonRadiusOverride = 0.5f;
        [Tooltip("Used only if the black hole material has no _SpherePercentage property.")]
        [SerializeField] private float fallbackSpherePercentage = 0.007f;

        [Header("Stars")]
        [Range(50, 16000)] [SerializeField] private int starCount = 1500;
        [Tooltip("Seconds for a star to fall from its start orbit into the hole.")]
        [Range(3f, 90f)] [SerializeField] private float lifetime = 20f;
        [Tooltip("Star size in metres.")]
        [SerializeField] private float starSize = 0.03f;
        [SerializeField] private float brightness = 2f;

        [Header("Orbits (multiples of the horizon radius)")]
        [SerializeField] private float outerRadiusMultiple = 8f;
        [SerializeField] private float innerStartMultiple = 2.5f;
        [Tooltip("0 = stars fill a sphere, 1 = all stars in one flat disc.")]
        [Range(0f, 1f)] [SerializeField] private float flatten = 0.85f;

        [Header("Motion")]
        [Tooltip("Orbits completed during one fall.")]
        [Range(0f, 20f)] [SerializeField] private float orbitTurns = 3f;
        [Tooltip("How much faster stars swirl as they get close.")]
        [Range(0f, 10f)] [SerializeField] private float spinUp = 2f;
        [Tooltip("Higher = slower drift at first, sharper plunge at the end.")]
        [Range(1f, 6f)] [SerializeField] private float infallExponent = 2.5f;

        private static readonly int CenterId = Shader.PropertyToID("_Center");
        private static readonly int AxisId = Shader.PropertyToID("_Axis");
        private static readonly int P0Id = Shader.PropertyToID("_P0");
        private static readonly int P1Id = Shader.PropertyToID("_P1");
        private static readonly int P2Id = Shader.PropertyToID("_P2");
        private static readonly int BrightnessId = Shader.PropertyToID("_Brightness");
        private static readonly int SpherePercentageId = Shader.PropertyToID("_SpherePercentage");

        private GameObject _starObject;
        private MeshRenderer _starRenderer;
        private Material _material;
        private Mesh _mesh;
        private float _nextSearchTime;

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (shader == null)
            {
                shader = Shader.Find("StarInflow/Stars");
            }
#endif
        }

        private void OnEnable()
        {
            if (shader == null)
            {
                Debug.LogError("StarInflow: assign the 'StarInflow/Stars' shader.", this);
                return;
            }

            BuildMesh();
            _material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave, renderQueue = 3001 };

            _starObject = new GameObject("StarInflow Stars") { hideFlags = HideFlags.HideAndDontSave };
            _starObject.transform.SetParent(transform, false);
            _starObject.AddComponent<MeshFilter>().sharedMesh = _mesh;
            _starRenderer = _starObject.AddComponent<MeshRenderer>();
            _starRenderer.sharedMaterial = _material;
            _starRenderer.shadowCastingMode = ShadowCastingMode.Off;
            _starRenderer.receiveShadows = false;
            _starRenderer.lightProbeUsage = LightProbeUsage.Off;
            _starRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            _starRenderer.enabled = false; // until a black hole is found
        }

        private void OnDisable()
        {
            if (_starObject != null) Destroy(_starObject);
            if (_material != null) Destroy(_material);
            if (_mesh != null) Destroy(_mesh);
            _starObject = null;
            _starRenderer = null;
            _material = null;
            _mesh = null;
        }

        private void Update()
        {
            if (_material == null)
            {
                return;
            }

            if (blackHoleRenderer == null && Time.unscaledTime >= _nextSearchTime)
            {
                _nextSearchTime = Time.unscaledTime + 0.5f;
                blackHoleRenderer = FindBlackHole();
            }

            if (blackHoleRenderer == null)
            {
                _starRenderer.enabled = false;
                return;
            }

            _starRenderer.enabled = blackHoleRenderer.enabled && blackHoleRenderer.gameObject.activeInHierarchy;

            float horizon = HorizonRadius();
            Vector3 center = blackHoleRenderer.transform.position;
            Vector3 axis = transform.up;

            _material.SetVector(CenterId, center);
            _material.SetVector(AxisId, axis);
            _material.SetVector(P0Id, new Vector4(
                horizon,
                horizon * outerRadiusMultiple,
                horizon * Mathf.Min(innerStartMultiple, outerRadiusMultiple),
                starSize));
            _material.SetVector(P1Id, new Vector4(lifetime, orbitTurns, spinUp, infallExponent));
            _material.SetVector(P2Id, new Vector4(flatten, 0f, 0f, 0f));
            _material.SetFloat(BrightnessId, brightness);
        }

        private static Renderer FindBlackHole()
        {
            foreach (var r in FindObjectsByType<Renderer>())
            {
                var m = r.sharedMaterial;
                if (m != null && m.HasProperty(SpherePercentageId) && r.gameObject.activeInHierarchy)
                {
                    return r;
                }
            }
            return null;
        }

        private float HorizonRadius()
        {
            if (!autoHorizonRadius)
            {
                return horizonRadiusOverride;
            }
            float percentage = fallbackSpherePercentage;
            var m = blackHoleRenderer.sharedMaterial;
            if (m != null && m.HasProperty(SpherePercentageId))
            {
                percentage = m.GetFloat(SpherePercentageId);
            }
            return percentage * blackHoleRenderer.transform.lossyScale.x;
        }

        private void BuildMesh()
        {
            int n = Mathf.Clamp(starCount, 1, 16000);
            var rng = new System.Random(12345); // fixed seed: same star field every run

            var corners = new Vector3[n * 4];
            var seedA = new List<Vector4>(n * 4);
            var seedB = new List<Vector4>(n * 4);
            var indices = new int[n * 6];
            Vector2[] quad = { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(1, 1), new Vector2(-1, 1) };

            for (int i = 0; i < n; i++)
            {
                var a = new Vector4(Next(rng), Next(rng), Next(rng), Next(rng));
                var b = new Vector4(Next(rng), Next(rng), Next(rng), Next(rng));
                for (int c = 0; c < 4; c++)
                {
                    corners[i * 4 + c] = new Vector3(quad[c].x, quad[c].y, 0f);
                    seedA.Add(a);
                    seedB.Add(b);
                }
                int v = i * 4;
                int t = i * 6;
                indices[t] = v; indices[t + 1] = v + 1; indices[t + 2] = v + 2;
                indices[t + 3] = v; indices[t + 4] = v + 2; indices[t + 5] = v + 3;
            }

            _mesh = new Mesh
            {
                name = "StarInflowMesh",
                indexFormat = n * 4 > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16
            };
            _mesh.vertices = corners;
            _mesh.SetUVs(1, seedA);
            _mesh.SetUVs(2, seedB);
            _mesh.triangles = indices;
            // Positions are computed in the shader, so tell Unity the mesh is huge to avoid culling.
            _mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100000f);
        }

        private static float Next(System.Random rng) => (float)rng.NextDouble();

        private void OnDrawGizmosSelected()
        {
            // Preview with the horizon override (the automatic size needs the black hole at runtime).
            Gizmos.color = Color.black;
            Gizmos.DrawWireSphere(transform.position, horizonRadiusOverride);
            Gizmos.color = new Color(0.6f, 0.8f, 1f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, horizonRadiusOverride * outerRadiusMultiple);
        }
    }
}
