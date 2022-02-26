namespace TerrariaServer.Application.Features.Systemd;

internal record CreateAndStartSystemdRequest(Unit Unit, Service Service, bool Enable) : IRequest<CreateAndStartSystemdResponse>;
internal record Unit(string? Description);
internal record Service(string ExecStart, SystemdType Type);
enum SystemdType
{
	Simple,
	Forking,
	OneShot,
	DBus,
	Notify,
	Idle
}

internal record CreateAndStartSystemdResponse;

internal class CreateAndStartSystemdHandler : IRequestHandler<CreateAndStartSystemdRequest, CreateAndStartSystemdResponse>
{
	public async Task<CreateAndStartSystemdResponse> Handle(CreateAndStartSystemdRequest request, CancellationToken cancellationToken)
	{
		await Task.Delay(5 * 1000, cancellationToken); // create the Systemd service
		return new CreateAndStartSystemdResponse(); // need a response? can be unit?
	}
}
