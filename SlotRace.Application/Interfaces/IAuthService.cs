using SlotRace.Application.Common;
using SlotRace.Application.DTOs.Auth;

namespace SlotRace.Application.Interfaces;

public interface IAuthService
{
    Task<ServiceResult<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct);
    Task<ServiceResult<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct);
}
