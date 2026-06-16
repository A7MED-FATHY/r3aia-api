using System;

namespace R3AIA.DTOs
{
    public class AdminCompletedStatsResponse
    {
        public int TotalCompleted { get; set; }
        public int MedicalCompleted { get; set; }
        public int MedicineCompleted { get; set; }
        public int VolunteerCompleted { get; set; }
        public List<CompletedServiceDto> Services { get; set; } = new List<CompletedServiceDto>();
    }

    public class CompletedServiceDto
    {
        public int Id { get; set; }
        public string ServiceType { get; set; } = null!; // Medical, Medicine, Volunteer
        public string PatientName { get; set; } = null!;
        public string? ProviderName { get; set; }
        public string GovernorateName { get; set; } = null!;
        public DateTime ServiceDate { get; set; }
        
        // Type specific details
        public string? Description { get; set; }
        
        // Doctor specific
        public string? DoctorSpecialty { get; set; }
        public string? ClinicAddress { get; set; }
        public string? DoctorNotes { get; set; }
        
        // Pharmacy specific
        public string? MedicineName { get; set; }
        public string? PharmacyNotes { get; set; }
        
        // Volunteer specific
        public string? VolunteerType { get; set; } // Delivery, Donation, etc.
    }
}
