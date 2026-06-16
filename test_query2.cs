using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using R3AIA;
using R3AIA.Models;
using System.Text.Json;

var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
optionsBuilder.UseSqlServer("Server=localhost;Database=R3AIA;Trusted_Connection=True;MultipleActiveResultSets=true;Encrypt=False");
using var context = new AppDbContext(optionsBuilder.Options);

var doctor = context.Doctors.FirstOrDefault();
if (doctor == null) {
    Console.WriteLine("No doctor found");
    return;
}

var queryList = context.MedicalRequests
    .Include(r => r.Patient)
        .ThenInclude(p => p.City)
    .Include(r => r.Patient)
        .ThenInclude(p => p.Governorate)
    .Include(r => r.Specialty)
    .Where(r => r.RequestStatus == Enums.RequestStatus.Pending &&
                r.Patient.GovernorateId == doctor.GovernorateId &&
                r.SpecialtyId == doctor.SpecialtyId)
    .Select(r => new {
        id = r.Id,
        patientName = r.Patient.FullName,
        specialtyName = r.Specialty.Name,
        description = r.Description,
        patientPhone = r.Patient.PhoneNumber,
        patientGovernorate = r.Patient.Governorate.Name,
        createdAt = r.CreatedAt,
        status = r.RequestStatus.ToString(),
        chronicDisease = r.ChronicDisease,
        hasAttachments = r.HasAttachments
    })
    .ToList();

Console.WriteLine("Data:");
Console.WriteLine(JsonSerializer.Serialize(queryList, new JsonSerializerOptions { WriteIndented = true }));
