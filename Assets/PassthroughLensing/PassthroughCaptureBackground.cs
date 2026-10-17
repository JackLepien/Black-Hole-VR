using UnityEngine;

namespace PassthroughLensing
{
    /// <summary>
    /// Optional "capture mode". When Capture Mode is ticked before the session starts, a full-screen
    /// quad shows the passthrough camera image behind everything, so headset screenshots and
    /// recordings contain a background. It replaces the live passthrough in your view with the
    /// lower-resolution camera image, so leave it off for normal use.
    /// Requires PassthroughLensDriver in the scene (it publishes the camera data).
    /// </summary>
    public class PassthroughCaptureBackground : MonoBehaviour
    {
        [Tooltip("Tick before entering Play / building. When off, this component does nothing.")]
        [SerializeField] private bool captureMode;

        [Tooltip("Assigned automatically in the editor. Must reference PassthroughLensing/CaptureBackground.")]
        [SerializeField] private Shader shader;

        [Tooltip("Metres. How far away the real-world content is assumed to be. Use the same value as the lens.")]
        [Range(0.5f, 20f)] [SerializeField] private float assumedDistance = 3f;
        [Range(0f, 1f)] [SerializeField] private float opacity = 1f;
        [SerializeField] private bool invertY;
        [SerializeField] private bool flipV;

        private static readonly int DistanceId = Shader.PropertyToID("_Distance");
        private static readonly int OpacityId = Shader.PropertyToID("_Opacity");
        private static readonly int InvertYId = Shader.PropertyToID("_InvertY");
        private static readonly int FlipVId = Shader.PropertyToID("_FlipV");

        private GameObject _quad;
        private Material _material;
        private Mesh _mesh;

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (shader == null)
            {
                shader = Shader.Find("PassthroughLensing/CaptureBackground");
            }
#endif
        }

        private void OnEnable()
        {
            if (!captureMode)
            {
                return;
            }
            if (shader == null)
            {
                Debug.LogError("PassthroughCaptureBackground: assign the 'PassthroughLensing/CaptureBackground' shader.", this);
                return;
            }

            // Three vertices only; the shader positions them from SV_VertexID. Huge bounds stop culling.
            _mesh = new Mesh { name = "PassthroughCaptureTriangle" };
            _mesh.vertices = new[] { Vector3.zero, Vector3.right, Vector3.up };
            _mesh.triangles = new[] { 0, 1, 2 };
            _mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100000f);

            _material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };

            _quad = new GameObject("PassthroughCaptureBackground") { hideFlags = HideFlags.HideAndDontSave };
            _quad.transform.SetParent(transform, false);
            _quad.AddComponent<MeshFilter>().sharedMesh = _mesh;
            var renderer = _quad.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

            ApplySettings();
        }

        private void OnDisable()
        {
            if (_quad != null) Destroy(_quad);
            if (_material != null) Destroy(_material);
            if (_mesh != null) Destroy(_mesh);
            _quad = null;
            _material = null;
            _mesh = null;
        }

        private void Update()
        {
            if (_material != null)
            {
                ApplySettings(); // lets you tweak values live in the Inspector
            }
        }

        private void ApplySettings()
        {
            _material.SetFloat(DistanceId, assumedDistance);
            _material.SetFloat(OpacityId, opacity);
            _material.SetFloat(InvertYId, invertY ? 1f : 0f);
            _material.SetFloat(FlipVId, flipV ? 1f : 0f);
        }
    }
}
