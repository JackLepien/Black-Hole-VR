using Meta.XR;
using UnityEngine;

namespace PassthroughLensing
{
    /// <summary>
    /// Publishes the Quest passthrough camera image and its projection data as shader globals,
    /// so a shader can sample the real-world image along any world-space ray.
    /// Add next to a PassthroughCameraAccess component (Meta MR Utility Kit) and assign it.
    /// Globals are read by PassthroughCamera.hlsl.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public class PassthroughLensDriver : MonoBehaviour
    {
        [SerializeField] private PassthroughCameraAccess cameraAccess;

        private static readonly int TextureId = Shader.PropertyToID("_PCA_Texture");
        private static readonly int PositionId = Shader.PropertyToID("_PCA_Position");
        private static readonly int WorldToCameraId = Shader.PropertyToID("_PCA_WorldToCamera");
        private static readonly int IntrinsicsId = Shader.PropertyToID("_PCA_Intrinsics");
        private static readonly int CropId = Shader.PropertyToID("_PCA_Crop");
        private static readonly int ValidId = Shader.PropertyToID("_PCA_Valid");

        private void Reset()
        {
            cameraAccess = GetComponent<PassthroughCameraAccess>();
        }

        private void OnDisable()
        {
            Shader.SetGlobalFloat(ValidId, 0f);
        }

        private void LateUpdate()
        {
            if (cameraAccess == null || !cameraAccess.enabled || !cameraAccess.IsPlaying)
            {
                Shader.SetGlobalFloat(ValidId, 0f);
                return;
            }

            var texture = cameraAccess.GetTexture();
            if (texture == null)
            {
                Shader.SetGlobalFloat(ValidId, 0f);
                return;
            }

            // Pose of the camera at the moment the image was captured (not the current head pose).
            Pose pose = cameraAccess.GetCameraPose();
            var intrinsics = cameraAccess.Intrinsics;

            // Same crop logic as PassthroughCameraAccess.CalcSensorCropRegion (private there).
            Vector2 sensor = intrinsics.SensorResolution;
            Vector2 current = cameraAccess.CurrentResolution;
            Vector2 scale = current / sensor;
            scale /= Mathf.Max(scale.x, scale.y);
            var crop = new Vector4(
                sensor.x * (1f - scale.x) * 0.5f,
                sensor.y * (1f - scale.y) * 0.5f,
                sensor.x * scale.x,
                sensor.y * scale.y);

            Shader.SetGlobalTexture(TextureId, texture);
            Shader.SetGlobalVector(PositionId, pose.position);
            Shader.SetGlobalMatrix(WorldToCameraId, Matrix4x4.Rotate(Quaternion.Inverse(pose.rotation)));
            Shader.SetGlobalVector(IntrinsicsId, new Vector4(
                intrinsics.FocalLength.x, intrinsics.FocalLength.y,
                intrinsics.PrincipalPoint.x, intrinsics.PrincipalPoint.y));
            Shader.SetGlobalVector(CropId, crop);
            Shader.SetGlobalFloat(ValidId, 1f);
        }
    }
}
