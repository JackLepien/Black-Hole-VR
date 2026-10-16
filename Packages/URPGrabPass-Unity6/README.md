# URPGrabPass – Unity 6 port

Port of [Haruma-K/URPGrabPass](https://github.com/Haruma-K/URPGrabPass) (MIT) from the legacy
`ScriptableRenderPass.Execute` API to the URP **Render Graph** API, targeting Unity 6000.5.5f1.

## Install
Copy this folder into `Packages/` (embedded package) or add it via *Package Manager > Add package from disk*
(`package.json`). Then add **Grab Pass Renderer Feature** to your URP Renderer asset.
Shaders use the same convention as the original: a pass with `"LightMode" = "UseColorTexture"` sampling
`_GrabbedTexture`.

## Changes from the original
- Passes implement `RecordRenderGraph` instead of `Execute`/`Configure`/`FrameCleanup`.
- `cameraColorTarget`, `Blit`, `GetTemporaryRT` replaced by `UniversalResourceData.activeColorTexture`,
  `Blitter.BlitTexture` and a Render Graph transient texture.
- Objects are drawn via a `RendererList` (`RenderingUtils.CreateDrawingSettings`).
- Compatibility Mode (non-Render Graph) is **not** supported.
- The grab is skipped when the camera renders directly to the back buffer.

## Status
Written against the URP 17 Render Graph API without being compiled in the Unity editor. Verify in
6000.5.5f1; if the `com.unity.render-pipelines.universal` version bundled with that editor differs from the
`package.json` dependency, adjust it.
