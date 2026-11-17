using AutoMapper;
using MediVerify.Models;
using MediVerify.ViewModels;

namespace MediVerify.Mapping
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // Prescription create viewmodel to entity mappings
            CreateMap<PrescriptionCreateViewModel, Prescription>()
                .ForMember(dest => dest.PrescriptionNumber, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Items, opt => opt.Ignore());

            // Additional mappings can be added as needed
        }
    }
}
