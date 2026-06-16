-- Seeding diverse doctors for testing the 'All' filter
USE reaiaDb;

-- Ensure we have enough doctors with Khair profiles
INSERT INTO KhairDoctors (DoctorId, ConsultationType, DiscountedPrice, FreeDailyLimit, BioNotes, IsActive, CreatedAt)
SELECT Id, 1, 100.0, 3, N'طبيب متميز في مبادرة الرعاية الصحية', 1, GETDATE()
FROM Doctors
WHERE Id NOT IN (SELECT DoctorId FROM KhairDoctors)
AND Id > 0;

-- Update some to be in different governorates/specialties if needed
-- But they are already linked to the Doctors table which has GovId.

-- Let's make sure at least 10 are active and discounted
UPDATE TOP (10) KhairDoctors
SET ConsultationType = 1, -- Discounted
    IsActive = 1
WHERE IsActive = 0 OR ConsultationType != 1;

-- If we still don't have enough, we'll manually update a few to ensure diversity
-- Doc 9: khald Ahmed (Gov 26) - already set
-- Doc 10: ليلى المنصورى (Gov 25) - already set
-- Doc 11: خالد السعيد (Gov 1) - already set
-- Doc 12: حسنى (Gov 1?) - let's check

UPDATE KhairDoctors SET IsActive = 1, ConsultationType = 1 WHERE DoctorId IN (9, 10, 11, 12, 7, 8);

SELECT 'Seeding check:' as Info, COUNT(*) as TotalDiscountedActive FROM KhairDoctors WHERE ConsultationType = 1 AND IsActive = 1;
