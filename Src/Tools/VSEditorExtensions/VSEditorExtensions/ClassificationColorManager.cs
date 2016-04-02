using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Formatting;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;
using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;

namespace VSEditorExtensions
{
    public class FontStyle
    {
        public readonly Color? Foreground;
        public readonly Color? Background;
        public readonly TextDecorationCollection Decorations;

        public FontStyle(Color? foreground = null, Color? background = null, TextDecorationCollection decorations = null)
        {
            Foreground = foreground;
            Background = background;
            Decorations = decorations;
        }
    }

    [Export]
    public class ClassificationColorManager
    {
        static readonly Dictionary<string, FontStyle> LightAndBlueColors = new Dictionary<string, FontStyle>
            {
                { ClassificationTypes.PKeyword, new FontStyle(Color.FromRgb(0, 0, 255)) },
                { ClassificationTypes.PIdentifier, new FontStyle(Color.FromRgb(0, 0, 0)) },
                { ClassificationTypes.PComment, new FontStyle(Color.FromRgb(0, 128, 0)) },
                { ClassificationTypes.PString, new FontStyle(Color.FromRgb(163, 21, 21)) }
            };

        static readonly Dictionary<string, FontStyle> DarkColors = new Dictionary<string, FontStyle>
            {
                { ClassificationTypes.PKeyword, new FontStyle(Color.FromRgb(86, 156, 214)) },
                { ClassificationTypes.PIdentifier, new FontStyle(Color.FromRgb(220, 220, 220)) },
                { ClassificationTypes.PComment, new FontStyle(Color.FromRgb(87, 166, 74)) },
                { ClassificationTypes.PString, new FontStyle(Color.FromRgb(214, 157, 133)) }
            };

        private ThemeManager _themeManager;
        private IClassificationFormatMapService _classificationFormatMapService;
        private IClassificationTypeRegistryService _classificationTypeRegistry;

        private Guid _currentTheme;

        [ImportingConstructor]
        public ClassificationColorManager(
            ThemeManager themeManager,
            IClassificationFormatMapService classificationFormatMapService,
            IClassificationTypeRegistryService classificationTypeRegistry)
        {
            _themeManager = themeManager;
            _classificationFormatMapService = classificationFormatMapService;
            _classificationTypeRegistry = classificationTypeRegistry;

            // Theme changed event may fire even though the same theme is still in use.
            // We save a current theme and skip color updates in these cases. 
            _currentTheme = _themeManager.GetCurrentTheme();
        }

        public FontStyle GetDefaultColors(string category)
        {
            bool success;
            FontStyle color;
            if (_currentTheme == KnownColorThemes.Dark)
            {
                color = new FontStyle(Color.FromRgb(220, 220, 220), Color.FromRgb(30, 30, 30));
                success = DarkColors.TryGetValue(category, out color);
                return color;
            }

            // KnownColorThemes.Light
            // KnownColorThemes.Blue
            // KnownColorThemes.Default
            // KnownColorThemes.HighContrast // hmmm, what about this one?
            color = new FontStyle(Colors.Black, Colors.White);
            success = LightAndBlueColors.TryGetValue(category, out color);
            return color;
        }

        public void UpdateColors()
        {
            var newTheme = _themeManager.GetCurrentTheme();

            if (newTheme != KnownColorThemes.Debug && newTheme != _currentTheme)
            {
                _currentTheme = newTheme;

                var colors = newTheme == KnownColorThemes.Dark ? DarkColors : LightAndBlueColors;
                var formatMap = _classificationFormatMapService.GetClassificationFormatMap(category: "text");

                try
                {
                    formatMap.BeginBatchUpdate();
                    foreach (var pair in colors)
                    {
                        string type = pair.Key;
                        FontStyle color = pair.Value;

                        var classificationType = _classificationTypeRegistry.GetClassificationType(type);
                        var oldProp = formatMap.GetTextProperties(classificationType);

                        var foregroundBrush =
                            color.Foreground == null
                                ? null
                                : new SolidColorBrush(color.Foreground.Value);

                        var backgroundBrush =
                            color.Background == null
                                ? null
                                : new SolidColorBrush(color.Background.Value);

                        var newProp = TextFormattingRunProperties.CreateTextFormattingRunProperties(
                            foregroundBrush, backgroundBrush, oldProp.Typeface, null, null, oldProp.TextDecorations,
                            oldProp.TextEffects, oldProp.CultureInfo);

                        formatMap.SetTextProperties(classificationType, newProp);
                    }
                }
                finally
                {
                    formatMap.EndBatchUpdate();
                }
            }
        }
    }


    [Export]
    public class ThemeManager
    {
        IServiceProvider _serviceProvider;

        [ImportingConstructor]
        ThemeManager([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Guid GetCurrentTheme()
        {
            dynamic colorThemeService = _serviceProvider.GetService(typeof(SVsColorThemeService));
            Guid id = colorThemeService.CurrentTheme.ThemeId;
            return id; // should be one of the KnownColorThemes
        }
    }

    [Guid("0D915B59-2ED7-472A-9DE8-9161737EA1C5")]
    interface SVsColorThemeService
    {
    }

}

