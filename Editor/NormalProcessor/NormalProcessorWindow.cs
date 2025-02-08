using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

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
        private Preset currentPreset = new("Default Preset");
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
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            inputTexture = (Texture2D)EditorGUILayout.ObjectField("Input Texture2D:", inputTexture, typeof(Texture2D), false, GUILayout.ExpandWidth(false));
            if (EditorGUI.EndChangeCheck())
            {
                //rebind new texture to processor
                processor?.RebindTexture(inputTexture);
                changes |= Changes.Everything;
            }

            //Something like justify-content: space-between XD
            GUILayout.FlexibleSpace();

            //presets menu
            if (EditorGUILayout.DropdownButton(new GUIContent("Load Preset"), FocusType.Passive))
            {
                ShowPresetMenu();
            }

            EditorGUILayout.EndHorizontal();

            //Help box
            if (inputTexture == null)
            {
                EditorGUILayout.HelpBox("Please assign a Texture2D first.", MessageType.Info);
                return;
            }

            //LUT color curve
            EditorGUI.BeginChangeCheck();
            currentPreset.bwCurve = EditorGUILayout.CurveField("Grayscale Curve", currentPreset.bwCurve);
            if (EditorGUI.EndChangeCheck())
            {
                changes |= Changes.LUTChanged;
            }

            //Smoothness slider for gaussian blur
            EditorGUI.BeginChangeCheck();
            currentPreset.smoothness = EditorGUILayout.Slider("Smoothness", currentPreset.smoothness, 0, 10);
            if (EditorGUI.EndChangeCheck())
            {
                changes |= Changes.GaussChanged;
            }

            //Intensity slider for normal map generation
            EditorGUI.BeginChangeCheck();
            currentPreset.intensity = EditorGUILayout.Slider("Intensity", currentPreset.intensity, 0, 10);
            if (EditorGUI.EndChangeCheck())
            {
                changes |= Changes.NormalChanged;
            }

            //tiling checkbox
            EditorGUI.BeginChangeCheck();
            currentPreset.doTiling = EditorGUILayout.Toggle("Do Tiling", currentPreset.doTiling);
            if (EditorGUI.EndChangeCheck())
            {
                changes |= Changes.KeywordChanged;
            }

            //Scharr operator checkbox
            EditorGUI.BeginChangeCheck();
            currentPreset.useScharr = EditorGUILayout.Toggle("Use Scharr Operator", currentPreset.useScharr);
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

                //save preset name
                currentPreset.name = System.IO.Path.GetFileNameWithoutExtension(assetPath);

                // Save the normal map
                var path = EditorUtility.SaveFilePanel(
                    "Save Normal Map PNG",
                    System.IO.Path.GetDirectoryName(assetPath),
                    currentPreset.name + "_Normal.png",
                    "png");

                if (path.Length != 0)
                {
                    System.IO.File.WriteAllBytes(path, normalMap.EncodeToPNG());
                    AssetDatabase.Refresh();
                    //save preset
                    PresetData.instance.Add(currentPreset);
                }
            }

        }
        private Texture GetPreview(){
            processor ??= new NormalProcessorGPU(inputTexture);

            //Recompute changed values
            if(changes.HasFlag(Changes.KeywordChanged)){
                processor.UpdateKeywords(currentPreset.doTiling, currentPreset.useScharr);
                //cascade the change to everything else.
                changes = Changes.Everything;
            }
            if(changes.HasFlag(Changes.LUTChanged)){
                processor.ComputeLUT(currentPreset.bwCurve);
                changes = Changes.Everything;
            }
            if(changes.HasFlag(Changes.GaussChanged)){
                processor.ComputeGauss(currentPreset.smoothness);
                changes = Changes.Everything;
            }
            if(changes.HasFlag(Changes.NormalChanged)){
                processor.ComputeNormal(currentPreset.intensity);
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

        private void ShowPresetMenu()
        {
            // Create the dropdown menu
            GenericMenu menu = new GenericMenu();

            // Add preset options
            foreach(var preset in PresetData.instance){
                menu.AddItem(new GUIContent(preset.name), false, () => LoadPreset(preset));
            }
            menu.AddItem(new GUIContent("-----CLEAR ALL-----"), false, () => PresetData.instance.Clear());

            // Display the menu
            menu.ShowAsContext();
        }

        private void LoadPreset(Preset preset)
        {
            PresetData.instance.Select(preset);
            currentPreset = preset;
            changes |= Changes.Everything;
        }

        public void Dispose()
        {
            processor?.Dispose();
        }
    }
}