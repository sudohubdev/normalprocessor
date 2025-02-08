# Normal Map Creator
![Screenshot 2025-01-12 190801](https://github.com/user-attachments/assets/75c65dc7-e319-4451-b1ab-5d2616f3698b)

This Unity package allows for the creation of normal maps from 2D textures using compute shaders. The package offers various customization options including Smoothness, Intensity, Tiling, and the choice between Scharr and Sobel operators.

## Features

- **Compute Shaders**: Utilizes compute shaders for efficient and fast normal map generation.
- **Color Curve LUT** Adjust the grayscale curve of the input texture for better results.
- **Smoothness Control**: Adjust the smoothness (Gaussian filter) of the generated normal map.
- **Intensity Control**: Modify the intensity of the normal map effect.
- **Tiling Options**: Set the seamless tiling for the normal map.
- **Operator Selection**: Choose between Scharr and Sobel operators for edge detection.

## Installation

Install a package from Git URL `https://github.com/sudohubdev/normalprocessor.git`

[Unity Documentation](https://docs.unity3d.com/Manual/cus-share.html)

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

## TODOs
- ~~Add support for texture atlases~~
- Add support for Sprite texture atlases with different sizes and offsets
- Add support for Texture2DArray 

## Licensing

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
