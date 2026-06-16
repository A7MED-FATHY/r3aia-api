namespace R3AIA.Services;

public interface IFileService
{
	Task<string> SaveImageAsync(IFormFile file, string folderName = "Uploads");
}

public class FileService : IFileService
{
	private readonly ICloudinaryService _cloudinaryService;

	public FileService(ICloudinaryService cloudinaryService)
	{
		_cloudinaryService = cloudinaryService;
	}

	public async Task<string> SaveImageAsync(IFormFile file, string folderName = "Uploads")
	{
		// Upload to Cloudinary using the folderName as the Cloudinary folder
		var result = await _cloudinaryService.UploadImageAsync(file, folderName);
		
		// Return the Cloudinary secure URL directly
		return result.Url;
	}
}


