using Paramore.Brighter;
using TerrariaServer.Application.Features.Terraria.Vanilla;

namespace TerrariaServer.Application.Features.Systemd;

// good message name? good handler name?
public class CreateAndStartSystemdHandler : RequestHandlerAsync<WorldStartedMessage>
{
	public override async Task<WorldStartedMessage> HandleAsync(WorldStartedMessage request, CancellationToken cancellationToken)
	{
		await Task.Delay(5 * 1000, cancellationToken); // create the Systemd service
		return await base.HandleAsync(request, cancellationToken);
	}
}
