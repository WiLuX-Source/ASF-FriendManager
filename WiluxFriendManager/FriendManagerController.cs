using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;
using ArchiSteamFarm.IPC.Controllers.Api;
using ArchiSteamFarm.IPC.Responses;
using ArchiSteamFarm.Steam;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace FriendManager;

[Route("/Api/Bot")]
public sealed class FriendManagerController : ArchiController {
	/// <summary>
	///     Adds a friend.
	/// </summary>
	[HttpPost("{botName:required}/friends/add")]
	[SwaggerOperation(Summary = "Adds a friend.", Description = "Add a friend with IPC.")]
	[ProducesResponseType(typeof(GenericResponse), (int) HttpStatusCode.OK)]
	[ProducesResponseType(typeof(GenericResponse), (int) HttpStatusCode.BadRequest)]
	public async Task<ActionResult<GenericResponse>> Addfriend(string botName, [Required] [FromBody] string NamesOrIDS) {
		if (string.IsNullOrEmpty(botName)) {
			return BadRequest(new GenericResponse(false, "Bot is not defined."));
		}

		if (string.IsNullOrEmpty(NamesOrIDS)) {
			return BadRequest(new GenericResponse(false, "No bot name is provided."));
		}

		string? result = await FriendManager.AddFriendWithBot(EAccess.Master, botName, NamesOrIDS, true).ConfigureAwait(false)!;

		return result switch {
			"Incorrect parameters." => BadRequest(new GenericResponse(false, "Incorrect parameters.")),
			"Bot not found." => BadRequest(new GenericResponse(false, "Bot not found.")),
			"Bot is not logged in." => Ok(new GenericResponse(false, "Bot is not logged in.")),
			"Target bots not found." => Ok(new GenericResponse(false, "Target bots not found.")),
			_ => Ok(new GenericResponse(true, result))
		};
	}

	/// <summary>
	///     Removes a friend.
	/// </summary>
	[HttpPost("{botName:required}/friends/remove")]
	[SwaggerOperation(Summary = "Removes a friend.", Description = "Remove a friend with IPC.")]
	[ProducesResponseType(typeof(GenericResponse), (int) HttpStatusCode.OK)]
	[ProducesResponseType(typeof(GenericResponse), (int) HttpStatusCode.BadRequest)]
	public async Task<ActionResult<GenericResponse>> Remfriend(string botName, [Required] [FromBody] string NamesOrIDS) {
		if (string.IsNullOrEmpty(botName)) {
			return BadRequest(new GenericResponse(false, "Bot is not defined."));
		}

		if (string.IsNullOrEmpty(NamesOrIDS)) {
			return BadRequest(new GenericResponse(false, "No bot name is provided."));
		}

		string? result = await FriendManager.RemFriendWithBot(EAccess.Master, botName, NamesOrIDS, true).ConfigureAwait(false)!;

		return result switch {
			"Incorrect parameters." => BadRequest(new GenericResponse(false, "Incorrect parameters.")),
			"Bot not found." => BadRequest(new GenericResponse(false, "Bot not found.")),
			"Bot is not logged in." => Ok(new GenericResponse(false, "Bot is not logged in.")),
			"Target bots not found." => Ok(new GenericResponse(false, "Target bots not found.")),
			_ => Ok(new GenericResponse(true, result))
		};
	}
}
