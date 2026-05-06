using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SharpDX.DXGI;
using NTE_Fishing_Bot.Addon.DiscordInteractive;
using WindowsHook;

namespace NTE_Fishing_Bot;

public partial class SettingsWindow : Window, IComponentConnector
{
	private IAppSettings settings;

	private IKeyboardMouseEvents m_GlobalHook;

	private Button defaultButton = new Button();

	private TextBlock defaultLabel = new TextBlock();

	private Button activeButton;

	private TextBlock activeLabel;

	private string savedHotkeyText = "";

	private int _updateDiscordUser;

	private int _updateDiscordUrl;

	private Factory1 factory = new Factory1();

	public SettingsWindow(IAppSettings _settings, IKeyboardMouseEvents _m_GlobalHook)
	{
		InitializeComponent();
		Adapter[] adapters = factory.Adapters;
		Adapter1[] adapters2 = factory.Adapters1;
		var enumerable = adapters.Select((Adapter adapter) => new
		{
			name = adapter.Description.Description,
			count = adapter.GetOutputCount()
		});
		var brehbreh = adapters2.Select((Adapter1 adapter) => new
		{
			name = adapter.Description.Description,
			count = adapter.GetOutputCount()
		});
		foreach (var breh1 in enumerable)
		{
			GPUList.Items.Add(breh1.name + " (" + breh1.count + ")");
		}
		GPUList.Items.Add("--------");
		foreach (var breh2 in brehbreh)
		{
			GPUList.Items.Add(breh2.name + " (" + breh2.count + ")");
		}
		settings = _settings;
		m_GlobalHook = _m_GlobalHook;
		m_GlobalHook.KeyUp += GlobalHookKeyUp;
		activeButton = defaultButton;
		activeLabel = defaultLabel;
		InitTheme(_settings.IsDarkMode == 1);
	}

	private void GlobalHookKeyUp(object sender, WindowsHook.KeyEventArgs e)
	{
		if (activeButton != defaultButton)
		{
			activeLabel.Text = KeycodeHelper.KeycodeToString(e.KeyValue);
			WriteSettings(e.KeyValue);
			ResetHotkeyButtons();
			ResetHotkeyLabels(activeLabel, resetText: false);
		}
	}

	private bool WriteSettings(int keyCode)
	{
		switch (activeButton.Name)
		{
		case "MoveLeftBtn":
			settings.KeyCode_MoveLeft = keyCode;
			break;
		case "MoveRightBtn":
			settings.KeyCode_MoveRight = keyCode;
			break;
		case "ReelInBtn":
			settings.KeyCode_FishCapture = keyCode;
			break;
		case "DismissBtn":
			settings.KeyCode_DismissFishDialogue = keyCode;
			break;
		}
		return true;
	}

	private void InitTheme(bool darkModeTheme)
	{
		SettingsWindow1.Background = (darkModeTheme ? Theme.ColorAccent1 : Theme.WhiteColor);
		ButtonsGBox.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		DelayGBox.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		DiscordGBox.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		MoveLeftLabel.Text = KeycodeHelper.KeycodeToString(settings.KeyCode_MoveLeft);
		MoveRightLabel.Text = KeycodeHelper.KeycodeToString(settings.KeyCode_MoveRight);
		ReelInLabel.Text = KeycodeHelper.KeycodeToString(settings.KeyCode_FishCapture);
		DismissLabel.Text = KeycodeHelper.KeycodeToString(settings.KeyCode_DismissFishDialogue);
		RestartDelayTextBox.Text = settings.Delay_Restart.ToString();
		LagCompensationDelayTextBox.Text = settings.Delay_LagCompensation.ToString();
		DimissDelayTextBox.Text = settings.Delay_DismissFishCaptureDialogue.ToString();
		FishCaptureDelayTextBox.Text = settings.Delay_FishCapture.ToString();
		DiscordUserIdTextBox.Text = settings.DiscordUserId;
		DiscordWebHookTextBox.Text = settings.DiscordHookUrl;
		GPUList.SelectedIndex = settings.DefaultAdapter;
		if (MoveLeftBtn.Background.ToString().Equals(Theme.ButtonDefaultBGColor.ToString()) || MoveLeftBtn.Background.ToString().Equals(Theme.ColorAccent2.ToString()))
		{
			MoveLeftBtn.Background = (darkModeTheme ? Theme.ColorAccent2 : Theme.ButtonDefaultBGColor);
			MoveLeftBtn.Style = (darkModeTheme ? Theme.DarkStyle : Theme.LightStyle);
			MoveLeftBtn.Foreground = (darkModeTheme ? Theme.ColorAccent5 : Theme.BlackColor);
			MoveLeftDescription.Foreground = (darkModeTheme ? Theme.ColorAccent5 : Theme.BlackColor);
		}
		if (MoveRightBtn.Background.ToString().Equals(Theme.ButtonDefaultBGColor.ToString()) || MoveRightBtn.Background.ToString().Equals(Theme.ColorAccent2.ToString()))
		{
			MoveRightBtn.Background = (darkModeTheme ? Theme.ColorAccent2 : Theme.ButtonDefaultBGColor);
			MoveRightBtn.Style = (darkModeTheme ? Theme.DarkStyle : Theme.LightStyle);
			MoveRightBtn.Foreground = (darkModeTheme ? Theme.ColorAccent5 : Theme.BlackColor);
			MoveRightDescription.Foreground = (darkModeTheme ? Theme.ColorAccent5 : Theme.BlackColor);
		}
		if (ReelInBtn.Background.ToString().Equals(Theme.ButtonDefaultBGColor.ToString()) || ReelInBtn.Background.ToString().Equals(Theme.ColorAccent2.ToString()))
		{
			ReelInBtn.Background = (darkModeTheme ? Theme.ColorAccent2 : Theme.ButtonDefaultBGColor);
			ReelInBtn.Style = (darkModeTheme ? Theme.DarkStyle : Theme.LightStyle);
			ReelInBtn.Foreground = (darkModeTheme ? Theme.ColorAccent5 : Theme.BlackColor);
			ReelInDescription.Foreground = (darkModeTheme ? Theme.ColorAccent5 : Theme.BlackColor);
		}
		if (DismissBtn.Background.ToString().Equals(Theme.ButtonDefaultBGColor.ToString()) || DismissBtn.Background.ToString().Equals(Theme.ColorAccent2.ToString()))
		{
			DismissBtn.Background = (darkModeTheme ? Theme.ColorAccent2 : Theme.ButtonDefaultBGColor);
			DismissBtn.Style = (darkModeTheme ? Theme.DarkStyle : Theme.LightStyle);
			DismissBtn.Foreground = (darkModeTheme ? Theme.ColorAccent5 : Theme.BlackColor);
			DismissDescription.Foreground = (darkModeTheme ? Theme.ColorAccent5 : Theme.BlackColor);
		}
		if (ResetBtn.Background.ToString().Equals(Theme.ButtonDefaultBGColor.ToString()) || ResetBtn.Background.ToString().Equals(Theme.ColorAccent2.ToString()))
		{
			ResetBtn.Background = (darkModeTheme ? Theme.ColorAccent2 : Theme.ButtonDefaultBGColor);
			ResetBtn.Style = (darkModeTheme ? Theme.DarkStyle : Theme.LightStyle);
			ResetBtn.Foreground = (darkModeTheme ? Theme.ColorAccent5 : Theme.BlackColor);
			MoveLeftDescription.Foreground = (darkModeTheme ? Theme.ColorAccent5 : Theme.BlackColor);
		}
		LabelRow1.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		ArrowRow2.Source = RotateImage(darkModeTheme ? Theme.DayArrowImage : Theme.NightArrowImage, 90);
		DelayRestartLabel.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		InfoRow3Column1.Source = (darkModeTheme ? Theme.DayInfoImage : Theme.NightInfoImage);
		RestartDelayTextBox.Background = (darkModeTheme ? Theme.ColorAccent2 : Theme.WhiteColor);
		RestartDelayTextBox.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		ArrowRow3Column1.Source = (darkModeTheme ? Theme.DayArrowImage : Theme.NightArrowImage);
		ArrowRow3Column2.Source = (darkModeTheme ? Theme.DayArrowImage : Theme.NightArrowImage);
		DelayLagCompensationLabel.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		InfoRow3Column2.Source = (darkModeTheme ? Theme.DayInfoImage : Theme.NightInfoImage);
		LagCompensationDelayTextBox.Background = (darkModeTheme ? Theme.ColorAccent2 : Theme.WhiteColor);
		LagCompensationDelayTextBox.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		ArrowRow4Column1.Source = RotateImage(darkModeTheme ? Theme.DayArrowImage : Theme.NightArrowImage, 270);
		ArrowRow4Column2.Source = RotateImage(darkModeTheme ? Theme.DayArrowImage : Theme.NightArrowImage, 90);
		GPULabel.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		ArrowRow6Column1.Source = RotateImage(darkModeTheme ? Theme.DayArrowImage : Theme.NightArrowImage, 270);
		ArrowRow6Column2.Source = RotateImage(darkModeTheme ? Theme.DayArrowImage : Theme.NightArrowImage, 90);
		DelayDimissLabel.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		InfoRow7Column1.Source = (darkModeTheme ? Theme.DayInfoImage : Theme.NightInfoImage);
		DimissDelayTextBox.Background = (darkModeTheme ? Theme.ColorAccent2 : Theme.WhiteColor);
		DimissDelayTextBox.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		ArrowRow7Column1.Source = RotateImage(darkModeTheme ? Theme.DayArrowImage : Theme.NightArrowImage, 180);
		ArrowRow7Column2.Source = RotateImage(darkModeTheme ? Theme.DayArrowImage : Theme.NightArrowImage, 180);
		DelayFishCaptureLabel.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		InfoRow7Column2.Source = (darkModeTheme ? Theme.DayInfoImage : Theme.NightInfoImage);
		FishCaptureDelayTextBox.Background = (darkModeTheme ? Theme.ColorAccent2 : Theme.WhiteColor);
		FishCaptureDelayTextBox.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		PostCatchSectionLabel.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		PostCatchDelayLabel.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		PostCatchDelayTextBox.Text = settings.Delay_PostCatch.ToString();
		PostCatchDelayTextBox.Background = (darkModeTheme ? Theme.ColorAccent2 : Theme.WhiteColor);
		PostCatchDelayTextBox.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		AfterClickDelayLabel.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		AfterClickDelayTextBox.Text = settings.Delay_AfterClick.ToString();
		AfterClickDelayTextBox.Background = (darkModeTheme ? Theme.ColorAccent2 : Theme.WhiteColor);
		AfterClickDelayTextBox.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		DiscordUserIdLabel.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		DiscordInfoRow1.Source = (darkModeTheme ? Theme.DayInfoImage : Theme.NightInfoImage);
		DiscordUserIdTextBox.Background = (darkModeTheme ? Theme.ColorAccent2 : Theme.WhiteColor);
		DiscordUserIdTextBox.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		DiscordWebHookLabel.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		DiscordInfoRow2.Source = (darkModeTheme ? Theme.DayInfoImage : Theme.NightInfoImage);
		DiscordWebHookTextBox.Background = (darkModeTheme ? Theme.ColorAccent2 : Theme.WhiteColor);
		DiscordWebHookTextBox.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		DelayRestartTooltip.Background = (darkModeTheme ? Theme.ColorAccent1 : Theme.WhiteColor);
		DelayRestartTooltipHeader.Background = (darkModeTheme ? Theme.ColorAccent2 : Brushes.Tan);
		DelayRestartTooltipHeaderValue.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		DelayRestartTooltipDescription.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		DelayRestartTooltipLine.Stroke = (darkModeTheme ? Theme.ColorAccent5 : Brushes.Gray);
		DelayRestartTooltipDefault.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		DelayLagCompensationTooltip.Background = (darkModeTheme ? Theme.ColorAccent1 : Theme.WhiteColor);
		DelayLagCompensationTooltipHeader.Background = (darkModeTheme ? Theme.ColorAccent2 : Brushes.Tan);
		DelayLagCompensationTooltipHeaderValue.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		DelayLagCompensationTooltipDescription.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		DelayLagCompensationTooltipLine.Stroke = (darkModeTheme ? Theme.ColorAccent5 : Brushes.Gray);
		DelayLagCompensationTooltipDefault.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		DelayDimissTooltip.Background = (darkModeTheme ? Theme.ColorAccent1 : Theme.WhiteColor);
		DelayDimissTooltipHeader.Background = (darkModeTheme ? Theme.ColorAccent2 : Brushes.Tan);
		DelayDimissTooltipHeaderValue.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		DelayDimissTooltipDescription.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		DelayDimissTooltipLine.Stroke = (darkModeTheme ? Theme.ColorAccent5 : Brushes.Gray);
		DelayDimissTooltipDefault.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		DelayFishCaptureTooltip.Background = (darkModeTheme ? Theme.ColorAccent1 : Theme.WhiteColor);
		DelayFishCaptureTooltipHeader.Background = (darkModeTheme ? Theme.ColorAccent2 : Brushes.Tan);
		DelayFishCaptureTooltipHeaderValue.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		DelayFishCaptureTooltipDescription.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		DelayFishCaptureTooltipLine.Stroke = (darkModeTheme ? Theme.ColorAccent5 : Brushes.Gray);
		DelayFishCaptureTooltipDefault.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		DiscordUserIdTooltip.Background = (darkModeTheme ? Theme.ColorAccent1 : Theme.WhiteColor);
		DiscordUserIdTooltipHeader.Background = (darkModeTheme ? Theme.ColorAccent2 : Brushes.Tan);
		DiscordUserIdTooltipHeaderValue.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		DiscordUserIdTooltipDescription.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		DiscordUserIdTooltipLine.Stroke = (darkModeTheme ? Theme.ColorAccent5 : Brushes.Gray);
		DiscordUserIdTooltipDefault.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		DiscordWebHookTooltip.Background = (darkModeTheme ? Theme.ColorAccent1 : Theme.WhiteColor);
		DiscordWebHookTooltipHeader.Background = (darkModeTheme ? Theme.ColorAccent2 : Brushes.Tan);
		DiscordWebHookTooltipHeaderValue.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		DiscordWebHookTooltipDescription.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		DiscordWebHookTooltipLine.Stroke = (darkModeTheme ? Theme.ColorAccent5 : Brushes.Gray);
		DiscordWebHookTooltipDefault.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);

		// TextBlocks not previously themed
		DismissLabel2.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		FishStamDepleteLabel.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		ReelInButtonLabel.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		ResetLabel.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		StaminaBarAppearsLabel.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		// Button Setup description labels — condition check in the blocks above fails when buttons
		// use gradient styles, so set these unconditionally
		MoveLeftDescription.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		MoveRightDescription.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		ReelInDescription.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
		DismissDescription.Foreground = (darkModeTheme ? Theme.ColorAccent4 : Theme.BlackColor);
	}

	private void HandleHotkeyButtonClick(Button btn, TextBlock label)
	{
		activeButton = btn;
		activeLabel = label;
		DisableHotkeyButtons(activeButton);
		savedHotkeyText = label.Text;
		label.FontSize = 12.0;
		label.Text = "Press a key to set as hotkey";
	}

	private void MoveLeftBtn_Click(object sender, RoutedEventArgs e)
	{
		if (activeButton == defaultButton)
		{
			HandleHotkeyButtonClick(MoveLeftBtn, MoveLeftLabel);
			return;
		}
		ResetHotkeyButtons();
		ResetHotkeyLabels(MoveLeftLabel);
	}

	private void MoveRightBtn_Click(object sender, RoutedEventArgs e)
	{
		if (activeButton == defaultButton)
		{
			HandleHotkeyButtonClick(MoveRightBtn, MoveRightLabel);
			return;
		}
		ResetHotkeyButtons();
		ResetHotkeyLabels(MoveRightLabel);
	}

	private void ReelInBtn_Click(object sender, RoutedEventArgs e)
	{
		if (activeButton == defaultButton)
		{
			HandleHotkeyButtonClick(ReelInBtn, ReelInLabel);
			return;
		}
		ResetHotkeyButtons();
		ResetHotkeyLabels(ReelInLabel);
	}

	private void DismissBtn_Click(object sender, RoutedEventArgs e)
	{
		if (activeButton == defaultButton)
		{
			HandleHotkeyButtonClick(DismissBtn, DismissLabel);
			return;
		}
		ResetHotkeyButtons();
		ResetHotkeyLabels(DismissLabel);
	}

	private void DisableHotkeyButtons(Button clickedButton)
	{
		MoveLeftBtn.IsEnabled = MoveLeftBtn.Equals(clickedButton);
		MoveRightBtn.IsEnabled = MoveRightBtn.Equals(clickedButton);
		ReelInBtn.IsEnabled = ReelInBtn.Equals(clickedButton);
		DismissBtn.IsEnabled = DismissBtn.Equals(clickedButton);
	}

	private void ResetHotkeyButtons()
	{
		MoveLeftBtn.IsEnabled = true;
		MoveRightBtn.IsEnabled = true;
		ReelInBtn.IsEnabled = true;
		DismissBtn.IsEnabled = true;
		activeButton = defaultButton;
	}

	private void ResetHotkeyLabels(TextBlock label, bool resetText = true)
	{
		label.FontSize = 24.0;
		if (resetText)
		{
			label.Text = savedHotkeyText;
		}
		savedHotkeyText = "";
		activeLabel = defaultLabel;
	}

	private void SettingsWindow1_Closed(object sender, EventArgs e)
	{
		m_GlobalHook.KeyUp -= GlobalHookKeyUp;
		if (!string.IsNullOrEmpty(settings.DiscordHookUrl) && !string.IsNullOrEmpty(settings.DiscordUserId) && _updateDiscordUrl > 2 && _updateDiscordUser > 2)
		{
			try
			{
				DiscordService discordService = new DiscordService(settings.DiscordHookUrl, settings.DiscordUserId);
				Task<HookContent> task = discordService.BuildGenericNotification("If you receive this message, then the Discord Integration settings is succesfuly setup");
				task.Wait();
				HookContent notificationMsg = task.Result;
				discordService.SendMessage(notificationMsg).Wait();
			}
			catch (ArgumentException ex)
			{
				MessageBox.Show(ex.Message, "Discord Hook URL invalid", MessageBoxButton.OK, MessageBoxImage.Hand);
			}
		}
	}

	private TransformedBitmap RotateImage(ImageSource imageSource, int rotation)
	{
		TransformedBitmap transformedBitmap = new TransformedBitmap();
		transformedBitmap.BeginInit();
		transformedBitmap.Source = (BitmapImage)imageSource;
		transformedBitmap.Transform = new RotateTransform(rotation);
		transformedBitmap.EndInit();
		return transformedBitmap;
	}

	private void PositiveNumbersOnlyValidation(object sender, TextCompositionEventArgs e)
	{
		e.Handled = !int.TryParse(((TextBox)sender).Text + e.Text, out var i) || i < 0;
	}

	private void DelayTextBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		SaveSettings(((TextBox)sender).Name, ((TextBox)sender).Text);
	}

	private void SaveSettings(string textBoxName, string value)
	{
		int parsedInt = (int.TryParse(value, out parsedInt) ? parsedInt : 0);
		switch (textBoxName)
		{
		case "RestartDelayTextBox":
			settings.Delay_Restart = parsedInt;
			break;
		case "LagCompensationDelayTextBox":
			settings.Delay_LagCompensation = parsedInt;
			break;
		case "DimissDelayTextBox":
			settings.Delay_DismissFishCaptureDialogue = parsedInt;
			break;
		case "FishCaptureDelayTextBox":
			settings.Delay_FishCapture = parsedInt;
			break;
		case "PostCatchDelayTextBox":
			settings.Delay_PostCatch = parsedInt;
			break;
		case "AfterClickDelayTextBox":
			settings.Delay_AfterClick = parsedInt;
			break;
		case "DiscordUserIdTextBox":
			if (_updateDiscordUser > 1)
			{
				settings.DiscordUserId = value;
			}
			_updateDiscordUser++;
			break;
		case "DiscordWebHookTextBox":
			if (_updateDiscordUrl > 1)
			{
				settings.DiscordHookUrl = value;
			}
			_updateDiscordUrl++;
			break;
		}
	}

	private void ResetBtn_Click(object sender, RoutedEventArgs e)
	{
		settings.KeyCode_MoveLeft = 65;
		settings.KeyCode_MoveRight = 68;
		settings.KeyCode_FishCapture = 49;
		settings.KeyCode_DismissFishDialogue = 27;
		settings.Delay_LagCompensation = 5000;
		settings.Delay_FishCapture = 3000;
		settings.Delay_DismissFishCaptureDialogue = 4000;
		settings.Delay_Restart = 2000;
		settings.DiscordHookUrl = "";
		settings.DiscordUserId = "";
		settings.Delay_PostCatch = 500;
		settings.Delay_AfterClick = 1000;
		InitTheme(settings.IsDarkMode == 1);
	}

	private void GPUList_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		settings.DefaultAdapter = GPUList.SelectedIndex;
	}
}
