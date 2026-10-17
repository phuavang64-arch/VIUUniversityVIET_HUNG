namespace VIUUniversity.Services
{
    /// <summary>
    /// Lưu tệp đính kèm (PDF) của văn bản học phí vào wwwroot.
    ///
    /// Tách khỏi <see cref="ImageUploadHelper"/> vì kiểm tra khác nhau: ảnh kiểm tra
    /// magic byte theo đuôi, còn PDF phải chặn tệp HTML/JS đổi tên <c>.pdf</c> — tệp
    /// đó sẽ được phục vụ ngay trên chính origin của site và chạy script trong
    /// trình duyệt của người đọc.
    /// </summary>
    public static class DocumentUploadHelper
    {
        public const long MaxPdfBytes = 20 * 1024 * 1024; // 20 MB

        /// <summary>Chữ ký định dạng PDF theo đặc tả (ISO 32000-1 §7.5.2).</summary>
        private static readonly byte[] PdfMagic = System.Text.Encoding.ASCII.GetBytes("%PDF-");

        public static async Task<string> SavePdfAsync(IFormFile file, string webRootPath, string folderName)
        {
            if (file.Length == 0)
                throw new InvalidOperationException("Tệp tải lên rỗng.");

            if (file.Length > MaxPdfBytes)
                throw new InvalidOperationException(
                    $"Kích thước tệp vượt quá {MaxPdfBytes / (1024 * 1024)} MB.");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".pdf")
                throw new InvalidOperationException("Chỉ chấp nhận tệp PDF.");

            await VerifyPdfMagicBytesAsync(file);

            var folder = Path.Combine(webRootPath, "uploads", folderName);
            Directory.CreateDirectory(folder);

            var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}.pdf";
            var fullPath = Path.Combine(folder, fileName);

            await using var stream = System.IO.File.Create(fullPath);
            await file.CopyToAsync(stream);

            return $"/uploads/{folderName}/{fileName}";
        }

        /// <summary>
        /// Đọc 5 byte đầu rồi mới ghi đĩa. Tệp .pdf thật bắt đầu bằng "%PDF-"; tệp
        /// HTML/JS/SVG đổi tên .pdf thì không có chuỗi này ở đầu, nên bị chặn trước
        /// khi nó kịp trở thành nội dung tĩnh do chính site phục vụ.
        /// </summary>
        private static async Task VerifyPdfMagicBytesAsync(IFormFile file)
        {
            using var stream = file.OpenReadStream();
            var header = new byte[PdfMagic.Length];
            var read = await stream.ReadAsync(header.AsMemory(0, header.Length));

            if (read < header.Length || !header.AsSpan(0, read).SequenceEqual(PdfMagic))
                throw new InvalidOperationException("Tệp tải lên không phải tệp PDF hợp lệ.");
        }
    }
}