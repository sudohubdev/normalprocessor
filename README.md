# Normal Map Creator
![Screenshot 2025-01-12 190801](https://github.com/user-attachments/assets/75c65dc7-e319-4451-b1ab-5d2616f3698b)

A Unity Editor package for generating and processing normal maps from 2D textures, powered by GPU compute shaders. Works with both individual textures and full texture atlases.

## Features

### Generation
- **GPU Compute Pipeline** — Efficient compute shader–based generation with separated O(n) Gaussian blur passes.
- **Color Curve LUT** — Non-destructive grayscale curve adjustment on the input texture before generation.
- **Smoothness Control** — Gaussian filter strength (0 bypasses the filter entirely).
- **Intensity & Fine Detail** — Independent intensity slider plus a micro-detail pass that blends unblurred high-frequency data back in.
- **Invert Height** — Flip bump polarity to swap bumps and dents.
- **Operator Selection** — Toggle between Sobel and Scharr edge-detection operators.
- **Seamless Tiling** — Wrap-aware generation for tileable textures.

### Processing Existing Normal Maps
- **Format Conversion** — Load an existing normal map and re-process it (flip green channel for DirectX ↔ OpenGL, rebuild missing blue/Z channel).

### Atlas Processor
- **Texture Atlas Support** — Process individual tiles of a grid atlas independently with per-tile offset dispatching.
- **Lock Params mode** — Apply the same settings across every tile in a single click.

### 2D Lighting Preview
- **Interactive Preview** — Real-time Blinn-Phong light preview rendered entirely on the GPU.
- **Drag & Drop Lights** — Left-click drag to move lights, Right-click to spawn (up to 8), Middle-click to delete.
- **Light Controls** — Scroll wheel to adjust radius/height; Shift/Ctrl + Scroll to adjust intensity.
- **Gizmo Overlay** — Wire-disc gizmos with shortcut tooltip overlay (toggle with the "Toggle Shortcuts" button).
- **Pan & Zoom** — Middle-click drag to pan; Alt + Scroll to zoom in/out the preview.
- **Export Lit Preview** — Save the current lit preview directly as a `_Lit.png`.

### UI & Theming
- **Modern UI Toolkit** — Fully rebuilt with `UIElements` / UI Toolkit for a clean, scalable layout.
- **Glassmorphism Theme System** — Built-in Dark Glass and Nord Arctic themes, editable via the Theme Editor.
- **Preset Manager** — Save and load generation parameter presets across sessions.
- **Theme Editor Window** — Customize colors, glass opacity, and border radii live.

## Installation

Install via Unity Package Manager using the Git URL:

```
https://github.com/sudohubdev/normalprocessor.git
```

See [Unity Documentation](https://docs.unity3d.com/Manual/cus-share.html) for how to add a package from a Git URL.

## Usage

### Single Texture
1. Right-click a `Texture2D` asset in the Project window and choose `Darkness Team → Normal Processor → Single Texture Processor`, or open it from `Window → Darkness Team → Normal Processor → Single Texture Processor`.
2. Assign the source texture in the **Input Texture** field.
3. Adjust parameters as needed.
4. Click **Generate & Save Normal Map** to export.
5. Optionally switch to the **Lighting** preview layer to interactively preview the result under dynamic lights, then click **Export Lit Preview** to save.

### Atlas Processor
1. Open via `Window → Darkness Team → Normal Processor → Atlas Processor`.
2. Assign the atlas texture and configure the grid size.
3. Click individual tiles in the grid to preview and tune per-tile settings.
4. Enable **Lock Params** to apply and compute all tiles at once.
5. Click **Save Normal Map Atlas** to export the full atlas.

## Parameters

| Parameter | Description |
|-----------|-------------|
| **Input Texture** | Source `Texture2D` to convert. |
| **Grayscale Curve** | Adjusts tonal response before generation. Logarithmic curve can help bring out detail. |
| **Smoothness** | Gaussian blur radius. `0` bypasses blurring. |
| **Intensity** | Strength of the surface gradient → normal conversion. |
| **Fine Detail** | Blends high-frequency (unblurred) detail back into the normal map. |
| **Invert Height** | Flips perceived depth (bumps ↔ dents). |
| **Seamless Tiling** | Wrap-aware padding to produce tileable normal maps. |
| **Use Scharr Operator** | Scharr often preserves finer detail than Sobel. Try both. |
| **Process Existing Normal Map** | Re-processes an existing normal map instead of generating from scratch. |
| **Flip Green Channel (Y)** | Converts between DirectX (Y-down) and OpenGL (Y-up) formats. |
| **Rebuild Blue Channel (Z)** | Reconstructs the Z component from X and Y for maps with a missing blue channel. |

## TODOs
- ~~Add support for texture atlases~~
- Add support for Sprite texture atlases with non-uniform tile sizes and offsets
- Add support for `Texture2DArray`
- Smoothness / AO map derivation
- URP / HDRP channel packing utilities

## Licensing

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.
