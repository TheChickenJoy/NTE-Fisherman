using System.Windows.Forms;

namespace NTE_Fishing_Bot;

public class KeycodeHelper
{
	private static readonly KeysConverter kc = new KeysConverter();

	public static string KeycodeToString(int keyCode)
	{
		return (kc.ConvertToString(keyCode) ?? "???").ToUpper();
	}
}
