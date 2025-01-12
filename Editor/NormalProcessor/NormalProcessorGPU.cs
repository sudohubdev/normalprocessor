using System;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;

namespace dev.sudohub.normalprocessor
{

    public class NormalProcessorGPU : System.IDisposable
    {
        private readonly ComputeShader computeShader;
        //pipeline textures
        public Texture2D InputTexture { get; private set; }
        public RenderTexture TempTexture { get; private set; }
        public RenderTexture OutputTexture { get; private set; }

        //curve LUT
        private static readonly int resolution = 256;
        private Texture2D curveLUT = new(resolution, 1, TextureFormat.RFloat, false, true);

        public NormalProcessorGPU(Texture2D tex)
        {
            computeShader = AssetDatabase.LoadAssetAtPath<ComputeShader>("Packages/dev.sudohub.normalprocessor/Editor Resources/Shaders/NormalMapComputeShader.compute");
            InputTexture = tex;
            GenTextures();
        }

        public void RebindTexture(Texture2D tex)
        {
            //check dimensions
            if (InputTexture.width != tex.width || InputTexture.height != tex.height)
            {
                InputTexture = tex;
                Dispose();
                GenTextures();
            }
            else
            {
                InputTexture = tex;
            }
        }

        private void GenTextures()
        {
            TempTexture = new(InputTexture.width, InputTexture.height, 0, RenderTextureFormat.RFloat)
            {
                enableRandomWrite = true
            };
            TempTexture.Create();


            OutputTexture = new(InputTexture.width, InputTexture.height, 0, RenderTextureFormat.ARGB32)
            {
                enableRandomWrite = true
            };
            OutputTexture.Create();
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
            int gaussian = computeShader.FindKernel("GaussianBlur");

            // Set shader parameters
            computeShader.SetInt("_Width", InputTexture.width);
            computeShader.SetInt("_Height", InputTexture.height);
            computeShader.SetFloat("_Smoothness", smoothness);

            // Execute the compute shader
            int threadGroupsX = Mathf.CeilToInt(InputTexture.width / 8.0f);
            int threadGroupsY = Mathf.CeilToInt(InputTexture.height / 8.0f);

            computeShader.SetTexture(gaussian, "InputTexture", InputTexture);
            computeShader.SetTexture(gaussian, "TempTexture", TempTexture);
            computeShader.SetTexture(gaussian, "OutputTexture", OutputTexture);
            computeShader.SetTexture(gaussian, "CurveLUTTexture", curveLUT);
            computeShader.Dispatch(gaussian, threadGroupsX, threadGroupsY, 1);
            //Debug.Log("Gaussian blur applied");
        }

        public void ComputeNormal(float intensity)
        {
            int sobel = computeShader.FindKernel("NormalKernel");
            computeShader.SetInt("_Width", InputTexture.width);
            computeShader.SetInt("_Height", InputTexture.height);
            computeShader.SetFloat("_Intensity", intensity);

            // Execute the compute shader
            int threadGroupsX = Mathf.CeilToInt(InputTexture.width / 8.0f);
            int threadGroupsY = Mathf.CeilToInt(InputTexture.height / 8.0f);

            computeShader.SetTexture(sobel, "InputTexture", InputTexture);
            computeShader.SetTexture(sobel, "TempTexture", TempTexture);
            computeShader.SetTexture(sobel, "OutputTexture", OutputTexture);
            computeShader.SetTexture(sobel, "CurveLUTTexture", curveLUT);
            computeShader.Dispatch(sobel, threadGroupsX, threadGroupsY, 1);
            //Debug.Log("Normal computation applied");
        }

        public Texture2D GetTexture()
        {
            // Convert the output texture to Texture2D
            Texture2D result = new(InputTexture.width, InputTexture.height, TextureFormat.RGB24, false);
            RenderTexture.active = OutputTexture;
            result.ReadPixels(new Rect(0, 0, OutputTexture.width, OutputTexture.height), 0, 0);
            result.Apply();

            RenderTexture.active = null;
            return result;
        }

        public void Dispose()
        {
            if (TempTexture != null)
            {
                TempTexture.Release();
            }
            if (OutputTexture != null)
            {
                OutputTexture.Release();
            }
        }
    }
}