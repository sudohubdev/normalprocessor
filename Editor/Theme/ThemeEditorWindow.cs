using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace dev.sudohub.normalprocessor
{
    public class ThemeEditorWindow : EditorWindow
    {
        private NormalProcessorTheme _editingTheme;

        [MenuItem("Window/Darkness Team/Normal Processor/Theme Editor")]
        [MenuItem("Assets/Darkness Team/Normal Processor/Theme Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<ThemeEditorWindow>();
            window.titleContent = new GUIContent("Theme Editor", EditorGUIUtility.IconContent("d_Settings").image);
            window.minSize = new Vector2(350, 450);
            window.Show();
        }

        private void OnEnable()
        {
            // Create a copy of the current theme to edit safely
            _editingTheme = JsonUtility.FromJson<NormalProcessorTheme>(JsonUtility.ToJson(ThemeManager.CurrentTheme));
        }

        public void CreateGUI()
        {
            var root = rootVisualElement;
            
            // Try to load current theme so the editor matches the rest of the tool
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
            
            var container = new ScrollView(ScrollViewMode.Vertical) { style = { paddingBottom=15, paddingLeft=15, paddingRight=15, paddingTop=15 } };
            
            var title = new Label("Theme Customizer") { style = { fontSize = 20, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 15 } };
            container.Add(title);

            var presetsGroup = new GroupBox("Built-in Presets") { style = { marginBottom = 15 } };
            presetsGroup.AddToClassList("unity-group-box");
            
            var presetsRow1 = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 5 } };
            presetsRow1.Add(CreatePresetButton("Dark Glass", NormalProcessorTheme.DarkGlass()));
            presetsRow1.Add(CreatePresetButton("Crimson", NormalProcessorTheme.CrimsonCyber()));
            presetsRow1.Add(CreatePresetButton("Unity", NormalProcessorTheme.DefaultUnityDark()));
            presetsRow1.Add(CreatePresetButton("Nord", NormalProcessorTheme.NordArctic()));
            presetsGroup.Add(presetsRow1);

            var presetsRow2 = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
            presetsRow2.Add(CreatePresetButton("Emerald", NormalProcessorTheme.EmeraldMatrix()));
            presetsRow2.Add(CreatePresetButton("Amethyst", NormalProcessorTheme.AmethystNebula()));
            presetsRow2.Add(CreatePresetButton("Solar", NormalProcessorTheme.SolarFlare()));
            presetsRow2.Add(CreatePresetButton("Mocha", NormalProcessorTheme.CatppuccinMocha()));
            presetsGroup.Add(presetsRow2);
            container.Add(presetsGroup);

            var colorsGroup = new GroupBox("Custom Colors") { style = { marginBottom = 15 } };
            colorsGroup.AddToClassList("unity-group-box");

            var nameField = new TextField("Theme Name") { value = _editingTheme.ThemeName };
            nameField.RegisterValueChangedCallback(evt => _editingTheme.ThemeName = evt.newValue);
            colorsGroup.Add(nameField);

            colorsGroup.Add(CreateColorField("Background", _editingTheme.BackgroundColor, c => _editingTheme.BackgroundColor = c));
            colorsGroup.Add(CreateColorField("Panel Background", _editingTheme.PanelBackgroundColor, c => _editingTheme.PanelBackgroundColor = c));
            colorsGroup.Add(CreateColorField("Accent (Primary)", _editingTheme.AccentColor, c => _editingTheme.AccentColor = c));
            colorsGroup.Add(CreateColorField("Accent (Secondary)", _editingTheme.AccentAltColor, c => _editingTheme.AccentAltColor = c));
            colorsGroup.Add(CreateColorField("Button Color", _editingTheme.ButtonColor, c => _editingTheme.ButtonColor = c));
            colorsGroup.Add(CreateColorField("Button Hover", _editingTheme.ButtonHoverColor, c => _editingTheme.ButtonHoverColor = c));
            colorsGroup.Add(CreateColorField("Text Color", _editingTheme.TextColor, c => _editingTheme.TextColor = c));
            
            container.Add(colorsGroup);

            var actionRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginTop = 10 } };
            
            var applyBtn = new Button(() => {
                ThemeManager.ApplyTheme(_editingTheme);
            }) { text = "Apply Theme" };
            applyBtn.AddToClassList("glass-button");
            applyBtn.style.flexGrow = 1;
            applyBtn.style.marginRight = 5;
            actionRow.Add(applyBtn);
            
            container.Add(actionRow);

            var fileRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginTop = 10 } };

            var exportBtn = new Button(() => {
                string path = EditorUtility.SaveFilePanel("Export Theme", "", _editingTheme.ThemeName + ".json", "json");
                if (!string.IsNullOrEmpty(path)) {
                    ThemeManager.ExportTheme(path);
                }
            }) { text = "Export to Disk" };
            exportBtn.AddToClassList("glass-button");
            exportBtn.style.flexGrow = 1;
            exportBtn.style.marginRight = 5;

            var importBtn = new Button(() => {
                string path = EditorUtility.OpenFilePanel("Import Theme", "", "json");
                if (!string.IsNullOrEmpty(path)) {
                    ThemeManager.ImportTheme(path);
                    OnEnable(); // reload editor data
                    rootVisualElement.Clear();
                    CreateGUI(); // refresh UI
                }
            }) { text = "Import from Disk" };
            importBtn.AddToClassList("glass-button");
            importBtn.style.flexGrow = 1;

            fileRow.Add(exportBtn);
            fileRow.Add(importBtn);
            container.Add(fileRow);

            root.Add(container);
        }

        private Button CreatePresetButton(string text, NormalProcessorTheme themeData)
        {
            var btn = new Button(() => {
                _editingTheme = themeData;
                rootVisualElement.Clear();
                CreateGUI(); // Refresh the color fields
                ThemeManager.ApplyTheme(themeData);
            }) { text = text };
            btn.AddToClassList("glass-button");
            btn.style.flexGrow = 1;
            btn.style.marginLeft = 2;
            btn.style.marginRight = 2;
            return btn;
        }

        private ColorField CreateColorField(string label, Color value, System.Action<Color> onValueChanged)
        {
            var field = new ColorField(label) { value = value };
            field.RegisterValueChangedCallback(evt => onValueChanged(evt.newValue));
            return field;
        }
    }
}
