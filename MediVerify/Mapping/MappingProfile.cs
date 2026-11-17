using AutoMapper;
using MediVerify.Models;
using MediVerify.ViewModels;

namespace MediVerify.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<PrescriptionCreateViewModel, Prescription>()
                .ForMember(destination => destination.Items, options => options.Ignore())
                .ForMember(destination => destination.Status, options => options.MapFrom(_ => "Draft"))
                .ForMember(destination => destination.IsVerified, options => options.MapFrom(_ => false));

            CreateMap<PrescriptionItemViewModel, PrescriptionItem>()
                .ForMember(destination => destination.PrescribedInstructions,
                    options => options.MapFrom(source => source.Instructions));
            CreateMap<PrescriptionItem, PrescriptionItemDetailsViewModel>()
                .ForMember(destination => destination.MedicineName,
                    options => options.MapFrom(source => source.Medicine == null ? null : source.Medicine.MedicineName));

            CreateMap<Prescription, PrescriptionDetailsViewModel>()
                .ForMember(destination => destination.PatientName,
                    options => options.MapFrom(source => source.Patient == null || source.Patient.ApplicationUser == null
                        ? null
                        : source.Patient.ApplicationUser.FullName))
                .ForMember(destination => destination.DoctorName,
                    options => options.MapFrom(source => source.Doctor == null ? null : source.Doctor.FullName));

            CreateMap<MedicineViewModel, Medicine>();
            CreateMap<Medicine, MedicineViewModel>();
            CreateMap<InventoryViewModel, Inventory>();
            CreateMap<Inventory, InventoryViewModel>();
            CreateMap<MedicineSafetyViewModel, SafetyAlert>();
            CreateMap<SafetyAlert, MedicineSafetyViewModel>();
            CreateMap<Notification, NotificationViewModel>();
            CreateMap<NotificationViewModel, Notification>();
            CreateMap<AuditLog, AuditLogViewModel>()
                .ForMember(destination => destination.UserName,
                    options => options.MapFrom(source => source.User == null ? null : source.User.FullName));
        }
    }
}