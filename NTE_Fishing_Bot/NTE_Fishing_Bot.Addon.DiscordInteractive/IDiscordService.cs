using System;
using System.Threading.Tasks;

namespace NTE_Fishing_Bot.Addon.DiscordInteractive;

public interface IDiscordService
{
	Task SendMessage(HookContent content);

	Task<HookContent> BuildOutOfBaitNotification(DateTime fishingStartAt);
}
