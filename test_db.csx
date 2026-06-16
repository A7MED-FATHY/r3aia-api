using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using R3AIA;
using R3AIA.Models;
using Microsoft.Extensions.DependencyInjection;

var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
optionsBuilder.UseSqlServer("Server=localhost;Database=r3aia_db;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True");
using var context = new AppDbContext(optionsBuilder.Options);

var pendingDocs = context.Doctors.Include(d => d.Specialty).Include(d => d.Governorate).ToList();
Console.WriteLine("\n--- All Doctors ---");
foreach (var d in pendingDocs) {
    Console.WriteLine($"Dr. {d.FullName} - ID: {d.Id} - User: {d.IdentityUserId} - SpecId: {d.SpecialtyId} - GovId: {d.GovernorateId}");
}

var allReqs = context.MedicalRequests.Include(r => r.Patient).ToList();
Console.WriteLine("\n--- All Medical Requests ---");
foreach (var r in allReqs) {
    Console.WriteLine($"Req: {r.Id} - Status: {r.RequestStatus} - SpecId: {r.SpecialtyId} - PatId: {r.PatientId}");
    if (r.Patient != null) {
        Console.WriteLine($"   Patient GovId: {r.Patient.GovernorateId} - CityId: {r.Patient.CityId}");
    } else {
        Console.WriteLine("   Patient is NULL!");
    }
}
