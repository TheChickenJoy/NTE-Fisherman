using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NTE_Fishing_Bot;

internal class ResolutionSelectWindow : Window
{
    private static readonly (string Label, int MonW, int MonH, int GameW, int GameH)[] Presets =
    {
        ("Windowed 1280×720  —  2560×1440 monitor", 2560, 1440, 1280, 720),
        ("Windowed 1280×720  —  1920×1080 monitor", 1920, 1080, 1280, 720),
        ("Windowed 1280×720  —  3840×2160 monitor", 3840, 2160, 1280, 720),
    };

    private const int BASE_GAME_W = 1280;
    private const int BASE_GAME_H =  720;
    private const int BASE_OFF_X  =  640;
    private const int BASE_OFF_Y  =  360;

    public int MonitorWidth  { get; private set; }
    public int MonitorHeight { get; private set; }
    public int GameWidth     { get; private set; }
    public int GameHeight    { get; private set; }

    public ResolutionSelectWindow(bool isDark)
    {
        Title  = "Pre-configured: Select Monitor";
        Width  = 500;
        Height = 320;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode  = ResizeMode.NoResize;
        WindowStyle = WindowStyle.ToolWindow;
        Resources.MergedDictionaries.Add(Theme.Styling);

        Background = isDark ? Theme.ColorAccent1 : Theme.WhiteColor;
        var fg    = isDark ? Theme.ColorAccent5 : Theme.BlackColor;
        var fgSub = isDark ? Theme.ColorAccent4 : new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66));
        var btnBg = isDark ? Theme.ColorAccent2 : Theme.ButtonDefaultBGColor;
        var btnSt = isDark ? Theme.DarkStyle    : Theme.LightStyle;

        var root = new Grid { Margin = new Thickness(20) };
        for (int i = 0; i < 5; i++)
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        int row = 0;
        void AddRow(UIElement el) { Grid.SetRow(el, row++); root.Children.Add(el); }

        AddRow(new TextBlock
        {
            Text = "Select your monitor resolution:",
            FontSize = 12, Foreground = fg,
            Margin = new Thickness(0, 0, 0, 8)
        });

        var combo = new ComboBox { Height = 28, Margin = new Thickness(0, 0, 0, 12) };
        foreach (var p in Presets) combo.Items.Add(p.Label);
        combo.SelectedIndex = 0;
        AddRow(combo);

        // Warning box
        var warnBorder = new Border
        {
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xE6, 0xA8, 0x17)),
            Background  = isDark
                ? new SolidColorBrush(Color.FromArgb(0x33, 0xE6, 0xA8, 0x17))
                : new SolidColorBrush(Color.FromArgb(0x22, 0xE6, 0xA8, 0x17)),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(10, 8, 10, 8),
            Margin  = new Thickness(0, 0, 0, 10)
        };
        warnBorder.Child = new TextBlock
        {
            Text = "⚠  After applying, you must manually set up Successful Cast Detect in the settings. " +
                   "Place it on your player's username — aim for the middle-most letter of the name if possible. " +
                   "Pre-configured does not auto-calibrate that detection area.",
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromRgb(0xE6, 0xA8, 0x17)),
            TextWrapping = TextWrapping.Wrap
        };
        AddRow(warnBorder);

        AddRow(new TextBlock
        {
            Text = "Make sure the game is set to Windowed mode at 1280×720 in the in-game display settings.",
            FontSize = 10, Foreground = fgSub,
            TextWrapping = TextWrapping.Wrap
        });

        row++; // spacer

        var btnRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        Grid.SetRow(btnRow, row);
        root.Children.Add(btnRow);

        var cancelBtn = new Button
        {
            Content = "Cancel", Width = 80, Height = 30,
            Margin = new Thickness(0, 0, 8, 0),
            Background = btnBg, Style = btnSt
        };
        cancelBtn.Click += (_, _) => DialogResult = false;

        var applyBtn = new Button
        {
            Content = "Apply", Width = 80, Height = 30,
            Background = btnBg, Style = btnSt
        };
        applyBtn.Click += (_, _) =>
        {
            var p = Presets[combo.SelectedIndex];
            MonitorWidth  = p.MonW;
            MonitorHeight = p.MonH;
            GameWidth     = p.GameW;
            GameHeight    = p.GameH;
            DialogResult = true;
        };

        btnRow.Children.Add(cancelBtn);
        btnRow.Children.Add(applyBtn);
        Content = root;
    }

    /// <summary>
    /// Scales a coordinate from the base setup (2560×1440 monitor, 1280×720 windowed game)
    /// to a different monitor/game size.
    /// </summary>
    public static (int x, int y) Scale(int baseX, int baseY, int monW, int monH, int gameW, int gameH)
    {
        double scaleX = gameW / (double)BASE_GAME_W;
        double scaleY = gameH / (double)BASE_GAME_H;
        int offX = (monW - gameW) / 2;
        int offY = (monH - gameH) / 2;
        return (
            (int)Math.Round((baseX - BASE_OFF_X) * scaleX + offX),
            (int)Math.Round((baseY - BASE_OFF_Y) * scaleY + offY)
        );
    }
}
