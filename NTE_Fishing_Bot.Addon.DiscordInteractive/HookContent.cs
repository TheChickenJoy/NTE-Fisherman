using System.Collections.Generic;

namespace NTE_Fishing_Bot.Addon.DiscordInteractive;

public class HookContent
{
	public string Content { get; set; } = string.Empty;

	public IList<HookEmbedContent> Embeds { get; set; } = new List<HookEmbedContent>();

	public HookEmbedFooter? Footer { get; set; }
}
