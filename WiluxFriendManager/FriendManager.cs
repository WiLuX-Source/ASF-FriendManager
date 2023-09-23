using System;
using System.Threading.Tasks;
using ArchiSteamFarm.Core;
using ArchiSteamFarm.Plugins.Interfaces;
using JetBrains.Annotations;

namespace MyAwesomePlugin;

#pragma warning disable CA1812 // ASF uses this class during runtime
[UsedImplicitly]
internal sealed class WiluxFriendManager : IPlugin {
	public string Name => nameof(WiluxFriendManager);
	public Version Version => typeof(WiluxFriendManager).Assembly.GetName().Version ?? throw new InvalidOperationException(nameof(Version));

	public Task OnLoaded() {
		ASF.ArchiLogger.LogGenericInfo($"Hello {Name}!");

		return Task.CompletedTask;
	}
}
#pragma warning restore CA1812 // ASF uses this class during runtime
