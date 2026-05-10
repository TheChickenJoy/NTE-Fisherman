using System.ComponentModel;

namespace NTE_Fishing_Bot;

public interface IAppSettings
{
	[DefaultValue(222)]
	int FishStaminaColor_A { get; set; }

	[DefaultValue(222)]
	int FishStaminaColor_R { get; set; }

	[DefaultValue(222)]
	int FishStaminaColor_G { get; set; }

	[DefaultValue(222)]
	int FishStaminaColor_B { get; set; }

	[DefaultValue(0)]
	int FishStaminaPoint_X { get; set; }

	[DefaultValue(0)]
	int FishStaminaPoint_Y { get; set; }

	[DefaultValue(222)]
	int MiddleBarColor_A { get; set; }

	[DefaultValue(222)]
	int MiddleBarColor_R { get; set; }

	[DefaultValue(222)]
	int MiddleBarColor_G { get; set; }

	[DefaultValue(222)]
	int MiddleBarColor_B { get; set; }

	[DefaultValue(255)]
	int CursorColor_A { get; set; }

	[DefaultValue(225)]
	int CursorColor_R { get; set; }

	[DefaultValue(225)]
	int CursorColor_G { get; set; }

	[DefaultValue(225)]
	int CursorColor_B { get; set; }

	[DefaultValue(222)]
	int PlayerStaminaColor_A { get; set; }

	[DefaultValue(222)]
	int PlayerStaminaColor_R { get; set; }

	[DefaultValue(222)]
	int PlayerStaminaColor_G { get; set; }

	[DefaultValue(222)]
	int PlayerStaminaColor_B { get; set; }

	[DefaultValue(0)]
	int PlayerStaminaPoint_X { get; set; }

	[DefaultValue(0)]
	int PlayerStaminaPoint_Y { get; set; }

	[DefaultValue(0)]
	int UpperLeftBarPoint_X { get; set; }

	[DefaultValue(0)]
	int UpperLeftBarPoint_Y { get; set; }

	[DefaultValue(0)]
	int LowerRightBarPoint_X { get; set; }

	[DefaultValue(0)]
	int LowerRightBarPoint_Y { get; set; }

	[DefaultValue(0)]
	int IsDarkMode { get; set; }

	[DefaultValue(0)]
	int IsBackgroundInput { get; set; }

	[DefaultValue(0)]
	int IsAlwaysOnTop { get; set; }

	[DefaultValue(0)]
	int IsWindowPinEnabled { get; set; }

	[DefaultValue(300)]
	int ZoomSize_X { get; set; }

	[DefaultValue(300)]
	int ZoomSize_Y { get; set; }

	[DefaultValue(4)]
	int ZoomFactor { get; set; }

	[DefaultValue("HTGame")]
	string GameProcessName { get; set; }

	[DefaultValue(40.0)]
	double StaminaColorDetectionThreshold { get; set; }

	[DefaultValue(10.0)]
	double MiddlebarColorDetectionThreshold { get; set; }

	[DefaultValue(5000)]
	int Delay_LagCompensation { get; set; }

	[DefaultValue(3000)]
	int Delay_FishCapture { get; set; }

	[DefaultValue(4000)]
	int Delay_DismissFishCaptureDialogue { get; set; }

	[DefaultValue(2000)]
	int Delay_Restart { get; set; }

	[DefaultValue(5)]
	int MinimumMiddleBarHeight { get; set; }

	[DefaultValue(70)]
	int KeyCode_FishCapture { get; set; }

	[DefaultValue(27)]
	int KeyCode_DismissFishDialogue { get; set; }

	[DefaultValue(65)]
	int KeyCode_MoveLeft { get; set; }

	[DefaultValue(68)]
	int KeyCode_MoveRight { get; set; }

	[DefaultValue("")]
	string DiscordHookUrl { get; set; }

	[DefaultValue("")]
	string DiscordUserId { get; set; }

	[DefaultValue(0)]
	int DefaultAdapter { get; set; }

	[DefaultValue(0)]
	int DefaultDevice { get; set; }

	[DefaultValue(32)]
	int CastButtonColor_R { get; set; }

	[DefaultValue(124)]
	int CastButtonColor_G { get; set; }

	[DefaultValue(255)]
	int CastButtonColor_B { get; set; }

	[DefaultValue(0)]
	int CastButtonPoint_X { get; set; }

	[DefaultValue(0)]
	int CastButtonPoint_Y { get; set; }

	[DefaultValue(70)]
	int KeyCode_CastFish { get; set; }

	[DefaultValue(1000)]
	int Delay_PostCatch { get; set; }

	[DefaultValue(1000)]
	int Delay_AfterClick { get; set; }

	[DefaultValue(255)]
	int XpBarColor_A { get; set; }

	[DefaultValue(250)]
	int XpBarColor_R { get; set; }

	[DefaultValue(75)]
	int XpBarColor_G { get; set; }

	[DefaultValue(144)]
	int XpBarColor_B { get; set; }

	[DefaultValue(0)]
	int XpBarPoint_X { get; set; }

	[DefaultValue(0)]
	int XpBarPoint_Y { get; set; }

	[DefaultValue(192)]
	int DailyRewardColor_R { get; set; }

	[DefaultValue(63)]
	int DailyRewardColor_G { get; set; }

	[DefaultValue(34)]
	int DailyRewardColor_B { get; set; }

	[DefaultValue(0)]
	int DailyRewardPoint_X { get; set; }

	[DefaultValue(0)]
	int DailyRewardPoint_Y { get; set; }

	[DefaultValue(1280)]
	int PinnedWindowWidth { get; set; }

	[DefaultValue(720)]
	int PinnedWindowHeight { get; set; }

	[DefaultValue(1)]
	int IsFirstRun { get; set; }

}
