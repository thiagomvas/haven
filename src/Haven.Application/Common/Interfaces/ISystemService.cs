using Haven.Application.Common.Contracts;

namespace Haven.Application.Common.Interfaces;

public interface ISystemService
{
    Task<Result<SystemInformation>> GetSystemInformationAsync(CancellationToken ct);
}