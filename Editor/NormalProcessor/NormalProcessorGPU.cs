using System;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;

namespace dev.sudohub.normalprocessor
{

    public class NormalProcessorGPU : System.IDisposable
    {
        private readonly ComputeShader computeShader;
        private readonly ComputeShader lightCompute;
        //pipeline textures
        public Texture2D InputTexture { get; private set; }
        public RenderTexture TempTexture { get; private set; }
        public RenderTexture TempTexture2 { get; private set; }
        public RenderTexture OutputTexture { get; private set; }
        public RenderTexture LitTexture { get; private set; }

        //curve LUT
        private static readonly int resolution = 256;
        private Texture2D curveLUT = new(resolution, 1, TextureFormat.RFloat, false, true);

        //partial texture edit (for tiles)
        private Vector2Int tileSize = Vector2Int.one; //scale factor
        private Vector2Int tileOffset = Vector2Int.zero; //no offsset by default


        public NormalProcessorGPU()
        {

            computeShader = AssetDatabase.LoadAssetAtPath<ComputeShader>("Packages/dev.sudohub.normalprocessor/Editor Resources/Shaders/NormalMapComputeShader.compute");
            lightCompute = AssetDatabase.LoadAssetAtPath<ComputeShader>("Packages/dev.sudohub.normalprocessor/Editor Resources/Shaders/LightPreviewCompute.compute");

            if (computeShader == null || lightCompute == null)
            {
                Debug.LogError("[Normal Processor] Failed to load Compute Shaders. Check the package integrity!");
                return;
            }
        }

        public NormalProcessorGPU(Texture2D tex) : this()
        {
            this.RebindTexture(tex);
        }

        public void RebindTexture(Texture2D tex)
        {
            if (tex == null) return;

            //check dimensions
            if (InputTexture == null || InputTexture.width != tex.width || InputTexture.height != tex.height)
            {
                InputTexture = tex;
                ReleaseRenderTextures();
                GenTextures();
            }
            else
            {
                InputTexture = tex;
            }
        }

        private void ReleaseRenderTextures()
        {
            if (TempTexture != null)
            {
                TempTexture.Release();
                UnityEngine.Object.DestroyImmediate(TempTexture);
                TempTexture = null;
            }
            if (TempTexture2 != null)
            {
                TempTexture2.Release();
                UnityEngine.Object.DestroyImmediate(TempTexture2);
                TempTexture2 = null;
            }
            if (OutputTexture != null)
            {
                OutputTexture.Release();
                UnityEngine.Object.DestroyImmediate(OutputTexture);
                OutputTexture = null;
            }
            if (LitTexture != null)
            {
                LitTexture.Release();
                UnityEngine.Object.DestroyImmediate(LitTexture);
                LitTexture = null;
            }
        }

        private void GenTextures()
        {
            TempTexture = new(InputTexture.width, InputTexture.height, 0, RenderTextureFormat.RFloat)
            {
                enableRandomWrite = true
            };
            TempTexture.Create();

            TempTexture2 = new(InputTexture.width, InputTexture.height, 0, RenderTextureFormat.RFloat)
            {
                enableRandomWrite = true
            };
            TempTexture2.Create();

            OutputTexture = new(InputTexture.width, InputTexture.height, 0, RenderTextureFormat.ARGB32)
            {
                enableRandomWrite = true
            };
            OutputTexture.Create();
            
            LitTexture = new(InputTexture.width, InputTexture.height, 0, RenderTextureFormat.ARGB32)
            {
                enableRandomWrite = true
            };
            LitTexture.Create();
        }

        public void SetTiling(Vector2Int size, Vector2Int offset)
        {
            tileSize = size;
            tileOffset = offset;
        }

        public void UpdateKeywords(bool doTiling, bool useScharr)
        {
            if (doTiling)
                computeShader.EnableKeyword("DO_TILING");
            else
                computeShader.DisableKeyword("DO_TILING");

            if (useScharr)
                computeShader.EnableKeyword("USE_SCHARR");
            else
                computeShader.DisableKeyword("USE_SCHARR");
        }

        internal void ComputeLUT(AnimationCurve curve)
        {
            for (int i = 0; i < resolution; i++)
            {
                float t = i / (float)(resolution - 1); // Normalize to [0, 1]
                float value = curve.Evaluate(t); // Sample the curve
                curveLUT.SetPixel(i, 0, new Color(value, 0, 0, 0));
            }
            curveLUT.Apply();
            //Debug.Log("Generated LUT");
        }

        public void ComputeGauss(float smoothness)
        {
            int gaussHor = computeShader.FindKernel("GaussianBlurHorizontal");
            int gaussVer = computeShader.FindKernel("GaussianBlurVertical");

            // Set shader parameters
            int blockWidth = InputTexture.width / tileSize.x;
            int blockHeight = InputTexture.height / tileSize.y;
            computeShader.SetInt("_Width", blockWidth);
            computeShader.SetInt("_Height", blockHeight);
            computeShader.SetInts("_Offset", blockWidth * tileOffset.x,
                                             blockHeight * (tileSize.y - 1 - tileOffset.y));
            computeShader.SetFloat("_Smoothness", smoothness);

            int threadGroupsX = Mathf.CeilToInt(blockWidth / 8.0f);
            int threadGroupsY = Mathf.CeilToInt(blockHeight / 8.0f);

            // Pass 1: Horizontal Blur
            computeShader.SetTexture(gaussHor, "InputTexture", InputTexture);
            computeShader.SetTexture(gaussHor, "Pass1Texture", TempTexture2);
            computeShader.SetTexture(gaussHor, "CurveLUTTexture", curveLUT);
            computeShader.Dispatch(gaussHor, threadGroupsX, threadGroupsY, 1);

            // Pass 2: Vertical Blur
            computeShader.SetTexture(gaussVer, "Pass1Texture", TempTexture2);
            computeShader.SetTexture(gaussVer, "TempTexture", TempTexture);
            computeShader.Dispatch(gaussVer, threadGroupsX, threadGroupsY, 1);
        }

        public void ComputeNormal(float intensity, float detailIntensity, bool invertHeight)
        {
            int sobel = computeShader.FindKernel("NormalKernel");

            // Set shader parameters
            int blockWidth = InputTexture.width / tileSize.x;
            int blockHeight = InputTexture.height / tileSize.y;
            computeShader.SetInt("_Width", blockWidth);
            computeShader.SetInt("_Height", blockHeight);
            computeShader.SetInts("_Offset", blockWidth * tileOffset.x,
                                             blockHeight * (tileSize.y - 1 - tileOffset.y));
            computeShader.SetFloat("_Intensity", intensity);
            computeShader.SetFloat("_DetailIntensity", detailIntensity);
            computeShader.SetFloat("_InvertHeight", invertHeight ? -1.0f : 1.0f);

            // Execute the compute shader
            int threadGroupsX = Mathf.CeilToInt(blockWidth / 8.0f);
            int threadGroupsY = Mathf.CeilToInt(blockHeight / 8.0f);

            computeShader.SetTexture(sobel, "InputTexture", InputTexture);
            computeShader.SetTexture(sobel, "TempTexture", TempTexture);
            computeShader.SetTexture(sobel, "OutputTexture", OutputTexture);
            computeShader.SetTexture(sobel, "CurveLUTTexture", curveLUT);
            computeShader.Dispatch(sobel, threadGroupsX, threadGroupsY, 1);
            //Debug.Log("Normal computation applied");
        }

        public Texture2D GetTexture(RenderTexture rt = null)
        {
            if (rt == null) rt = OutputTexture;
            // Convert the output texture to Texture2D
            Texture2D result = new(rt.width, rt.height, TextureFormat.RGB24, false);
            RenderTexture.active = rt;
            result.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            result.Apply();

            RenderTexture.active = null;
            return result;
        }

        public void Dispose()
        {
            ReleaseRenderTextures();
            if (curveLUT != null)
            {
                UnityEngine.Object.DestroyImmediate(curveLUT);
                curveLUT = null;
            }
        }
        public void ProcessExistingNormalMap(float intensity, bool flipGreen, bool rebuildZ)
        {
            int kernel = computeShader.FindKernel("ProcessExistingNormal");
            int blockWidth = InputTexture.width / tileSize.x;
            int blockHeight = InputTexture.height / tileSize.y;
            computeShader.SetInt("_Width", blockWidth);
            computeShader.SetInt("_Height", blockHeight);
            computeShader.SetInts("_Offset", blockWidth * tileOffset.x, blockHeight * (tileSize.y - 1 - tileOffset.y));
            computeShader.SetFloat("_Intensity", intensity);
            computeShader.SetFloat("_FlipGreen", flipGreen ? 1.0f : 0.0f);
            computeShader.SetFloat("_RebuildZ", rebuildZ ? 1.0f : 0.0f);
            
            int threadGroupsX = Mathf.CeilToInt(blockWidth / 8.0f);
            int threadGroupsY = Mathf.CeilToInt(blockHeight / 8.0f);
            computeShader.SetTexture(kernel, "InputTexture", InputTexture);
            computeShader.SetTexture(kernel, "OutputTexture", OutputTexture);
            computeShader.Dispatch(kernel, threadGroupsX, threadGroupsY, 1);
        }

        public struct LightData
        {
            public Vector4 position; // x,y (uv), z (height), w (radius)
            public Vector4 color;    // r,g,b, a (intensity)
        }

        public void ComputeLighting(LightData[] lights)
        {
            if (lightCompute == null || LitTexture == null) return;
            
            int kernel = lightCompute.FindKernel("LightingPass");
            int blockWidth = InputTexture.width / tileSize.x;
            int blockHeight = InputTexture.height / tileSize.y;
            lightCompute.SetInt("_Width", blockWidth);
            lightCompute.SetInt("_Height", blockHeight);
            
            lightCompute.SetInt("_LightCount", lights.Length);
            
            Vector4[] posArray = new Vector4[16];
            Vector4[] colArray = new Vector4[16];
            for (int i = 0; i < lights.Length && i < 16; i++) {
                posArray[i] = lights[i].position;
                colArray[i] = lights[i].color;
            }
            lightCompute.SetVectorArray("_LightPositions", posArray);
            lightCompute.SetVectorArray("_LightColors", colArray);
            
            lightCompute.SetFloat("_UseAlbedo", 1.0f); // Default to on, could be dynamic
            
            int threadGroupsX = Mathf.CeilToInt(blockWidth / 8.0f);
            int threadGroupsY = Mathf.CeilToInt(blockHeight / 8.0f);
            lightCompute.SetTexture(kernel, "AlbedoTexture", InputTexture);
            lightCompute.SetTexture(kernel, "NormalTexture", OutputTexture);
            lightCompute.SetTexture(kernel, "OutputTexture", LitTexture);
            lightCompute.Dispatch(kernel, threadGroupsX, threadGroupsY, 1);
        }
    }
}

