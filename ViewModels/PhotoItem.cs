namespace VIUUniversity.ViewModels
{
    /// <summary>
    /// Một ô ảnh trong các khối "hình ảnh nhà trường".
    ///
    /// Các trang nội dung tĩnh (About / Research / StudentLife / International)
    /// không có bảng riêng trong database nên phải khai báo ảnh từ view. Dùng
    /// kiểu có tên thay vì tuple để Razor không phải truy cận
    /// <c>item.Item1</c> — đọc được hơn nhiều khi sửa template.
    /// </summary>
    public class PhotoItem
    {
        /// <summary>Đường dẫn ảnh trong <c>wwwroot/images</c>, ví dụ <c>/images/viu/viu-garden.jpg</c>.</summary>
        public string Src { get; set; } = "";

        /// <summary>Chữ thay thế (alt). Bắt buộc về mặt nghĩa: ảnh ở đây mang thông tin.</summary>
        public string Alt { get; set; } = "";

        /// <summary>Chú thích hiện dưới ảnh.</summary>
        public string Caption { get; set; } = "";

        /// <summary>Liên kết đi kèm ảnh. Rỗng = ảnh không bấm được.</summary>
        public string? Href { get; set; }

        /// <summary>
        /// Ô rộng 2 cột trong lưới ảnh — dành cho ảnh toàn cảnh.
        /// </summary>
        public bool Wide { get; set; }

        /// <summary>Ô cao 2 hàng, khung dọc 3:4.</summary>
        public bool Tall { get; set; }
    }
}