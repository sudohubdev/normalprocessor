using System;
using UnityEditor;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System.ComponentModel;

namespace dev.sudohub.normalprocessor
{
    public class NormalProcessorArrayWindow : EditorWindow
    {
        private NormalProcessorArrayState state = new(new Vector2Int(8, 8));
        private GridState gridState = new();
        private PreviewState previewState = new();
        private Lazy<NormalProcessorGPU> _processor = new();

        private VisualElement gridViewport;//store grid viewport to change grid count based on size
        private Image prevSelected;

        #region menu
        [MenuItem("Window/Darkness Team/Normal Processor/Atlas Processor")]
        [MenuItem("Assets/Darkness Team/Normal Processor/Atlas Processor")]
        public static void ShowWindow()
        {
            var window = GetWindow<NormalProcessorArrayWindow>("Normal Processor (Atlas)");
            window.minSize = new Vector2(600f, 400f);
        }
        public void OnEnable()
        {
            if (Selection.activeObject is Texture2D tex)
            {
                _processor.Value.RebindTexture(tex);
                state.InputTexture = tex;
            }
        }

        public void CreateGUI()
        {
            var root = rootVisualElement;
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/dev.sudohub.normalprocessor/Editor Resources/UI/CurrentTheme.uss");
            if (styleSheet != null)
            {
                root.styleSheets.Add(styleSheet);
                root.AddToClassList("root-container");
            }

            // Main horizontal split
            var mainSplit = new TwoPaneSplitView(0, 300, TwoPaneSplitViewOrientation.Horizontal);

            // Left Panel (Tile Grid)
            var leftPanel = new VisualElement();
            CreateLeftPanel(leftPanel);

            // Right Panel (Vertical split between params/preview)
            var rightSplit = new TwoPaneSplitView(0, 400, TwoPaneSplitViewOrientation.Vertical);
            rightSplit.style.minWidth = 300;
            CreateParamsPanel(rightSplit);
            CreatePreviewPanel(rightSplit);

            mainSplit.Add(leftPanel);
            mainSplit.Add(rightSplit);
            rootVisualElement.Add(mainSplit);
        }

        #region Left Panel (texture select + tile chooser)
        private void CreateLeftPanel(VisualElement container)
        {
            var paramsContainer = new VisualElement() { style = { flexShrink = 0 } };
            container.style.minWidth = 300;
            container.AddToClassList("glass-panel");
            paramsContainer.style.minHeight = 80;

            // Input texture
            var textureField = new ObjectField("Input Texture")
            {
                objectType = typeof(Texture2D),
                value = state.InputTexture,
                style = { flexShrink = 0 }
            };
            textureField.RegisterValueChangedCallback(evt =>
            {
                _processor.Value.RebindTexture((Texture2D)evt.newValue);
                state.InputTexture = (Texture2D)evt.newValue;
                UpdateGrid();
            });
            paramsContainer.Add(textureField);

            // Atlas Size
            var atlasField = new Vector2IntField("Atlas Size") { value = state.Size };
            atlasField.RegisterValueChangedCallback(evt =>
            {
                var vec = evt.newValue;
                vec.Clamp(Vector2Int.one, new Vector2Int(16, 16));
                atlasField.SetValueWithoutNotify(vec);

                state.Resize(vec);
                UpdateGrid();
            });
            paramsContainer.Add(atlasField);

            var helpBox = new HelpBox("Thread groups aren't integers. Make sure Texture Size / Atlas Size is a power of two.", HelpBoxMessageType.Warning)
            { style = { display = DisplayStyle.None } };
            paramsContainer.Add(helpBox);

            state.PropertyChanged += (s, e) =>
            {
                if (_processor.Value.InputTexture == null) return;
                var isPowerOfTwo = (_processor.Value.InputTexture.width / state.Size.x % 8) == 0 &&
                                    (_processor.Value.InputTexture.height / state.Size.y % 8) == 0;
                helpBox.style.display = isPowerOfTwo ? DisplayStyle.None : DisplayStyle.Flex;
            };

            // Grid controls
            var gridControls = new VisualElement() { style = { flexDirection = FlexDirection.Row, flexShrink = 0, minHeight = 24 } };

            var gridLayerMenu = new ToolbarMenu { text = "Layers", style = { marginRight = 5, minWidth = 60 } };
            gridLayerMenu.menu.AppendAction("Input", _ => gridState.TextureType = TextureType.Input);
            gridLayerMenu.menu.AppendAction("Gauss", _ => gridState.TextureType = TextureType.Gauss);
            gridLayerMenu.menu.AppendAction("Normal", _ => gridState.TextureType = TextureType.Normal);
            gridControls.Add(gridLayerMenu);

            var zoom = new Slider("Zoom", 0.5f, 4f) { value = 1, showInputField = true, style = { minWidth = 100, flexGrow = 1 } };
            zoom.RegisterValueChangedCallback(evt => gridState.ElementSize = evt.newValue * 100);
            gridControls.Add(zoom);

            // Grid viewport
            gridViewport = new VisualElement()
            {
                style = {
                    flexDirection = FlexDirection.Row,
                    flexShrink = 1,
                    flexWrap = Wrap.Wrap,
                    justifyContent = Justify.Center,
                    alignItems = Align.Center
                }
            };

            var gridScrollView = new ScrollView()
            {
                verticalScrollerVisibility = ScrollerVisibility.Auto,
                horizontalScrollerVisibility = ScrollerVisibility.Auto,
            };
            gridScrollView.Add(gridViewport);

            paramsContainer.Add(gridControls);
            container.Add(paramsContainer);
            container.Add(gridScrollView);

            container.Add(new VisualElement() { style = { flexGrow = 1 } });

            //save button
            var saveBtn = new Button(SaveNormalMap) { tooltip = "Process and save as PNG" };
            saveBtn.AddToClassList("glass-button");
            saveBtn.style.height = 36;
            saveBtn.style.flexShrink = 0;
            saveBtn.style.flexDirection = FlexDirection.Row;
            saveBtn.style.justifyContent = Justify.Center;
            saveBtn.style.alignItems = Align.Center;
            saveBtn.Add(new Image() { image = EditorGUIUtility.IconContent("d_SaveAs").image, style = { width = 16, height = 16, marginRight = 8 } });
            saveBtn.Add(new Label("Generate & Save Atlas") { style = { unityFontStyleAndWeight = FontStyle.Bold, fontSize = 14 } });
            container.Add(saveBtn);

            UpdateGrid();
        }

        private void UpdateGrid()
        {
            gridViewport.Clear();
            gridState.ClearEvents(); // Clear event subscriptions
            state.ActiveCoord = Vector2Int.zero; // Reset active element to first

            if (state.InputTexture == null)
            {
                var helpBox = new HelpBox("Please assign a Texture2D first.", HelpBoxMessageType.Info);
                gridViewport.Add(helpBox);
                return;
            }
            if (_processor.Value.InputTexture == null)
            {
                Debug.LogError("[Normal Processor] InputTexture is null! Check states.");
            }

            float segmentWidth = state.InputTexture.width / (float)state.Size.x;
            float segmentHeight = state.InputTexture.height / (float)state.Size.y;

            int index = 0;
            for (int y = 0; y < state.Size.y; y++)
            {
                for (int x = 0; x < state.Size.x; x++)
                {
                    // Note: we flip the y-axis so that cell (0,0) is at bottom-left.
                    Rect spriteRect = new(
                        x / (float)state.Size.x,
                        1.0f - (y + 1) / (float)state.Size.y,
                        1.0f / state.Size.x,
                        1.0f / state.Size.y
                    );

                    var cell = new VisualElement();

                    // Create a thumbnail for the cell.
                    var texture = gridState.GetTexture(_processor.Value);
                    var img = new Image()
                    {
                        image = texture,
                        sourceRect = new Rect(0, 0, texture.width, texture.height),
                        uv = spriteRect,
                        style =  {
                            borderLeftWidth = 1,
                            borderRightWidth = 1,
                            borderTopWidth = 1,
                            borderBottomWidth = 1,
                            borderLeftColor = Color.black,
                            borderRightColor = Color.black,
                            borderTopColor = Color.black,
                            borderBottomColor = Color.black,
                            width = gridState.ElementSize,
                            height = gridState.ElementSize
                        }
                    };
                    // Update thumbnails on event
                    gridState.PropertyChanged += (e, a) =>
                    {
                        img.image = gridState.GetTexture(_processor.Value);
                        img.style.width = img.style.height = gridState.ElementSize;
                    };
                    cell.Add(img);

                    // Add a label as a child (centered over the image)
                    Label indexLabel = new(index.ToString())
                    {
                        style = {
                            // Complementary to #8080ff, aka nice contrast
                            color = new Color(1f,0.91f,0.5f),
                            unityTextAlign = TextAnchor.MiddleCenter,
                            position = Position.Absolute,
                            top = 0,
                            left = 0,
                            right = 0,
                            bottom = 0
                        },
                        // Make sure the label does not block mouse events.
                        pickingMode = PickingMode.Ignore
                    };
                    cell.Add(indexLabel);

                    // Pass to delegate
                    var coord = new Vector2Int(x, y);
                    if (state.ActiveCoord == coord)
                    {
                        prevSelected = img;
                        img.style.borderLeftColor = Color.yellow;
                        img.style.borderRightColor = Color.yellow;
                        img.style.borderTopColor = Color.yellow;
                        img.style.borderBottomColor = Color.yellow;
                    }

                    // Register click event on the cell.
                    cell.RegisterCallback<MouseDownEvent>(evt =>
                    {
                        //activate Format Painter
                        if (evt.button == 0 && gridState.ActivateFormatPainter())
                        {
                            evt.StopPropagation();
                            return;
                        }
                        //selection
                        if (evt.button == 0 && img != prevSelected)
                        {
                            state.ActiveCoord = coord;
                            //
                            img.style.borderLeftColor = Color.yellow;
                            img.style.borderRightColor = Color.yellow;
                            img.style.borderTopColor = Color.yellow;
                            img.style.borderBottomColor = Color.yellow;
                            //
                            prevSelected.style.borderLeftColor = Color.black;
                            prevSelected.style.borderRightColor = Color.black;
                            prevSelected.style.borderTopColor = Color.black;
                            prevSelected.style.borderBottomColor = Color.black;

                            prevSelected = img;
                            //previewState.OnPropertyChanged();
                            evt.StopPropagation();
                        }
                    });
                    // Pass to delegate
                    int savedIndex = index;
                    cell.RegisterCallback<MouseMoveEvent>(evt =>
                    {
                        if (gridState.FormatPaintActive(coord))
                        {
                            state.presets[savedIndex] = state.CurrentPreset;
                            _processor.Value.SetTiling(state.Size, coord);
                            Compute(Changes.Everything);
                        }
                        evt.StopPropagation();
                    });
                    cell.RegisterCallback<MouseUpEvent>(evt =>
                    {
                        gridState.StopFormatPaint();
                        evt.StopPropagation();
                    });

                    gridViewport.Add(cell);
                    index++;
                }
            }
        }

        #endregion

        #region Right Panel (Options + Preview)
        private void CreateParamsPanel(VisualElement container)
        {
            var paramsContainer = new VisualElement() { style = { flexShrink = 0 } };
            paramsContainer.style.minHeight = 140;
            paramsContainer.AddToClassList("glass-panel");

            // Params Header
            var paramsHeader = new VisualElement()
            {
                style = {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    marginBottom = 5
                }
            };
            paramsHeader.Add(new Label("Options")
            {
                style = {
                    fontSize = 14,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginRight = 10
                }
            });

            //Format Painter (like in Excel)
            var formatBtn = new Button(() => gridState.RequestFormatPainter()) { text = "Format Painter", tooltip = "Copies Options into other cells. As seen on Excel." };
            formatBtn.AddToClassList("glass-button");
            paramsHeader.Add(formatBtn);

            //spacer
            paramsHeader.Add(new VisualElement() { style = { flexGrow = 1 } });

            // Theme Editor Button
            var themeBtn = new Button(ThemeEditorWindow.ShowWindow) { tooltip = "Open Theme Editor", style = { flexShrink = 0, flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            themeBtn.Add(new Image() { image = EditorGUIUtility.IconContent("d_Settings").image, style = { width = 16, height = 16 } });
            themeBtn.AddToClassList("glass-button");
            paramsHeader.Add(themeBtn);

            //Lock Params
            var lockParams = new Toggle("Lock Options") { style = { flexShrink = 10 }, tooltip = "Locks Options for all Cells, aka editing all in once." };
            lockParams.labelElement.style.minWidth = 10;
            lockParams.RegisterValueChangedCallback(evt => state.doLockParams = evt.newValue);
            paramsHeader.Add(lockParams);
            paramsContainer.Add(paramsHeader);


            // Preset controls
            var curveField = new CurveField("Grayscale Curve") { value = AnimationCurve.Linear(0, 0, 1, 1), tooltip = "Basic color correction curve, use this to amplify/attenuate details." };
            curveField.RegisterValueChangedCallback(evt =>
            {
                //evt is an impostor. value getter copies AnimationCurve, evt.newValue does not
                state.CurrentPreset.bwCurve = curveField.value;
                state.Changes |= Changes.LUTChanged;
            });

            var smoothnessSlider = new Slider("Smoothness", 0, 10) { value = state.CurrentPreset.smoothness, showInputField = true, tooltip = "Gaussian Smoothhness, increases blur." };
            smoothnessSlider.RegisterValueChangedCallback(evt =>
            {
                state.CurrentPreset.smoothness = evt.newValue;
                state.Changes |= Changes.GaussChanged;
            });

            var intensitySlider = new Slider("Intensity", 0, 10) { value = state.CurrentPreset.intensity, showInputField = true, tooltip = "Normal Map Intensity." };
            intensitySlider.RegisterValueChangedCallback(evt =>
            {
                state.CurrentPreset.intensity = evt.newValue;
                state.Changes |= Changes.NormalChanged;
            });

            var tilingToggle = new Toggle("Do Tiling") { value = state.CurrentPreset.doTiling, tooltip = "Repeat the Texture, use this with seamless tiles." };
            tilingToggle.RegisterValueChangedCallback(evt =>
            {
                state.CurrentPreset.doTiling = evt.newValue;
                state.Changes |= Changes.KeywordChanged;
            });

            var scharrToggle = new Toggle("Use Scharr Operator") { value = state.CurrentPreset.useScharr, tooltip = "Scharr filter may be better than Sobel filter. Try both and pick a better one for your needs." };
            scharrToggle.RegisterValueChangedCallback(evt =>
            {
                state.CurrentPreset.useScharr = evt.newValue;
                state.Changes |= Changes.KeywordChanged;
            });

            var detailIntensitySlider = new Slider("Fine Detail", 0, 10) { value = state.CurrentPreset.detailIntensity, showInputField = true, tooltip = "Blends high-frequency micro-details from the unblurred texture back into the normal map." };
            detailIntensitySlider.RegisterValueChangedCallback(evt =>
            {
                state.CurrentPreset.detailIntensity = evt.newValue;
                state.Changes |= Changes.NormalChanged;
            });
            
            var invertToggle = new Toggle("Invert Height") { value = state.CurrentPreset.invertHeight, tooltip = "Inverts the perceived depth of the generated normal map (e.g. making bumps into dents)." };
            invertToggle.RegisterValueChangedCallback(evt =>
            {
                state.CurrentPreset.invertHeight = evt.newValue;
                state.Changes |= Changes.NormalChanged;
            });

            //update controls on property changed
            state.PropertyChanged += (s, e) =>
            {
                curveField.SetValueWithoutNotify(state.CurrentPreset.bwCurve);
                smoothnessSlider.SetValueWithoutNotify(state.CurrentPreset.smoothness);
                intensitySlider.SetValueWithoutNotify(state.CurrentPreset.intensity);
                detailIntensitySlider.SetValueWithoutNotify(state.CurrentPreset.detailIntensity);
                tilingToggle.SetValueWithoutNotify(state.CurrentPreset.doTiling);
                scharrToggle.SetValueWithoutNotify(state.CurrentPreset.useScharr);
                invertToggle.SetValueWithoutNotify(state.CurrentPreset.invertHeight);
                //update display
                state.Changes |= Changes.Everything;
            };

            paramsContainer.Add(curveField);
            paramsContainer.Add(smoothnessSlider);
            paramsContainer.Add(intensitySlider);
            paramsContainer.Add(detailIntensitySlider);
            paramsContainer.Add(invertToggle);
            paramsContainer.Add(tilingToggle);
            paramsContainer.Add(scharrToggle);

            container.Add(paramsContainer);
        }


        private void CreatePreviewPanel(VisualElement container)
        {
            // Preview viewport
            var previewImageContainer = new VisualElement()
            {
                style = {
                    backgroundColor = new Color(0, 0, 0, 0.3f), // Match glass transparency but darker for image
                    flexGrow = 1,
                    opacity = 1,
                    minHeight = 200
                }
            };
            previewImageContainer.AddToClassList("glass-panel");

            // Preview controls
            var previewHeader = new VisualElement()
            {
                style = {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    marginBottom = 5,
                    flexShrink = 0,
                    minHeight = 24
                }
            };

            #region preview options panel

            previewHeader.Add(new Label("Preview")
            {
                style = {
                    fontSize = 14,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginRight = 10
                }
            });

            // Layer selection dropdown
            var layerMenu = new ToolbarMenu { text = "Layers", style = { marginRight = 5 } };
            layerMenu.menu.AppendAction("Input", _ => previewState.TextureType = TextureType.Input);
            layerMenu.menu.AppendAction("Gauss", _ => previewState.TextureType = TextureType.Gauss);
            layerMenu.menu.AppendAction("Normal", _ => previewState.TextureType = TextureType.Normal);
            layerMenu.menu.AppendAction("Input (Full)", _ => previewState.TextureType = TextureType.InputFull);
            layerMenu.menu.AppendAction("Gauss (Full)", _ => previewState.TextureType = TextureType.GaussFull);
            layerMenu.menu.AppendAction("Normal (Full)", _ => previewState.TextureType = TextureType.NormalFull);
            previewHeader.Add(layerMenu);

            // Interpolation selection dropdown
            var interpMenu = new ToolbarMenu { text = "View Filter", style = { marginRight = 5 } };
            interpMenu.menu.AppendAction("Point", _ => previewState.FilterMode = FilterMode.Point);
            interpMenu.menu.AppendAction("Bilinear", _ => previewState.FilterMode = FilterMode.Bilinear);
            previewHeader.Add(interpMenu);

            // Background selection dropdown
            var bgMenu = new ToolbarMenu { text = "Background", style = { marginRight = 5 } };
            bgMenu.menu.AppendAction("Black", _ => previewState.Background = BackgroudType.Black);
            bgMenu.menu.AppendAction("Gray", _ => previewState.Background = BackgroudType.Gray);
            bgMenu.menu.AppendAction("White", _ => previewState.Background = BackgroudType.White);
            previewHeader.Add(bgMenu);

            previewImageContainer.Add(previewHeader);

            #endregion

            // Create a sprite for the cell.
            var img = new Image() { style = { flexGrow = 1 } };

            //preview settings changed
            previewState.PropertyChanged += (e, a) =>
            {
                if (_processor.Value.InputTexture == null)
                {
                    Debug.Log("[Normal Processor] Why are you even doing this. Assign a Texture first.");
                    return;
                }
                img.image = previewState.GetTexture(_processor.Value);
                img.image.filterMode = previewState.FilterMode;
                previewImageContainer.style.backgroundImage = previewState.BackgroundTexture;
                img.uv = previewState.GetPrevievUVRect(state);
                img.MarkDirtyRepaint();
            };

            //update image for the first run
            if(_processor.Value.InputTexture != null)
            {
                ComputePreview();
                previewState.OnPropertyChanged();
            }

            //active tile changed
            state.NeedsCompute += (e, a) =>
            {
                if (_processor.Value.InputTexture == null)
                {
                    Debug.Log("[Normal Processor] Why are you even doing this. Assign a Texture first.");
                    return;
                }

                //check if texture rebind event
                if (a.PropertyName == "InputTexture")
                {
                    FullCompute(Changes.Everything);
                    state.Changes = Changes.None;
                    previewState.OnPropertyChanged("By InputTexture Change");
                }
                else
                {
                    ComputePreview();
                }
                img.uv = previewState.GetPrevievUVRect(state);
                img.MarkDirtyRepaint();
            };


            previewImageContainer.Add(img);


            container.Add(previewImageContainer);
        }

        #endregion

        #region compute
        private void ComputePreview()
        {
            if (state.doLockParams)
            {
                FullCompute(state.Changes);
                state.Changes = Changes.None;
            }
            else
            {
                _processor.Value.SetTiling(state.Size, state.ActiveCoord);
                Compute(state.Changes);
            }
        }

        private void FullCompute(Changes changes)
        {
            for (int x = 0; x < state.Size.x; x++)
            {
                for (int y = 0; y < state.Size.y; y++)
                {
                    _processor.Value.SetTiling(state.Size, new Vector2Int(x, y));
                    Compute(changes, false);
                }
            }
        }

        private void Compute(Changes changes, bool doResetFlag = true)
        {
            //Recompute for changed values
            if (changes.HasFlag(Changes.KeywordChanged))
            {
                _processor.Value.UpdateKeywords(state.CurrentPreset.doTiling, state.CurrentPreset.useScharr);
                //cascade the change to everything else.
                changes = Changes.Everything;
            }
            if (changes.HasFlag(Changes.LUTChanged))
            {
                _processor.Value.ComputeLUT(state.CurrentPreset.bwCurve);
                changes = Changes.Everything;
            }
            if (changes.HasFlag(Changes.GaussChanged))
            {
                _processor.Value.ComputeGauss(state.CurrentPreset.smoothness);
                changes = Changes.Everything;
            }
            if (changes.HasFlag(Changes.NormalChanged))
            {
                _processor.Value.ComputeNormal(state.CurrentPreset.intensity, state.CurrentPreset.detailIntensity, state.CurrentPreset.invertHeight);
            }
            //Up to date
            if (doResetFlag)
                state.Changes = Changes.None;
        }

        private void SaveNormalMap()
        {
            var normalMap = _processor.Value.GetTexture();

            // Original asset path
            var assetPath = AssetDatabase.GetAssetPath(state.InputTexture);

            //save preset name
            state.CurrentPreset.name = System.IO.Path.GetFileNameWithoutExtension(assetPath);

            // Save the normal map
            var path = EditorUtility.SaveFilePanel(
                "Save Normal Map Atlas PNG",
                System.IO.Path.GetDirectoryName(assetPath),
                state.CurrentPreset.name + "_Normal.png",
                "png");

            if (path.Length != 0)
            {
                System.IO.File.WriteAllBytes(path, normalMap.EncodeToPNG());
                AssetDatabase.Refresh();
                //save preset
                state.SavePresetInfo();
            }
            
            // Clean up the CPU texture to prevent memory leaks
            DestroyImmediate(normalMap);
        }
        #endregion

        private void OnDisable()
        {
            if (_processor != null && _processor.IsValueCreated) {
                _processor.Value.Dispose();
            }
        }
        #endregion
    }
}
