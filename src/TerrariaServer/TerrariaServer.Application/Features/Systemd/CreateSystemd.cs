using MediatR;

namespace TerrariaServer.Application.Features.Systemd;

internal record CreateSystemdRequest(Unit Unit, Service Service, bool Enable) : IRequest<CreateSystemdResponse>;
internal record Unit(string? Description);
internal record Service(SystemdType Type, string ExecStart);
enum SystemdType
{
	Simple,
	Forking,
	OneShot,
	DBus,
	Notify,
	Idle
}

internal record CreateSystemdResponse;

// should be called out of process with a message queue like rabbitmq to keep slices independent
internal class CreateSystemdHandler : IRequestHandler<CreateSystemdRequest, CreateSystemdResponse>
{
	public async Task<CreateSystemdResponse> Handle(CreateSystemdRequest request, CancellationToken cancellationToken)
	{
		await Task.Delay(5 * 1000, cancellationToken); // create the Systemd service
		return new CreateSystemdResponse(); // need a response? can be unit?
	}
}
