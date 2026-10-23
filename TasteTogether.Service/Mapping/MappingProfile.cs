using AutoMapper;
using TasteTogether.Core.DTOs;
using TasteTogether.Core.Entities;

namespace TasteTogether.Service.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserResponse>();

        // RegisteredCount ו-IsPast לא נמצאים בישות. ה-Service ממלא אותם
        CreateMap<Workshop, WorkshopResponse>()
            .ForMember(dest => dest.RegisteredCount, opt => opt.Ignore())
            .ForMember(dest => dest.IsPast, opt => opt.Ignore());

        // WorkshopTitle, UserEmail ו-UserFullName מוזנים על ידי ה-Service, כי לא תמיד הניווטים נטענים
        // WorkshopTitle, WorkshopStatus, UserEmail ו-UserFullName מוזנים על ידי ה-Service, כי לא תמיד הניווטים נטענים
        CreateMap<WorkshopRegistration, RegistrationResponse>()
            .ForMember(dest => dest.WorkshopTitle, opt => opt.Ignore())
            .ForMember(dest => dest.WorkshopStatus, opt => opt.Ignore())
            .ForMember(dest => dest.UserEmail, opt => opt.Ignore())
            .ForMember(dest => dest.UserFullName, opt => opt.Ignore());

        CreateMap<Recipe, RecipeResponse>();
    }
}