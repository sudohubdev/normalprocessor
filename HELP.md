# Normal Processor
![Normal Processor Screenshot](https://github.com/user-attachments/assets/75c65dc7-e319-4451-b1ab-5d2616f3698b)

## Usage
1. Right click the Texture2D asset
2. Choose `Dark/Normal Processor`
3. Alternatively, you can access the Window in `Assets/Dark/Normal Processor`
4. Adjust the parameters as needed
5. At the bottom of the window, click `Save` to create the normal map. You will be prompted to save the generated normal map.

## Parameters
- **Input Texture**: The source 2D texture to convert into a normal map.
- **Grayscale Curve**: Adjusts the curve (Linear by default). Logarithmic curve can be useful in some cases.
- **Smoothness**: Controls the smoothness of the generated normal map using a Gaussian filter. Zero value bypasses the filter.
- **Intensity**: Adjusts the intensity of the normal map effect.
- **Do Tiling**: Sets the seamless tiling for the normal map.
- **Use Scharr Operator**: If enabled, uses the Scharr operator for edge detection; otherwise, uses the Sobel operator. Try both and pick a better one for your needs.
- **View**: Choose the Texture to view (Input/Gauss/Normal). It is a visual settiing and it won't affect Normal Map generation.
### Note
- Use `Dark/Normal Processor (Atlas)` if you want to generate normals for texture atlases.



# Normal Processor (Atlas)
![Normal Processor (Atlas) Screenshot](https://github.com/user-attachments/assets/18277ad1-aaf3-4a90-85e7-ad21a29fbdaf)

# **Usage**
1. **Right-click** a `Texture2D` asset in the Unity Editor.
2. Select **Dark/Normal Processor (Atlas)** from the context menu.
3. Alternatively, open the tool via **Window > Assets/Dark/Normal Processor (Atlas)**.
4. In the editor window:
   - Select your **Texture** (e.g., `rock_albedo.png`).
   - Set **Atlas Size** (e.g., `2048x2048`).
5. **Click tiles** in the Grid Menu to select them for processing.
6. Adjust settings in the **Options** panel (top-right).
7. Preview changes in the **Preview** panel (bottom-right).
8. Click **Save** to export the normal map.
9. Generated `.normproc` presets can be deleted if needed.

---

## **Grid View**
- **Layers**:  
  Choose the texture to view (`Input`, `Gauss`, or `Normal`). This is a visual setting and does not affect normal map generation.
- **Zoom**:  
  Adjust the size of grid icons. This is a visual setting and does not affect normal map generation.

**Warning**: If your tile size is not a power of two (e.g., `256x256`), you may see artifacts like edges or bleeding. Resize your texture to a power-of-two resolution for best results.

---

## **Options**
- **Grayscale Curve**:  
  Adjust the curve for grayscale conversion.
- **Smoothness**:  
  Controls the smoothness of the normal map using a Gaussian filter.  
  - `0`: No smoothing.  
  - Higher values increase blur.  
- **Intensity**:  
  Adjusts the strength of the normal map effect.  
- **Do Tiling**:  
  Enables seamless tiling for the normal map.  
- **Use Scharr Operator**:  
  - `Enabled`: Uses the Scharr operator for sharper edge detection.  
  - `Disabled`: Uses the Sobel operator for faster processing.  

---

## **Format Painter**
Copy settings from one tile to others:
1. Click the **Format Painter** button.
2. Drag over target tiles to apply the settings.

---

## **Lock Options**
- **Enabled**:  
  Shares options across all tiles. Computes the entire grid at once, which may stress your GPU for large textures.  
- **Disabled**:  
  Allows per-tile Options customization.  

---

## **Preview**
- **Layers**:  
  Choose the texture to view (`Input`, `Gauss`, or `Normal`).
- **View Filter**:  
  - `Point`: Crisp, pixel-perfect preview.  
  - `Bilinear`: Smoothed interpolation.  
- **Background Color**:  
  Change the background for better visibility of your texture.  

### These are visual settings and do not affect normal map generation.
---

## **Troubleshooting**
- **Warning: Tile size not power-of-two**:  
  Resize your texture to a power-of-two resolution (e.g., `256x256`, `512x512`).  
- **GPU performance issues**:  
  Reduce the **Atlas Size** or disable **Lock Options**.  
- **Artifacts in normal map**:  
  Adjust **Smoothness** or enable **Do Tiling**.  

---

## **Presets (.normproc files)**
- Presets save your settings, so you don't lose them. 
- They are stored alongside with the source texture.
- Delete them if no longer needed.  
