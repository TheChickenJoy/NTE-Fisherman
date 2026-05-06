using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NTE_Fishing_Bot;

public static class Theme
{
	public static readonly SolidColorBrush WhiteColor = new SolidColorBrush(Color.FromArgb(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue));

	public static readonly SolidColorBrush BlackColor = new SolidColorBrush(Color.FromArgb(byte.MaxValue, 0, 0, 0));

	public static readonly SolidColorBrush GreenColor = new SolidColorBrush(Color.FromArgb(byte.MaxValue, 0, byte.MaxValue, 0));

	public static readonly SolidColorBrush GBoxDefaultBorderColor = new SolidColorBrush(Color.FromArgb(byte.MaxValue, 213, 223, 229));

	public static readonly SolidColorBrush ButtonDefaultBGColor = new SolidColorBrush(Color.FromArgb(byte.MaxValue, 221, 221, 221));

	public static readonly SolidColorBrush ColorAccent1 = new SolidColorBrush(Color.FromArgb(byte.MaxValue, 9, 25, 40));

	public static readonly SolidColorBrush ColorAccent2 = new SolidColorBrush(Color.FromArgb(byte.MaxValue, 26, 72, 116));

	public static readonly SolidColorBrush ColorAccent3 = new SolidColorBrush(Color.FromArgb(byte.MaxValue, 53, 167, 241));

	public static readonly SolidColorBrush ColorAccent4 = new SolidColorBrush(Color.FromArgb(byte.MaxValue, 141, 203, 246));

	public static readonly SolidColorBrush ColorAccent5 = new SolidColorBrush(Color.FromArgb(byte.MaxValue, 226, 242, 252));

	public static ImageSource DayImage = new BitmapImage(new Uri("pack://application:,,,/img/day.png"));

	public static ImageSource NightImage = new BitmapImage(new Uri("pack://application:,,,/img/night.png"));

	public static ImageSource DaySettingImage = new BitmapImage(new Uri("pack://application:,,,/img/setting_day.png"));

	public static ImageSource NightSettingImage = new BitmapImage(new Uri("pack://application:,,,/img/setting_night.png"));

	public static ImageSource DayArrowImage = new BitmapImage(new Uri("pack://application:,,,/img/arrow_day.png"));

	public static ImageSource NightArrowImage = new BitmapImage(new Uri("pack://application:,,,/img/arrow_night.png"));

	public static ImageSource DayInfoImage = new BitmapImage(new Uri("pack://application:,,,/img/info_day.png"));

	public static ImageSource NightInfoImage = new BitmapImage(new Uri("pack://application:,,,/img/info_night.png"));

	public static ResourceDictionary Styling = new ResourceDictionary
	{
		Source = new Uri("/NTE_Fishing_Bot;component/Resources/StylingDictionary.xaml", UriKind.RelativeOrAbsolute)
	};

	public static Style DarkStyle = Styling["btnRoundDark"] as Style;

	public static Style LightStyle = Styling["btnRoundLight"] as Style;

	public static Style SideMenuDarkStyle = Styling["btnSideMenuDark"] as Style;

	public static Style SideMenuLightStyle = Styling["btnSideMenuLight"] as Style;
}
