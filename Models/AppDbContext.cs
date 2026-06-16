using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace R3AIA.Models
{
	public class AppDbContext : IdentityDbContext<ApplicationUser>

	{
		public AppDbContext(DbContextOptions<AppDbContext> options):base(options)
		{
			
		}
		public DbSet<Patient> Patients { get; set; }
		public DbSet<Doctor> Doctors { get; set; }
		public DbSet<Pharmacy> Pharmacies { get; set; }
		public DbSet<Volunteer> Volunteers { get; set; }

		public DbSet<Governorate> Governorates { get; set; }
		public DbSet<City> Cities { get; set; }
		public DbSet<Specialty> Specialties { get; set; }

		public DbSet<MedicalRequest> MedicalRequests { get; set; }
		public DbSet<MedicineRequest> MedicineRequests { get; set; }
		public DbSet<VolunteerRequest> VolunteerRequests { get; set; }
		public DbSet<DeliveryTask> DeliveryTasks { get; set; }

		public DbSet<DonationCase> DonationCases { get; set; }
		public DbSet<Donation> Donations { get; set; }
		public DbSet<PatientCase> PatientCases { get; set; }
		public DbSet<PatientDonation> PatientDonations { get; set; }
		public DbSet<Notification> Notifications { get; set; }
		public DbSet<UserReport> UserReports { get; set; }
		public DbSet<SupportMessage> SupportMessages { get; set; }
		public DbSet<SupportTicket> SupportTickets { get; set; }
		public DbSet<SupportReply> SupportReplies { get; set; }

		// === جداول طبيب الخير ===
		public DbSet<KhairDoctor> KhairDoctors { get; set; }
		public DbSet<KhairAppointmentSlot> KhairAppointmentSlots { get; set; }
		public DbSet<KhairBooking> KhairBookings { get; set; }

		// === جداول سند والاشتراك ===
		public DbSet<SubscriptionRequest> SubscriptionRequests { get; set; }
		public DbSet<SanadSetting> SanadSettings { get; set; }
		public DbSet<SanadLog> SanadLogs { get; set; }

		// === جداول رعاية بوكس ===
		public DbSet<R3aiaBoxSetting> R3aiaBoxSettings { get; set; }
		public DbSet<R3aiaBoxImage> R3aiaBoxImages { get; set; }
		public DbSet<R3aiaBoxOrder> R3aiaBoxOrders { get; set; }
		protected override void OnModelCreating(ModelBuilder builder)
		{
			base.OnModelCreating(builder); 
										  
			builder.Entity<UserReport>()
				.HasOne(r => r.Reporter)
				.WithMany()
				.HasForeignKey(r => r.ReporterId)
				.OnDelete(DeleteBehavior.NoAction); // منع الحذف التلقائي المتعدد

			// 2. ربط الشخص المُبلغ عنه (ReportedUser)
			builder.Entity<UserReport>()
				.HasOne(r => r.ReportedUser)
				.WithMany()
				.HasForeignKey(r => r.ReportedUserId)
				.OnDelete(DeleteBehavior.NoAction); // منع الحذف التلقائي المتعدد

			builder.Entity<Patient>()
				.HasIndex(p => p.NationalID).IsUnique();

			builder.Entity<Volunteer>()
				.HasIndex(v => v.NationalID).IsUnique();

			// الرقم القومي فريد على مستوى نظام الهوية بالكامل (مع تجاهل القيم الفارغة)
			builder.Entity<ApplicationUser>()
				.HasIndex(u => u.NationalID)
				.IsUnique()
				.HasFilter("[NationalID] IS NOT NULL AND [NationalID] <> ''");


			// تعطيل الحذف التلقائي للمحافظة لمنع تداخل المسارات
			builder.Entity<Doctor>()
				.HasOne(d => d.Governorate)
				.WithMany()
				.HasForeignKey(d => d.GovernorateId)
				.OnDelete(DeleteBehavior.NoAction); // هذا هو السطر السحري

			builder.Entity<Patient>()
				.HasOne(p => p.Governorate)
				.WithMany()
				.HasForeignKey(p => p.GovernorateId)
				.OnDelete(DeleteBehavior.NoAction);

			builder.Entity<Pharmacy>()
				.HasOne(ph => ph.Governorate)
				.WithMany()
				.HasForeignKey(ph => ph.GovernorateId)
				.OnDelete(DeleteBehavior.NoAction);

			builder.Entity<MedicalRequest>()
				.HasOne(m => m.Doctor)
				.WithMany()
				.HasForeignKey(m => m.DoctorId)
				.OnDelete(DeleteBehavior.Restrict);

			
			builder.Entity<MedicineRequest>()
				.HasOne(m => m.Pharmacy)
				.WithMany()
				.HasForeignKey(m => m.PharmacyId)
				.OnDelete(DeleteBehavior.Restrict);

	
			builder.Entity<City>()
				.HasOne(c => c.Governorate)
				.WithMany(g => g.Cities)
				.HasForeignKey(c => c.GovernorateId)
				.OnDelete(DeleteBehavior.Cascade);

            builder.Entity<VolunteerRequest>()
                .Property(v => v.Amount)
                .HasColumnType("decimal(18,2)");

			// PatientCase → Governorate (no cascade)
			builder.Entity<PatientCase>()
				.HasOne(pc => pc.Governorate)
				.WithMany()
				.HasForeignKey(pc => pc.GovernorateId)
				.OnDelete(DeleteBehavior.NoAction);

			// PatientDonation → Donor (no cascade on nullable FK)
			builder.Entity<PatientDonation>()
				.HasOne(d => d.Donor)
				.WithMany()
				.HasForeignKey(d => d.DonorId)
				.OnDelete(DeleteBehavior.NoAction);

			// PatientDonation → PatientCase (cascade delete)
			builder.Entity<PatientDonation>()
				.HasOne(d => d.PatientCase)
				.WithMany(pc => pc.Donations)
				.HasForeignKey(d => d.PatientCaseId)
				.OnDelete(DeleteBehavior.Cascade);

			// === طبيب الخير Relationships ===
			builder.Entity<KhairDoctor>()
				.HasOne(kd => kd.Doctor)
				.WithMany()
				.HasForeignKey(kd => kd.DoctorId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.Entity<KhairAppointmentSlot>()
				.HasOne(s => s.KhairDoctor)
				.WithMany(kd => kd.Slots)
				.HasForeignKey(s => s.KhairDoctorId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.Entity<KhairBooking>()
				.HasOne(b => b.KhairDoctor)
				.WithMany(kd => kd.Bookings)
				.HasForeignKey(b => b.KhairDoctorId)
				.OnDelete(DeleteBehavior.NoAction);

			builder.Entity<KhairBooking>()
				.HasOne(b => b.Slot)
				.WithOne(s => s.Booking)
				.HasForeignKey<KhairBooking>(b => b.SlotId)
				.OnDelete(DeleteBehavior.NoAction);

			builder.Entity<KhairBooking>()
				.HasOne(b => b.Patient)
				.WithMany()
				.HasForeignKey(b => b.PatientId)
				.OnDelete(DeleteBehavior.NoAction);

			// === سند والاشتراك ===
			builder.Entity<SubscriptionRequest>()
				.HasOne(s => s.User)
				.WithMany()
				.HasForeignKey(s => s.UserId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.Entity<SanadSetting>()
				.HasOne(s => s.User)
				.WithMany()
				.HasForeignKey(s => s.UserId)
				.OnDelete(DeleteBehavior.Cascade)
				.IsRequired();
			
			builder.Entity<SanadSetting>()
				.HasIndex(s => s.UserId)
				.IsUnique(); // كل مريض له إعداد واحد فقط

			builder.Entity<SanadLog>()
				.HasOne(l => l.User)
				.WithMany()
				.HasForeignKey(l => l.UserId)
				.OnDelete(DeleteBehavior.Cascade);

			// === رعاية بوكس ===
			builder.Entity<R3aiaBoxImage>()
				.HasOne(i => i.R3aiaBoxSetting)
				.WithMany(s => s.Images)
				.HasForeignKey(i => i.R3aiaBoxSettingId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.Entity<R3aiaBoxSetting>()
				.Property(s => s.Price)
				.HasColumnType("decimal(18,2)");
		}

	}
}
