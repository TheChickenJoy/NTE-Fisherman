using System;
using System.ComponentModel;
using System.IO;
using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Config.Net;
using SharpDX.DXGI;
using WindowsHook;
using NTE_Fishing_Bot.Addon.DiscordInteractive;

namespace NTE_Fishing_Bot;

public partial class MainWindow : Window, IComponentConnector
{
	private Button defaultButton = new Button();

	private DispatcherTimer timer;

	private System.Drawing.Point previousPosition = new System.Drawing.Point(-1, 1);

	private System.Drawing.Point mousePosition = new System.Drawing.Point(-1, 1);

	private bool inEyeDropMode;

	private bool inCoordSelectMode;

	private bool isDarkMode;

	private bool sideMenuExpanded = true;

	private Button spDefaultButton = new Button();
	private TextBlock spDefaultLabel = new TextBlock();
	private Button spActiveButton;
	private TextBlock spActiveLabel;
	private string spSavedHotkeyText = "";
	private int _spUpdateDiscordUser;
	private int _spUpdateDiscordUrl;
	private bool spGPUListInitialized = false;

	private int _fishCaughtCount = 0;

	private System.Windows.Media.Color activeColor;

	private Button activeButton;

	private TextBlock activeLabel = new TextBlock();

	private TextBlock activeCoordsLabel = new TextBlock();

	private string backupButtonText = string.Empty;

	private IAppSettings settings;

	private IKeyboardMouseEvents m_GlobalHook;

	private FishingThread fishBot;

	private Thread fishBotThread;

	private Lens_Form lens_form;

	public MainWindow()
	{
		InitializeComponent();
		m_GlobalHook = Hook.GlobalEvents();
		m_GlobalHook.MouseMoveExt += GlobalHookMouseMove;
		m_GlobalHook.MouseClick += GlobalHookMouseLeftClick;
		settings = new ConfigurationBuilder<IAppSettings>().UseJsonFile("settings.json").Build();
		activeButton = defaultButton;
		spActiveButton = spDefaultButton;
		spActiveLabel = spDefaultLabel;
		ReadSettings();
		InitTheme(isDarkMode);
		if (timer == null)
		{
			timer = new DispatcherTimer();
			timer.Interval = new TimeSpan(0, 0, 0, 0, 50);
			timer.Tick += timer_Tick;
		}
		base.Closing += MainWindow_Closing;
		BotLogger.EntryAdded += OnBotLogEntry;
		BotLogger.LastEntryUpdated += OnBotLogEntryUpdated;
		fishBot = new FishingThread(settings, LeftBox, RightBox, cursor, bar, StatusLabel, middleBarImage, cursorImage, castReadyImage, FishStaminaIndicator, PlayerStaminaIndicator, CastBtnIndicator, XpBarIndicator, DailyRewardIndicator, EscBox, FBox, null);
		fishBot.OnFishCaught = OnFishCaughtCallback;
		fishBotThread = new Thread(fishBot.Start);
		if (settings.IsFirstRun == 1)
			WelcomePage.Visibility = Visibility.Visible;
		bool isAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
		if (!isAdmin)
			AdminWarningBanner.Visibility = Visibility.Visible;
	}

	private void GlobalHookMouseLeftClick(object sender, MouseEventArgs e)
	{
		if (inEyeDropMode || inCoordSelectMode)
		{
			WriteSettings();
			timer.Stop();
			ResetTempVars();
			lens_form.Dispose();
		}
	}

	private void ResetTempVars()
	{
		activeLabel.Text = backupButtonText;
		inEyeDropMode = false;
		inCoordSelectMode = false;
		activeButton = defaultButton;
		activeLabel = new TextBlock();
		activeCoordsLabel = new TextBlock();
		backupButtonText = string.Empty;
	}

	private void GlobalHookMouseMove(object sender, MouseEventExtArgs e)
	{
		if (inEyeDropMode)
		{
			mousePosition = new System.Drawing.Point(e.Location.X, e.Location.Y);
		}
		if (inCoordSelectMode)
		{
			activeCoordsLabel.Text = "X: " + e.Location.X + "\nY: " + e.Location.Y;
		}
	}

	private void OnBotLogEntry(object sender, string entry)
	{
		LogListBox.Dispatcher.InvokeAsync(() =>
		{
			if (entry == null)
			{
				LogListBox.Items.Clear();
				LogCountLabel.Text = "0 entries";
				return;
			}
			if (LogListBox.Items.Count >= 1000)
				LogListBox.Items.RemoveAt(0);
			LogListBox.Items.Add(entry);
			LogListBox.ScrollIntoView(LogListBox.Items[LogListBox.Items.Count - 1]);
			LogCountLabel.Text = $"{LogListBox.Items.Count} entries";
		});
	}

	private void OnBotLogEntryUpdated(object sender, string entry)
	{
		LogListBox.Dispatcher.InvokeAsync(() =>
		{
			if (LogListBox.Items.Count > 0)
			{
				LogListBox.Items[LogListBox.Items.Count - 1] = entry;
				LogListBox.ScrollIntoView(LogListBox.Items[LogListBox.Items.Count - 1]);
			}
		});
	}

	private void MainWindow_Closing(object sender, CancelEventArgs e)
	{
		BotLogger.EntryAdded -= OnBotLogEntry;
		BotLogger.LastEntryUpdated -= OnBotLogEntryUpdated;
		if (SettingsPage.Visibility == Visibility.Visible)
			m_GlobalHook.KeyUp -= SpGlobalHookKeyUp;
		timer.Stop();
		fishBot.Stop();
		inEyeDropMode = false;
		inCoordSelectMode = false;
		activeButton = new Button();
		activeLabel = new TextBlock();
		activeCoordsLabel = new TextBlock();
	}

	private void timer_Tick(object sender, EventArgs e)
	{
		if (previousPosition != mousePosition && inEyeDropMode)
		{
			Bitmap bmp = new Bitmap(1, 1);
			using (Graphics g = Graphics.FromImage(bmp))
			{
				g.CopyFromScreen(mousePosition, new System.Drawing.Point(0, 0), new System.Drawing.Size(1, 1));
			}
			System.Drawing.Color color = bmp.GetPixel(0, 0);
			activeColor = System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
			activeButton.Background = new SolidColorBrush(activeColor);
			System.Drawing.Color invertedColor = System.Drawing.Color.FromArgb(color.ToArgb() ^ 0xFFFFFF);
			activeLabel.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(invertedColor.A, invertedColor.R, invertedColor.G, invertedColor.B));
		}
		previousPosition = mousePosition;
	}

	private void FishStaminaColorBtn_Click(object sender, RoutedEventArgs e)
	{
		if (activeButton == defaultButton)
		{
			HandleButtonClick(FishStaminaColorBtn, FishStaminaColorLabel, "Press Left click to select\nColor and bottom most point", FishStaminaCoords);
		}
	}

	private void MiddleBarColorBtn_Click(object sender, RoutedEventArgs e)
	{
		if (activeButton == defaultButton)
		{
			HandleButtonClick(MiddleBarColorBtn, MiddleBarColorLabel, "Press Left click\nto select Color");
		}
	}

	private void CursorColorBtn_Click(object sender, RoutedEventArgs e)
	{
		if (activeButton == defaultButton)
		{
			HandleButtonClick(CursorColorBtn, CursorColorLabel, "Press Left click\nto select Color");
		}
	}

	private void PlayerStaminaColorBtn_Click(object sender, RoutedEventArgs e)
	{
		if (activeButton == defaultButton)
		{
			HandleButtonClick(PlayerStaminaColorBtn, PlayerStaminaColorLabel, "Press Left click to select\nColor and bottom most point", PlayerStaminaCoords);
		}
	}

	private void UpperLeftBtn_Click(object sender, RoutedEventArgs e)
	{
		if (activeButton == defaultButton)
		{
			HandleButtonClick(UpperLeftBtn, UpperLeftLabel, "Press Left click\nto specify coords", UpperLeftCoords);
		}
	}

	private void LowerRightBtn_Click(object sender, RoutedEventArgs e)
	{
		if (activeButton == defaultButton)
		{
			HandleButtonClick(LowerRightBtn, LowerRightLabel, "Press Left click\nto specify coords", LowerRightCoords);
		}
	}

	private void CastBtnBtn_Click(object sender, RoutedEventArgs e)
	{
		if (activeButton == defaultButton)
		{
			HandleButtonClick(CastBtnBtn, CastBtnLabel, "Press Left click to select\nColor and point", CastBtnCoords);
		}
	}

	private void XpBarColorBtn_Click(object sender, RoutedEventArgs e)
	{
		if (activeButton == defaultButton)
		{
			HandleButtonClick(XpBarColorBtn, XpBarColorLabel, "Press Left click to select\nColor and point", XpBarCoords);
		}
	}

	private void DailyRewardBtn_Click(object sender, RoutedEventArgs e)
	{
		if (activeButton == defaultButton)
		{
			HandleButtonClick(DailyRewardBtn, DailyRewardLabel, "Press Left click to select\nColor and point", DailyRewardCoords);
		}
	}

	private void AlwaysOnTopBtn_Click(object sender, RoutedEventArgs e)
	{
		settings.IsAlwaysOnTop = settings.IsAlwaysOnTop == 0 ? 1 : 0;
		Topmost = settings.IsAlwaysOnTop == 1;
		UpdateToggleIconColors(isDarkMode);
	}

	private void HamburgerBtn_Click(object sender, RoutedEventArgs e)
	{
		sideMenuExpanded = !sideMenuExpanded;
		SideMenuBorder.Width = sideMenuExpanded ? 155 : 44;
		var vis = sideMenuExpanded ? Visibility.Visible : Visibility.Collapsed;
		CloseMenuLabel.Visibility = vis;
		SideSettingLabel.Visibility = vis;
		SideThemeLabel.Visibility = vis;
		SideAlwaysOnTopLabel.Visibility = vis;
		BackgroundInputLabel.Visibility = vis;
		SideFaqLabel.Visibility = vis;
		SideLogLabel.Visibility = vis;
		SideKofiLabel.Visibility = vis;
	}

	private void FaqBtn_Click(object sender, RoutedEventArgs e)
	{
		if (FaqPage.Visibility == Visibility.Visible)
			FaqPage.Visibility = Visibility.Collapsed;
		else
		{
			SettingsPage.Visibility = Visibility.Collapsed;
			LogPage.Visibility = Visibility.Collapsed;
			FaqPage.Visibility = Visibility.Visible;
		}
	}

	private void LogBtn_Click(object sender, RoutedEventArgs e)
	{
		if (LogPage.Visibility == Visibility.Visible)
			LogPage.Visibility = Visibility.Collapsed;
		else
		{
			SettingsPage.Visibility = Visibility.Collapsed;
			FaqPage.Visibility = Visibility.Collapsed;
			LogPage.Visibility = Visibility.Visible;
		}
	}

	private void LogCloseBtn_Click(object sender, RoutedEventArgs e)
	{
		LogPage.Visibility = Visibility.Collapsed;
	}

	private void LogClearBtn_Click(object sender, RoutedEventArgs e)
	{
		BotLogger.Clear();
	}

	private void LogExportBtn_Click(object sender, RoutedEventArgs e)
	{
		var dlg = new SaveFileDialog
		{
			FileName = $"NTE_FishBot_Log_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}",
			DefaultExt = ".txt",
			Filter = "Text file (.txt)|*.txt"
		};
		if (dlg.ShowDialog() != true) return;
		var entries = LogListBox.Items.Cast<string>();
		File.WriteAllLines(dlg.FileName, entries);
	}

	private void OnFishCaughtCallback()
	{
		Dispatcher.InvokeAsync(() =>
		{
			_fishCaughtCount++;
			FishCaughtLabel.Text = $"🎣  Caught: {_fishCaughtCount}";
		});
	}

	private void FishCaughtResetBtn_Click(object sender, RoutedEventArgs e)
	{
		_fishCaughtCount = 0;
		FishCaughtLabel.Text = "🎣  Caught: 0";
	}

	private void FaqCloseBtn_Click(object sender, RoutedEventArgs e)
	{
		FaqPage.Visibility = Visibility.Collapsed;
	}

	private void FaqWelcomeBtn_Click(object sender, RoutedEventArgs e)
	{
		WelcomePage.Visibility = Visibility.Visible;
	}

	private void FaqVideoBtn_Click(object sender, RoutedEventArgs e)
	{
		Process.Start(new ProcessStartInfo { FileName = "https://www.youtube.com/watch?v=pxeqgBSCB0U", UseShellExecute = true });
	}

	private void DismissWelcomePage()
	{
		settings.IsFirstRun = 0;
		WelcomePage.Visibility = Visibility.Collapsed;
	}

	private void WelcomeCloseBtn_Click(object sender, RoutedEventArgs e) => DismissWelcomePage();
	private void WelcomeGotItBtn_Click(object sender, RoutedEventArgs e) => DismissWelcomePage();
	private void WelcomeFaqBtn_Click(object sender, RoutedEventArgs e)
	{
		DismissWelcomePage();
		SettingsPage.Visibility = Visibility.Collapsed;
		LogPage.Visibility = Visibility.Collapsed;
		FaqPage.Visibility = Visibility.Visible;
	}

	private void SpCloseBtn_Click(object sender, RoutedEventArgs e)
	{
		CloseSettingsPage();
	}

	private void OpenSettingsPage()
	{
		if (!spGPUListInitialized)
		{
			var factory = new Factory1();
			foreach (var a in factory.Adapters.Select(a => new { name = a.Description.Description, count = a.GetOutputCount() }))
				SpGPUList.Items.Add(a.name + " (" + a.count + ")");
			SpGPUList.Items.Add("--------");
			foreach (var a in factory.Adapters1.Select(a => new { name = a.Description.Description, count = a.GetOutputCount() }))
				SpGPUList.Items.Add(a.name + " (" + a.count + ")");
			spGPUListInitialized = true;
		}
		_spUpdateDiscordUser = 0;
		_spUpdateDiscordUrl = 0;
		InitSettingsTheme(isDarkMode);
		m_GlobalHook.KeyUp += SpGlobalHookKeyUp;
		FaqPage.Visibility = Visibility.Collapsed;
		LogPage.Visibility = Visibility.Collapsed;
		SettingsPage.Visibility = Visibility.Visible;
	}

	private void CloseSettingsPage()
	{
		m_GlobalHook.KeyUp -= SpGlobalHookKeyUp;
		if (!string.IsNullOrEmpty(settings.DiscordHookUrl) && !string.IsNullOrEmpty(settings.DiscordUserId)
			&& _spUpdateDiscordUrl > 2 && _spUpdateDiscordUser > 2)
		{
			try
			{
				var ds = new DiscordService(settings.DiscordHookUrl, settings.DiscordUserId);
				var task = ds.BuildGenericNotification("Discord Integration is successfully set up.");
				task.Wait();
				ds.SendMessage(task.Result).Wait();
			}
			catch (ArgumentException ex)
			{
				MessageBox.Show(ex.Message, "Discord Hook URL invalid", MessageBoxButton.OK, MessageBoxImage.Hand);
			}
		}
		SettingsPage.Visibility = Visibility.Collapsed;
		ReadSettings();
	}

	private void SpGlobalHookKeyUp(object sender, WindowsHook.KeyEventArgs e)
	{
		if (spActiveButton != spDefaultButton)
		{
			spActiveLabel.Text = KeycodeHelper.KeycodeToString(e.KeyValue);
			SpWriteSettings(e.KeyValue);
			SpResetHotkeyButtons();
			SpResetHotkeyLabels(spActiveLabel, resetText: false);
		}
	}

	private void SpWriteSettings(int keyCode)
	{
		switch (spActiveButton.Name)
		{
		case "SpMoveLeftBtn":  settings.KeyCode_MoveLeft = keyCode; break;
		case "SpMoveRightBtn": settings.KeyCode_MoveRight = keyCode; break;
		case "SpReelInBtn":    settings.KeyCode_FishCapture = keyCode; break;
		case "SpDismissBtn":   settings.KeyCode_DismissFishDialogue = keyCode; break;
		}
	}

	private void SpHandleHotkeyButtonClick(Button btn, TextBlock label)
	{
		spActiveButton = btn;
		spActiveLabel = label;
		SpDisableHotkeyButtons(btn);
		spSavedHotkeyText = label.Text;
		label.FontSize = 11.0;
		label.Text = "Press a key";
	}

	private void SpMoveLeftBtn_Click(object sender, RoutedEventArgs e)
	{
		if (spActiveButton == spDefaultButton) SpHandleHotkeyButtonClick(SpMoveLeftBtn, SpMoveLeftLabel);
		else { SpResetHotkeyButtons(); SpResetHotkeyLabels(SpMoveLeftLabel); }
	}

	private void SpMoveRightBtn_Click(object sender, RoutedEventArgs e)
	{
		if (spActiveButton == spDefaultButton) SpHandleHotkeyButtonClick(SpMoveRightBtn, SpMoveRightLabel);
		else { SpResetHotkeyButtons(); SpResetHotkeyLabels(SpMoveRightLabel); }
	}

	private void SpReelInBtn_Click(object sender, RoutedEventArgs e)
	{
		if (spActiveButton == spDefaultButton) SpHandleHotkeyButtonClick(SpReelInBtn, SpReelInLabel);
		else { SpResetHotkeyButtons(); SpResetHotkeyLabels(SpReelInLabel); }
	}

	private void SpDismissBtn_Click(object sender, RoutedEventArgs e)
	{
		if (spActiveButton == spDefaultButton) SpHandleHotkeyButtonClick(SpDismissBtn, SpDismissLabel);
		else { SpResetHotkeyButtons(); SpResetHotkeyLabels(SpDismissLabel); }
	}

	private void SpDisableHotkeyButtons(Button clicked)
	{
		SpMoveLeftBtn.IsEnabled  = SpMoveLeftBtn.Equals(clicked);
		SpMoveRightBtn.IsEnabled = SpMoveRightBtn.Equals(clicked);
		SpReelInBtn.IsEnabled    = SpReelInBtn.Equals(clicked);
		SpDismissBtn.IsEnabled   = SpDismissBtn.Equals(clicked);
	}

	private void SpResetHotkeyButtons()
	{
		SpMoveLeftBtn.IsEnabled = SpMoveRightBtn.IsEnabled = SpReelInBtn.IsEnabled = SpDismissBtn.IsEnabled = true;
		spActiveButton = spDefaultButton;
	}

	private void SpResetHotkeyLabels(TextBlock label, bool resetText = true)
	{
		label.FontSize = 22.0;
		if (resetText) label.Text = spSavedHotkeyText;
		spSavedHotkeyText = "";
		spActiveLabel = spDefaultLabel;
	}

	private void SpPositiveNumbersOnlyValidation(object sender, System.Windows.Input.TextCompositionEventArgs e)
	{
		e.Handled = !int.TryParse(((TextBox)sender).Text + e.Text, out var i) || i < 0;
	}

	private void SpDelayTextBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		SpSaveSettings(((TextBox)sender).Name, ((TextBox)sender).Text);
	}

	private void SpSaveSettings(string name, string value)
	{
		int v = int.TryParse(value, out v) ? v : 0;
		switch (name)
		{
		case "SpRestartDelayTextBox":        settings.Delay_Restart = v; break;
		case "SpLagCompensationDelayTextBox": settings.Delay_LagCompensation = v; break;
		case "SpDimissDelayTextBox":         settings.Delay_DismissFishCaptureDialogue = v; break;
		case "SpFishCaptureDelayTextBox":    settings.Delay_FishCapture = v; break;
		case "SpPostCatchDelayTextBox":      settings.Delay_PostCatch = v; break;
		case "SpAfterClickDelayTextBox":     settings.Delay_AfterClick = v; break;
		case "SpDiscordUserIdTextBox":
			if (_spUpdateDiscordUser > 1) settings.DiscordUserId = value;
			_spUpdateDiscordUser++;
			break;
		case "SpDiscordWebHookTextBox":
			if (_spUpdateDiscordUrl > 1) settings.DiscordHookUrl = value;
			_spUpdateDiscordUrl++;
			break;
		}
	}

	private void SpResetBtn_Click(object sender, RoutedEventArgs e)
	{
		settings.KeyCode_MoveLeft = 65;
		settings.KeyCode_MoveRight = 68;
		settings.KeyCode_FishCapture = 70;
		settings.KeyCode_DismissFishDialogue = 27;
		settings.Delay_LagCompensation = 5000;
		settings.Delay_FishCapture = 3000;
		settings.Delay_DismissFishCaptureDialogue = 4000;
		settings.Delay_Restart = 2000;
		settings.DiscordHookUrl = "";
		settings.DiscordUserId = "";
		settings.Delay_PostCatch = 500;
		settings.Delay_AfterClick = 1000;
		InitSettingsTheme(isDarkMode);
	}

	private void SpGPUList_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		settings.DefaultAdapter = SpGPUList.SelectedIndex;
	}

	private TransformedBitmap RotateImage(ImageSource src, int degrees)
	{
		var tb = new TransformedBitmap();
		tb.BeginInit();
		tb.Source = (BitmapImage)src;
		tb.Transform = new RotateTransform(degrees);
		tb.EndInit();
		return tb;
	}

	private void InitSettingsTheme(bool dark)
	{
		var fg  = dark ? Theme.ColorAccent4 : Theme.BlackColor;
		var bg  = dark ? Theme.ColorAccent2 : Theme.WhiteColor;
		SettingsPageBorder.Background  = dark ? Theme.ColorAccent1 : Theme.WhiteColor;
		SettingsPageBorder.BorderBrush = dark ? Theme.ColorAccent2 : Theme.GBoxDefaultBorderColor;
		SettingsTitleLabel.Foreground  = dark ? Theme.ColorAccent5 : Theme.BlackColor;
		SpButtonsGBox.Foreground  = fg;
		SpDelayGBox.Foreground    = fg;
		SpDiscordGBox.Foreground  = fg;
		SpMoveLeftLabel.Text  = KeycodeHelper.KeycodeToString(settings.KeyCode_MoveLeft);
		SpMoveRightLabel.Text = KeycodeHelper.KeycodeToString(settings.KeyCode_MoveRight);
		SpReelInLabel.Text    = KeycodeHelper.KeycodeToString(settings.KeyCode_FishCapture);
		SpDismissLabel.Text   = KeycodeHelper.KeycodeToString(settings.KeyCode_DismissFishDialogue);
		foreach (var btn in new[] { SpMoveLeftBtn, SpMoveRightBtn, SpReelInBtn, SpDismissBtn, SpResetBtn })
		{
			btn.Background = dark ? Theme.ColorAccent2 : Theme.ButtonDefaultBGColor;
			btn.Style = dark ? Theme.DarkStyle : Theme.LightStyle;
		}
		foreach (var lbl in new[] { SpMoveLeftDescription, SpMoveRightDescription, SpReelInDescription, SpDismissDescription })
			lbl.Foreground = fg;
		foreach (var lbl in new[] { SpRestartDelayLabel, SpLagCompensationLabel, SpDimissDelayLabel, SpFishCaptureLabel, SpPostCatchDelayLabel, SpAfterClickDelayLabel, SpGPULabel, SpDiscordUserIdLabel, SpDiscordWebHookLabel })
			lbl.Foreground = fg;
		SpResetLabel.Foreground = dark ? Theme.ColorAccent5 : Theme.BlackColor;
		foreach (var tb in new[] { SpRestartDelayTextBox, SpLagCompensationDelayTextBox, SpDimissDelayTextBox, SpFishCaptureDelayTextBox, SpPostCatchDelayTextBox, SpAfterClickDelayTextBox, SpDiscordUserIdTextBox, SpDiscordWebHookTextBox })
		{
			tb.Background = bg;
			tb.Foreground = fg;
		}
		SpRestartDelayTextBox.Text        = settings.Delay_Restart.ToString();
		SpLagCompensationDelayTextBox.Text = settings.Delay_LagCompensation.ToString();
		SpDimissDelayTextBox.Text         = settings.Delay_DismissFishCaptureDialogue.ToString();
		SpFishCaptureDelayTextBox.Text    = settings.Delay_FishCapture.ToString();
		SpPostCatchDelayTextBox.Text      = settings.Delay_PostCatch.ToString();
		SpAfterClickDelayTextBox.Text     = settings.Delay_AfterClick.ToString();
		SpDiscordUserIdTextBox.Text       = settings.DiscordUserId ?? "";
		SpDiscordWebHookTextBox.Text      = settings.DiscordHookUrl ?? "";
		SpGPUList.SelectedIndex           = settings.DefaultAdapter;
		if (SpCloseBtn.Background.ToString().Equals(Theme.ButtonDefaultBGColor.ToString()) || SpCloseBtn.Background.ToString().Equals(Theme.ColorAccent2.ToString()))
		{
			SpCloseBtn.Background = dark ? Theme.ColorAccent2 : Theme.ButtonDefaultBGColor;
			SpCloseBtn.Style = dark ? Theme.DarkStyle : Theme.LightStyle;
		}
	}

	private void BackgroundInputBtn_Click(object sender, RoutedEventArgs e)
	{
		bool turningOn = settings.IsBackgroundInput == 0;
		if (turningOn)
		{
			MessageBoxResult result = MessageBox.Show(
				"Background Inputs now works properly for multitasking, but can still occasionally bug out, best when keeping an eye on the game. For fully AFK sessions, keep the game focused.",
				"Background Inputs",
				MessageBoxButton.OKCancel,
				MessageBoxImage.Information);
			if (result != MessageBoxResult.OK) return;
		}
		settings.IsBackgroundInput = turningOn ? 1 : 0;
		BackgroundInputLabel.Text = settings.IsBackgroundInput == 1 ? "Background Inputs: On" : "Background Inputs: Off";
		UpdateToggleIconColors(isDarkMode);
	}

	private void UpdateToggleIconColors(bool darkMode)
	{
		var offColor = darkMode ? Theme.ColorAccent5 : Theme.BlackColor;
		AlwaysOnTopIcon.Foreground     = settings.IsAlwaysOnTop    == 1 ? Theme.GreenColor : offColor;
		BackgroundInputIcon.Foreground = settings.IsBackgroundInput == 1 ? Theme.GreenColor : offColor;
	}

	[DllImport("user32.dll")]
	private static extern bool GetWindowRect(IntPtr hWnd, out PreConfigRECT lpRect);
	[DllImport("user32.dll")]
	private static extern bool GetClientRect(IntPtr hWnd, out PreConfigRECT lpRect);
	[DllImport("user32.dll")]
	private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);
	[DllImport("user32.dll")]
	private static extern int GetSystemMetrics(int nIndex);
	[DllImport("user32.dll")]
	private static extern bool ClientToScreen(IntPtr hWnd, ref System.Drawing.Point lpPoint);
	[DllImport("user32.dll")]
	private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
	[DllImport("user32.dll")]
	private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

	[StructLayout(LayoutKind.Sequential)]
	private struct PreConfigRECT { public int Left, Top, Right, Bottom; }

	private const int  GWL_STYLE           = -16;
	private const uint WS_OVERLAPPEDWINDOW = 0x00CF0000;
	private const uint WS_VISIBLE          = 0x10000000;
	private const uint SWP_NOSIZE          = 0x0001;
	private const uint SWP_NOMOVE          = 0x0002;
	private const uint SWP_NOZORDER        = 0x0004;
	private const uint SWP_FRAMECHANGED    = 0x0020;

	private IntPtr? TryGetGameHandle()
	{
		var processes = Process.GetProcessesByName(settings.GameProcessName);
		return processes.Length == 1 ? processes.First().MainWindowHandle : (IntPtr?)null;
	}

	private void PreConfigBtn_Click(object sender, RoutedEventArgs e)
	{
		if (settings.IsWindowPinEnabled == 1)
		{
			settings.IsWindowPinEnabled = 0;
			UpdatePreConfigIndicator();
			return;
		}

		var confirm = MessageBox.Show(
			"Auto-Scale will resize and center the game window to 1280×720, then apply all detection coordinates automatically.\n\nMake sure NTE is running before continuing.\n\n⚠ Successful Cast Detect is NOT auto-configured. After applying, set it manually by clicking the button and placing it on the middle letter of your character's name.",
			"1280x720 Auto-Scale",
			MessageBoxButton.OKCancel,
			MessageBoxImage.Information);
		if (confirm != MessageBoxResult.OK) return;

		IntPtr? handle = TryGetGameHandle();
		if (!handle.HasValue)
		{
			MessageBox.Show(
				"Could not find the game window. Make sure NTE is running and try again.",
				"Auto-Scale Failed",
				MessageBoxButton.OK,
				MessageBoxImage.Warning);
			return;
		}

		// Switch the game window to standard windowed mode (adds title bar + borders).
		// This handles borderless and fullscreen — without this the resize has no chrome.
		SetWindowLong(handle.Value, GWL_STYLE, unchecked((int)(WS_OVERLAPPEDWINDOW | WS_VISIBLE)));
		// Force the frame to recalculate so GetClientRect returns the updated measurements.
		SetWindowPos(handle.Value, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);

		// Measure the non-client frame (title bar + borders) so we set the correct outer window size.
		GetWindowRect(handle.Value, out PreConfigRECT outerRect);
		GetClientRect(handle.Value, out PreConfigRECT clientRect);
		int frameW = (outerRect.Right - outerRect.Left) - clientRect.Right;
		int frameH = (outerRect.Bottom - outerRect.Top) - clientRect.Bottom;

		int targetW = 1280 + frameW;
		int targetH = 720  + frameH;

		// Resize and center in one SetWindowPos call, re-applying SWP_FRAMECHANGED to fully commit the style.
		int screenW = GetSystemMetrics(0);
		int screenH = GetSystemMetrics(1);
		SetWindowPos(handle.Value, IntPtr.Zero, (screenW - targetW) / 2, (screenH - targetH) / 2, targetW, targetH, SWP_NOZORDER | SWP_FRAMECHANGED);

		if (!GetWindowRect(handle.Value, out PreConfigRECT _))
		{
			MessageBox.Show(
				"Could not read the game window position. Make sure NTE is running and try again.",
				"Auto-Scale Failed",
				MessageBoxButton.OK,
				MessageBoxImage.Warning);
			return;
		}

		// Use ClientToScreen to get the physical screen coordinates of the client area.
		// Converting both (0,0) and (1280,720) gives us the actual physical pixel extent,
		// which handles any Windows DPI scaling (100%, 125%, 150%, 200%, etc.) automatically.
		var ptOrigin = new System.Drawing.Point(0, 0);
		ClientToScreen(handle.Value, ref ptOrigin);
		var ptCorner = new System.Drawing.Point(1280, 720);
		ClientToScreen(handle.Value, ref ptCorner);

		int cox = ptOrigin.X;
		int coy = ptOrigin.Y;
		double sx = (double)(ptCorner.X - cox) / 1280.0;
		double sy = (double)(ptCorner.Y - coy) / 720.0;

		// Base offsets are client-area relative at 1280x720 (derived from 4K fullscreen reference ÷ 3).
		// Multiplying by sx/sy scales them to physical pixels at any DPI.
		settings.UpperLeftBarPoint_X      = cox + (int)Math.Round(407 * sx);  settings.UpperLeftBarPoint_Y     = coy + (int)Math.Round(42  * sy);
		settings.LowerRightBarPoint_X     = cox + (int)Math.Round(879 * sx);  settings.LowerRightBarPoint_Y    = coy + (int)Math.Round(56  * sy);
		settings.FishStaminaPoint_X       = cox + (int)Math.Round(353 * sx);  settings.FishStaminaPoint_Y      = coy + (int)Math.Round(23  * sy);
		settings.PlayerStaminaPoint_X     = cox + (int)Math.Round(932 * sx);  settings.PlayerStaminaPoint_Y    = coy + (int)Math.Round(22  * sy);
		settings.CastButtonPoint_X        = cox + (int)Math.Round(1185 * sx); settings.CastButtonPoint_Y       = coy + (int)Math.Round(632 * sy);
		settings.XpBarPoint_X             = cox + (int)Math.Round(576 * sx);  settings.XpBarPoint_Y            = coy + (int)Math.Round(85  * sy);
		settings.DailyRewardPoint_X       = cox + (int)Math.Round(685 * sx);  settings.DailyRewardPoint_Y      = coy + (int)Math.Round(235 * sy);

		settings.MiddleBarColor_A = 255; settings.MiddleBarColor_R = 48;
		settings.MiddleBarColor_G = 216; settings.MiddleBarColor_B = 181;

		settings.CursorColor_A = 255; settings.CursorColor_R = 254;
		settings.CursorColor_G = 247; settings.CursorColor_B = 165;

		settings.FishStaminaColor_A = 255; settings.FishStaminaColor_R = 236;
		settings.FishStaminaColor_G = 208; settings.FishStaminaColor_B = 60;

		settings.PlayerStaminaColor_A = 255; settings.PlayerStaminaColor_R = 51;
		settings.PlayerStaminaColor_G = 209; settings.PlayerStaminaColor_B = 236;

		settings.CastButtonColor_R = 32; settings.CastButtonColor_G = 124;
		settings.CastButtonColor_B = 255;

		settings.XpBarColor_A = 255; settings.XpBarColor_R = 246;
		settings.XpBarColor_G = 73;  settings.XpBarColor_B = 142;

		settings.PinnedWindowWidth  = 1280;
		settings.PinnedWindowHeight = 720;
		settings.IsWindowPinEnabled = 1;
		ReadSettings();
	}

	private void UpdatePreConfigIndicator()
	{
		PreConfigIndicator.Fill = settings.IsWindowPinEnabled == 1
			? System.Windows.Media.Brushes.Lime
			: new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x44, 0x44, 0x44));
	}

	private void HandleButtonClick(Button btn, TextBlock label, string labelText, TextBlock coordLabel = null)
	{
		activeButton = btn;
		activeLabel = label;
		if (coordLabel != null)
		{
			activeCoordsLabel = coordLabel;
			inCoordSelectMode = true;
		}
		backupButtonText = new TextRange(activeLabel.ContentStart, activeLabel.ContentEnd).Text;
		activeLabel.Text = labelText;
		timer.Start();
		inEyeDropMode = true;
		lens_form = new Lens_Form
		{
			Size = new System.Drawing.Size(settings.ZoomSize_X, settings.ZoomSize_Y),
			AutoClose = true,
			HideCursor = false,
			ZoomFactor = settings.ZoomFactor,
			NearestNeighborInterpolation = false
		};
		lens_form.Show();
	}

	private void ReadSettings()
	{
		FishStaminaColorBtn.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb((byte)settings.FishStaminaColor_A, (byte)settings.FishStaminaColor_R, (byte)settings.FishStaminaColor_G, (byte)settings.FishStaminaColor_B));
		System.Drawing.Color tempColor = System.Drawing.Color.FromArgb(System.Drawing.Color.FromArgb(settings.FishStaminaColor_A, settings.FishStaminaColor_R, settings.FishStaminaColor_G, settings.FishStaminaColor_B).ToArgb() ^ 0xFFFFFF);
		FishStaminaColorLabel.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(tempColor.A, tempColor.R, tempColor.G, tempColor.B));
		MiddleBarColorBtn.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb((byte)settings.MiddleBarColor_A, (byte)settings.MiddleBarColor_R, (byte)settings.MiddleBarColor_G, (byte)settings.MiddleBarColor_B));
		tempColor = System.Drawing.Color.FromArgb(System.Drawing.Color.FromArgb(settings.MiddleBarColor_A, settings.MiddleBarColor_R, settings.MiddleBarColor_G, settings.MiddleBarColor_B).ToArgb() ^ 0xFFFFFF);
		MiddleBarColorLabel.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(tempColor.A, tempColor.R, tempColor.G, tempColor.B));
		CursorColorBtn.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb((byte)settings.CursorColor_A, (byte)settings.CursorColor_R, (byte)settings.CursorColor_G, (byte)settings.CursorColor_B));
		tempColor = System.Drawing.Color.FromArgb(System.Drawing.Color.FromArgb(settings.CursorColor_A, settings.CursorColor_R, settings.CursorColor_G, settings.CursorColor_B).ToArgb() ^ 0xFFFFFF);
		CursorColorLabel.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(tempColor.A, tempColor.R, tempColor.G, tempColor.B));
		PlayerStaminaColorBtn.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb((byte)settings.PlayerStaminaColor_A, (byte)settings.PlayerStaminaColor_R, (byte)settings.PlayerStaminaColor_G, (byte)settings.PlayerStaminaColor_B));
		tempColor = System.Drawing.Color.FromArgb(System.Drawing.Color.FromArgb(settings.PlayerStaminaColor_A, settings.PlayerStaminaColor_R, settings.PlayerStaminaColor_G, settings.PlayerStaminaColor_B).ToArgb() ^ 0xFFFFFF);
		PlayerStaminaColorLabel.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(tempColor.A, tempColor.R, tempColor.G, tempColor.B));
		FishStaminaCoords.Text = "X: " + settings.FishStaminaPoint_X + "\nY: " + settings.FishStaminaPoint_Y;
		PlayerStaminaCoords.Text = "X: " + settings.PlayerStaminaPoint_X + "\nY: " + settings.PlayerStaminaPoint_Y;
		UpperLeftCoords.Text = "X: " + settings.UpperLeftBarPoint_X + "\nY: " + settings.UpperLeftBarPoint_Y;
		LowerRightCoords.Text = "X: " + settings.LowerRightBarPoint_X + "\nY: " + settings.LowerRightBarPoint_Y;
		CastBtnCoords.Text = "X: " + settings.CastButtonPoint_X + "\nY: " + settings.CastButtonPoint_Y;
		CastBtnBtn.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, (byte)settings.CastButtonColor_R, (byte)settings.CastButtonColor_G, (byte)settings.CastButtonColor_B));
		XpBarColorBtn.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb((byte)settings.XpBarColor_A, (byte)settings.XpBarColor_R, (byte)settings.XpBarColor_G, (byte)settings.XpBarColor_B));
		tempColor = System.Drawing.Color.FromArgb(System.Drawing.Color.FromArgb(settings.XpBarColor_A, settings.XpBarColor_R, settings.XpBarColor_G, settings.XpBarColor_B).ToArgb() ^ 0xFFFFFF);
		XpBarColorLabel.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(tempColor.A, tempColor.R, tempColor.G, tempColor.B));
		XpBarCoords.Text = "X: " + settings.XpBarPoint_X + "\nY: " + settings.XpBarPoint_Y;
		DailyRewardBtn.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, (byte)settings.DailyRewardColor_R, (byte)settings.DailyRewardColor_G, (byte)settings.DailyRewardColor_B));
		tempColor = System.Drawing.Color.FromArgb(System.Drawing.Color.FromArgb(255, settings.DailyRewardColor_R, settings.DailyRewardColor_G, settings.DailyRewardColor_B).ToArgb() ^ 0xFFFFFF);
		DailyRewardLabel.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(tempColor.A, tempColor.R, tempColor.G, tempColor.B));
		DailyRewardCoords.Text = "X: " + settings.DailyRewardPoint_X + "\nY: " + settings.DailyRewardPoint_Y;
		BackgroundInputLabel.Text = settings.IsBackgroundInput == 1 ? "Background Inputs: On" : "Background Inputs: Off";
		Topmost = settings.IsAlwaysOnTop == 1;
		isDarkMode = settings.IsDarkMode > 0;
		UpdateToggleIconColors(isDarkMode);
		MoveLeftLabel.Text = KeycodeHelper.KeycodeToString(settings.KeyCode_MoveLeft);
		MoveRightLabel.Text = KeycodeHelper.KeycodeToString(settings.KeyCode_MoveRight);
		UpdatePreConfigIndicator();
	}

	private bool WriteSettings()
	{
		switch (activeButton.Name)
		{
		case "FishStaminaColorBtn":
			settings.FishStaminaColor_A = activeColor.A;
			settings.FishStaminaColor_R = activeColor.R;
			settings.FishStaminaColor_G = activeColor.G;
			settings.FishStaminaColor_B = activeColor.B;
			settings.FishStaminaPoint_X = mousePosition.X;
			settings.FishStaminaPoint_Y = mousePosition.Y;
			break;
		case "MiddleBarColorBtn":
			settings.MiddleBarColor_A = activeColor.A;
			settings.MiddleBarColor_R = activeColor.R;
			settings.MiddleBarColor_G = activeColor.G;
			settings.MiddleBarColor_B = activeColor.B;
			break;
		case "CursorColorBtn":
			settings.CursorColor_A = activeColor.A;
			settings.CursorColor_R = activeColor.R;
			settings.CursorColor_G = activeColor.G;
			settings.CursorColor_B = activeColor.B;
			break;
		case "PlayerStaminaColorBtn":
			settings.PlayerStaminaColor_A = activeColor.A;
			settings.PlayerStaminaColor_R = activeColor.R;
			settings.PlayerStaminaColor_G = activeColor.G;
			settings.PlayerStaminaColor_B = activeColor.B;
			settings.PlayerStaminaPoint_X = mousePosition.X;
			settings.PlayerStaminaPoint_Y = mousePosition.Y;
			break;
		case "UpperLeftBtn":
			settings.UpperLeftBarPoint_X = mousePosition.X;
			settings.UpperLeftBarPoint_Y = mousePosition.Y;
			break;
		case "LowerRightBtn":
			settings.LowerRightBarPoint_X = mousePosition.X;
			settings.LowerRightBarPoint_Y = mousePosition.Y;
			break;
		case "CastBtnBtn":
			settings.CastButtonColor_R = activeColor.R;
			settings.CastButtonColor_G = activeColor.G;
			settings.CastButtonColor_B = activeColor.B;
			settings.CastButtonPoint_X = mousePosition.X;
			settings.CastButtonPoint_Y = mousePosition.Y;
			break;
		case "XpBarColorBtn":
			settings.XpBarColor_A = activeColor.A;
			settings.XpBarColor_R = activeColor.R;
			settings.XpBarColor_G = activeColor.G;
			settings.XpBarColor_B = activeColor.B;
			settings.XpBarPoint_X = mousePosition.X;
			settings.XpBarPoint_Y = mousePosition.Y;
			break;
		case "DailyRewardBtn":
			settings.DailyRewardColor_R = activeColor.R;
			settings.DailyRewardColor_G = activeColor.G;
			settings.DailyRewardColor_B = activeColor.B;
			settings.DailyRewardPoint_X = mousePosition.X;
			settings.DailyRewardPoint_Y = mousePosition.Y;
			break;
		case "ThemeModeBtn":
			settings.IsDarkMode = (isDarkMode ? 1 : 0);
			break;
		}
		return true;
	}

	private void StartBtn_Click(object sender, RoutedEventArgs e)
	{
		if (!SanityCheck())
		{
			return;
		}
		IntPtr? gameHandle = GetGameHandle();
		if (!fishBot.isRunning)
		{
			fishBot.isRunning = true;
			StartLabel.Text = "Stop\nFishing";
			if (!fishBotThread.IsAlive)
			{
				fishBot.GameHandle = gameHandle;
				fishBotThread.Start();
			}
		}
		else
		{
			fishBot.Stop();
			StartLabel.Text = "Start\nFishing";
			fishBot = new FishingThread(settings, LeftBox, RightBox, cursor, bar, StatusLabel, middleBarImage, cursorImage, castReadyImage, FishStaminaIndicator, PlayerStaminaIndicator, CastBtnIndicator, XpBarIndicator, DailyRewardIndicator, EscBox, FBox, gameHandle);
			fishBot.OnFishCaught = OnFishCaughtCallback;
			fishBotThread = new Thread(fishBot.Start);
		}
	}

	private IntPtr? GetGameHandle()
	{
		string message = string.Empty;
		bool noErrors = true;
		Process[] processes = Process.GetProcessesByName(settings.GameProcessName);
		if (processes.Length == 0)
		{
			message = "Failed to find the Game. Either it's not running or the tool is not ran as admin";
			noErrors = false;
		}
		else if (processes.Length > 1)
		{
			message = "Found more than one instance of the Game. This is not normal";
			noErrors = false;
		}
		if (!noErrors)
		{
			MessageBox.Show(message, "Game Not Found. Running Tool as Simulation", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			return null;
		}
		return processes.First().MainWindowHandle;
	}

	private bool SanityCheck()
	{
		return true;
	}

	private void ThemeModeBtn_Click(object sender, RoutedEventArgs e)
	{
		isDarkMode = !isDarkMode;
		activeButton = ThemeModeBtn;
		WriteSettings();
		activeButton = defaultButton;
		InitTheme(isDarkMode);
	}

	private void InitTheme(bool darkModeTheme)
	{
		MainWindows.Background = (darkModeTheme ? Theme.ColorAccent1 : Theme.WhiteColor);
		MiddleBarGBox.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		MiddleBarGBox.BorderBrush = (darkModeTheme ? Theme.ColorAccent2 : Theme.GBoxDefaultBorderColor);
		StaminaGBox.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		StaminaGBox.BorderBrush = (darkModeTheme ? Theme.ColorAccent2 : Theme.GBoxDefaultBorderColor);
		DetectionGBox.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		DetectionGBox.BorderBrush = (darkModeTheme ? Theme.ColorAccent2 : Theme.GBoxDefaultBorderColor);
		OutputGBox.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		OutputGBox.BorderBrush = (darkModeTheme ? Theme.ColorAccent2 : Theme.GBoxDefaultBorderColor);
		cursor.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		bar.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		StatusLabel.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		FishCaughtLabel.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		VersionLabel.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		if (FishCaughtResetBtn.Background.ToString().Equals(Theme.ButtonDefaultBGColor.ToString()) || FishCaughtResetBtn.Background.ToString().Equals(Theme.ColorAccent2.ToString()))
		{
			FishCaughtResetBtn.Background = (darkModeTheme ? Theme.ColorAccent2 : Theme.ButtonDefaultBGColor);
			FishCaughtResetBtn.Style = (darkModeTheme ? Theme.DarkStyle : Theme.LightStyle);
		}
		LeftBox.Stroke = (darkModeTheme ? Theme.ColorAccent3 : Theme.BlackColor);
		RightBox.Stroke = (darkModeTheme ? Theme.ColorAccent3 : Theme.BlackColor);
		EscBox.Stroke = (darkModeTheme ? Theme.ColorAccent3 : Theme.BlackColor);
		FBox.Stroke = (darkModeTheme ? Theme.ColorAccent3 : Theme.BlackColor);
		if (UpperLeftBtn.Background.ToString().Equals(Theme.ButtonDefaultBGColor.ToString()) || UpperLeftBtn.Background.ToString().Equals(Theme.ColorAccent2.ToString()))
		{
			UpperLeftBtn.Background = (darkModeTheme ? Theme.ColorAccent2 : Theme.ButtonDefaultBGColor);
			UpperLeftBtn.Style = (darkModeTheme ? Theme.DarkStyle : Theme.LightStyle);
			UpperLeftLabel.Foreground = (darkModeTheme ? Theme.ColorAccent5 : Theme.BlackColor);
		}
		if (MiddleBarColorBtn.Background.ToString().Equals(Theme.ButtonDefaultBGColor.ToString()) || MiddleBarColorBtn.Background.ToString().Equals(Theme.ColorAccent2.ToString()))
		{
			MiddleBarColorBtn.Background = (darkModeTheme ? Theme.ColorAccent2 : Theme.ButtonDefaultBGColor);
			MiddleBarColorBtn.Style = (darkModeTheme ? Theme.DarkStyle : Theme.LightStyle);
			MiddleBarColorLabel.Foreground = (darkModeTheme ? Theme.ColorAccent5 : Theme.BlackColor);
		}
		if (CursorColorBtn.Background.ToString().Equals(Theme.ButtonDefaultBGColor.ToString()) || CursorColorBtn.Background.ToString().Equals(Theme.ColorAccent2.ToString()))
		{
			CursorColorBtn.Background = (darkModeTheme ? Theme.ColorAccent2 : Theme.ButtonDefaultBGColor);
			CursorColorBtn.Style = (darkModeTheme ? Theme.DarkStyle : Theme.LightStyle);
			CursorColorLabel.Foreground = (darkModeTheme ? Theme.ColorAccent5 : Theme.BlackColor);
		}
		if (LowerRightBtn.Background.ToString().Equals(Theme.ButtonDefaultBGColor.ToString()) || LowerRightBtn.Background.ToString().Equals(Theme.ColorAccent2.ToString()))
		{
			LowerRightBtn.Background = (darkModeTheme ? Theme.ColorAccent2 : Theme.ButtonDefaultBGColor);
			LowerRightBtn.Style = (darkModeTheme ? Theme.DarkStyle : Theme.LightStyle);
			LowerRightLabel.Foreground = (darkModeTheme ? Theme.ColorAccent5 : Theme.BlackColor);
		}
		if (FishStaminaColorBtn.Background.ToString().Equals(Theme.ButtonDefaultBGColor.ToString()) || FishStaminaColorBtn.Background.ToString().Equals(Theme.ColorAccent2.ToString()))
		{
			FishStaminaColorBtn.Background = (darkModeTheme ? Theme.ColorAccent2 : Theme.ButtonDefaultBGColor);
			FishStaminaColorBtn.Style = (darkModeTheme ? Theme.DarkStyle : Theme.LightStyle);
			FishStaminaColorLabel.Foreground = (darkModeTheme ? Theme.ColorAccent5 : Theme.BlackColor);
		}
		if (PlayerStaminaColorBtn.Background.ToString().Equals(Theme.ButtonDefaultBGColor.ToString()) || PlayerStaminaColorBtn.Background.ToString().Equals(Theme.ColorAccent2.ToString()))
		{
			PlayerStaminaColorBtn.Background = (darkModeTheme ? Theme.ColorAccent2 : Theme.ButtonDefaultBGColor);
			PlayerStaminaColorBtn.Style = (darkModeTheme ? Theme.DarkStyle : Theme.LightStyle);
			PlayerStaminaColorLabel.Foreground = (darkModeTheme ? Theme.ColorAccent5 : Theme.BlackColor);
		}
		if (CastBtnBtn.Background.ToString().Equals(Theme.ButtonDefaultBGColor.ToString()) || CastBtnBtn.Background.ToString().Equals(Theme.ColorAccent2.ToString()))
		{
			CastBtnBtn.Background = (darkModeTheme ? Theme.ColorAccent2 : Theme.ButtonDefaultBGColor);
			CastBtnBtn.Style = (darkModeTheme ? Theme.DarkStyle : Theme.LightStyle);
			CastBtnLabel.Foreground = (darkModeTheme ? Theme.ColorAccent5 : Theme.BlackColor);
		}
		if (XpBarColorBtn.Background.ToString().Equals(Theme.ButtonDefaultBGColor.ToString()) || XpBarColorBtn.Background.ToString().Equals(Theme.ColorAccent2.ToString()))
		{
			XpBarColorBtn.Background = (darkModeTheme ? Theme.ColorAccent2 : Theme.ButtonDefaultBGColor);
			XpBarColorBtn.Style = (darkModeTheme ? Theme.DarkStyle : Theme.LightStyle);
			XpBarColorLabel.Foreground = (darkModeTheme ? Theme.ColorAccent5 : Theme.BlackColor);
		}
		if (DailyRewardBtn.Background.ToString().Equals(Theme.ButtonDefaultBGColor.ToString()) || DailyRewardBtn.Background.ToString().Equals(Theme.ColorAccent2.ToString()))
		{
			DailyRewardBtn.Background = (darkModeTheme ? Theme.ColorAccent2 : Theme.ButtonDefaultBGColor);
			DailyRewardBtn.Style = (darkModeTheme ? Theme.DarkStyle : Theme.LightStyle);
			DailyRewardLabel.Foreground = (darkModeTheme ? Theme.ColorAccent5 : Theme.BlackColor);
		}
		// Side menu buttons
		HamburgerBtn.Style = (darkModeTheme ? Theme.DarkStyle : Theme.LightStyle);
		SettingBtn.Style = (darkModeTheme ? Theme.SideMenuDarkStyle : Theme.SideMenuLightStyle);
		ThemeModeBtn.Style = (darkModeTheme ? Theme.SideMenuDarkStyle : Theme.SideMenuLightStyle);
		AlwaysOnTopBtn.Style = (darkModeTheme ? Theme.SideMenuDarkStyle : Theme.SideMenuLightStyle);
		BackgroundInputBtn.Style = (darkModeTheme ? Theme.SideMenuDarkStyle : Theme.SideMenuLightStyle);
		FaqBtn.Style = (darkModeTheme ? Theme.SideMenuDarkStyle : Theme.SideMenuLightStyle);
		SideMenuBorder.BorderBrush = (darkModeTheme ? Theme.ColorAccent2 : Theme.GBoxDefaultBorderColor);
		SideSettingLabel.Foreground = (darkModeTheme ? Theme.ColorAccent5 : Theme.BlackColor);
		SideThemeLabel.Foreground = (darkModeTheme ? Theme.ColorAccent5 : Theme.BlackColor);
		SideAlwaysOnTopLabel.Foreground = (darkModeTheme ? Theme.ColorAccent5 : Theme.BlackColor);
		BackgroundInputLabel.Foreground = (darkModeTheme ? Theme.ColorAccent5 : Theme.BlackColor);
		SideFaqLabel.Foreground = (darkModeTheme ? Theme.ColorAccent5 : Theme.BlackColor);
		UpdateToggleIconColors(darkModeTheme);
		// Log page
		LogPageBorder.Background  = (darkModeTheme ? Theme.ColorAccent1 : Theme.WhiteColor);
		LogPageBorder.BorderBrush = (darkModeTheme ? Theme.ColorAccent2 : Theme.GBoxDefaultBorderColor);
		LogTitleLabel.Foreground  = (darkModeTheme ? Theme.ColorAccent5 : Theme.BlackColor);
		LogCountLabel.Foreground  = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		LogListBox.Background     = (darkModeTheme ? Theme.ColorAccent1 : Theme.WhiteColor);
		LogListBox.Foreground     = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		LogListBox.BorderBrush    = (darkModeTheme ? Theme.ColorAccent2 : Theme.GBoxDefaultBorderColor);
		SideLogLabel.Foreground   = (darkModeTheme ? Theme.ColorAccent5 : Theme.BlackColor);
		LogBtn.Style   = (darkModeTheme ? Theme.SideMenuDarkStyle : Theme.SideMenuLightStyle);
		KofiBtn.Style  = (darkModeTheme ? Theme.SideMenuDarkStyle : Theme.SideMenuLightStyle);
		if (LogClearBtn.Background.ToString().Equals(Theme.ButtonDefaultBGColor.ToString()) || LogClearBtn.Background.ToString().Equals(Theme.ColorAccent2.ToString()))
		{
			LogClearBtn.Background = (darkModeTheme ? Theme.ColorAccent2 : Theme.ButtonDefaultBGColor);
			LogClearBtn.Style = (darkModeTheme ? Theme.DarkStyle : Theme.LightStyle);
		}
		if (LogCloseBtn.Background.ToString().Equals(Theme.ButtonDefaultBGColor.ToString()) || LogCloseBtn.Background.ToString().Equals(Theme.ColorAccent2.ToString()))
		{
			LogCloseBtn.Background = (darkModeTheme ? Theme.ColorAccent2 : Theme.ButtonDefaultBGColor);
			LogCloseBtn.Style = (darkModeTheme ? Theme.DarkStyle : Theme.LightStyle);
		}
		// FAQ page
		FaqPageBorder.Background = (darkModeTheme ? Theme.ColorAccent1 : Theme.WhiteColor);
		FaqPageBorder.BorderBrush = (darkModeTheme ? Theme.ColorAccent2 : Theme.GBoxDefaultBorderColor);
		FaqTitleLabel.Foreground = (darkModeTheme ? Theme.ColorAccent5 : Theme.BlackColor);
		TextElement.SetForeground(FaqContentPanel, (darkModeTheme ? Theme.ColorAccent5 : Theme.BlackColor));
		FaqQuoteBorder.BorderBrush = (darkModeTheme ? Theme.ColorAccent3 : Theme.ColorAccent2);
		FaqQuoteBorder.Background = darkModeTheme
			? new SolidColorBrush(System.Windows.Media.Color.FromArgb(40, 53, 167, 241))
			: new SolidColorBrush(System.Windows.Media.Color.FromArgb(20, 26, 72, 116));
		if (FaqCloseBtn.Background.ToString().Equals(Theme.ButtonDefaultBGColor.ToString()) || FaqCloseBtn.Background.ToString().Equals(Theme.ColorAccent2.ToString()))
		{
			FaqCloseBtn.Background = (darkModeTheme ? Theme.ColorAccent2 : Theme.ButtonDefaultBGColor);
			FaqCloseBtn.Style = (darkModeTheme ? Theme.DarkStyle : Theme.LightStyle);
		}
		if (FaqVideoBtn.Background.ToString().Equals(Theme.ButtonDefaultBGColor.ToString()) || FaqVideoBtn.Background.ToString().Equals(Theme.ColorAccent2.ToString()))
		{
			FaqVideoBtn.Background = (darkModeTheme ? Theme.ColorAccent2 : Theme.ButtonDefaultBGColor);
			FaqVideoBtn.Style = (darkModeTheme ? Theme.DarkStyle : Theme.LightStyle);
		}
		ChibiHintLabel.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		// Welcome page
		WelcomePageBorder.Background  = (darkModeTheme ? Theme.ColorAccent1 : Theme.WhiteColor);
		WelcomePageBorder.BorderBrush = (darkModeTheme ? Theme.ColorAccent2 : Theme.GBoxDefaultBorderColor);
		WelcomeTitleLabel.Foreground  = (darkModeTheme ? Theme.ColorAccent5 : Theme.BlackColor);
		TextElement.SetForeground(WelcomeContentPanel, (darkModeTheme ? Theme.ColorAccent5 : Theme.BlackColor));
		WelcomeFaqBorder.BorderBrush  = (darkModeTheme ? Theme.ColorAccent3 : Theme.ColorAccent2);
		WelcomeFaqBorder.Background   = darkModeTheme
			? new SolidColorBrush(System.Windows.Media.Color.FromArgb(40, 53, 167, 241))
			: new SolidColorBrush(System.Windows.Media.Color.FromArgb(20, 26, 72, 116));
		WelcomeFocusWarnBorder.Background = darkModeTheme
			? new SolidColorBrush(System.Windows.Media.Color.FromArgb(0x22, 0xDC, 0x35, 0x45))
			: new SolidColorBrush(System.Windows.Media.Color.FromArgb(0x11, 0xDC, 0x35, 0x45));
		foreach (var btn in new[] { WelcomeCloseBtn, WelcomeFaqBtn, WelcomeGotItBtn })
		{
			if (btn.Background.ToString().Equals(Theme.ButtonDefaultBGColor.ToString()) || btn.Background.ToString().Equals(Theme.ColorAccent2.ToString()))
			{
				btn.Background = (darkModeTheme ? Theme.ColorAccent2 : Theme.ButtonDefaultBGColor);
				btn.Style = (darkModeTheme ? Theme.DarkStyle : Theme.LightStyle);
			}
		}
		if (StartBtn.Background.ToString().Equals(Theme.ButtonDefaultBGColor.ToString()) || StartBtn.Background.ToString().Equals(Theme.ColorAccent2.ToString()))
		{
			StartBtn.Background = (darkModeTheme ? Theme.ColorAccent2 : Theme.ButtonDefaultBGColor);
			StartBtn.Style = (darkModeTheme ? Theme.DarkStyle : Theme.LightStyle);
			StartLabel.Foreground = (darkModeTheme ? Theme.ColorAccent5 : Theme.BlackColor);
		}
		if (LeftBox.Fill.ToString().Equals(Theme.ColorAccent3.ToString()) || LeftBox.Fill.ToString().Equals(Theme.GreenColor.ToString()))
		{
			LeftBox.Fill = (darkModeTheme ? Theme.ColorAccent3 : Theme.GreenColor);
		}
		if (RightBox.Fill.ToString().Equals(Theme.ColorAccent3.ToString()) || RightBox.Fill.ToString().Equals(Theme.GreenColor.ToString()))
		{
			RightBox.Fill = (darkModeTheme ? Theme.ColorAccent3 : Theme.GreenColor);
		}
		if (SettingsPage.Visibility == Visibility.Visible)
			InitSettingsTheme(darkModeTheme);
	}

	private void KofiBtn_Click(object sender, RoutedEventArgs e)
	{
		Process.Start(new ProcessStartInfo { FileName = "https://ko-fi.com/R6R4QC036", UseShellExecute = true });
	}

	private void SettingBtn_Click(object sender, RoutedEventArgs e)
	{
		if (!inEyeDropMode && !inCoordSelectMode)
		{
			if (SettingsPage.Visibility == Visibility.Visible)
				CloseSettingsPage();
			else
				OpenSettingsPage();
		}
	}
}
