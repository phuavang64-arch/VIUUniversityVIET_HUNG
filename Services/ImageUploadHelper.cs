namespace VIUUniversity.Services
{
    /// <summary>
    /// Saves uploaded images to wwwroot.
    ///
    /// Three checks matter here, and the original implementation only had the first:
    ///   1. extension allow-list - cheap, but purely attacker-controlled;
    ///   2. file size cap       - without it a single request can fill the disk;
    ///   3. magic-byte sniffing - a ".jpg" that is really HTML/SVG gets served back with
    ///      the extension's content type and becomes stored XSS on the site's own origin.
    /// </summary>
    public static class ImageUploadHelper
    {
        public const long MaxImageBytes = 5 * 1024 * 1024; // 5 MB

        private static readonly Dictionary<string, byte[]> MagicBytes = new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = new byte[] { 0xFF, 0xD8, 0xFF },
            [".jpeg"] = new byte[] { 0xFF, 0xD8, 0xFF },
            [".png"] = new byte[] { 0x89, 0x50, 0x4E, 0x47 },
            [".gif"] = new byte[] { 0x47, 0x49, 0x46, 0x38 },
            [".webp"] = new byte[] { 0x52, 0x49, 0x46, 0x46 }
        };

        /// <summary>
        /// Validates the upload and returns the generated file name (without the leading
        /// slash). Throws <see cref="InvalidOperationException"/> with a user-facing message
        /// when the file is not an acceptable image.
        /// </summary>
        public static async Task<string> SaveAsync(IFormFile file, string webRootPath, string folderName)
        {
            if (file.Length == 0)
                throw new InvalidOperationException("Tệp tải lên rỗng.");

            if (file.Length > MaxImageBytes)
                throw new InvalidOperationException($"Kích thước ảnh vượt quá {MaxImageBytes / (1024 * 1024)} MB.");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!MagicBytes.ContainsKey(ext))
                throw new InvalidOperationException("Chỉ chấp nhận ảnh JPG, JPEG, PNG, WEBP hoặc GIF.");

            await VerifyMagicBytesAsync(file, ext);

            var folder = Path.Combine(webRootPath, "uploads", folderName);
            Directory.CreateDirectory(folder);

            var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(folder, fileName);

            await using var stream = System.IO.File.Create(fullPath);
            await file.CopyToAsync(stream);

            return $"/uploads/{folderName}/{fileName}";
        }

        private static async Task VerifyMagicBytesAsync(IFormFile file, string ext)
        {
            var expected = MagicBytes[ext];

            using var stream = file.OpenReadStream();
            var header = new byte[expected.Length];
            var read = await stream.ReadAsync(header.AsMemory(0, header.Length));

            if (read < expected.Length || !header.AsSpan(0, read).SequenceEqual(expected))
                throw new InvalidOperationException("Tệp tải lên không phải là ảnh hợp lệ.");
        }
    }
}