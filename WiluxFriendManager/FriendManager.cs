using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Composition;
using System.Text;
using ArchiSteamFarm.Core;
using ArchiSteamFarm.Plugins.Interfaces;
using ArchiSteamFarm.Steam;
using JetBrains.Annotations;
using SteamKit2;

namespace FriendManager;

#pragma warning disable CA1812 // ASF uses this class during runtime
[UsedImplicitly]
[Export(typeof(IPlugin))]
internal sealed class FriendManager : IBotCommand2, IBotFriendRequest {
	public string Name => nameof(FriendManager);
	public Version Version => typeof(FriendManager).Assembly.GetName().Version ?? throw new InvalidOperationException(nameof(Version));

	public Task OnLoaded() {
		ASF.ArchiLogger.LogGenericInfo($"{Name} by Wilux Loaded!");

		return Task.CompletedTask;
	}

	public async Task<string?> OnBotCommand(Bot bot, EAccess access, string message, string[] args, ulong steamID = 0) {
		if (string.IsNullOrEmpty(message)) {
			return null;
		}

		return args[0].ToUpperInvariant() switch {
			"ADDFRIEND" when args.Length > 2 => await AddFriendWithBot(access, args[1], args[2]).ConfigureAwait(false),
			"ADDFRIEND" => await AddFriendWithoutBot(bot, access, args[1]).ConfigureAwait(false),
			"DELFRIEND" when args.Length > 2 => await DelFriendWithBot(access, args[1], args[2]).ConfigureAwait(false),
			"DELFRIEND" => await DelFriendWithoutBot(bot, access, args[1]).ConfigureAwait(false),
			_ => null
		};
	}

	private static async Task<string?> AddFriendWithBot(EAccess access, string botName, string targetBots) {
		if (string.IsNullOrEmpty(botName) || string.IsNullOrEmpty(targetBots)) {
			ASF.ArchiLogger.LogNullError(null, nameof(botName) + " || " + nameof(targetBots));

			FormatStaticResponse("Incorrect parameters.");
		}

		Bot? bot = Bot.GetBot(botName);

		if (access < EAccess.Master) {
			return null;
		}

		if (bot == null) {
			return FormatStaticResponse("Bot not found.");
		}

		if (!bot.IsConnectedAndLoggedOn) {
			return FormatBotResponse(bot, "Bot is not logged in.");
		}

		HashSet<Bot>? targetBotList = Bot.GetBots(targetBots)?.Where(x => x.SteamID != bot.SteamID).ToHashSet();

		if ((targetBotList == null) || (targetBotList.Count == 0)) {
			return FormatBotResponse(bot, "Target bots not found.");
		}

		StringBuilder sb = new StringBuilder();
#pragma warning disable CA1305
		foreach (Bot targetBot in targetBotList) {
			EFriendRelationship relation = bot.SteamFriends.GetFriendRelationship(targetBot.SteamID);

			if (relation is EFriendRelationship.Friend or EFriendRelationship.RequestInitiator) {
				sb.AppendLine($"You already sent/friend with {targetBot.BotName}.");
			} else {
				bot.SteamFriends.AddFriend(targetBot.SteamID);
				sb.AppendLine($"Successfully added {targetBot.BotName} as friend.");
				await Task.Delay(200).ConfigureAwait(false);
			}
		}
#pragma warning restore CA1305
		return FormatBotResponse(bot, sb.ToString());
	}

	private static async Task<string?> AddFriendWithoutBot(Bot bot, EAccess access, string targetBots) {
		if (string.IsNullOrEmpty(targetBots)) {
			ASF.ArchiLogger.LogNullError(null, nameof(targetBots));

			return null;
		}

		if (access < EAccess.Master) {
			return null;
		}

		if (!bot.IsConnectedAndLoggedOn) {
			return FormatBotResponse(bot, "Bot is not logged in.");
		}

		HashSet<Bot>? targetBotList = Bot.GetBots(targetBots)?.Where(x => x.SteamID != bot.SteamID).ToHashSet();

		if ((targetBotList == null) || (targetBotList.Count == 0)) {
			return FormatBotResponse(bot, "Target bots not found.");
		}

		StringBuilder sb = new StringBuilder();
#pragma warning disable CA1305
		foreach (Bot targetBot in targetBotList) {
			EFriendRelationship relation = bot.SteamFriends.GetFriendRelationship(targetBot.SteamID);

			if (relation is EFriendRelationship.Friend or EFriendRelationship.RequestInitiator) {
				sb.AppendLine($"You already sent/friend with {targetBot.BotName}.");
			} else {
				bot.SteamFriends.AddFriend(targetBot.SteamID);
				sb.AppendLine($"Successfully added {targetBot.BotName} as friend.");
				await Task.Delay(200).ConfigureAwait(false);
			}
		}
#pragma warning restore CA1305
		return FormatBotResponse(bot, sb.ToString());
	}

	private static async Task<string?> DelFriendWithBot(EAccess access, string botName, string targetBots) {
		if (string.IsNullOrEmpty(botName) || string.IsNullOrEmpty(targetBots)) {
			ASF.ArchiLogger.LogNullError(null, nameof(botName) + " || " + nameof(targetBots));

			FormatStaticResponse("Incorrect parameters.");
		}

		Bot? bot = Bot.GetBot(botName);

		if (access < EAccess.Master) {
			return null;
		}

		if (bot == null) {
			return FormatStaticResponse("Bot not found.");
		}

		if (!bot.IsConnectedAndLoggedOn) {
			return FormatBotResponse(bot, "Bot is not logged in.");
		}

		HashSet<Bot>? targetBotList = Bot.GetBots(targetBots)?.Where(x => x.SteamID != bot.SteamID).ToHashSet();

		if ((targetBotList == null) || (targetBotList.Count == 0)) {
			return FormatBotResponse(bot, "Target bots not found.");
		}

		StringBuilder sb = new StringBuilder();
#pragma warning disable CA1305
		foreach (Bot targetBot in targetBotList) {
			EFriendRelationship relation = bot.SteamFriends.GetFriendRelationship(targetBot.SteamID);

			if (relation is EFriendRelationship.Friend) {
				bot.SteamFriends.RemoveFriend(targetBot.SteamID);
				sb.AppendLine($"Successfully removed {targetBot.BotName} from friends.");
				ASF.ArchiLogger.LogGenericInfo($"[FriendManager] {bot.BotName} removed {targetBot.SteamID} from friends.");
				await Task.Delay(200).ConfigureAwait(false);
			} else {
				sb.AppendLine($"You are not friends with {targetBot.BotName}.");
			}
		}
#pragma warning restore CA1305
		return FormatBotResponse(bot, sb.ToString());
	}

	private static async Task<string?> DelFriendWithoutBot(Bot bot, EAccess access, string targetBots) {
		if (string.IsNullOrEmpty(targetBots)) {
			ASF.ArchiLogger.LogNullError(null, nameof(targetBots));

			return null;
		}

		if (access < EAccess.Master) {
			return null;
		}

		if (!bot.IsConnectedAndLoggedOn) {
			return FormatBotResponse(bot, "Bot is not logged in.");
		}

		HashSet<Bot>? targetBotList = Bot.GetBots(targetBots)?.Where(x => x.SteamID != bot.SteamID).ToHashSet();

		if ((targetBotList == null) || (targetBotList.Count == 0)) {
			return FormatBotResponse(bot, "Target bots not found.");
		}

		StringBuilder sb = new StringBuilder();
#pragma warning disable CA1305
		foreach (Bot targetBot in targetBotList) {
			EFriendRelationship relation = bot.SteamFriends.GetFriendRelationship(targetBot.SteamID);

			if (relation is EFriendRelationship.Friend) {
				bot.SteamFriends.RemoveFriend(targetBot.SteamID);
				sb.AppendLine($"Successfully added {targetBot.BotName} as friend.");
				ASF.ArchiLogger.LogGenericInfo($"[FriendManager] {bot.BotName} removed {targetBot.SteamID} from friends.");
				await Task.Delay(200).ConfigureAwait(false);
			} else {
				sb.AppendLine($"You are not friends with {targetBot.BotName}.");
			}
		}
#pragma warning restore CA1305
		return FormatBotResponse(bot, sb.ToString());
	}

	private static string FormatStaticResponse(string response) => ArchiSteamFarm.Steam.Interaction.Commands.FormatStaticResponse(response);
	private static string FormatBotResponse(Bot bot, string response) => bot.Commands.FormatBotResponse(response);

	public Task<bool> OnBotFriendRequest(Bot bot, ulong steamId) {
		List<ulong>? bots = Bot.GetBots("ASF")?.Select(static b => b.SteamID).ToList();
		bool approve = bots?.Contains(steamId) ?? false;

		if (approve) {
			ASF.ArchiLogger.LogGenericInfo($"[FriendManager] {bot.BotName} Accepted friend request from {steamId} ");
		} else {
			ASF.ArchiLogger.LogGenericInfo($"[FriendManager] {bot.BotName} Got a friend request from {steamId} ");
		}

		return Task.FromResult(approve);
	}
}

#pragma warning restore CA1812 // ASF uses this class during runtime
