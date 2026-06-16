using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using R3AIA.Models;
using R3AIA;

var hostStr = args.Length > 0 ? args[0] : "doctorfayoum@r3aia.com";
Console.WriteLine($"Testing doctor email: {hostStr}");

// Create instance of DbContext
var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
optionsBuilder.UseSqlServer("Server=localhost;Database=R3AIA;Trusted_Connection=True;MultipleActiveResultSets=true;Encrypt=False");
using var context = new AppDbContext(optionsBuilder.Options);

var u = context.Users.FirstOrDefault(x => x.Email == hostStr);
if (u == null) {
    Console.WriteLine("Doctor user not found.");
    return;
}
Console.WriteLine($"Found user: {u.Id}");

var doctor = context.Doctors.FirstOrDefault(d => d.IdentityUserId == u.Id);
if (doctor == null) {
    Console.WriteLine("Doctor profile not found.");
    return;
}
Console.WriteLine($"Doctor GovernorateId: {doctor.GovernorateId}, SpecialtyId: {doctor.SpecialtyId}");

var queryCount = context.MedicalRequests
    .Include(r => r.Patient)
    .Count(r => r.RequestStatus == Enums.RequestStatus.Pending
                  && r.SpecialtyId == doctor.SpecialtyId
                  && r.Patient.GovernorateId == doctor.GovernorateId);

Console.WriteLine($"Stats available requests count: {queryCount}");

var queryList = context.MedicalRequests
    .Include(r => r.Patient)
        .ThenInclude(p => p.City)
    .Include(r => r.Patient)
        .ThenInclude(p => p.Governorate)
    .Include(r => r.Specialty)
    .Where(r => r.RequestStatus == Enums.RequestStatus.Pending &&
                r.Patient.GovernorateId == doctor.GovernorateId &&
                r.SpecialtyId == doctor.SpecialtyId)
    .ToList();

Console.WriteLine($"GetForDoctor list count: {queryList.Count}");
