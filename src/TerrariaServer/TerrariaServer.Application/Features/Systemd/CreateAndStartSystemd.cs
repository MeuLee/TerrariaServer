using Paramore.Brighter;

namespace TerrariaServer.Application.Features.Systemd;

public record CreateAndStartSystemdRequest(Unit Unit, Service Service, bool Enable) : IRequest
{
	public Guid Id { get; set; } = Guid.NewGuid();
}

public record Unit(string? Description);
public record Service(string ExecStart, SystemdType Type);
public enum SystemdType
{
	Simple,
	Forking,
	OneShot,
	DBus,
	Notify,
	Idle
}

public record CreateAndStartSystemdResponse;

public class CreateAndStartSystemdHandler : RequestHandlerAsync<CreateAndStartSystemdRequest>
{
	public override async Task<CreateAndStartSystemdRequest> HandleAsync(CreateAndStartSystemdRequest request, CancellationToken cancellationToken)
	{
		await Task.Delay(5 * 1000, cancellationToken); // create the Systemd service
		return await base.HandleAsync(request, cancellationToken);
	}
}
