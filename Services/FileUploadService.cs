namespace CinemaManagement.Services
{
    public class FileUploadService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FileUploadService> _logger;
        private readonly long _maxFileSize = 5 * 1024 * 1024; // 5MB
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };

        public FileUploadService(IWebHostEnvironment environment, ILogger<FileUploadService> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        public async Task<(bool Success, string? FilePath, string? ErrorMessage)> UploadImageAsync(IFormFile file, string folder = "support")
        {
            try
            {
                // Validate file
                if (file == null || file.Length == 0)
                {
                    return (false, null, "Không có file được chọn");
                }

                // Check file size
                if (file.Length > _maxFileSize)
                {
                    return (false, null, $"File quá lớn. Kích thước tối đa cho phép: {_maxFileSize / (1024 * 1024)}MB");
                }

                // Check file extension
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!_allowedExtensions.Contains(extension))
                {
                    return (false, null, "Định dạng file không được hỗ trợ. Chỉ chấp nhận: jpg, jpeg, png, gif, bmp");
                }

                // Create directory if not exists
                var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", folder);
                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                // Generate unique filename
                var fileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadPath, fileName);

                // Save file
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                // Return relative path for database storage
                var relativePath = $"/uploads/{folder}/{fileName}";
                _logger.LogInformation("File uploaded successfully: {FilePath}", relativePath);
                
                return (true, relativePath, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file: {FileName}", file?.FileName);
                return (false, null, "Có lỗi xảy ra khi upload file");
            }
        }

        public bool DeleteFile(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                    return true;

                var fullPath = Path.Combine(_environment.WebRootPath, filePath.TrimStart('/'));
                
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    _logger.LogInformation("File deleted successfully: {FilePath}", filePath);
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file: {FilePath}", filePath);
                return false;
            }
        }
    }
} 