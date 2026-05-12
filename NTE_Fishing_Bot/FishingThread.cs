using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using OpenCvSharp.XImgProc;
using NTE_Fishing_Bot.Addon.DiscordInteractive;
using WindowsInput;
using WindowsInput.Native;

namespace NTE_Fishing_Bot;

internal class FishingThread
{
	private enum FishingState
	{
		NotFishing,
		Fishing,
		ReelingStart,
		Reeling,
		CaptureStart,
		Captured,
		ResetStart,
		Reset
	}

	private IAppSettings settings;

	public bool isRunning;

	private DateTime _startTime = DateTime.UtcNow;

	private DateTime? _lastResetTime;

	private InputSimulator InputSimulator;

	private System.Windows.Shapes.Rectangle left;

	private System.Windows.Shapes.Rectangle right;

	private Label cursorLabel;

	private Label middleBarLabel;

	private Label statusLabel;

	private System.Windows.Controls.Image middleBarImage;

	private System.Windows.Controls.Image cursorImage;

	private System.Windows.Controls.Image castReadyImage;

	private System.Windows.Shapes.Rectangle fishStaminaIndicator;

	private System.Windows.Shapes.Rectangle playerStaminaIndicator;

	private System.Windows.Shapes.Rectangle castIndicator;

	private System.Windows.Shapes.Rectangle xpBarIndicator;

	private System.Windows.Shapes.Rectangle dailyRewardIndicator;

	private System.Windows.Shapes.Rectangle escBox;

	private System.Windows.Shapes.Rectangle castKeyBox;

	private bool castButtonWasDetected;

	private DateTime? _lastCastPressedAt;

	private DateTime? _lastFPressedAt;

	private bool APressed;

	private bool DPressed;

	private bool PlayerStamina_lagCompensationDone;

	private DateTime? _fishingEnteredAt;
	private DateTime? _staminaGoneAt;

	private DateTime? _captureStartEnteredAt;

	private Bitmap bmp = new Bitmap(1, 1);

	private double colorThreshold;

	private double middleBarCenterThreshold;

	private ScreenStateLogger screenStateLogger;

	private Dispatcher dis = Dispatcher.CurrentDispatcher;


	private bool _postCatchRunning;
	private DateTime? _postCatchStartedAt;
	private DateTime? _lastDailyRewardClickAt;
	private bool _dailyRewardDismissRunning;

	private bool _fishCapturedNormally;

	private volatile bool _xpBarVisible;
	private volatile bool _xpBarSeenThisCycle;
	private volatile bool _castButtonVisible;

	public Action OnFishCaught;

	public Func<bool> OnCycleComplete;

	private CancellationTokenSource _cts = new CancellationTokenSource();

	private FishingState state;

	private string _statusMessage = "Waiting...";

	public IntPtr? GameHandle;

	private IDiscordService discordService;

	public FishingThread(IAppSettings _settings, System.Windows.Shapes.Rectangle _left, System.Windows.Shapes.Rectangle _right, Label _cursorLabel, Label _middleBarLabel, Label _statusLabel, System.Windows.Controls.Image _middleBarImage, System.Windows.Controls.Image _cursorImage, System.Windows.Controls.Image _castReadyImage, System.Windows.Shapes.Rectangle _fishStaminaIndicator, System.Windows.Shapes.Rectangle _playerStaminaIndicator, System.Windows.Shapes.Rectangle _castIndicator, System.Windows.Shapes.Rectangle _xpBarIndicator, System.Windows.Shapes.Rectangle _dailyRewardIndicator, System.Windows.Shapes.Rectangle _escBox, System.Windows.Shapes.Rectangle _castKeyBox, IntPtr? _gameHandle)
	{
		settings = _settings;
		left = _left;
		right = _right;
		cursorLabel = _cursorLabel;
		middleBarLabel = _middleBarLabel;
		statusLabel = _statusLabel;
		middleBarImage = _middleBarImage;
		cursorImage = _cursorImage;
		castReadyImage = _castReadyImage;
		fishStaminaIndicator = _fishStaminaIndicator;
		playerStaminaIndicator = _playerStaminaIndicator;
		castIndicator = _castIndicator;
		xpBarIndicator = _xpBarIndicator;
		dailyRewardIndicator = _dailyRewardIndicator;
		escBox = _escBox;
		castKeyBox = _castKeyBox;
		InputSimulator = new InputSimulator();
		dis = Dispatcher.CurrentDispatcher;
		state = FishingState.NotFishing;
		if (_gameHandle.HasValue)
		{
			GameHandle = _gameHandle.Value;
		}
		colorThreshold = settings.StaminaColorDetectionThreshold;
		middleBarCenterThreshold = settings.MiddlebarColorDetectionThreshold;
		screenStateLogger = new ScreenStateLogger(_settings);
		if (!string.IsNullOrEmpty(_settings.DiscordHookUrl))
		{
			try
			{
				_lastResetTime = null;
				discordService = new DiscordService(_settings.DiscordHookUrl, _settings.DiscordUserId);
			}
			catch (ArgumentException ex)
			{
				MessageBox.Show(ex.Message, "Discord Hook URL invalid", MessageBoxButton.OK, MessageBoxImage.Hand);
			}
		}
	}

	public void Start()
	{
		ScreenStateLogger obj = screenStateLogger;
		obj.ScreenRefreshed = (EventHandler<byte[]>)Delegate.Combine(obj.ScreenRefreshed, (EventHandler<byte[]>)delegate(object sender, byte[] data)
		{
			if (!isRunning) return;
			statusLabel.Dispatcher.InvokeAsync(delegate
			{
				statusLabel.Content = "Status: " + _statusMessage + ((!GameHandle.HasValue) ? " (Game not running)" : string.Empty);
			});
			bmp = new Bitmap(new MemoryStream(data));
			bool fishStaminaDetected = FishStaminaDetector(bmp);
			bool playerStaminaDetected = PlayerStaminaDetector(bmp);
			bool xpBarDetected = XpBarDetector(bmp);
			_xpBarVisible = xpBarDetected;
			if (xpBarDetected) _xpBarSeenThisCycle = true;
			_castButtonVisible = CastReadyDetector(bmp);
			fishStaminaIndicator.Dispatcher.InvokeAsync(delegate
			{
				fishStaminaIndicator.Fill = fishStaminaDetected ? System.Windows.Media.Brushes.Lime : System.Windows.Media.Brushes.Red;
			});
			playerStaminaIndicator.Dispatcher.InvokeAsync(delegate
			{
				playerStaminaIndicator.Fill = playerStaminaDetected ? System.Windows.Media.Brushes.Lime : System.Windows.Media.Brushes.Red;
			});
			castIndicator.Dispatcher.InvokeAsync(delegate
			{
				castIndicator.Fill = _castButtonVisible ? System.Windows.Media.Brushes.Lime : System.Windows.Media.Brushes.Red;
			});
			xpBarIndicator.Dispatcher.InvokeAsync(delegate
			{
				xpBarIndicator.Fill = xpBarDetected ? System.Windows.Media.Brushes.Lime : System.Windows.Media.Brushes.Red;
			});
			bool dailyRewardDetected = DailyRewardDetector(bmp);
			dailyRewardIndicator.Dispatcher.InvokeAsync(delegate
			{
				dailyRewardIndicator.Fill = dailyRewardDetected ? System.Windows.Media.Brushes.Lime : System.Windows.Media.Brushes.Red;
			});
			if (dailyRewardDetected && !_dailyRewardDismissRunning &&
				(!_lastDailyRewardClickAt.HasValue ||
				 (DateTime.UtcNow - _lastDailyRewardClickAt.Value).TotalSeconds >= 5))
			{
				_lastDailyRewardClickAt = DateTime.UtcNow;
				_dailyRewardDismissRunning = true;
				BotLogger.Log("Daily reward popup detected. Clicking to dismiss (3x)");
				var drToken = _cts.Token;
				Task.Run(async () =>
				{
					try
					{
						for (int i = 0; i < 3; i++)
						{
							ClickCenterOfGame();
							if (i < 2) await Task.Delay(3000, drToken);
						}
					}
					catch (OperationCanceledException) { }
					finally { _dailyRewardDismissRunning = false; }
				});
			}
			using Mat frame = ExtractCroppedFrame(bmp);
			double middleBarAveragePos = GetMiddleBarAveragePos(frame);
			double fishingCursorPos = GetFishingCursorPos(frame);
			CenterCursor(middleBarAveragePos, fishingCursorPos);

			// Watchdog 1: stuck in post-catch dismiss loop but game indicators say we've resumed
			if (_postCatchRunning && _postCatchStartedAt.HasValue &&
				(DateTime.UtcNow - _postCatchStartedAt.Value).TotalSeconds >= 8)
			{
				bool gameResumed = (fishStaminaDetected && playerStaminaDetected) ||
				                   (_castButtonVisible && state == FishingState.ResetStart);
				if (gameResumed)
				{
					BotLogger.Log("Watchdog 1: post-catch exceeded 8s and game resumed. Recovering");
					_cts.Cancel();
					_cts = new CancellationTokenSource();
					_postCatchRunning = false;
					_postCatchStartedAt = null;
					state = FishingState.NotFishing;
					castButtonWasDetected = false;
					_statusMessage = "Recovered, resuming...";
				}
			}
			// Watchdog 2: task exited but state never got reset from ResetStart — recover immediately
			if (state == FishingState.ResetStart && !_postCatchRunning)
			{
				BotLogger.Log("Watchdog 2: stuck in ResetStart. Recovering, casting again");
				state = FishingState.NotFishing;
				castButtonWasDetected = false;
				_statusMessage = "Casting again...";
				if (isRunning) PressCastKey();
			}

			switch (state)
			{
			case FishingState.NotFishing:
				if (discordService != null && _lastResetTime.HasValue)
				{
					DateTime utcNow = DateTime.UtcNow;
					DateTime? lastResetTime = _lastResetTime;
					if (utcNow - lastResetTime > TimeSpan.FromMinutes(3.0))
					{
						Task<HookContent> task = discordService.BuildOutOfBaitNotification(_startTime);
						task.Wait();
						HookContent result = task.Result;
						discordService.SendMessage(result).Wait();
						_lastResetTime = null;
					}
				}
				if (_castButtonVisible)
				{
					bool retryNeeded = _lastCastPressedAt.HasValue &&
						(DateTime.UtcNow - _lastCastPressedAt.Value).TotalMilliseconds >= 5000;
					if (!castButtonWasDetected || retryNeeded)
					{
						_statusMessage = "Casting...";
						PressCastKey();
						_lastCastPressedAt = DateTime.UtcNow;
					}
				}
				else
				{
					_lastCastPressedAt = null;
				}
				castButtonWasDetected = _castButtonVisible;
				break;
			case FishingState.Captured:
				ResetKeys();
				StartPostCatchSequence();
				break;
			}
			switch (state)
			{
			case FishingState.NotFishing:
				if (fishStaminaDetected && playerStaminaDetected)
				{
					state = FishingState.Fishing;
					_statusMessage = "Fishing...";
					PlayerStamina_lagCompensationDone = false;
					_fishingEnteredAt = DateTime.UtcNow;
					_staminaGoneAt = null;
					BotLogger.Log("State: NotFishing → Fishing (fish + player stamina detected)");
				}
				break;
			case FishingState.Fishing:
				if (!PlayerStamina_lagCompensationDone && _fishingEnteredAt.HasValue &&
					(DateTime.UtcNow - _fishingEnteredAt.Value).TotalMilliseconds >= settings.Delay_LagCompensation)
				{
					PlayerStamina_lagCompensationDone = true;
				}
				if (PlayerStamina_lagCompensationDone)
				{
					if (!fishStaminaDetected || !playerStaminaDetected)
					{
						// Require bars to be absent for 500ms before transitioning — filters
						// single-frame detection blips that would otherwise start the countdown
						// while the fish is still actively being reeled in.
						if (!_staminaGoneAt.HasValue)
							_staminaGoneAt = DateTime.UtcNow;
						else if ((DateTime.UtcNow - _staminaGoneAt.Value).TotalMilliseconds >= 500)
						{
							_staminaGoneAt = null;
							_statusMessage = "Waiting for EXP menu...";
							state = FishingState.CaptureStart;
							_captureStartEnteredAt = DateTime.UtcNow;
							_xpBarSeenThisCycle = false;
							BotLogger.Log($"State: Fishing → CaptureStart (stamina gone 500ms, fish:{fishStaminaDetected} player:{playerStaminaDetected})");
						}
					}
					else
					{
						_staminaGoneAt = null;
					}
				}
				break;
			case FishingState.CaptureStart:
				if (xpBarDetected)
				{
					_fishCapturedNormally = true;
					_statusMessage = "Caught fish!";
					state = FishingState.Captured;
					BotLogger.Log("State: CaptureStart → Captured (XP bar detected. Fish caught!)");
					OnFishCaught?.Invoke();
				}
				else if (_captureStartEnteredAt.HasValue &&
					(DateTime.UtcNow - _captureStartEnteredAt.Value).TotalMilliseconds >= 12000)
				{
					_fishCapturedNormally = false;
					_statusMessage = "No EXP detected, dismissing anyway...";
					state = FishingState.Captured;
					BotLogger.Log("State: CaptureStart → Captured (12s timeout. No XP bar, treating as escaped)");
				}
				else
				{
					if (_captureStartEnteredAt.HasValue)
					{
						int remaining = (int)Math.Ceiling(12.0 - (DateTime.UtcNow - _captureStartEnteredAt.Value).TotalSeconds);
						_statusMessage = $"Waiting for EXP... {remaining}s";
					}
				}
				break;
			case FishingState.Captured:
				BotLogger.Log("State: Captured → ResetStart");
				state = FishingState.ResetStart;
				break;
			case FishingState.ResetStart:
				_lastResetTime = DateTime.UtcNow;
				break;
			case FishingState.Reset:
				state = FishingState.NotFishing;
				break;
			}
		});
		screenStateLogger.CaptureError += (sender, error) =>
		{
			dis.Invoke(delegate
			{
				MessageBox.Show("Screen capture failed:\n" + error, "Capture Error", MessageBoxButton.OK, MessageBoxImage.Error);
			});
		};
		isRunning = true;
		_startTime = DateTime.UtcNow;
		BotLogger.Log($"Bot started. Game handle: {(GameHandle.HasValue ? "found" : "NOT found (simulation mode)")}");
		screenStateLogger.Start();

		// Mirrors the F-spam loop in post-catch so the very first cycle behaves the same as every
		// subsequent one — without this, the bot only presses F once on startup and then stops when
		// the button turns blue (fish bite), never hooking the fish until the first full loop runs.
		var initToken = _cts.Token;
		Task.Run(async () =>
		{
			try
			{
				while (isRunning && state == FishingState.NotFishing)
				{
					await Task.Delay(700, initToken);
					if (isRunning && state == FishingState.NotFishing)
						PressCastKey(silent: true);
				}
			}
			catch (OperationCanceledException) { }
		});
	}

	private Mat ExtractCroppedFrame(Bitmap image)
	{
		using Bitmap cropped = image.CropSmall(settings.UpperLeftBarPoint_X, settings.UpperLeftBarPoint_Y, settings.LowerRightBarPoint_X - settings.UpperLeftBarPoint_X, settings.LowerRightBarPoint_Y - settings.UpperLeftBarPoint_Y);
		if (cropped.Height > 5)
		{
			using Bitmap bordered = new Bitmap(cropped.Width + 20, cropped.Height);
			using (Graphics g = Graphics.FromImage(bordered))
				g.DrawImage(cropped, new System.Drawing.Point(9, 0));
			return bordered.ToMat();
		}
		using Bitmap bordered2 = new Bitmap(cropped.Width * 4 + 20, cropped.Height * 4);
		using (Graphics g2 = Graphics.FromImage(bordered2))
		{
			g2.InterpolationMode = InterpolationMode.HighQualityBicubic;
			g2.DrawImage(cropped, 9, 0, cropped.Width * 4 + 20, cropped.Height * 4);
		}
		return bordered2.ToMat();
	}

	private bool FishStaminaDetector(Bitmap image)
	{
		System.Drawing.Color pixelColor = image.GetPixel(settings.FishStaminaPoint_X, settings.FishStaminaPoint_Y);
		double colorDistance = Math.Sqrt(Math.Pow(pixelColor.R - settings.FishStaminaColor_R, 2.0) + Math.Pow(pixelColor.G - settings.FishStaminaColor_G, 2.0) + Math.Pow(pixelColor.B - settings.FishStaminaColor_B, 2.0));
		cursorLabel.Dispatcher.InvokeAsync(delegate
		{
			cursorLabel.Content = colorDistance.ToString("0.##");
		});
		return colorDistance < colorThreshold;
	}

	private bool PlayerStaminaDetector(Bitmap image)
	{
		if (settings.PlayerStaminaPoint_X == 0 && settings.PlayerStaminaPoint_Y == 0)
		{
			middleBarLabel.Dispatcher.InvokeAsync(delegate { middleBarLabel.Content = "N/A"; });
			return true;
		}
		System.Drawing.Color pixelColor = image.GetPixel(settings.PlayerStaminaPoint_X, settings.PlayerStaminaPoint_Y);
		double colorDistance = Math.Sqrt(Math.Pow(pixelColor.R - settings.PlayerStaminaColor_R, 2.0) + Math.Pow(pixelColor.G - settings.PlayerStaminaColor_G, 2.0) + Math.Pow(pixelColor.B - settings.PlayerStaminaColor_B, 2.0));
		middleBarLabel.Dispatcher.InvokeAsync(delegate
		{
			middleBarLabel.Content = colorDistance.ToString("0.##");
		});
		// Bar is present if it matches configured color (light blue) OR if pixel is white (bar is draining but not gone)
		bool isWhite = pixelColor.R > 180 && pixelColor.G > 180 && pixelColor.B > 180;
		return colorDistance < colorThreshold || isWhite;
	}

	private void CenterCursor(double middleBarPos, double fishingCursorPos)
	{
		if (!isRunning)
		{
			ResetKeys();
			return;
		}
		if (middleBarPos - middleBarCenterThreshold > fishingCursorPos)
		{
			if (!DPressed)
			{
				ReleaseKey((VirtualKeyCode)settings.KeyCode_MoveLeft);
				HoldKey((VirtualKeyCode)settings.KeyCode_MoveRight);
				DPressed = true;
				APressed = false;
				right.Dispatcher.InvokeAsync(delegate
				{
					right.Fill = ((settings.IsDarkMode > 0) ? Theme.ColorAccent2 : Theme.GreenColor);
				});
				left.Dispatcher.InvokeAsync(delegate
				{
					left.Fill = System.Windows.Media.Brushes.Transparent;
				});
			}
			return;
		}
		if (middleBarPos + middleBarCenterThreshold < fishingCursorPos)
		{
			if (!APressed)
			{
				ReleaseKey((VirtualKeyCode)settings.KeyCode_MoveRight);
				HoldKey((VirtualKeyCode)settings.KeyCode_MoveLeft);
				DPressed = false;
				APressed = true;
				left.Dispatcher.InvokeAsync(delegate
				{
					left.Fill = ((settings.IsDarkMode > 0) ? Theme.ColorAccent2 : Theme.GreenColor);
				});
				right.Dispatcher.InvokeAsync(delegate
				{
					right.Fill = System.Windows.Media.Brushes.Transparent;
				});
			}
			return;
		}
		if (APressed)
		{
			ReleaseKey((VirtualKeyCode)settings.KeyCode_MoveLeft);
			APressed = false;
			left.Dispatcher.InvokeAsync(delegate
			{
				left.Fill = System.Windows.Media.Brushes.Transparent;
			});
		}
		if (DPressed)
		{
			ReleaseKey((VirtualKeyCode)settings.KeyCode_MoveRight);
			DPressed = false;
			right.Dispatcher.InvokeAsync(delegate
			{
				right.Fill = System.Windows.Media.Brushes.Transparent;
			});
		}
	}

	public void Stop()
	{
		BotLogger.Log("Bot stopped");
		isRunning = false;
		_cts.Cancel();
		_cts = new CancellationTokenSource();
		_lastResetTime = null;
		_lastFPressedAt = null;
		_lastDailyRewardClickAt = null;
		state = FishingState.NotFishing;
		_statusMessage = "Stopped";
		ResetKeys();
		ResetIndicators();
		screenStateLogger.Stop();
		screenStateLogger = new ScreenStateLogger(settings);
	}

	private void ResetIndicators()
	{
		var gray = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x44, 0x44, 0x44));
		gray.Freeze();
		fishStaminaIndicator.Dispatcher.Invoke(() => fishStaminaIndicator.Fill = gray);
		playerStaminaIndicator.Dispatcher.Invoke(() => playerStaminaIndicator.Fill = gray);
		castIndicator.Dispatcher.Invoke(() => castIndicator.Fill = gray);
		xpBarIndicator.Dispatcher.Invoke(() => xpBarIndicator.Fill = gray);
		dailyRewardIndicator.Dispatcher.Invoke(() => dailyRewardIndicator.Fill = gray);
		statusLabel.Dispatcher.Invoke(() => statusLabel.Content = "Status: Stopped");
		escBox.Dispatcher.Invoke(() => escBox.Fill = System.Windows.Media.Brushes.Transparent);
		castKeyBox.Dispatcher.Invoke(() => castKeyBox.Fill = System.Windows.Media.Brushes.Transparent);
	}

	public double GetMiddleBarAveragePos(Mat frame)
	{
		try
		{
			using Mat hsv = new Mat();
			Cv2.CvtColor(frame, hsv, ColorConversionCodes.BGR2HSV);

			using Mat colorSample = new Mat(1, 1, MatType.CV_8UC3,
				new Scalar(settings.MiddleBarColor_B, settings.MiddleBarColor_G, settings.MiddleBarColor_R));
			using Mat colorHSV = new Mat();
			Cv2.CvtColor(colorSample, colorHSV, ColorConversionCodes.BGR2HSV);
			Vec3b h = colorHSV.At<Vec3b>(0, 0);
			Scalar lower = new Scalar(Math.Max(0, h[0] - 15), Math.Max(40, h[1] - 60), Math.Max(50, h[2] - 60));
			Scalar upper = new Scalar(Math.Min(180, h[0] + 15), 255, 255);

			using Mat masked = new Mat();
			Cv2.InRange(hsv, lower, upper, masked);

			using Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3));
			using Mat opened = new Mat();
			using Mat closed = new Mat();
			Cv2.MorphologyEx(masked, opened, MorphTypes.Open, kernel);
			Cv2.MorphologyEx(opened, closed, MorphTypes.Close, kernel);

			var capturedMs1 = new MemoryStream();
			using (var debugBmp = closed.ToBitmap())
				debugBmp.Save(capturedMs1, ImageFormat.Png);
			capturedMs1.Position = 0L;
			dis.InvokeAsync(() =>
			{
				var bmi = new BitmapImage();
				bmi.BeginInit();
				bmi.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
				bmi.StreamSource = capturedMs1;
				bmi.EndInit();
				bmi.Freeze();
				middleBarImage.Source = bmi;
				capturedMs1.Dispose();
			});

			Cv2.FindContours(closed, out OpenCvSharp.Point[][] contours, out _,
				RetrievalModes.External, ContourApproximationModes.ApproxSimple);

			if (contours.Length == 0) return 0.0;

			// Top-2 contours by area are the two halves of the green zone; combine their extents
			var sorted = contours.OrderByDescending(c => Cv2.ContourArea(c)).Take(2).ToArray();
			var rects = sorted.Select(c => Cv2.BoundingRect(c)).ToArray();
			int zoneLeft  = rects.Min(r => r.Left);
			int zoneRight = rects.Max(r => r.Right);
			return (zoneLeft + zoneRight) / 2.0;
		}
		catch (Exception)
		{
			return 0.0;
		}
	}

	public double GetFishingCursorPos(Mat frame)
	{
		try
		{
			using Mat hsv = new Mat();
			Cv2.CvtColor(frame, hsv, ColorConversionCodes.BGR2HSV);

			using Mat colorSample = new Mat(1, 1, MatType.CV_8UC3,
				new Scalar(settings.CursorColor_B, settings.CursorColor_G, settings.CursorColor_R));
			using Mat colorHSV = new Mat();
			Cv2.CvtColor(colorSample, colorHSV, ColorConversionCodes.BGR2HSV);
			Vec3b h = colorHSV.At<Vec3b>(0, 0);
			Scalar lower = new Scalar(Math.Max(0, h[0] - 15), Math.Max(40, h[1] - 60), Math.Max(50, h[2] - 60));
			Scalar upper = new Scalar(Math.Min(180, h[0] + 15), 255, 255);

			using Mat masked = new Mat();
			Cv2.InRange(hsv, lower, upper, masked);

			using Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3));
			using Mat opened = new Mat();
			using Mat closed = new Mat();
			Cv2.MorphologyEx(masked, opened, MorphTypes.Open, kernel);
			Cv2.MorphologyEx(opened, closed, MorphTypes.Close, kernel);

			var capturedMs2 = new MemoryStream();
			using (var debugBmp = closed.ToBitmap())
				debugBmp.Save(capturedMs2, ImageFormat.Png);
			capturedMs2.Position = 0L;
			dis.InvokeAsync(() =>
			{
				var bmi = new BitmapImage();
				bmi.BeginInit();
				bmi.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
				bmi.StreamSource = capturedMs2;
				bmi.EndInit();
				bmi.Freeze();
				cursorImage.Source = bmi;
				capturedMs2.Dispose();
			});

			Cv2.FindContours(closed, out OpenCvSharp.Point[][] contours, out _,
				RetrievalModes.External, ContourApproximationModes.ApproxSimple);

			if (contours.Length == 0) return 0.0;

			// Largest contour by area is the cursor indicator
			var largest = contours.OrderByDescending(c => Cv2.ContourArea(c)).First();
			var rect = Cv2.BoundingRect(largest);
			return rect.X + rect.Width / 2.0;
		}
		catch (Exception)
		{
			return 0.0;
		}
	}

	private void HoldKey(VirtualKeyCode key)
	{
		if (!GameHandle.HasValue) return;
		if (settings.IsBackgroundInput == 1)
		{
			PostActivate();
			PostKeyDown(key);
		}
		else
			InputSimulator.Keyboard.KeyDown(key);
	}

	private void ReleaseKey(VirtualKeyCode key)
	{
		if (!GameHandle.HasValue) return;
		if (settings.IsBackgroundInput == 1)
			PostKeyUp(key);
		else
			InputSimulator.Keyboard.KeyUp(key);
	}

	private void PressKey(VirtualKeyCode key)
	{
		if (!GameHandle.HasValue) return;
		if (settings.IsBackgroundInput == 1)
		{
			PostActivate();
			PostKeyDown(key);
			System.Threading.Thread.Sleep(25);
			PostKeyUp(key);
		}
		else
		{
			InputSimulator.Keyboard.KeyDown(key);
			InputSimulator.Mouse.Sleep(15);
			InputSimulator.Keyboard.KeyUp(key);
		}
	}

	[DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
	[DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
	[DllImport("user32.dll")] private static extern bool BringWindowToTop(IntPtr hWnd);
	[DllImport("user32.dll")] private static extern void SwitchToThisWindow(IntPtr hWnd, bool fAltTab);
	[DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
	[DllImport("user32.dll")] private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);
	[DllImport("user32.dll")] private static extern bool ClientToScreen(IntPtr hWnd, ref System.Drawing.Point lpPoint);
	[DllImport("user32.dll")] private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
	[DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
	[DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);
	[DllImport("user32.dll")] private static extern int GetSystemMetrics(int nIndex);
	[DllImport("user32.dll")] private static extern bool GetCursorPos(out System.Drawing.Point lpPoint);
	[DllImport("user32.dll")] private static extern bool SetCursorPos(int x, int y);
	[DllImport("user32.dll")] private static extern uint MapVirtualKey(uint uCode, uint uMapType);
	[DllImport("user32.dll")] private static extern IntPtr ChildWindowFromPoint(IntPtr hWndParent, System.Drawing.Point pt);
	[DllImport("user32.dll")] private static extern bool ScreenToClient(IntPtr hWnd, ref System.Drawing.Point lpPoint);
	[DllImport("user32.dll")] private static extern bool BlockInput(bool fBlockIt);

	private const uint WM_ACTIVATE    = 0x0006;
	private const uint WA_ACTIVE      = 1;
	private const uint WM_KEYDOWN     = 0x0100;
	private const uint WM_KEYUP       = 0x0101;
	private const uint WM_MOUSEMOVE   = 0x0200;
	private const uint WM_LBUTTONDOWN = 0x0201;
	private const uint WM_LBUTTONUP   = 0x0202;
	private const uint MK_LBUTTON     = 0x0001;

	[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
	private struct RECT { public int Left, Top, Right, Bottom; }

	private const uint SWP_NOSIZE = 0x0001;
	private const uint SWP_NOZORDER = 0x0004;

	// Send WM_ACTIVATE to tell the game it's active without stealing focus
	private void PostActivate()
	{
		if (!GameHandle.HasValue) return;
		PostMessage(GameHandle.Value, WM_ACTIVATE, new IntPtr(WA_ACTIVE), IntPtr.Zero);
	}

	private void PostKeyDown(VirtualKeyCode key)
	{
		if (!GameHandle.HasValue) return;
		uint vk = (uint)key;
		uint sc = MapVirtualKey(vk, 0);
		uint lp = (sc << 16) | 1u;
		PostMessage(GameHandle.Value, WM_KEYDOWN, new IntPtr(vk), new IntPtr(unchecked((int)lp)));
	}

	private void PostKeyUp(VirtualKeyCode key)
	{
		if (!GameHandle.HasValue) return;
		uint vk = (uint)key;
		uint sc = MapVirtualKey(vk, 0);
		uint lp = ((sc << 16) | 1u) | 0xC0000000u;
		PostMessage(GameHandle.Value, WM_KEYUP, new IntPtr(vk), new IntPtr(unchecked((int)lp)));
	}

	public void ClickFishCaptureButton()
	{
		PressKey((VirtualKeyCode)settings.KeyCode_FishCapture);
	}

	private bool CastButtonDetector(Bitmap image)
	{
		if (settings.CastButtonPoint_X == 0 && settings.CastButtonPoint_Y == 0)
			return false;
		System.Drawing.Color pixelColor = image.GetPixel(settings.CastButtonPoint_X, settings.CastButtonPoint_Y);
		double colorDistance = Math.Sqrt(Math.Pow(pixelColor.R - settings.CastButtonColor_R, 2.0) + Math.Pow(pixelColor.G - settings.CastButtonColor_G, 2.0) + Math.Pow(pixelColor.B - settings.CastButtonColor_B, 2.0));
		return colorDistance < colorThreshold;
	}

	// Detects the idle/ready-to-cast state of the fish hooked button: grey background + white hook icon.
	// The same button turns blue when a fish bites (CastButtonDetector); here we look for the resting state.
	// Shows the mask in cursorImage when not actively reeling so it doesn't stomp the reeling debug.
	private bool CastReadyDetector(Bitmap image)
	{
		if (settings.CastButtonPoint_X == 0 && settings.CastButtonPoint_Y == 0)
			return false;

		int cx = settings.CastButtonPoint_X;
		int cy = settings.CastButtonPoint_Y;
		const int radius = 20;

		int x1 = Math.Max(0, cx - radius);
		int y1 = Math.Max(0, cy - radius);
		int w  = Math.Min(image.Width  - x1, radius * 2);
		int h  = Math.Min(image.Height - y1, radius * 2);
		if (w <= 0 || h <= 0) return false;

		using Bitmap crop = image.Clone(new System.Drawing.Rectangle(x1, y1, w, h), image.PixelFormat);
		using Mat mat = crop.ToMat();
		using Mat hsv = new Mat();
		Cv2.CvtColor(mat, hsv, ColorConversionCodes.BGR2HSV);

		// Grey + white = low saturation (S < 60) with sufficient brightness (V > 60)
		using Mat mask = new Mat();
		Cv2.InRange(hsv, new Scalar(0, 0, 60), new Scalar(180, 60, 255), mask);

		var ms = new MemoryStream();
		using (var debugBmp = mask.ToBitmap())
			debugBmp.Save(ms, ImageFormat.Png);
		ms.Position = 0L;
		dis.InvokeAsync(() =>
		{
			var bmi = new BitmapImage();
			bmi.BeginInit();
			bmi.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
			bmi.StreamSource = ms;
			bmi.EndInit();
			bmi.Freeze();
			castReadyImage.Source = bmi;
			ms.Dispose();
		});

		return Cv2.CountNonZero(mask) >= 50;
	}

	private bool DailyRewardDetector(Bitmap image)
	{
		if (settings.DailyRewardPoint_X == 0 && settings.DailyRewardPoint_Y == 0)
			return false;
		System.Drawing.Color pixel = image.GetPixel(settings.DailyRewardPoint_X, settings.DailyRewardPoint_Y);
		double dist = Math.Sqrt(Math.Pow(pixel.R - settings.DailyRewardColor_R, 2.0) + Math.Pow(pixel.G - settings.DailyRewardColor_G, 2.0) + Math.Pow(pixel.B - settings.DailyRewardColor_B, 2.0));
		return dist < colorThreshold;
	}

	private bool XpBarDetector(Bitmap image)
	{
		if (settings.XpBarPoint_X == 0 && settings.XpBarPoint_Y == 0)
			return false;

		// Primary: check configured point for pink XP bar
		System.Drawing.Color pixel = image.GetPixel(settings.XpBarPoint_X, settings.XpBarPoint_Y);
		double dist = Math.Sqrt(Math.Pow(pixel.R - settings.XpBarColor_R, 2.0) + Math.Pow(pixel.G - settings.XpBarColor_G, 2.0) + Math.Pow(pixel.B - settings.XpBarColor_B, 2.0));
		if (dist < colorThreshold)
			return true;

		// Double-check: same color 20px to the right (catches the bar when it's partially off-center)
		int x2 = Math.Min(settings.XpBarPoint_X + 20, image.Width - 1);
		System.Drawing.Color pixel2 = image.GetPixel(x2, settings.XpBarPoint_Y);
		double dist2 = Math.Sqrt(Math.Pow(pixel2.R - settings.XpBarColor_R, 2.0) + Math.Pow(pixel2.G - settings.XpBarColor_G, 2.0) + Math.Pow(pixel2.B - settings.XpBarColor_B, 2.0));
		if (dist2 < colorThreshold)
			return true;

		// Fallback: scan right for the EXP-gained indicator rgba(185, 231, 4) shown when bar is too low
		int endX = Math.Min(settings.XpBarPoint_X + 400, image.Width - 1);
		for (int x = settings.XpBarPoint_X; x <= endX; x += 4)
		{
			System.Drawing.Color c = image.GetPixel(x, settings.XpBarPoint_Y);
			double d = Math.Sqrt(Math.Pow(c.R - 185, 2.0) + Math.Pow(c.G - 231, 2.0) + Math.Pow(c.B - 4, 2.0));
			if (d < colorThreshold)
				return true;
		}
		return false;
	}

	private void PressCastKey(bool silent = false)
	{
		if (!GameHandle.HasValue) return;
		if (!silent) BotLogger.Log("Action: Pressing cast key (F)");
		_lastFPressedAt = DateTime.UtcNow;
		castKeyBox.Dispatcher.InvokeAsync(() => castKeyBox.Fill = (settings.IsDarkMode > 0) ? Theme.ColorAccent3 : Theme.GreenColor);
		PressKey((VirtualKeyCode)settings.KeyCode_CastFish);
		Task.Delay(300).ContinueWith(_ => castKeyBox.Dispatcher.InvokeAsync(() => castKeyBox.Fill = System.Windows.Media.Brushes.Transparent));
	}

	private void StartPostCatchSequence()
	{
		if (_postCatchRunning) return;
		_postCatchRunning = true;
		_postCatchStartedAt = DateTime.UtcNow;
		bool capturedNormally = _fishCapturedNormally;
		BotLogger.Log($"Post-catch: sequence started (fish captured normally: {capturedNormally})");
		var token = _cts.Token;
		Task.Run(async () =>
		{
			try
			{
				await Task.Delay(settings.Delay_PostCatch, token);
				bool didDismiss = false;
				bool xpBarConfigured = settings.XpBarPoint_X != 0 || settings.XpBarPoint_Y != 0;

				if (xpBarConfigured)
				{
					// Always wait for the XP bar before recasting. The cast button can appear during
					// rare-fish catch animations before the XP bar shows — never use it as an escape
					// signal from CaptureStart. Post-catch detects genuine escapes via cast-ready
					// stable 5s with no XP bar seen.
					_statusMessage = "Waiting for XP bar...";
					BotLogger.Log("Post-catch: waiting for XP bar (blocks cast until confirmed and dismissed)");
					int xpWaitMs = 0;
					int castStableMs = 0;
					bool castReadyConfigured = settings.CastButtonPoint_X != 0 || settings.CastButtonPoint_Y != 0;
					while (!_xpBarSeenThisCycle && isRunning && xpWaitMs < 15000)
					{
						await Task.Delay(200, token);
						xpWaitMs += 200;
						if (castReadyConfigured && _castButtonVisible) castStableMs += 200;
						else castStableMs = 0;
						if (castStableMs >= 5000)
						{
							BotLogger.Log("Post-catch: cast-ready stable 5s, no XP bar. Fast genuine escape");
							break;
						}
					}

					if (_xpBarSeenThisCycle)
					{
						// XP bar confirmed — wait briefly in case it flickered off before we got here
						int visWaitMs = 0;
						while (!_xpBarVisible && isRunning && visWaitMs < 2000)
						{
							await Task.Delay(100, token);
							visWaitMs += 100;
						}
						_statusMessage = "Dismissing dialog...";
						BotLogger.Log("Post-catch: XP bar confirmed. Dismissing caught fish display");
						ClickCenterOfGame();
						await Task.Delay(800, token);
						int dismissRetries = 0;
						while (_xpBarVisible && isRunning && dismissRetries < 8)
						{
							dismissRetries++;
							_statusMessage = $"XP bar still visible, retrying ({dismissRetries})...";
							BotLogger.Log($"Post-catch: XP bar still visible. Retrying dismiss ({dismissRetries}/8)");
							ClickCenterOfGame();
							await Task.Delay(500, token);
						}
						BotLogger.Log("Post-catch: dismiss complete");
						didDismiss = true;
					}
					else
					{
						BotLogger.Log("Post-catch: no XP bar after 15s. Genuine escape, waiting cooldown");
						_statusMessage = "Fish escaped...";
						await Task.Delay(settings.Delay_AfterClick, token);
					}
				}
				else
				{
					// XP bar not configured — fall back to capturedNormally logic
					if (capturedNormally)
					{
						_statusMessage = "Dismissing dialog...";
						BotLogger.Log("Post-catch: XP bar not configured, capturedNormally. Dismissing");
						ClickCenterOfGame();
						await Task.Delay(800, token);
						int dismissRetries = 0;
						while (_xpBarVisible && isRunning && dismissRetries < 8)
						{
							dismissRetries++;
							_statusMessage = $"XP bar still visible, retrying ({dismissRetries})...";
							BotLogger.Log($"Post-catch: XP bar still visible. Retrying dismiss ({dismissRetries}/8)");
							ClickCenterOfGame();
							await Task.Delay(500, token);
						}
						BotLogger.Log("Post-catch: dismiss complete");
						didDismiss = true;
					}
					else
					{
						BotLogger.Log("Post-catch: XP bar not configured, fish escaped. Waiting cooldown");
						_statusMessage = "Fish escaped...";
						await Task.Delay(settings.Delay_AfterClick, token);
					}
				}

				// Fallback: if XP bar is still somehow visible, keep clicking until it clears
				if (_xpBarVisible && isRunning)
				{
					BotLogger.Log("Post-catch: XP bar persists after retries. Fallback clicking every 2s");
					while (_xpBarVisible && isRunning)
					{
						_statusMessage = "XP bar stuck, clicking...";
						ClickCenterOfGame();
						await Task.Delay(2000, token);
					}
					BotLogger.Log("Post-catch: XP bar cleared");
					didDismiss = true;
				}

				// Cast button wait — only after a confirmed dismiss; skip for genuine escapes
				if (didDismiss && (settings.CastButtonPoint_X != 0 || settings.CastButtonPoint_Y != 0))
				{
					_statusMessage = "Waiting for cast button...";
					BotLogger.Log("Post-catch: waiting for cast button before recasting");
					int castWaitMs = 0;
					while ((!_castButtonVisible || _xpBarVisible) && isRunning && castWaitMs < 3000)
					{
						await Task.Delay(100, token);
						castWaitMs += 100;
					}
					if (_castButtonVisible && !_xpBarVisible)
						BotLogger.Log("Post-catch: cast button visible. Game is ready");
					else
						BotLogger.Log("Post-catch: cast button wait timed out. Casting anyway");
				}
				await Task.Delay(200, token);
				_statusMessage = "Casting again...";
				BotLogger.Log("Post-catch: casting again");
				if (OnCycleComplete != null && !OnCycleComplete())
				{
					BotLogger.Log("Out of bait. Bot stopped.");
					isRunning = false;
					_statusMessage = "Out of bait.";
					return;
				}
				state = FishingState.NotFishing;
				PressCastKey();
				_lastResetTime = DateTime.UtcNow;

				_statusMessage = "Waiting for fish...";
				int spamCount = 0;
				BotLogger.Log("Waiting: spammed F (x1)");
				while (isRunning && state == FishingState.NotFishing)
				{
					await Task.Delay(700, token);
					if (!isRunning || state != FishingState.NotFishing) break;
					spamCount++;
					PressCastKey(silent: true);
					BotLogger.UpdateLast($"Waiting: spammed F (x{spamCount + 1})");
				}
				BotLogger.Log("Fishing bar detected. Stopping F spam");
			}
			catch (OperationCanceledException) { }
			catch (Exception ex)
			{
				BotLogger.Log($"Post-catch: unexpected error. {ex.GetType().Name}: {ex.Message}");
				state = FishingState.NotFishing;
				castButtonWasDetected = false;
				_statusMessage = "Casting again...";
				if (isRunning) PressCastKey();
			}
			finally
			{
				_postCatchRunning = false;
				_postCatchStartedAt = null;
			}
		});
	}

	private void ClickCenterOfGame()
	{
		if (!GameHandle.HasValue) return;
		BotLogger.Log("Action: Clicking corner of game (dismiss caught fish display)");
		if (!GetClientRect(GameHandle.Value, out RECT client)) return;
		int cx = client.Right * 12 / 100;
		int cy = client.Bottom * 88 / 100;

		if (settings.IsBackgroundInput == 1)
		{
			// Game validates clicks via GetCursorPos internally, so cursor must be physically at
			// the game position. Save cursor, block input, move cursor, PostMessage,
			// restore cursor — no focus steal, no visible disruption to the user.
			GetCursorPos(out System.Drawing.Point bgSavedCursor);
			var gamePt = new System.Drawing.Point(cx, cy);
			ClientToScreen(GameHandle.Value, ref gamePt);
			BlockInput(true);
			SetCursorPos(gamePt.X, gamePt.Y);
			System.Threading.Thread.Sleep(20);
			IntPtr targetHwnd = ChildWindowFromPoint(GameHandle.Value, new System.Drawing.Point(cx, cy));
			if (targetHwnd == IntPtr.Zero) targetHwnd = GameHandle.Value;
			var localPt = gamePt;
			ScreenToClient(targetHwnd, ref localPt);
			IntPtr lParam = new IntPtr((localPt.Y << 16) | (localPt.X & 0xFFFF));
			PostActivate();
			PostMessage(targetHwnd, WM_MOUSEMOVE, IntPtr.Zero, lParam);
			System.Threading.Thread.Sleep(10);
			PostMessage(targetHwnd, WM_LBUTTONDOWN, new IntPtr(MK_LBUTTON), lParam);
			System.Threading.Thread.Sleep(10);
			PostMessage(targetHwnd, WM_LBUTTONUP, IntPtr.Zero, lParam);
			System.Threading.Thread.Sleep(20);
			SetCursorPos(bgSavedCursor.X, bgSavedCursor.Y);
			BlockInput(false);
			return;
		}

		// Foreground mode: physically move mouse and click, then restore user's position
		GetCursorPos(out System.Drawing.Point savedCursor);
		IntPtr prevWindow = GetForegroundWindow();
		var pt = new System.Drawing.Point(cx, cy);
		ClientToScreen(GameHandle.Value, ref pt);
		int vsLeft   = GetSystemMetrics(76);
		int vsTop    = GetSystemMetrics(77);
		int vsWidth  = GetSystemMetrics(78);
		int vsHeight = GetSystemMetrics(79);
		double vx = (pt.X - vsLeft) * 65535.0 / vsWidth;
		double vy = (pt.Y - vsTop)  * 65535.0 / vsHeight;
		BringWindowToTop(GameHandle.Value);
		SetForegroundWindow(GameHandle.Value);
		InputSimulator.Mouse.Sleep(30);
		InputSimulator.Mouse.MoveMouseToPositionOnVirtualDesktop(vx, vy);
		InputSimulator.Mouse.Sleep(20);
		InputSimulator.Mouse.LeftButtonClick();
		InputSimulator.Mouse.Sleep(15);
		SetCursorPos(savedCursor.X, savedCursor.Y);
		if (prevWindow != IntPtr.Zero && prevWindow != GameHandle.Value)
		{
			SetForegroundWindow(prevWindow);
			double rx = (savedCursor.X - vsLeft) * 65535.0 / vsWidth;
			double ry = (savedCursor.Y - vsTop)  * 65535.0 / vsHeight;
			InputSimulator.Mouse.MoveMouseToPositionOnVirtualDesktop(rx, ry);
			InputSimulator.Mouse.Sleep(15);
			InputSimulator.Mouse.LeftButtonClick();
		}
	}

	public void CloseFishCaptureDialog()
	{
		if (!GameHandle.HasValue) return;
		BotLogger.Log("Action: Pressing dismiss key (ESC)");
		escBox.Dispatcher.InvokeAsync(() => escBox.Fill = (settings.IsDarkMode > 0) ? Theme.ColorAccent3 : Theme.GreenColor);
		PressKey((VirtualKeyCode)settings.KeyCode_DismissFishDialogue);
		Task.Delay(300).ContinueWith(_ => escBox.Dispatcher.InvokeAsync(() => escBox.Fill = System.Windows.Media.Brushes.Transparent));
	}

	public void ResetKeys()
	{
		ReleaseKey((VirtualKeyCode)settings.KeyCode_MoveLeft);
		APressed = false;
		left.Dispatcher.Invoke(delegate
		{
			left.Fill = System.Windows.Media.Brushes.Transparent;
		});
		ReleaseKey((VirtualKeyCode)settings.KeyCode_MoveRight);
		DPressed = false;
		right.Dispatcher.Invoke(delegate
		{
			right.Fill = System.Windows.Media.Brushes.Transparent;
		});
	}
}
