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

	private static List<ulong> Acceptany = new List<ulong>();

	public Task OnLoaded() {
		ASF.ArchiLogger.LogGenericInfo($"{Name} by Wilux Loaded!");

		return Task.CompletedTask;
	}

	public async Task<string?> OnBotCommand(Bot bot, EAccess access, string message, string[] args, ulong steamID = 0) {
		if (string.IsNullOrEmpty(message)) {
			return null;
		}

		return args[0].ToUpperInvariant() switch {
			"ADDFRIEND" when args.Length > 2 => await AddFriendWithBot(access, args[1], args[2], false).ConfigureAwait(false),
			"ADDFRIEND" => await AddFriendWithoutBot(bot, access, args[1]).ConfigureAwait(false),
			"REMFRIEND" when args.Length > 2 => await RemFriendWithBot(access, args[1], args[2], false).ConfigureAwait(false),
			"REMFRIEND" => await RemFriendWithoutBot(bot, access, args[1]).ConfigureAwait(false),
			"REMFRIENDALL" when args.Length > 0 => await RemFriendAllWithBot(access, args[1]).ConfigureAwait(false),
			"REMFRIENDALL" => await RemFriendAllWithoutBot(bot, access).ConfigureAwait(false),
			"ACCEPTALL" when args.Length > 0 => await ToggleAcceptAllWithBot(access, args[1], false).ConfigureAwait(false),
			"ACCEPTALL" => await ToggleAcceptAllWithoutBot(bot, access, false).ConfigureAwait(false),
			_ => null
		};
	}

	public static Task<string?> ToggleAcceptAllWithBot(EAccess access, string botNames, bool ipc) {
		if (string.IsNullOrEmpty(botNames)) {
			ASF.ArchiLogger.LogNullError(null, nameof(botNames));

			return Task.FromResult(ipc ? "Incorrect parameters" : FormatStaticResponse("Incorrect parameters."))!;
		}

		if (access < EAccess.Master) {
			return Task.FromResult<string?>(null);
		}

		HashSet<Bot>? bots = Bot.GetBots(botNames);

		if (bots == null) {
			return Task.FromResult(ipc ? "Bot not found." : FormatStaticResponse(ArchiSteamFarm.Localization.Strings.BotNotFound))!;
		}

		StringBuilder sb = new StringBuilder();

		foreach (Bot bot in bots) {
			if (bot.IsConnectedAndLoggedOn) {
				if (Acceptany.Contains(bot.SteamID)) {
					Acceptany.Remove(bot.SteamID);
#pragma warning disable CA1305
					sb.AppendLine($"{bot.BotName} turned off accepting requests.");
				} else {
					Acceptany.Add(bot.SteamID);
					sb.AppendLine($"{bot.BotName} turned on accepting requests.");
				}
			} else {
				sb.AppendLine($"{bot.BotName} is not logged in.");
			}
		}

		return Task.FromResult(ipc ? sb.ToString().TrimEnd() : FormatStaticResponse(sb.ToString().TrimEnd()))!;
#pragma warning restore CA1305
	}

	private static Task<string?> ToggleAcceptAllWithoutBot(Bot bot, EAccess access, bool ipc) {
		if (access < EAccess.Master) {
			return Task.FromResult<string?>(null);
		}

		if (!bot.IsConnectedAndLoggedOn) {
			return Task.FromResult(FormatBotResponse(bot, ArchiSteamFarm.Localization.Strings.BotNotConnected))!;
		}

		if (Acceptany.Contains(bot.SteamID)) {
			Acceptany.Remove(bot.SteamID);
#pragma warning disable CA1305
			return Task.FromResult(ipc ? $"{bot.BotName} turned off accepting requests." : FormatBotResponse(bot, "turned off accepting requests."))!;
		}

		Acceptany.Add(bot.SteamID);

		return Task.FromResult(ipc ? $"{bot.BotName} turned on accepting requests." : FormatBotResponse(bot, "turned on accepting requests."))!;
#pragma warning restore CA1305
	}

	public static async Task<string?> AddFriendWithBot(EAccess access, string botName, string targets, bool ipc) {
		if (string.IsNullOrEmpty(botName) || string.IsNullOrEmpty(targets)) {
			ASF.ArchiLogger.LogNullError(null, nameof(botName) + " || " + nameof(targets));

			return ipc ? "Incorrect parameters" : FormatStaticResponse("Incorrect parameters.");
		}

		Bot? bot = Bot.GetBot(botName);

		if (access < EAccess.Master) {
			return null;
		}

		if (bot == null) {
			return ipc ? "Bot not found." : FormatStaticResponse(ArchiSteamFarm.Localization.Strings.BotNotFound);
		}

		if (!bot.IsConnectedAndLoggedOn) {
			return ipc ? "Bot is not logged in." : FormatBotResponse(bot, ArchiSteamFarm.Localization.Strings.BotNotConnected);
		}

		string[] determineList = targets.Split(',').Select(static s => s.Trim()).ToArray();
		List<string> steamIDs = new List<string>();
		List<string> stringBotList = new List<string>();

		foreach (string item in determineList) {
			if (item.Length == 17) {
				steamIDs.Add(item);
			} else {
				stringBotList.Add(item);
			}
		}

		StringBuilder sb = new StringBuilder();

		if (stringBotList.Count > 0) {
			HashSet<Bot>? targetBotList = Bot.GetBots(string.Join(",", stringBotList))?.Where(x => x.SteamID != bot.SteamID).ToHashSet();

			if ((targetBotList == null) || (targetBotList.Count == 0)) {
				return ipc ? "Target bots not found." : FormatBotResponse(bot, "Target bots not found.");
			}
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
		}

		if (steamIDs.Count > 0) {
			foreach (string steamID in steamIDs) {
				if (new SteamID(ulong.Parse(steamID)).IsValid) {
					EFriendRelationship relation = bot.SteamFriends.GetFriendRelationship(ulong.Parse(steamID));

					if (relation is EFriendRelationship.Friend or EFriendRelationship.RequestInitiator) {
						sb.AppendLine($"You already sent/friend with {steamID}.");
					} else {
						bot.SteamFriends.AddFriend(ulong.Parse(steamID));
						sb.AppendLine($"Successfully added {steamID} as friend.");
						await Task.Delay(200).ConfigureAwait(false);
					}
				} else {
					sb.AppendLine($"{steamID} Is invalid.");
				}
			}
		}

#pragma warning restore CA1305
		return ipc ? sb.ToString().TrimEnd() : FormatBotResponse(bot, sb.ToString().TrimEnd());
	}

	private static async Task<string?> AddFriendWithoutBot(Bot bot, EAccess access, string targets) {
		if (string.IsNullOrEmpty(targets)) {
			ASF.ArchiLogger.LogNullError(null, nameof(targets));

			return null;
		}

		if (access < EAccess.Master) {
			return null;
		}

		if (!bot.IsConnectedAndLoggedOn) {
			return FormatBotResponse(bot, ArchiSteamFarm.Localization.Strings.BotNotConnected);
		}

		string[] determineList = targets.Split(',').Select(static s => s.Trim()).ToArray();
		List<string> steamIDs = new List<string>();
		List<string> stringBotList = new List<string>();

		foreach (string item in determineList) {
			if (item.Length == 17) {
				steamIDs.Add(item);
			} else {
				stringBotList.Add(item);
			}
		}

		StringBuilder sb = new StringBuilder();

		if (stringBotList.Count > 0) {
			HashSet<Bot>? targetBotList = Bot.GetBots(string.Join(",", stringBotList))?.Where(x => x.SteamID != bot.SteamID).ToHashSet();

			if ((targetBotList == null) || (targetBotList.Count == 0)) {
				return FormatBotResponse(bot, "Target bots not found.");
			}
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
		}

		if (steamIDs.Count > 0) {
			foreach (string steamID in steamIDs) {
				if (new SteamID(ulong.Parse(steamID)).IsValid) {
					EFriendRelationship relation = bot.SteamFriends.GetFriendRelationship(ulong.Parse(steamID));

					if (relation is EFriendRelationship.Friend or EFriendRelationship.RequestInitiator) {
						sb.AppendLine($"You already sent/friend with {steamID}.");
					} else {
						bot.SteamFriends.AddFriend(ulong.Parse(steamID));
						sb.AppendLine($"Successfully added {steamID} as friend.");
						await Task.Delay(200).ConfigureAwait(false);
					}
				} else {
					sb.AppendLine($"{steamID} Is invalid.");
				}
			}
		}

#pragma warning restore CA1305
		return FormatBotResponse(bot, sb.ToString().TrimEnd());
	}

	public static async Task<string?> RemFriendWithBot(EAccess access, string botName, string targets, bool ipc) {
		if (string.IsNullOrEmpty(botName) || string.IsNullOrEmpty(targets)) {
			ASF.ArchiLogger.LogNullError(null, nameof(botName) + " || " + nameof(targets));

			return ipc ? "Incorrect parameters" : FormatStaticResponse("Incorrect parameters.");
		}

		Bot? bot = Bot.GetBot(botName);

		if (access < EAccess.Master) {
			return null;
		}

		if (bot == null) {
			return ipc ? "Bot not found." : FormatStaticResponse(ArchiSteamFarm.Localization.Strings.BotNotFound);
		}

		if (!bot.IsConnectedAndLoggedOn) {
			return ipc ? "Bot is not logged in." : FormatBotResponse(bot, ArchiSteamFarm.Localization.Strings.BotNotConnected);
		}

		string[] determineList = targets.Split(',').Select(static s => s.Trim()).ToArray();
		List<string> steamIDs = new List<string>();
		List<string> stringBotList = new List<string>();

		foreach (string item in determineList) {
			if (item.Length == 17) {
				steamIDs.Add(item);
			} else {
				stringBotList.Add(item);
			}
		}

		StringBuilder sb = new StringBuilder();

		if (stringBotList.Count > 0) {
			HashSet<Bot>? targetBotList = Bot.GetBots(string.Join(",", stringBotList))?.Where(x => x.SteamID != bot.SteamID).ToHashSet();

			if ((targetBotList == null) || (targetBotList.Count == 0)) {
				return ipc ? "Target bots not found." : FormatBotResponse(bot, "Target bots not found.");
			}

			foreach (Bot targetBot in targetBotList) {
				EFriendRelationship relation = bot.SteamFriends.GetFriendRelationship(targetBot.SteamID);
#pragma warning disable CA1305
				if (relation is EFriendRelationship.Friend) {
					bot.SteamFriends.RemoveFriend(targetBot.SteamID);
					sb.AppendLine($"Successfully removed {targetBot.BotName} from friends.");
					ASF.ArchiLogger.LogGenericInfo($"[FriendManager] {bot.BotName} removed {targetBot.SteamID} from friends.");
					await Task.Delay(200).ConfigureAwait(false);
				} else {
					sb.AppendLine($"You are not friends with {targetBot.BotName}.");
				}
			}
		}

		if (steamIDs.Count > 0) {
			foreach (string steamID in steamIDs) {
				if (new SteamID(ulong.Parse(steamID)).IsValid) {
					EFriendRelationship relation = bot.SteamFriends.GetFriendRelationship(ulong.Parse(steamID));

					if (relation is EFriendRelationship.Friend) {
						bot.SteamFriends.RemoveFriend(ulong.Parse(steamID));
						sb.AppendLine($"Successfully removed {steamID} from friends.");
						ASF.ArchiLogger.LogGenericInfo($"[FriendManager] {bot.BotName} removed {steamID} from friends.");
						await Task.Delay(200).ConfigureAwait(false);
					} else {
						sb.AppendLine($"You are not friends with {steamID}.");
					}
				} else {
					sb.AppendLine($"{steamID} Is invalid.");
				}
			}
		}
#pragma warning restore CA1305
		return ipc ? sb.ToString().TrimEnd() : FormatBotResponse(bot, sb.ToString().TrimEnd());
	}

	private static async Task<string?> RemFriendWithoutBot(Bot bot, EAccess access, string targets) {
		if (string.IsNullOrEmpty(targets)) {
			ASF.ArchiLogger.LogNullError(null, nameof(targets));

			return null;
		}

		if (access < EAccess.Master) {
			return null;
		}

		if (!bot.IsConnectedAndLoggedOn) {
			return FormatBotResponse(bot, ArchiSteamFarm.Localization.Strings.BotNotConnected);
		}

		string[] determineList = targets.Split(',').Select(static s => s.Trim()).ToArray();
		List<string> steamIDs = new List<string>();
		List<string> stringBotList = new List<string>();

		foreach (string item in determineList) {
			if (item.Length == 17) {
				steamIDs.Add(item);
			} else {
				stringBotList.Add(item);
			}
		}

		StringBuilder sb = new StringBuilder();

		if (stringBotList.Count > 0) {
			HashSet<Bot>? targetBotList = Bot.GetBots(string.Join(",", stringBotList))?.Where(x => x.SteamID != bot.SteamID).ToHashSet();

			if ((targetBotList == null) || (targetBotList.Count == 0)) {
				return FormatBotResponse(bot, "Target bots not found.");
			}

			foreach (Bot targetBot in targetBotList) {
				EFriendRelationship relation = bot.SteamFriends.GetFriendRelationship(targetBot.SteamID);
#pragma warning disable CA1305
				if (relation is EFriendRelationship.Friend) {
					bot.SteamFriends.RemoveFriend(targetBot.SteamID);
					sb.AppendLine($"Successfully removed {targetBot.BotName} from friends.");
					ASF.ArchiLogger.LogGenericInfo($"[FriendManager] {bot.BotName} removed {targetBot.SteamID} from friends.");
					await Task.Delay(200).ConfigureAwait(false);
				} else {
					sb.AppendLine($"You are not friends with {targetBot.BotName}.");
				}
			}
		}

		if (steamIDs.Count > 0) {
			foreach (string steamID in steamIDs) {
				if (new SteamID(ulong.Parse(steamID)).IsValid) {
					EFriendRelationship relation = bot.SteamFriends.GetFriendRelationship(ulong.Parse(steamID));

					if (relation is EFriendRelationship.Friend) {
						bot.SteamFriends.RemoveFriend(ulong.Parse(steamID));
						sb.AppendLine($"Successfully removed {steamID} from friends.");
						ASF.ArchiLogger.LogGenericInfo($"[FriendManager] {bot.BotName} removed {steamID} from friends.");
						await Task.Delay(200).ConfigureAwait(false);
					} else {
						sb.AppendLine($"You are not friends with {steamID}.");
					}
				} else {
					sb.AppendLine($"{steamID} Is invalid.");
				}
			}
		}
#pragma warning restore CA1305
		return FormatBotResponse(bot, sb.ToString().TrimEnd());
	}

	private static async Task<string?> RemFriendAllWithoutBot(Bot bot, EAccess access) {
		if (access < EAccess.Master) {
			return null;
		}

		if (!bot.IsConnectedAndLoggedOn) {
			return FormatBotResponse(bot, ArchiSteamFarm.Localization.Strings.BotNotConnected);
		}

		int friendCount = bot.SteamFriends.GetFriendCount();

		if (friendCount > 0) {
			for (int i = 0; i < friendCount; i++) {
				SteamID steamId = bot.SteamFriends.GetFriendByIndex(i);
				bot.SteamFriends.RemoveFriend(steamId);
				await Task.Delay(500).ConfigureAwait(false);
			}

			return FormatBotResponse(bot, "All friends deleted.");
		}

		return FormatBotResponse(bot, "You don't have any friends.");
	}

	private static async Task<string?> RemFriendAllWithBot(EAccess access, string botNames) {
		if (string.IsNullOrEmpty(botNames)) {
			throw new ArgumentNullException(nameof(botNames));
		}

		if (access < EAccess.Master) {
			return null;
		}

		HashSet<Bot>? bots = Bot.GetBots(botNames);

		if ((bots == null) || (bots.Count == 0)) {
			return FormatStaticResponse("Bots not found.");
		}

		IList<string?> results = await Utilities.InParallel(bots.Select(bot => RemFriendAllWithoutBot(bot, access))).ConfigureAwait(false);

		return results.Any() ? string.Join(Environment.NewLine, results) : null;
	}

	private static string FormatStaticResponse(string response) => ArchiSteamFarm.Steam.Interaction.Commands.FormatStaticResponse(response);
	private static string FormatBotResponse(Bot bot, string response) => bot.Commands.FormatBotResponse(response);

	public Task<bool> OnBotFriendRequest(Bot bot, ulong steamId) {
		List<ulong>? bots = Bot.GetBots("ASF")?.Select(static b => b.SteamID).ToList();
		bool approve = (bots?.Contains(steamId) ?? false) || Acceptany.Contains(bot.SteamID);

		if (approve) {
			ASF.ArchiLogger.LogGenericInfo($"[FriendManager] {bot.BotName} Accepted friend request from {steamId} ");
		} else {
			ASF.ArchiLogger.LogGenericInfo($"[FriendManager] {bot.BotName} Got a friend request from {steamId} ");
		}

		return Task.FromResult(approve);
	}
}

#pragma warning restore CA1812 // ASF uses this class during runtime
