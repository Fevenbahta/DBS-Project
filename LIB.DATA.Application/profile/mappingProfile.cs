using AutoMapper;

using LIB.API.Application.DTOs.User;
using LIB.API.Domain;

namespace LIB.API.Application.profile
{
    public class mappingProfile : Profile
    {
        public mappingProfile()
        {
          
           
            CreateMap<Users, UserDto>().ReverseMap();



        }
    }
}

