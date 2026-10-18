using SlotRace.Application.Common;
using SlotRace.Application.DTOs.Students;
using SlotRace.Domain.Enums;

namespace SlotRace.Application.Interfaces;

public interface IStudentService
{
    Task<ServiceResult<StudentDto>> CreateAsync(CreateStudentRequest request, int teacherId, CancellationToken ct);
    Task<ServiceResult<List<StudentDto>>> GetMineAsync(int userId, Role role, CancellationToken ct);
    Task<ServiceResult<StudentDto>> GetByIdAsync(int id, int userId, CancellationToken ct);
    Task<ServiceResult<StudentDto>> LinkAsync(LinkStudentRequest request, int parentId, CancellationToken ct);
}
