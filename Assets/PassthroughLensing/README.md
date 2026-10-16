# Passthrough Lensing (experimental, self-contained)

Nothing in here modifies existing project assets. Delete this folder to remove it.
A full backup of Assets / ProjectSettings / Packages was taken before this was added:
`../../Black Hole VR_backup_2026-10-16`

## Files
- `PassthroughLensDriver.cs`  publishes the Quest passthrough camera image + projection data as shader globals.
- `PassthroughCamera.hlsl`    `PCA_SceneColor` custom function: a replacement for the Scene Color node.

## Setup (do all of this on DUPLICATES)
1. In the Project window, select `Assets/Scenes/Black Hole.unity` -> Ctrl+D -> rename `Black Hole PCA`. Work only in the copy.
2. Select `Assets/Shaders/Black Hole.shadergraph` -> Ctrl+D -> rename `Black Hole PCA`. Create a new Material using the copy and put it on a duplicate of the black hole object (or the duplicate prefab) in the PCA scene.
3. In the PCA scene: add a GameObject with the `PassthroughCameraAccess` component (Building Blocks -> "Passthrough Camera Access"), then add `PassthroughLensDriver` to the same object and assign the component.
4. Enable the feature (shared project settings, the only existing assets touched):
   - Meta XR project config: tick "Passthrough Camera Access" (OculusProjectConfig; adds the HEADSET_CAMERA permission to the manifest).
   - OVRManager in the PCA scene: tick `requestPassthroughCameraAccessPermissionOnStartup`.
5. In the duplicate graph, replace the Scene Color node (see chat for wiring).
6. Build to Quest 3 (the simulator has no camera feed; the effect simply shows nothing there).

## Tuning (in-headset)
- Image upside-down: toggle `FlipV`. Lens bends the wrong way vertically: toggle `InvertY`.
- Real objects seem to shift near the lens edge: adjust `AssumedDistance` to roughly how far away the room content is.
- Colours too bright/washed out: the camera image may need an sRGB-to-linear conversion on `Color`.
