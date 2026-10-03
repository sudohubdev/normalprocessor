using System;
using UnityEngine;

namespace dev.sudohub.normalprocessor
{
    [Serializable]
    public partial class NormalProcessorTheme
    {
        public string ThemeName;
        
        // Colors
        public Color BackgroundColor;
        public Color PanelBackgroundColor;
        public Color AccentColor;
        public Color AccentAltColor;
        public Color ButtonColor;
        public Color ButtonHoverColor;
        public Color TextColor;
        
        // Static built-in themes
        public static NormalProcessorTheme DarkGlass()
        {
            return new NormalProcessorTheme()
            {
                ThemeName = "Dark Glass (Built-in)",
                BackgroundColor = ColorUtility.TryParseHtmlString("#1a1a2e", out var c1) ? c1 : Color.black,
                PanelBackgroundColor = new Color(30f/255f, 30f/255f, 45f/255f, 0.6f),
                AccentColor = ColorUtility.TryParseHtmlString("#3a86ff", out var c2) ? c2 : Color.blue,
                AccentAltColor = ColorUtility.TryParseHtmlString("#00e5ff", out var c3) ? c3 : Color.cyan,
                ButtonColor = new Color(33f/255f, 150f/255f, 243f/255f, 0.8f),
                ButtonHoverColor = new Color(66f/255f, 165f/255f, 245f/255f, 0.95f),
                TextColor = ColorUtility.TryParseHtmlString("#e0e0e0", out var c4) ? c4 : Color.white
            };
        }

        public static NormalProcessorTheme CrimsonCyber()
        {
            return new NormalProcessorTheme()
            {
                ThemeName = "Crimson Cyber (Built-in)",
                BackgroundColor = ColorUtility.TryParseHtmlString("#110000", out var c1) ? c1 : Color.black,
                PanelBackgroundColor = new Color(45f/255f, 10f/255f, 10f/255f, 0.7f),
                AccentColor = ColorUtility.TryParseHtmlString("#ff0a54", out var c2) ? c2 : Color.red,
                AccentAltColor = ColorUtility.TryParseHtmlString("#ff477e", out var c3) ? c3 : Color.magenta,
                ButtonColor = ColorUtility.TryParseHtmlString("#d90429", out var c4) ? c4 : Color.red,
                ButtonHoverColor = ColorUtility.TryParseHtmlString("#ef233c", out var c5) ? c5 : Color.red,
                TextColor = ColorUtility.TryParseHtmlString("#ffccd5", out var c6) ? c6 : Color.white
            };
        }

        public static NormalProcessorTheme DefaultUnityDark()
        {
            return new NormalProcessorTheme()
            {
                ThemeName = "Unity Legacy Dark (Built-in)",
                BackgroundColor = ColorUtility.TryParseHtmlString("#383838", out var c1) ? c1 : Color.gray,
                PanelBackgroundColor = ColorUtility.TryParseHtmlString("#383838", out var cp) ? cp : Color.gray,
                AccentColor = ColorUtility.TryParseHtmlString("#4f4f4f", out var c2) ? c2 : Color.gray,
                AccentAltColor = ColorUtility.TryParseHtmlString("#4f4f4f", out var c3) ? c3 : Color.gray,
                ButtonColor = ColorUtility.TryParseHtmlString("#585858", out var c4) ? c4 : Color.gray,
                ButtonHoverColor = ColorUtility.TryParseHtmlString("#686868", out var c5) ? c5 : Color.gray,
                TextColor = ColorUtility.TryParseHtmlString("#eeeeee", out var c6) ? c6 : Color.white
            };
        }
    }
}

namespace dev.sudohub.normalprocessor {
    public partial class NormalProcessorTheme {
        public static NormalProcessorTheme EmeraldMatrix() {
            return new NormalProcessorTheme() {
                ThemeName = "Emerald Matrix (Built-in)",
                BackgroundColor = ColorUtility.TryParseHtmlString("#051911", out var c1) ? c1 : Color.black,
                PanelBackgroundColor = new Color(5f/255f, 30f/255f, 20f/255f, 0.7f),
                AccentColor = ColorUtility.TryParseHtmlString("#00ff7f", out var c2) ? c2 : Color.green,
                AccentAltColor = ColorUtility.TryParseHtmlString("#70ffb8", out var c3) ? c3 : Color.green,
                ButtonColor = ColorUtility.TryParseHtmlString("#00b359", out var c4) ? c4 : Color.green,
                ButtonHoverColor = ColorUtility.TryParseHtmlString("#00e673", out var c5) ? c5 : Color.green,
                TextColor = ColorUtility.TryParseHtmlString("#d9ffeb", out var c6) ? c6 : Color.white
            };
        }
        
        public static NormalProcessorTheme AmethystNebula() {
            return new NormalProcessorTheme() {
                ThemeName = "Amethyst Nebula (Built-in)",
                BackgroundColor = ColorUtility.TryParseHtmlString("#150529", out var c1) ? c1 : Color.black,
                PanelBackgroundColor = new Color(30f/255f, 15f/255f, 50f/255f, 0.7f),
                AccentColor = ColorUtility.TryParseHtmlString("#b5179e", out var c2) ? c2 : Color.magenta,
                AccentAltColor = ColorUtility.TryParseHtmlString("#f72585", out var c3) ? c3 : Color.magenta,
                ButtonColor = ColorUtility.TryParseHtmlString("#7209b7", out var c4) ? c4 : Color.magenta,
                ButtonHoverColor = ColorUtility.TryParseHtmlString("#9a0ce8", out var c5) ? c5 : Color.magenta,
                TextColor = ColorUtility.TryParseHtmlString("#fcd5ff", out var c6) ? c6 : Color.white
            };
        }
        
        public static NormalProcessorTheme SolarFlare() {
            return new NormalProcessorTheme() {
                ThemeName = "Solar Flare (Built-in)",
                BackgroundColor = ColorUtility.TryParseHtmlString("#261005", out var c1) ? c1 : Color.black,
                PanelBackgroundColor = new Color(50f/255f, 25f/255f, 10f/255f, 0.7f),
                AccentColor = ColorUtility.TryParseHtmlString("#f77f00", out var c2) ? c2 : Color.red,
                AccentAltColor = ColorUtility.TryParseHtmlString("#fcbf49", out var c3) ? c3 : Color.yellow,
                ButtonColor = ColorUtility.TryParseHtmlString("#e85d04", out var c4) ? c4 : Color.red,
                ButtonHoverColor = ColorUtility.TryParseHtmlString("#f48c06", out var c5) ? c5 : Color.red,
                TextColor = ColorUtility.TryParseHtmlString("#ffedd9", out var c6) ? c6 : Color.white
            };
        }
    }
}


namespace dev.sudohub.normalprocessor {
    public partial class NormalProcessorTheme {
        public static NormalProcessorTheme CatppuccinMocha() {
            return new NormalProcessorTheme() {
                ThemeName = "Catppuccin Mocha (Built-in)",
                BackgroundColor = ColorUtility.TryParseHtmlString("#1e1e2e", out var c1) ? c1 : Color.black,
                PanelBackgroundColor = new Color(49f/255f, 50f/255f, 68f/255f, 0.7f),
                AccentColor = ColorUtility.TryParseHtmlString("#cba6f7", out var c2) ? c2 : Color.magenta,
                AccentAltColor = ColorUtility.TryParseHtmlString("#f5c2e7", out var c3) ? c3 : Color.magenta,
                ButtonColor = new Color(137f/255f, 180f/255f, 250f/255f, 0.7f),
                ButtonHoverColor = new Color(137f/255f, 180f/255f, 250f/255f, 0.95f),
                TextColor = ColorUtility.TryParseHtmlString("#cdd6f4", out var c6) ? c6 : Color.white
            };
        }
    }
}


namespace dev.sudohub.normalprocessor {
    public partial class NormalProcessorTheme {
        public static NormalProcessorTheme NordArctic() {
            return new NormalProcessorTheme() {
                ThemeName = "Nord Arctic (Built-in)",
                BackgroundColor = ColorUtility.TryParseHtmlString("#2e3440", out var c1) ? c1 : Color.black,
                PanelBackgroundColor = new Color(59f/255f, 66f/255f, 82f/255f, 0.7f),
                AccentColor = ColorUtility.TryParseHtmlString("#88c0d0", out var c2) ? c2 : Color.cyan,
                AccentAltColor = ColorUtility.TryParseHtmlString("#81a1c1", out var c3) ? c3 : Color.blue,
                ButtonColor = ColorUtility.TryParseHtmlString("#5e81ac", out var c4) ? c4 : Color.blue,
                ButtonHoverColor = ColorUtility.TryParseHtmlString("#81a1c1", out var c5) ? c5 : Color.blue,
                TextColor = ColorUtility.TryParseHtmlString("#d8dee9", out var c6) ? c6 : Color.white
            };
        }
    }
}

