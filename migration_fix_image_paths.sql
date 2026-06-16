-- SQL Script to fix absolute image paths and convert them to relative paths
-- This script removes the "http://ip:port/" prefix from image columns if it exists.

-- 1. Table: Doctors
-- Columns: ProfilePictureUrl, NIDImage
UPDATE Doctors
SET ProfilePictureUrl = SUBSTRING(ProfilePictureUrl, CHARINDEX('Uploads/', ProfilePictureUrl), LEN(ProfilePictureUrl))
WHERE ProfilePictureUrl LIKE 'http%Uploads/%';

UPDATE Doctors
SET NIDImage = SUBSTRING(NIDImage, CHARINDEX('Uploads/', NIDImage), LEN(NIDImage))
WHERE NIDImage LIKE 'http%Uploads/%';

-- 2. Table: PatientCases
-- Column: ImagesJson
UPDATE PatientCases
SET ImagesJson = REPLACE(ImagesJson, 
    SUBSTRING(ImagesJson, 1, CHARINDEX('Uploads/', ImagesJson) - 1), 
    '')
WHERE ImagesJson LIKE 'http%Uploads/%';
-- Note: If ImagesJson has multiple images with absolute paths, they are likely separated by '|'.
-- The above handles the first one. Let's do a more robust approach if possible, 
-- but usually they follow the same base URL.
-- RE-RUN multiple times if there are multiple absolute paths in the same string.
UPDATE PatientCases
SET ImagesJson = REPLACE(ImagesJson, 'http://192.168.0.6:5129/', '')
WHERE ImagesJson LIKE '%http://192.168.0.6:5129/%';

-- 3. Table: MedicineRequests
-- Column: PrescriptionImage
UPDATE MedicineRequests
SET PrescriptionImage = SUBSTRING(PrescriptionImage, CHARINDEX('Uploads/', PrescriptionImage), LEN(PrescriptionImage))
WHERE PrescriptionImage LIKE 'http%Uploads/%';

-- 4. Table: MedicalRequests
-- Column: MedicalImages
UPDATE MedicalRequests
SET MedicalImages = REPLACE(MedicalImages, 'http://192.168.0.6:5129/', '')
WHERE MedicalImages LIKE '%http://192.168.0.6:5129/%';

-- 5. Table: VolunteerRequests
-- Column: ReceiptUrl
UPDATE VolunteerRequests
SET ReceiptUrl = SUBSTRING(ReceiptUrl, CHARINDEX('Uploads/', ReceiptUrl), LEN(ReceiptUrl))
WHERE ReceiptUrl LIKE 'http%Uploads/%';

-- 6. Table: AspNetUsers (IdentityUser)
-- Column: ProfilePictureUrl (if exists)
IF COL_LENGTH('AspNetUsers', 'ProfilePictureUrl') IS NOT NULL
BEGIN
    EXEC('UPDATE AspNetUsers 
          SET ProfilePictureUrl = SUBSTRING(ProfilePictureUrl, CHARINDEX(''Uploads/'', ProfilePictureUrl), LEN(ProfilePictureUrl))
          WHERE ProfilePictureUrl LIKE ''http%Uploads/%''');
END
