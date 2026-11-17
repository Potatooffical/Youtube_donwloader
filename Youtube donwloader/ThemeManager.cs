using System.Windows;
using System.Windows.Media;

namespace Youtube_donwloader
{
    internal static class ThemeManager
    {
        private static readonly Dictionary<string, IReadOnlyDictionary<string, string>> Palettes = new()
        {
            ["Newest"] = new Dictionary<string, string>
            {
                ["WindowStart"] = "#2A1111", ["WindowMiddle"] = "#100909", ["WindowEnd"] = "#100909",
                ["Surface"] = "#170F0F", ["Input"] = "#282020", ["Accent"] = "#FF493F", ["AccentEnd"] = "#FF8228",
                ["Text"] = "#F7F1F1", ["Muted"] = "#A99D9D", ["Border"] = "#342727",
                ["Danger"] = "#FF493F", ["DangerPressed"] = "#D93632", ["Chrome"] = "#C4B8B8",
                ["Button"] = "#292121", ["Hover"] = "#3B2828", ["Pressed"] = "#4A2927",
                ["Quality"] = "#292121", ["QualityBorder"] = "#403333", ["QualitySelected"] = "#421918",
                ["Popup"] = "#201414", ["PopupBorder"] = "#423030", ["Selected"] = "#4A2122", ["Tooltip"] = "#241818"
            },
            ["Original"] = new Dictionary<string, string>
            {
                ["WindowStart"] = "#202234", ["WindowMiddle"] = "#14151B", ["WindowEnd"] = "#14151B",
                ["Surface"] = "#1D1F28", ["Input"] = "#171921", ["Accent"] = "#6975F5", ["AccentEnd"] = "#7D87FF",
                ["Text"] = "#F4F5FA", ["Muted"] = "#9296A8", ["Border"] = "#2B2E3A",
                ["Danger"] = "#E45B68", ["DangerPressed"] = "#C74654", ["Chrome"] = "#C4C7D2",
                ["Button"] = "#292C37", ["Hover"] = "#343746", ["Pressed"] = "#383C4A",
                ["Quality"] = "#20222C", ["QualityBorder"] = "#343746", ["QualitySelected"] = "#343B70",
                ["Popup"] = "#11131A", ["PopupBorder"] = "#343746", ["Selected"] = "#343B70", ["Tooltip"] = "#171921"
            },
            ["Ocean"] = new Dictionary<string, string>
            {
                ["WindowStart"] = "#DFF5F4", ["WindowMiddle"] = "#EDF8FA", ["WindowEnd"] = "#E7F2FB",
                ["Surface"] = "#FFFFFF", ["Input"] = "#E8F4F6", ["Accent"] = "#168A9E", ["AccentEnd"] = "#35B5C5",
                ["Text"] = "#18363D", ["Muted"] = "#5C7A80", ["Border"] = "#C8E1E5",
                ["Danger"] = "#D84F61", ["DangerPressed"] = "#B93C50", ["Chrome"] = "#315A62",
                ["Button"] = "#E5F1F3", ["Hover"] = "#D6EAED", ["Pressed"] = "#C8E2E6",
                ["Quality"] = "#EFF8F9", ["QualityBorder"] = "#C8E1E5", ["QualitySelected"] = "#D4F0F1",
                ["Popup"] = "#FFFFFF", ["PopupBorder"] = "#C8E1E5", ["Selected"] = "#D4F0F1", ["Tooltip"] = "#F8FEFF"
            },
            ["Light"] = new Dictionary<string, string>
            {
                ["WindowStart"] = "#E7EAF0", ["WindowMiddle"] = "#F3F4F7", ["WindowEnd"] = "#F3F4F7",
                ["Surface"] = "#FFFFFF", ["Input"] = "#F1F2F5", ["Accent"] = "#E94B58", ["AccentEnd"] = "#F28A42",
                ["Text"] = "#24232A", ["Muted"] = "#706F78", ["Border"] = "#D8DAE1",
                ["Danger"] = "#D83A4A", ["DangerPressed"] = "#B82E3D", ["Chrome"] = "#474650",
                ["Button"] = "#ECEEF3", ["Hover"] = "#DFE2EA", ["Pressed"] = "#D4D7E0",
                ["Quality"] = "#F1F2F5", ["QualityBorder"] = "#D6D8E0", ["QualitySelected"] = "#FBE1E5",
                ["Popup"] = "#FFFFFF", ["PopupBorder"] = "#D7D9E1", ["Selected"] = "#FBE1E5", ["Tooltip"] = "#FFFFFF"
            },
            ["Pastel"] = new Dictionary<string, string>
            {
                ["WindowStart"] = "#F4ECF8", ["WindowMiddle"] = "#FCF5F1", ["WindowEnd"] = "#F8EFF6",
                ["Surface"] = "#FFFAFF", ["Input"] = "#F3EBF6", ["Accent"] = "#B66AD6", ["AccentEnd"] = "#F08BAA",
                ["Text"] = "#392B43", ["Muted"] = "#8D7D94", ["Border"] = "#E2D2E8",
                ["Danger"] = "#DB6C82", ["DangerPressed"] = "#C45470", ["Chrome"] = "#65566C",
                ["Button"] = "#F2E8F5", ["Hover"] = "#EBDCF0", ["Pressed"] = "#E3CFEA",
                ["Quality"] = "#F7EFF8", ["QualityBorder"] = "#E4D5EA", ["QualitySelected"] = "#F1DDF3",
                ["Popup"] = "#FFFAFF", ["PopupBorder"] = "#E2D2E8", ["Selected"] = "#F1DDF3", ["Tooltip"] = "#FFF8FF"
            }
        };

        private static readonly (string ResourceKey, string PaletteKey)[] SolidBrushes =
        {
            ("SurfaceBackground", "Surface"),
            ("InputBackground", "Input"),
            ("AccentBrush", "Accent"),
            ("AccentHoverBrush", "AccentEnd"),
            ("PrimaryTextBrush", "Text"),
            ("MutedTextBrush", "Muted"),
            ("BorderBrush", "Border"),
            ("DangerBrush", "Danger"),
            ("DangerPressedBrush", "DangerPressed"),
            ("ChromeForegroundBrush", "Chrome"),
            ("ButtonBackgroundBrush", "Button"),
            ("ButtonHoverBrush", "Hover"),
            ("ButtonPressedBrush", "Pressed"),
            ("QualityOptionBrush", "Quality"),
            ("QualityBorderBrush", "QualityBorder"),
            ("QualitySelectedBrush", "QualitySelected"),
            ("PopupBackgroundBrush", "Popup"),
            ("PopupBorderBrush", "PopupBorder"),
            ("SelectedItemBrush", "Selected"),
            ("TooltipBackgroundBrush", "Tooltip")
        };

        public static void Apply(string themeName)
        {
            if (!Palettes.TryGetValue(themeName, out var palette))
            {
                return;
            }

            var resources = Application.Current.Resources;
            foreach (var (resourceKey, paletteKey) in SolidBrushes)
            {
                resources[resourceKey] = new SolidColorBrush(ParseColor(palette[paletteKey]));
            }

            resources["WindowBackground"] = new LinearGradientBrush(
                ParseColor(palette["WindowStart"]),
                ParseColor(palette["WindowEnd"]),
                new Point(0, 0),
                new Point(1, 1))
            {
                GradientStops = new GradientStopCollection
                {
                    new(ParseColor(palette["WindowStart"]), 0),
                    new(ParseColor(palette["WindowMiddle"]), 0.5),
                    new(ParseColor(palette["WindowEnd"]), 1)
                }
            };

            resources["AccentGradientBrush"] = new LinearGradientBrush(
                ParseColor(palette["Accent"]),
                ParseColor(palette["AccentEnd"]),
                new Point(0, 0),
                new Point(1, 0));
        }

        private static Color ParseColor(string value) => (Color)ColorConverter.ConvertFromString(value)!;
    }
}
