using UnityEditor;
using UnityEngine;
using System;

namespace dev.sudohub.normalprocessor
{
    //detect changes to recompute
    [Flags]
    public enum Changes
    {
        None = 0,
        KeywordChanged = 1,
        LUTChanged = 2,
        GaussChanged = 4,
        NormalChanged = 8,
        Everything = KeywordChanged | LUTChanged | GaussChanged | NormalChanged
    }
    
    public class NormalProcessorWindow : EditorWindow, IDisposable
    {
        private float smoothness = 2;
        private float intensity = 2;
        private bool doTiling = false;
        private bool useScharr = false;
        private AnimationCurve bwCurve = AnimationCurve.Linear(0, 0, 1, 1);

        private Texture2D inputTexture;
        private int previewLayer = 2;
        private readonly string[] previewLayerTxt = new string[] {"Input", "Gauss", "Normal"};

        private Changes changes = Changes.Everything;
        private NormalProcessorGPU processor;

        [MenuItem("Assets/Dark/Normal Processor")]
        public static void ShowWindow()
        {
            GetWindow<NormalProcessorWindow>("Normal Processor");
        }
        public void OnEnable(){
            if(Selection.activeObject is Texture2D tex)
            {
                inputTexture = tex;
            }
        }

        private void OnGUI()
        {   
            EditorGUI.BeginChangeCheck();
            inputTexture = (Texture2D)EditorGUILayout.ObjectField("Input Texture2D:", inputTexture, typeof(Texture2D), false, GUILayout.ExpandWidth(false));
            if (EditorGUI.EndChangeCheck())
            {
                //rebind new texture to processor
                processor.RebindTexture(inputTexture);
                changes |= Changes.Everything;
            }

            if (inputTexture == null)
            {
                EditorGUILayout.HelpBox("Please assign a Texture2D first.", MessageType.Info);
                return;
            }

            //LUT color curve
            EditorGUI.BeginChangeCheck();
            bwCurve = EditorGUILayout.CurveField("Grayscale Curve", bwCurve);
            if (EditorGUI.EndChangeCheck())
            {
                changes |= Changes.LUTChanged;
            }

            //Smoothness slider for gaussian blur
            EditorGUI.BeginChangeCheck();
            smoothness = EditorGUILayout.Slider("Smoothness", smoothness, 0, 10);
            if (EditorGUI.EndChangeCheck())
            {
                changes |= Changes.GaussChanged;
            }

            //Intensity slider for normal map generation
            EditorGUI.BeginChangeCheck();
            intensity = EditorGUILayout.Slider("Intensity", intensity, 0, 10);
            if (EditorGUI.EndChangeCheck())
            {
                changes |= Changes.NormalChanged;
            }

            //tiling checkbox
            EditorGUI.BeginChangeCheck();
            doTiling = EditorGUILayout.Toggle("Do Tiling", doTiling);
            if (EditorGUI.EndChangeCheck())
            {
                changes |= Changes.KeywordChanged;
            }

            //Scharr operator checkbox
            EditorGUI.BeginChangeCheck();
            useScharr = EditorGUILayout.Toggle("Use Scharr Operator", useScharr);
            if (EditorGUI.EndChangeCheck())
            {
                changes |= Changes.KeywordChanged;
            }

            //Preview
            GUILayout.Label("Preview", EditorStyles.boldLabel);
            previewLayer = EditorGUILayout.Popup("View", previewLayer, previewLayerTxt);

            GUI.DrawTexture(GUILayoutUtility.GetRect(128,2048,128,2048), GetPreview(), ScaleMode.ScaleToFit);
            
            //Save button
            if (GUILayout.Button("Save"))
            {
                Texture2D normalMap = processor.GetTexture();

                // Original asset path
                string assetPath = AssetDatabase.GetAssetPath(inputTexture);

                // Save the normal map
                var path = EditorUtility.SaveFilePanel(
                    "Save Normal Map PNG",
                    System.IO.Path.GetDirectoryName(assetPath),
                    System.IO.Path.GetFileName(assetPath).Replace(".png", "_Normal.png"),
                    "png");

                if (path.Length != 0)
                {
                    System.IO.File.WriteAllBytes(path, normalMap.EncodeToPNG());
                    AssetDatabase.Refresh();
                }
            }

        }
        private Texture GetPreview(){
            processor ??= new NormalProcessorGPU(inputTexture);

            //Recompute changed values
            if(changes.HasFlag(Changes.KeywordChanged)){
                processor.UpdateKeywords(doTiling, useScharr);
                //cascade the change to everything else.
                changes = Changes.Everything;
            }
            if(changes.HasFlag(Changes.LUTChanged)){
                processor.ComputeLUT(bwCurve);
                changes = Changes.Everything;
            }
            if(changes.HasFlag(Changes.GaussChanged)){
                processor.ComputeGauss(smoothness);
                changes = Changes.Everything;
            }
            if(changes.HasFlag(Changes.NormalChanged)){
                processor.ComputeNormal(intensity);
            }
            //Up to date
            changes = Changes.None;

            //return the preview texture.
            return previewLayer switch {
                1 => processor.TempTexture,
                2 => processor.OutputTexture,
                _ => processor.InputTexture
            };
        }

        public void Dispose()
        {
            processor?.Dispose();
        }
    }
}