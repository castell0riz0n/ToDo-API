using AutoMapper;
using TeamA.ToDo.Application.DTOs.Roles;
using TeamA.ToDo.Application.DTOs.Users;
using TeamA.ToDo.Core.Models;

namespace TeamA.ToDo.Application;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // User mappings
        CreateMap<ApplicationUser, UserProfileDto>()
            .ForMember(dest => dest.Roles, opt => opt.Ignore());

        CreateMap<UserUpdateDto, ApplicationUser>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        CreateMap<AdminUserUpdateDto, ApplicationUser>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        // Role mappings
        CreateMap<ApplicationRole, RoleDto>();

        CreateMap<RoleCreateDto, ApplicationRole>()
            .ForMember(dest => dest.NormalizedName, opt => opt.MapFrom(src => src.Name.ToUpper()));

        CreateMap<RoleUpdateDto, ApplicationRole>()
            .ForMember(dest => dest.NormalizedName, opt => opt.MapFrom(src => src.Name.ToUpper()))
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        // Permission mappings
        CreateMap<Permission, PermissionDto>();
    }
}