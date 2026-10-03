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

    public class NormalProcessorWindow : EditorWindow
    {
        private Preset currentPreset = new("Default Preset");
        private Texture2D inputTexture;
        private int previewLayer = 2; // 0=Input, 1=Gauss, 2=Normal, 3=Lighting
        private System.Collections.Generic.List<NormalProcessorGPU.LightData> previewLights = new() { new NormalProcessorGPU.LightData { position = new Vector4(0.5f, 0.5f, 0.5f, 1.5f), color = new Vector4(1f, 1f, 1f, 2f) } };
        private int grabbedLightIndex = -1;

        private Changes changes = Changes.Everything;
        private NormalProcessorGPU processor;

        private Image previewImage;
        private VisualElement rightPanel;

        [MenuItem("Window/Darkness Team/Normal Processor/Single Texture Processor")]
        [MenuItem("Assets/Darkness Team/Normal Processor/Single Texture Processor")]
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

            var ussGuids = AssetDatabase.FindAssets("CurrentTheme t:StyleSheet");
            if (ussGuids.Length > 0)
            {
                var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetDatabase.GUIDToAssetPath(ussGuids[0]));
                if (styleSheet != null)
                {
                    root.styleSheets.Add(styleSheet);
                    root.AddToClassList("root-container");
                }
            }

            // Main layout using TwoPaneSplitView
            var splitView = new TwoPaneSplitView(0, 320, TwoPaneSplitViewOrientation.Horizontal);
            root.Add(splitView);

            // Left panel for settings
            var leftPanel = new ScrollView(ScrollViewMode.Vertical);
            leftPanel.AddToClassList("glass-panel");
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
            var headerContainer = new VisualElement() { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 15, flexShrink = 0 } };
            var icon = new Image() { image = EditorGUIUtility.IconContent("d_PreTextureRGB").image, style = { width = 24, height = 24, marginRight = 5 } };
            var header = new Label("Normal Processor") { style = { fontSize = 18, unityFontStyleAndWeight = FontStyle.Bold } };
            headerContainer.Add(icon);
            headerContainer.Add(header);
            container.Add(headerContainer);

            // Input Texture
            var texField = new ObjectField("Input Texture") { objectType = typeof(Texture2D), value = inputTexture, style = { flexShrink = 0 } };
            texField.RegisterValueChangedCallback(evt => {
                inputTexture = (Texture2D)evt.newValue;
                processor?.RebindTexture(inputTexture);
                changes |= Changes.Everything;
                UpdatePreview();
            });
            container.Add(texField);

            // Presets Button
            var presetBtn = new Button(ShowPresetMenu) { style = { marginTop = 10, marginBottom = 15, height = 24, flexShrink = 0, flexDirection = FlexDirection.Row, justifyContent = Justify.Center, alignItems = Align.Center } };
            presetBtn.AddToClassList("glass-button");
            presetBtn.Add(new Image() { image = EditorGUIUtility.IconContent("d_Settings").image, style = { width = 16, height = 16, marginRight = 6 } });
            presetBtn.Add(new Label("Load Preset"));
            container.Add(presetBtn);

            // Help Box
            var helpBox = new HelpBox("Please assign a Texture2D first.", HelpBoxMessageType.Info);
            helpBox.style.display = inputTexture == null ? DisplayStyle.Flex : DisplayStyle.None;
            helpBox.style.flexShrink = 0;
            texField.RegisterValueChangedCallback(evt => helpBox.style.display = evt.newValue == null ? DisplayStyle.Flex : DisplayStyle.None);
            container.Add(helpBox);

            // --- Adjustments Group ---
            var adjustmentsGroup = new GroupBox("Adjustments") { style = { marginTop = 10, paddingBottom = 10, flexShrink = 0, borderLeftWidth=1, borderRightWidth=1, borderTopWidth=1, borderBottomWidth=1, borderTopLeftRadius=4, borderTopRightRadius=4, borderBottomLeftRadius=4, borderBottomRightRadius=4 } };
            
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
            var detailIntensityField = new Slider("Fine Detail", 0, 10) { value = currentPreset.detailIntensity, showInputField = true, tooltip = "Blends high-frequency micro-details from the unblurred texture back into the normal map." };
            detailIntensityField.RegisterValueChangedCallback(evt => {
                currentPreset.detailIntensity = evt.newValue;
                changes |= Changes.NormalChanged;
                UpdatePreview();
            });
            adjustmentsGroup.Add(detailIntensityField);
            container.Add(adjustmentsGroup);

            // --- Options Group ---
            var optionsGroup = new GroupBox("Options");

            var invertToggle = new Toggle("Invert Height") { value = currentPreset.invertHeight, tooltip = "Inverts the perceived depth of the generated normal map (e.g. making bumps into dents)." };
            invertToggle.RegisterValueChangedCallback(evt => {
                currentPreset.invertHeight = evt.newValue;
                changes |= Changes.NormalChanged;
                UpdatePreview();
            });
            optionsGroup.Add(invertToggle);

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

            // --- Existing Normal Processing ---
            var existingNormalGroup = new Foldout() { text = "Process Existing Normal Map", value = currentPreset.processExistingNormal };
            var existingNormalToggle = existingNormalGroup.Q<Toggle>();
            if (existingNormalToggle != null) {
                existingNormalToggle.tooltip = "Enable this if the input texture is ALREADY a normal map and you just want to convert its format.";
            }
            existingNormalGroup.RegisterValueChangedCallback(evt => {
                if (evt.target == existingNormalGroup) {
                    currentPreset.processExistingNormal = evt.newValue;
                    changes = Changes.Everything;
                    UpdatePreview();
                }
            });
            
            var flipGreenToggle = new Toggle("Flip Green Channel (Y)") { value = currentPreset.flipGreenChannel, tooltip = "Flips the Y channel to convert between DirectX and OpenGL normal map formats." };
            flipGreenToggle.RegisterValueChangedCallback(evt => {
                currentPreset.flipGreenChannel = evt.newValue;
                changes |= Changes.NormalChanged;
                UpdatePreview();
            });
            existingNormalGroup.Add(flipGreenToggle);
            
            var rebuildZToggle = new Toggle("Rebuild Blue Channel (Z)") { value = currentPreset.rebuildZChannel, tooltip = "Reconstructs the Z channel based on X and Y to fix missing blue channels." };
            rebuildZToggle.RegisterValueChangedCallback(evt => {
                currentPreset.rebuildZChannel = evt.newValue;
                changes |= Changes.NormalChanged;
                UpdatePreview();
            });
            existingNormalGroup.Add(rebuildZToggle);
            
            container.Add(existingNormalGroup);

            // Flexible space
            var spacer = new VisualElement() { style = { flexGrow = 1, minHeight = 20, flexShrink = 1 } };
            container.Add(spacer);

            // Save Button
            var saveBtn = new Button(SaveTexture) { name = "saveBtn", tooltip = "Process and save as PNG" };
            saveBtn.AddToClassList("glass-button");
            saveBtn.style.height = 36;
            saveBtn.style.flexShrink = 0;
            saveBtn.style.flexDirection = FlexDirection.Row;
            saveBtn.style.justifyContent = Justify.Center;
            saveBtn.style.alignItems = Align.Center;
            saveBtn.Add(new Image() { image = EditorGUIUtility.IconContent("d_SaveAs").image, style = { width = 16, height = 16, marginRight = 8 } });
            saveBtn.Add(new Label("Generate & Save Normal Map") { style = { unityFontStyleAndWeight = FontStyle.Bold, fontSize = 14 } });
            container.Add(saveBtn);
        }

        private void BuildRightPanel(VisualElement container)
        {
            // Toolbar
            var toolbar = new UnityEditor.UIElements.Toolbar();
            toolbar.style.flexShrink = 0;
            toolbar.style.minHeight = 24;
            toolbar.style.paddingLeft = 5;
            var layerMenu = new UnityEditor.UIElements.ToolbarMenu { text = "Preview Layer: Normal", style = { flexShrink = 0 } };
            layerMenu.menu.AppendAction("Input", _ => { previewLayer = 0; layerMenu.text = "Preview Layer: Input"; UpdatePreview(); });
            layerMenu.menu.AppendAction("Gauss", _ => { previewLayer = 1; layerMenu.text = "Preview Layer: Gauss"; UpdatePreview(); });
            layerMenu.menu.AppendAction("Normal", _ => { previewLayer = 2; layerMenu.text = "Preview Layer: Normal"; UpdatePreview(); });
            layerMenu.menu.AppendAction("Lighting", _ => { previewLayer = 3; layerMenu.text = "Preview Layer: Lighting"; UpdatePreview(); });
            toolbar.Add(layerMenu);

            var spacer = new VisualElement() { style = { flexGrow = 1, flexShrink = 1 } };
            toolbar.Add(spacer);

            var refreshBtn = new UnityEditor.UIElements.ToolbarButton(UpdatePreview) { tooltip = "Force Re-evaluate", style = { flexShrink = 0, flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            refreshBtn.Add(new Image() { image = EditorGUIUtility.IconContent("d_Refresh").image, style = { width = 16, height = 16, marginRight = 4 } });
            refreshBtn.Add(new Label("Refresh"));
            toolbar.Add(refreshBtn);
            
            var themeBtn = new UnityEditor.UIElements.ToolbarButton(ThemeEditorWindow.ShowWindow) { tooltip = "Open Theme Editor", style = { flexShrink = 0, flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            themeBtn.Add(new Image() { image = EditorGUIUtility.IconContent("d_Settings").image, style = { width = 16, height = 16 } });
            toolbar.Add(themeBtn);

            container.Add(toolbar);

            // Image Preview area
            var previewImageContainer = new VisualElement() { style = { flexGrow = 1, backgroundColor = new Color(0, 0, 0, 0.3f) } };
            previewImageContainer.AddToClassList("glass-panel");
            
            previewImage = new Image() { style = { flexGrow = 1, flexShrink = 1 } };
            previewImage.scaleMode = ScaleMode.ScaleToFit;
            
            previewImageContainer.Add(previewImage);
            SetupLightingInteractions();
            container.Add(previewImageContainer);

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
            if (currentPreset.processExistingNormal)
            {
                if (changes.HasFlag(Changes.NormalChanged))
                {
                    processor.ProcessExistingNormalMap(currentPreset.intensity, currentPreset.flipGreenChannel, currentPreset.rebuildZChannel);
                }
            }
            else
            {
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
                    processor.ComputeNormal(currentPreset.intensity, currentPreset.detailIntensity, currentPreset.invertHeight);
                }
            }
            
            changes = Changes.None;

            var saveBtn = rootVisualElement.Q<Button>("saveBtn");
            if (saveBtn != null) {
                var lbl = saveBtn.Q<Label>();
                if (lbl != null) lbl.text = previewLayer == 3 ? "Export Lit Preview" : "Generate & Save Normal Map";
            }

            if (previewLayer == 3)
            {
                processor.ComputeLighting(previewLights.ToArray());
                if (previewImage.image != processor.LitTexture)
                    previewImage.image = processor.LitTexture;
                else
                    previewImage.MarkDirtyRepaint();
            }
            else
            {
                var targetTex = previewLayer switch {
                    1 => (Texture)processor.TempTexture,
                    2 => processor.OutputTexture,
                    _ => processor.InputTexture
                };
                if (previewImage.image != targetTex)
                    previewImage.image = targetTex;
            }
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

            bool isLit = previewLayer == 3;
            Texture2D mapToSave = processor.GetTexture(isLit ? processor.LitTexture : processor.OutputTexture);
            string assetPath = AssetDatabase.GetAssetPath(inputTexture);
            currentPreset.name = System.IO.Path.GetFileNameWithoutExtension(assetPath);

            var path = EditorUtility.SaveFilePanel(
                isLit ? "Save Lit Image PNG" : "Save Normal Map PNG",
                System.IO.Path.GetDirectoryName(assetPath),
                currentPreset.name + (isLit ? "_Lit.png" : "_Normal.png"),
                "png");

            if (path.Length != 0)
            {
                System.IO.File.WriteAllBytes(path, mapToSave.EncodeToPNG());
                AssetDatabase.Refresh();
                
                // Only save preset if it's an actual normal map generation
                if (!isLit && !currentPreset.processExistingNormal) 
                {
                    PresetData.instance.Add(currentPreset);
                }
            }
            
            // Clean up the CPU texture to prevent memory leaks!
            DestroyImmediate(mapToSave);
        }

        private void OnDisable()
        {
            processor?.Dispose();
            processor = null;
        }

        private Rect GetTextureRect()
        {
            if (processor == null || processor.InputTexture == null || previewImage.layout.width == 0) return new Rect();
            float imgAspect = (float)processor.InputTexture.width / processor.InputTexture.height;
            float layAspect = previewImage.layout.width / previewImage.layout.height;
            
            float drawW, drawH, drawX, drawY;
            if (imgAspect > layAspect) {
                drawW = previewImage.layout.width;
                drawH = previewImage.layout.width / imgAspect;
                drawX = 0;
                drawY = (previewImage.layout.height - drawH) / 2.0f;
            } else {
                drawH = previewImage.layout.height;
                drawW = previewImage.layout.height * imgAspect;
                drawX = (previewImage.layout.width - drawW) / 2.0f;
                drawY = 0;
            }
            return new Rect(drawX, drawY, drawW, drawH);
        }

        private void SetupLightingInteractions()
        {
            var previewImageContainer = previewImage.parent;
            bool showLightingTips = true;
            
            var tipsBtn = new Button(() => { showLightingTips = !showLightingTips; previewImage.MarkDirtyRepaint(); }) { text = "Toggle Shortcuts" };
            tipsBtn.AddToClassList("glass-button");
            tipsBtn.style.position = Position.Absolute;
            tipsBtn.style.left = 10;
            tipsBtn.style.top = 10;
            tipsBtn.style.display = DisplayStyle.None;
            previewImageContainer.Add(tipsBtn);
            
            var gizmoOverlay = new IMGUIContainer(() => {
                if (previewLayer != 3) {
                    tipsBtn.style.display = DisplayStyle.None;
                    return;
                }
                tipsBtn.style.display = DisplayStyle.Flex;
                Handles.BeginGUI();
                
                if (showLightingTips) {
                    GUI.color = new Color(1, 1, 1, 0.8f);
                    GUI.Box(new Rect(10, 40, 290, 110), "");
                    GUIStyle labelStyle = new GUIStyle(EditorStyles.label) { wordWrap = true, normal = { textColor = Color.white } };
                    GUI.Label(new Rect(15, 45, 280, 110), "LIGHTING CONTROLS:\n• Left Click & Drag: Move Light\n• Right Click: Spawn Light\n• Middle Click: Delete Light\n• Scroll Wheel: Radius (Z-Height)\n• Shift/Ctrl + Scroll: Intensity\n*(Note: Cannot delete the final light)*", labelStyle);
                }
                
                Rect texRect = GetTextureRect();
                if (texRect.width > 0) {
                    for (int i = 0; i < previewLights.Count; i++) {
                        var l = previewLights[i];
                        Vector2 pos = new Vector2(texRect.x + l.position.x * texRect.width, texRect.y + (1.0f - l.position.y) * texRect.height);
                        
                        Handles.color = i == grabbedLightIndex ? Color.green : new Color(1, 0.8f, 0.2f, 0.8f);
                        float radius = l.position.w * 50f;
                        Handles.DrawWireDisc(pos, Vector3.forward, radius);
                        Handles.DrawWireDisc(pos, Vector3.forward, radius * 0.9f);
                        
                        GUI.color = i == grabbedLightIndex ? Color.green : Color.yellow;
                        GUI.Label(new Rect(pos.x - 10, pos.y - 10, 20, 20), "☼", new GUIStyle(EditorStyles.largeLabel) { alignment = TextAnchor.MiddleCenter });
                    }
                }
                Handles.EndGUI();
            }) { pickingMode = PickingMode.Ignore, style = { position = Position.Absolute, left = 0, top = 0, right = 0, bottom = 0 } };
            
            previewImage.Add(gizmoOverlay);
            
            float zoom = 1.0f;
            Vector2 pan = Vector2.zero;

            Vector2 MouseToUV(Vector2 mousePos) {
                Rect rect = GetTextureRect();
                if(rect.width == 0) return Vector2.zero;
                return new Vector2((mousePos.x - rect.x) / rect.width, 1.0f - ((mousePos.y - rect.y) / rect.height));
            }

            previewImage.RegisterCallback<MouseDownEvent>(evt => {
                if (evt.button == 2 && previewLayer != 3) {
                    previewImage.CaptureMouse();
                    evt.StopPropagation();
                    return;
                }

                if (previewLayer != 3) return;
                
                Vector2 uv = MouseToUV(evt.localMousePosition);

                if (evt.button == 1) {
                    if (previewLights.Count < 8) {
                        previewLights.Add(new NormalProcessorGPU.LightData { position = new Vector4(Mathf.Clamp01(uv.x), Mathf.Clamp01(uv.y), 0.5f, 1.5f), color = new Vector4(1f, 1f, 1f, 2f) });
                        processor?.ComputeLighting(previewLights.ToArray());
                        previewImage.MarkDirtyRepaint();
                    }
                    return;
                }
                
                if (evt.button == 2) { 
                    int closest = -1; float minDist = 0.05f;
                    for (int i = 0; i < previewLights.Count; i++) {
                        float dist = Vector2.Distance(new Vector2(previewLights[i].position.x, previewLights[i].position.y), uv);
                        if (dist < minDist) { closest = i; minDist = dist; }
                    }
                    if (closest != -1 && previewLights.Count > 1) {
                        previewLights.RemoveAt(closest);
                        processor?.ComputeLighting(previewLights.ToArray());
                        previewImage.MarkDirtyRepaint();
                    }
                    return;
                }

                if (evt.button == 0) {
                    grabbedLightIndex = -1;
                    float minDist = 0.1f;
                    for (int i = 0; i < previewLights.Count; i++) {
                        float dist = Vector2.Distance(new Vector2(previewLights[i].position.x, previewLights[i].position.y), uv);
                        if (dist < minDist) { grabbedLightIndex = i; minDist = dist; }
                    }
                    if (grabbedLightIndex != -1) {
                        previewImage.CaptureMouse();
                        previewImage.MarkDirtyRepaint();
                        evt.StopPropagation();
                    }
                }
            });

            previewImage.RegisterCallback<MouseMoveEvent>(evt => {
                if (previewImage.HasMouseCapture() && evt.pressedButtons == 4) {
                    pan += evt.mouseDelta;
                    previewImage.style.translate = new StyleTranslate(new Translate(pan.x, pan.y, 0));
                    evt.StopPropagation();
                    return;
                }

                if (previewLayer != 3 || grabbedLightIndex == -1 || !previewImage.HasMouseCapture()) return;
                
                Vector2 uv = MouseToUV(evt.localMousePosition);
                var l = previewLights[grabbedLightIndex];
                l.position.x = Mathf.Clamp01(uv.x);
                l.position.y = Mathf.Clamp01(uv.y);
                previewLights[grabbedLightIndex] = l;
                
                processor?.ComputeLighting(previewLights.ToArray());
                previewImage.MarkDirtyRepaint();
                evt.StopPropagation();
            });

            previewImage.RegisterCallback<MouseUpEvent>(evt => {
                if (previewImage.HasMouseCapture()) {
                    grabbedLightIndex = -1;
                    previewImage.ReleaseMouse();
                    previewImage.MarkDirtyRepaint();
                    evt.StopPropagation();
                }
            });

            previewImage.RegisterCallback<WheelEvent>(evt => {
                if (previewLayer != 3 || evt.altKey) {
                    float scrollDelta = evt.delta.y != 0 ? evt.delta.y : evt.delta.x;
                    float zoomDelta = -scrollDelta * 0.05f;
                    zoom = Mathf.Clamp(zoom + zoomDelta, 0.1f, 10f);
                    previewImage.style.scale = new StyleScale(new Scale(new Vector3(zoom, zoom, 1)));
                    evt.StopPropagation();
                    return;
                }

                Vector2 uv = MouseToUV(evt.localMousePosition);
                int closest = -1; float minDist = 0.1f;
                for (int i = 0; i < previewLights.Count; i++) {
                    float dist = Vector2.Distance(new Vector2(previewLights[i].position.x, previewLights[i].position.y), uv);
                    if (dist < minDist) { closest = i; minDist = dist; }
                }
                
                if (closest != -1) {
                    var l = previewLights[closest];
                    float scrollDelta = evt.delta.y != 0 ? evt.delta.y : evt.delta.x;
                    if (evt.shiftKey || evt.ctrlKey || evt.commandKey) {
                        l.color.w = Mathf.Clamp(l.color.w - scrollDelta * 0.1f, 0.1f, 10f);
                    } else {
                        l.position.z = Mathf.Clamp(l.position.z - scrollDelta * 0.05f, 0.05f, 5f);
                        l.position.w = l.position.z * 3f;
                    }
                    previewLights[closest] = l;
                    
                    processor?.ComputeLighting(previewLights.ToArray());
                    previewImage.MarkDirtyRepaint();
                    evt.StopPropagation();
                }
            });
        }
    }
}