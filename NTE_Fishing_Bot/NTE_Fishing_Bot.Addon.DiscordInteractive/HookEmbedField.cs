namespace NTE_Fishing_Bot.Addon.DiscordInteractive;

public class HookEmbedField
{
	public string Name { get; set; } = string.Empty;

	public string Value { get; set; } = string.Empty;

	public bool Inline { get; set; }

	public HookEmbedField(string name, string value, bool inline = false)
	{
		Name = name;
		Value = value;
		Inline = inline;
	}
}
