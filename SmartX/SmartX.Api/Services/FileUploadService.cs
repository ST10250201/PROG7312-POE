using System.Security.Cryptography;
using SmartX.Api.Models;

namespace SmartX.Api.Services
{
    public class FileUploadService
    {
        private readonly string _uploadFolder;

        public FileUploadService(IWebHostEnvironment environment)
        {
            _uploadFolder = Path.Combine(
                environment.ContentRootPath,
                "EncryptedUploads");

            Directory.CreateDirectory(_uploadFolder);
        }

        public async Task<SensorAttachment> SaveEncryptedAsync(
            IFormFile file,
            string sensorMacAddress)
        {
            if (file == null || file.Length == 0)
            {
                throw new InvalidOperationException(
                    "A valid file must be selected.");
            }

            string[] allowedExtensions =
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".txt",
                ".log",
                ".pdf"
            };

            string extension =
                Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException(
                    "Unsupported file type.");
            }

            const long maxFileSize = 5 * 1024 * 1024;

            if (file.Length > maxFileSize)
            {
                throw new InvalidOperationException(
                    "File size cannot exceed 5 MB.");
            }

            byte[] originalBytes;

            using (MemoryStream memoryStream = new())
            {
                await file.CopyToAsync(memoryStream);
                originalBytes = memoryStream.ToArray();
            }

            byte[] key = RandomNumberGenerator.GetBytes(32);
            byte[] iv = RandomNumberGenerator.GetBytes(16);

            byte[] encryptedBytes;

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;

                using ICryptoTransform encryptor =
                    aes.CreateEncryptor();

                encryptedBytes =
                    encryptor.TransformFinalBlock(
                        originalBytes,
                        0,
                        originalBytes.Length);
            }

            string storedFileName =
                $"{Guid.NewGuid()}.encrypted";

            string filePath =
                Path.Combine(_uploadFolder, storedFileName);

            await File.WriteAllBytesAsync(
                filePath,
                encryptedBytes);

            string keyFilePath =
                Path.Combine(
                    _uploadFolder,
                    $"{storedFileName}.key");

            byte[] keyData =
                key.Concat(iv).ToArray();

            await File.WriteAllBytesAsync(
                keyFilePath,
                keyData);

            return new SensorAttachment
            {
                SensorMacAddress = sensorMacAddress,
                OriginalFileName = file.FileName,
                StoredFileName = storedFileName,
                ContentType = file.ContentType,
                FileSize = file.Length,
                UploadedAt = DateTime.UtcNow
            };
        }
    }
}