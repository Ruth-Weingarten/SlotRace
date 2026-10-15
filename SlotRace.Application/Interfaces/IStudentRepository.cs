using SlotRace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SlotRace.Application.Interfaces
{
    public interface IStudentRepository
    {
       Task<Student?> GetWithParentsAsync(int id, CancellationToken ct);


    }
}
