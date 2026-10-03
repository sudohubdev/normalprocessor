using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

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
        private int previewLayer = 2; // 0=Input, 1=Gauss, 2=Normal

        private Changes changes = Changes.Everything;
        private NormalProcessorGPU processor;

        private Image previewImage;
        private VisualElement rightPanel;

        [MenuItem("Window/Darkness Team/Normal Processor")]
        [MenuItem("Assets/Darkness Team/Normal Processor")]
        public static void ShowWindow()
        {
            var window = GetWindow<NormalProcessorWindow>("Normal Processor");
            window.minSize = new Vector2(600, 400);
        }

        public void OnEnable(){
            if(Selection.activeObject is Texture2D tex)
            {
                inputTexture = tex;
            }
        }

        public void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.flexDirection = FlexDirection.Column;

            // Main layout using TwoPaneSplitView
            var splitView = new TwoPaneSplitView(0, 320, TwoPaneSplitViewOrientation.Horizontal);
            root.Add(splitView);

            // Left panel for settings
            var leftPanel = new ScrollView(ScrollViewMode.Vertical);
            leftPanel.style.paddingTop = 10;
            leftPanel.style.paddingBottom = 10;
            leftPanel.style.paddingLeft = 10;
            leftPanel.style.paddingRight = 10;
            leftPanel.style.minWidth = 300;

            // Right panel for preview
            rightPanel = new VisualElement();
            rightPanel.style.flexGrow = 1;
            rightPanel.style.backgroundColor = new Color(0.12f, 0.12f, 0.12f, 1f);

            splitView.Add(leftPanel);
            splitView.Add(rightPanel);

            BuildLeftPanel(leftPanel);
            BuildRightPanel(rightPanel);
        }

        private void BuildLeftPanel(VisualElement container)
        {
            // Title Header with Icon
            var headerContainer = new VisualElement() { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 15 } };
            var icon = new Image() { image = EditorGUIUtility.IconContent("d_PreTextureRGB").image, style = { width = 24, height = 24, marginRight = 5 } };
            var header = new Label("Normal Processor") { style = { fontSize = 18, unityFontStyleAndWeight = FontStyle.Bold } };
            headerContainer.Add(icon);
            headerContainer.Add(header);
            container.Add(headerContainer);

            // Input Texture
            var texField = new ObjectField("Input Texture") { objectType = typeof(Texture2D), value = inputTexture };
            texField.RegisterValueChangedCallback(evt => {
                inputTexture = (Texture2D)evt.newValue;
                processor?.RebindTexture(inputTexture);
                changes |= Changes.Everything;
                UpdatePreview();
            });
            container.Add(texField);

            // Presets Button
            var presetBtn = new Button(ShowPresetMenu) { text = "Load Preset", style = { marginTop = 10, marginBottom = 15 } };
            presetBtn.Add(new Image() { image = EditorGUIUtility.IconContent("d_Settings").image, style = { position = Position.Absolute, left = 5, top = 2, width = 16, height = 16 } });
            container.Add(presetBtn);

            // Help Box
            var helpBox = new HelpBox("Please assign a Texture2D first.", HelpBoxMessageType.Info);
            helpBox.style.display = inputTexture == null ? DisplayStyle.Flex : DisplayStyle.None;
            texField.RegisterValueChangedCallback(evt => helpBox.style.display = evt.newValue == null ? DisplayStyle.Flex : DisplayStyle.None);
            container.Add(helpBox);

            // --- Adjustments Group ---
            var adjustmentsGroup = new GroupBox("Adjustments") { style = { marginTop = 10, paddingBottom = 10, borderLeftWidth=1, borderRightWidth=1, borderTopWidth=1, borderBottomWidth=1, borderTopLeftRadius=4, borderTopRightRadius=4, borderBottomLeftRadius=4, borderBottomRightRadius=4 } };
            
            var curveField = new CurveField("Grayscale Curve") { value = currentPreset.bwCurve, tooltip = "Color correction curve to amplify details" };
            curveField.RegisterValueChangedCallback(evt => {
                currentPreset.bwCurve = curveField.value;
                changes |= Changes.LUTChanged;
                UpdatePreview();
            });
            adjustmentsGroup.Add(curveField);

            var smoothnessField = new Slider("Smoothness", 0, 10) { value = currentPreset.smoothness, showInputField = true };
            smoothnessField.RegisterValueChangedCallback(evt => {
                currentPreset.smoothness = evt.newValue;
                changes |= Changes.GaussChanged;
                UpdatePreview();
            });
            adjustmentsGroup.Add(smoothnessField);

            var intensityField = new Slider("Intensity", 0, 10) { value = currentPreset.intensity, showInputField = true };
            intensityField.RegisterValueChangedCallback(evt => {
                currentPreset.intensity = evt.newValue;
                changes |= Changes.NormalChanged;
                UpdatePreview();
            });
            adjustmentsGroup.Add(intensityField);
            container.Add(adjustmentsGroup);

            // --- Options Group ---
            var optionsGroup = new GroupBox("Options") { style = { marginTop = 10, paddingBottom = 10, borderLeftWidth=1, borderRightWidth=1, borderTopWidth=1, borderBottomWidth=1, borderTopLeftRadius=4, borderTopRightRadius=4, borderBottomLeftRadius=4, borderBottomRightRadius=4 } };

            var tilingToggle = new Toggle("Seamless Tiling") { value = currentPreset.doTiling };
            tilingToggle.RegisterValueChangedCallback(evt => {
                currentPreset.doTiling = evt.newValue;
                changes |= Changes.KeywordChanged;
                UpdatePreview();
            });
            optionsGroup.Add(tilingToggle);

            var scharrToggle = new Toggle("Use Scharr Operator") { value = currentPreset.useScharr, tooltip = "Scharr filter often preserves finer details than Sobel" };
            scharrToggle.RegisterValueChangedCallback(evt => {
                currentPreset.useScharr = evt.newValue;
                changes |= Changes.KeywordChanged;
                UpdatePreview();
            });
            optionsGroup.Add(scharrToggle);
            container.Add(optionsGroup);

            // Flexible space
            var spacer = new VisualElement() { style = { flexGrow = 1, minHeight = 20 } };
            container.Add(spacer);

            // Save Button
            var saveBtn = new Button(SaveTexture) { text = "Generate & Save Normal Map", tooltip = "Process and save as PNG" };
            saveBtn.style.height = 36;
            saveBtn.style.backgroundColor = new Color(0.13f, 0.59f, 0.95f, 1f); // Unity Blue Accent
            saveBtn.style.color = Color.white;
            saveBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            saveBtn.style.fontSize = 14;
            saveBtn.style.borderTopLeftRadius = 4;
            saveBtn.style.borderTopRightRadius = 4;
            saveBtn.style.borderBottomLeftRadius = 4;
            saveBtn.style.borderBottomRightRadius = 4;
            saveBtn.Add(new Image() { image = EditorGUIUtility.IconContent("d_SaveAs").image, style = { position = Position.Absolute, left = 10, top = 10, width = 16, height = 16 } });
            container.Add(saveBtn);
        }

        private void BuildRightPanel(VisualElement container)
        {
            // Toolbar
            var toolbar = new UnityEditor.UIElements.Toolbar();
            var layerMenu = new UnityEditor.UIElements.ToolbarMenu { text = "Preview Layer: Normal" };
            layerMenu.menu.AppendAction("Input", _ => { previewLayer = 0; layerMenu.text = "Preview Layer: Input"; UpdatePreview(); });
            layerMenu.menu.AppendAction("Gauss", _ => { previewLayer = 1; layerMenu.text = "Preview Layer: Gauss"; UpdatePreview(); });
            layerMenu.menu.AppendAction("Normal", _ => { previewLayer = 2; layerMenu.text = "Preview Layer: Normal"; UpdatePreview(); });
            toolbar.Add(layerMenu);

            var spacer = new VisualElement() { style = { flexGrow = 1 } };
            toolbar.Add(spacer);

            var refreshBtn = new UnityEditor.UIElements.ToolbarButton(UpdatePreview) { text = "Refresh", tooltip = "Force Re-evaluate" };
            refreshBtn.Add(new Image() { image = EditorGUIUtility.IconContent("d_Refresh").image, style = { position = Position.Absolute, left = 2, top = 2, width = 16, height = 16 } });
            toolbar.Add(refreshBtn);
            
            container.Add(toolbar);

            // Image Preview area
            previewImage = new Image() { style = { flexGrow = 1, marginTop = 10, marginBottom = 10, marginLeft = 10, marginRight = 10 } };
            previewImage.scaleMode = ScaleMode.ScaleToFit;
            container.Add(previewImage);

            if (inputTexture != null)
            {
                UpdatePreview();
            }
        }

        private void UpdatePreview()
        {
            if (inputTexture == null)
            {
                previewImage.image = null;
                return;
            }

            processor ??= new NormalProcessorGPU(inputTexture);

            //Recompute changed values
            if(changes.HasFlag(Changes.KeywordChanged)){
                processor.UpdateKeywords(currentPreset.doTiling, currentPreset.useScharr);
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
            
            changes = Changes.None;

            previewImage.image = previewLayer switch {
                1 => processor.TempTexture,
                2 => processor.OutputTexture,
                _ => processor.InputTexture
            };
        }

        private void ShowPresetMenu()
        {
            GenericMenu menu = new GenericMenu();
            foreach(var preset in PresetData.instance){
                menu.AddItem(new GUIContent(preset.name), false, () => LoadPreset(preset));
            }
            menu.AddItem(new GUIContent("-----CLEAR ALL-----"), false, () => PresetData.instance.Clear());
            menu.ShowAsContext();
        }

        private void LoadPreset(Preset preset)
        {
            PresetData.instance.Select(preset);
            currentPreset = preset;
            changes |= Changes.Everything;
            
            rootVisualElement.Clear();
            CreateGUI();
            UpdatePreview();
        }

        private void SaveTexture()
        {
            if (inputTexture == null || processor == null) return;

            Texture2D normalMap = processor.GetTexture();
            string assetPath = AssetDatabase.GetAssetPath(inputTexture);
            currentPreset.name = System.IO.Path.GetFileNameWithoutExtension(assetPath);

            var path = EditorUtility.SaveFilePanel(
                "Save Normal Map PNG",
                System.IO.Path.GetDirectoryName(assetPath),
                currentPreset.name + "_Normal.png",
                "png");

            if (path.Length != 0)
            {
                System.IO.File.WriteAllBytes(path, normalMap.EncodeToPNG());
                AssetDatabase.Refresh();
                PresetData.instance.Add(currentPreset);
            }
        }

        public void Dispose()
        {
            processor?.Dispose();
        }
    }
}