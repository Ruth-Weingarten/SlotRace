using AutoMapper;
using SlotRace.Application.DTOs.Slots;
using SlotRace.Application.DTOs.Students;
using SlotRace.Domain.Entities;

namespace SlotRace.Application.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // ישות → DTO (מה שיוצא לקליינט)
        CreateMap<Slot, SlotDto>()
            .ForMember(d => d.TeacherName, o => o.MapFrom(s => s.Teacher.FullName))
            .ForMember(d => d.IsBooked,    o => o.MapFrom(s => s.BookedByParentId != null))
            .ForMember(d => d.BookedByMe,  o => o.Ignore())   // תלוי במשתמשת המחוברת, השירות קובע (T16)
            .ForMember(d => d.StudentName, o => o.Ignore());  // פרטיות, השירות קובע (T16)

        CreateMap<Student, StudentDto>()
            .ForMember(d => d.TeacherName,  o => o.MapFrom(s => s.HomeroomTeacher.FullName))
            // secure by default: בלי Ignore הקוד היה מועתק לפי השם גם להורה.
            // השירות ממלא אותו רק למחנכת (T18)
            .ForMember(d => d.JoinCode,     o => o.Ignore())
            .ForMember(d => d.ParentsCount, o => o.Ignore()); // בלי Ignore היה מתמלא אוטומטית מ-Parents.Count

        // בקשה → ישות (מה שנכנס מהקליינט)
        // בלי ReverseMap: לא רוצים מיפוי חזרה מ-DTO לישות
        // UpdateSlotRequest לא ממופה בכוונה: השירות מעתיק את שני הזמנים ביד (T16)
        CreateMap<CreateSlotRequest, Slot>();
        CreateMap<CreateStudentRequest, Student>();
    }
}
