using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NTE_Fishing_Bot.Addon.DiscordInteractive;

public class DiscordService : IDiscordService
{
	private readonly string _hookUrl;

	private readonly string? _mentionId;

	private readonly HttpClient _httpClient = new HttpClient();

	private readonly DateTime EpochTime = new DateTime(1970, 1, 1);

	public DiscordService(string hookUrl, string? mentionId = null)
	{
		if (Uri.IsWellFormedUriString(hookUrl, UriKind.Absolute))
		{
			Uri uri = new Uri(hookUrl);
			if (!uri.Host.Contains("discord.com", StringComparison.CurrentCultureIgnoreCase) && !uri.Host.Contains("discordapp.com", StringComparison.CurrentCultureIgnoreCase))
			{
				throw new ArgumentException("Discord HookUrl provided is not valid");
			}
			if (!uri.AbsolutePath.Contains("webhooks", StringComparison.CurrentCultureIgnoreCase))
			{
				throw new ArgumentException("Discord HookUrl provided is not valid");
			}
			_hookUrl = hookUrl;
			_mentionId = mentionId;
			return;
		}
		throw new ArgumentException("Discord HookUrl provided is not valid");
	}

	public Task SendMessage(HookContent content)
	{
		JsonContent postContent = JsonContent.Create(content, new MediaTypeWithQualityHeaderValue("application/json"), new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		});
		return _httpClient.PostAsync(_hookUrl, postContent);
	}

	public Task<HookContent> BuildOutOfBaitNotification(DateTime fishingStartAt)
	{
		StringBuilder strBuild = new StringBuilder("Hello");
		if (!string.IsNullOrEmpty(_mentionId))
		{
			StringBuilder stringBuilder = strBuild;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(5, 1, stringBuilder);
			handler.AppendLiteral("<@!");
			handler.AppendFormatted(_mentionId);
			handler.AppendLiteral(">.");
			stringBuilder.Append(ref handler);
		}
		strBuild.AppendLine("NTE Fishing Bot notification.");
		return Task.FromResult(new HookContent
		{
			Content = strBuild.ToString(),
			Embeds = new List<HookEmbedContent>
			{
				new HookEmbedContent
				{
					Color = 5814783L,
					Title = "Ran out of bait",
					Description = "This message is sent because ran out of bait. Fishing Session detail below:",
					Fields = new List<HookEmbedField>
					{
						new HookEmbedField("Start at:", $"<t:{(int)(fishingStartAt - EpochTime).TotalSeconds}:f>", inline: true),
						new HookEmbedField("End at:", $"<t:{(int)(DateTime.UtcNow - EpochTime).TotalSeconds}:f>", inline: true)
					}
				}
			},
			Footer = new HookEmbedFooter
			{
				Text = "NTE Fishing Bot"
			}
		});
	}

	public Task<HookContent> BuildGenericNotification(string message)
	{
		StringBuilder strBuild = new StringBuilder("Hello");
		if (!string.IsNullOrEmpty(_mentionId))
		{
			StringBuilder stringBuilder = strBuild;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(5, 1, stringBuilder);
			handler.AppendLiteral("<@!");
			handler.AppendFormatted(_mentionId);
			handler.AppendLiteral(">.");
			stringBuilder.AppendLine(ref handler);
		}
		strBuild.AppendLine(message);
		return Task.FromResult(new HookContent
		{
			Content = strBuild.ToString()
		});
	}
}
