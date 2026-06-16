using AutoMapper;
using R3AIA.DTOs;
using R3AIA.Models;

namespace R3AIA.Mapping;

	public class MappingProfile : Profile
	{
		public MappingProfile()
		{
			CreateMap<MedicineRequest, MedicineRequestSummaryDto>()
			.ForMember(d => d.PatientName, opt => opt.MapFrom(s => s.Patient.FullName))
			.ForMember(d => d.PatientPhone, opt => opt.MapFrom(s => s.Patient.PhoneNumber))
			.ForMember(d => d.PatientAddress, opt => opt.MapFrom(s => s.Patient.Address))
			.ForMember(d => d.PatientCity, opt => opt.MapFrom(s => s.Patient.City.Name))
			.ForMember(d => d.PatientGovernorate, opt => opt.MapFrom(s => s.Patient.Governorate.Name))
			.ForMember(d => d.PrescriptionImageUrl, opt => opt.MapFrom(s => s.PrescriptionImage))
			.ForMember(d => d.MedicineName, opt => opt.MapFrom(s => s.MedicineName))
			.ForMember(d => d.Status, opt => opt.MapFrom(s => s.RequestStatus.ToString()));

		CreateMap<MedicalRequest, MedicalRequestSummaryDto>()
			.ForMember(d => d.PatientName, opt => opt.MapFrom(s => s.Patient != null ? s.Patient.FullName : "مريض غير معروف"))
			.ForMember(d => d.PatientPhone, opt => opt.MapFrom(s => s.Patient != null ? s.Patient.PhoneNumber : "-"))
			.ForMember(d => d.PatientGovernorate, opt => opt.MapFrom(s => s.Patient != null && s.Patient.Governorate != null ? s.Patient.Governorate.Name : "غير محدد"))
			.ForMember(d => d.SpecialtyName, opt => opt.MapFrom(s => s.Specialty != null ? s.Specialty.Name : "غير محدد"))
			.ForMember(d => d.Status, opt => opt.MapFrom(s => s.RequestStatus.ToString()))
			.ForMember(d => d.ChronicDisease, opt => opt.MapFrom(s => s.ChronicDisease))
			.ForMember(d => d.HasAttachments, opt => opt.MapFrom(s => s.HasAttachments))
			.ForMember(d => d.AppointmentDate, opt => opt.MapFrom(s => s.AppointmentDate));

		// Mapping for medical request detail
		CreateMap<MedicalRequest, MedicalRequestDetailDto>()
			.ForMember(dest => dest.RequestId, opt => opt.MapFrom(src => src.Id))
			.ForMember(dest => dest.SpecialtyName, opt => opt.MapFrom(src => src.Specialty != null ? src.Specialty.Name : "غير محدد"))
			.ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.RequestStatus.ToString()))
			.ForMember(dest => dest.ChronicDisease, opt => opt.MapFrom(src => src.ChronicDisease))
			.ForMember(dest => dest.Patient, opt => opt.MapFrom(src => src.Patient ?? new Patient()))
			.ForMember(dest => dest.MedicalImages, opt => opt.Ignore())
			.AfterMap((src, dest) => {
				if (!string.IsNullOrEmpty(src.MedicalImages))
				{
					dest.MedicalImages = src.MedicalImages.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
				}
				else
				{
				    dest.MedicalImages = new List<string>();
				}
			});

		// Mapping for patient info
		CreateMap<Patient, PatientInfoDto>()
			.ForMember(dest => dest.GovernorateName, opt => opt.MapFrom(src => src.Governorate != null ? src.Governorate.Name : "غير محدد"))
			.ForMember(dest => dest.CityName, opt => opt.MapFrom(src => src.City != null ? src.City.Name : "غير محدد"));

		// Mapping for patient's own requests
		CreateMap<MedicalRequest, MyMedicalRequestDto>()
			.ForMember(dest => dest.SpecialtyName, opt => opt.MapFrom(src => src.Specialty.Name))
			.ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.RequestStatus.ToString()))
			.ForMember(dest => dest.ChronicDisease, opt => opt.MapFrom(src => src.ChronicDisease))
			.ForMember(dest => dest.DoctorName, opt => opt.MapFrom(src => 
				src.Doctor != null ? src.Doctor.FullName : null))
			.ForMember(dest => dest.DoctorPhone, opt => opt.MapFrom(src => 
				src.Doctor != null ? src.Doctor.PhoneNumber : null))
			.ForMember(dest => dest.ClinicAddress, opt => opt.MapFrom(src => 
				src.Doctor != null ? src.Doctor.ClinicAddress : null));

		CreateMap<DonationCase, DonationCaseSummaryDto>();
		CreateMap<Donation, DonationResultDto>()
			.ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()));
		CreateMap<Notification, NotificationDto>();

		// ── PatientCase mappings ─────────────────────────────────────────────
		CreateMap<PatientCase, PatientCaseSummaryDto>()
			.ForMember(d => d.CaseType, opt => opt.MapFrom(s => s.CaseType.ToString()))
			.ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
			.ForMember(d => d.GovernorateName, opt => opt.MapFrom(s => s.Governorate != null ? s.Governorate.Name : null))
			.ForMember(d => d.Images, opt => opt.MapFrom(s => s.Images))
			.ForMember(d => d.ProgressPercent, opt => opt.MapFrom(s =>
				s.RequiredAmount > 0 ? Math.Round((double)(s.CollectedAmount / s.RequiredAmount) * 100, 1) : 0));

		CreateMap<PatientCase, PatientCaseDetailDto>()
			.IncludeBase<PatientCase, PatientCaseSummaryDto>()
			.ForMember(d => d.RecentDonations, opt => opt.MapFrom(s =>
				s.Donations.Where(d => d.Status == Enums.DonationStatus.Approved)
				           .OrderByDescending(d => d.CreatedAt)
				           .Take(10)));

		// ── PatientDonation mappings ─────────────────────────────────────────
		CreateMap<PatientDonation, PatientDonationSummaryDto>()
			.ForMember(d => d.CaseTitle, opt => opt.MapFrom(s => s.PatientCase != null ? s.PatientCase.Title : ""))
			.ForMember(d => d.DonorDisplayName, opt => opt.MapFrom(s => s.DonorName ?? "متبرع مجهول"))
			.ForMember(d => d.PaymentMethod, opt => opt.MapFrom(s => s.PaymentMethod.ToString()))
			.ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
			.ForMember(d => d.ProofImageUrl, opt => opt.MapFrom(s => s.ProofImage));

		CreateMap<PatientDonation, AdminPatientDonationDto>()
			.IncludeBase<PatientDonation, PatientDonationSummaryDto>()
			.ForMember(d => d.IsGuest, opt => opt.MapFrom(s => string.IsNullOrEmpty(s.DonorId)))
			.ForMember(d => d.DonorId, opt => opt.MapFrom(s => s.DonorId));

		// UserReport mapping
		CreateMap<UserReport, UserReportDto>()
			.ForMember(d => d.ReporterName, opt => opt.MapFrom(s => s.Reporter.UserName))
			.ForMember(d => d.ReportedUserName, opt => opt.MapFrom(s => s.ReportedUser.UserName))
			.ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()));
		
		
		// Medicine Request mappings
		CreateMap<MedicineRequest, MyMedicineRequestDto>()
			.ForMember(dest => dest.PrescriptionImageUrl, opt => opt.MapFrom(src => src.PrescriptionImage))
			.ForMember(dest => dest.MedicineName, opt => opt.MapFrom(src => src.MedicineName))
			.ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.RequestStatus.ToString()))
			.ForMember(dest => dest.PharmacyName, opt => opt.MapFrom(src => 
				src.Pharmacy != null ? src.Pharmacy.PharmacyName : null))
			.ForMember(dest => dest.PharmacyPhone, opt => opt.MapFrom(src => 
				src.Pharmacy != null ? src.Pharmacy.PhoneNumber : null))
			.ForMember(dest => dest.PharmacyAddress, opt => opt.MapFrom(src => 
				src.Pharmacy != null ? src.Pharmacy.Address : null));

		CreateMap<VolunteerRequest, VolunteerRequestSummaryDto>()
			.ForMember(d => d.PatientName, opt => opt.MapFrom(s => s.Patient.FullName))
			.ForMember(d => d.PatientPhone, opt => opt.MapFrom(s => s.Patient.PhoneNumber))
			.ForMember(d => d.PatientAddress, opt => opt.MapFrom(s => s.Patient.Address))
			.ForMember(d => d.PatientCity, opt => opt.MapFrom(s => s.Patient.City.Name))
			.ForMember(d => d.PatientGovernorate, opt => opt.MapFrom(s => s.Patient.Governorate.Name))
			.ForMember(d => d.Type, opt => opt.MapFrom(s => s.Type.ToString()))
			.ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()));

		CreateMap<VolunteerRequest, MyVolunteerRequestDto>()
			.ForMember(d => d.Type, opt => opt.MapFrom(s => s.Type.ToString()))
			.ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
			.ForMember(d => d.VolunteerName, opt => opt.MapFrom(s => s.Volunteer != null ? s.Volunteer.FullName : null))
			.ForMember(d => d.VolunteerPhone, opt => opt.MapFrom(s => s.Volunteer != null ? s.Volunteer.PhoneNumber : null));
		
		CreateMap<MedicineRequest, DeliveryTaskDto>()
			.ForMember(dest => dest.RequestId, opt => opt.MapFrom(src => src.Id))
			.ForMember(dest => dest.PharmacyName, opt => opt.MapFrom(src => src.Pharmacy!.PharmacyName))
			.ForMember(dest => dest.PharmacyAddress, opt => opt.MapFrom(src => src.Pharmacy!.Address))
			.ForMember(dest => dest.PharmacyPhone, opt => opt.MapFrom(src => src.Pharmacy!.PhoneNumber))
			.ForMember(dest => dest.PatientName, opt => opt.MapFrom(src => src.Patient.FullName))
			.ForMember(dest => dest.PatientAddress, opt => opt.MapFrom(src => 
				src.Patient.City != null ? $"{src.Patient.City.Name}, {src.Patient.Governorate.Name}" : ""))
			.ForMember(dest => dest.PatientPhone, opt => opt.MapFrom(src => src.Patient.PhoneNumber));
	}
	}
