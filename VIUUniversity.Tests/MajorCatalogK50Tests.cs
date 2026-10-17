using VIUUniversity.Data;

namespace VIUUniversity.Tests;

/// <summary>
/// Danh mục 21 ngành đại học chính quy khóa 50 được trích từ cổng tuyển sinh chính
/// thức của Trường. Nếu danh sách này lệch với trang chính thức thì trang /dao-tao sẽ
/// hiển thị mã ngành sai — lỗi không tự báo, chỉ lộ khi có người đối chiếu.
/// </summary>
public class MajorCatalogK50Tests
{
    [Fact]
    public void CoDung21Nganh()
    {
        Assert.Equal(21, MajorCatalogK50.All.Length);
    }

    [Fact]
    public void MaNganhKhongTrung()
    {
        var codes = MajorCatalogK50.All.Select(e => e.Code).ToList();
        Assert.Equal(codes.Count, codes.Distinct().Count());
    }

    [Fact]
    public void MaNganhCoDungDoDaiHopLe()
    {
        // Mã ngành do Bộ GD&ĐT cấp, thường 6 chữ số; mã 7140114 (Quản lý giáo dục)
        // có 7. Mã ngắn hơn 6 hoặc dài hơn 7 gần như chắc chắn là nhầm, và sẽ
        // không khớp với dữ liệu tuyển sinh thật.
        Assert.All(MajorCatalogK50.All, e => Assert.Matches("^\\d{6,7}$", e.Code));
    }

    [Fact]
    public void DuongDanAnhTonTaiTrongWwwroot()
    {
        // Ảnh đại diện phải nằm trong wwwroot, nếu không trang /dao-tao sẽ hiển thị
        // icon ảnh hỏng cho cả 21 ngành.
        var root = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
                                "..", "wwwroot");
        if (!Directory.Exists(root))
        {
            return; // chạy từ thư mục khác -> bỏ qua
        }

        Assert.All(MajorCatalogK50.All, e =>
        {
            var path = Path.Combine(root, e.ImageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(path), $"Thiếu ảnh: {e.ImageUrl} ({e.Name})");
        });
    }

    [Fact]
    public void AnhKhongTrungGiuaCacNganh()
    {
        // Dùng chung một ảnh cho nhiều ngành là lỗi dễ mắc khi lấy ảnh từ trang
        // tuyển sinh: mỗi ngành phải có ảnh riêng.
        var images = MajorCatalogK50.All.Select(e => e.ImageUrl).ToList();
        Assert.Equal(images.Count, images.Distinct().Count());
    }

    [Fact]
    public void NoiDungKhongDeTrong()
    {
        // Các trường bắt buộc phải có nội dung. Để trống nghĩa là bước scrape bị lỗi
        // và trang chi tiết sẽ hiện "Chưa có mô tả chi tiết" cho cả 21 ngành.
        Assert.All(MajorCatalogK50.All, e =>
        {
            Assert.False(string.IsNullOrWhiteSpace(e.Name), $"Thiếu tên ngành {e.Code}");
            Assert.False(string.IsNullOrWhiteSpace(e.Slug), $"Thiếu slug ngành {e.Code}");
            Assert.False(string.IsNullOrWhiteSpace(e.SourceUrl), $"Thiếu nguồn ngành {e.Code}");
            Assert.NotEmpty(e.IntroParagraphs);
            Assert.NotEmpty(e.CareerParagraphs);
        });
    }

    [Fact]
    public void KhongChuaHocPhiHayDiemChuan()
    {
        // Học phí và điểm chuẩn đổi theo từng năm tuyển sinh. Nếu có số trong danh
        // mục thì nó sẽ bị đọc là thông tin chính thức của Trường.
        var json = System.Text.Json.JsonSerializer.Serialize(MajorCatalogK50.All);
        Assert.DoesNotContain("đồng", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("điểm chuẩn", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("chỉ tiêu", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PhanKhoaPhuHopVoiKhoaDangCo()
    {
        // Cổng tuyển sinh không công bố khoa quản lý, nên danh mục phân nhóm theo tính
        // chất ngành vào 3 khoa đang có. Mã khoa lạ nghĩa là ngành sẽ không hiện ở
        // trang khoa nào.
        var hopLe = new[] { "CNTT", "DIEN", "KT" };
        Assert.All(MajorCatalogK50.All, e => Assert.Contains(e.DepartmentCode, hopLe));
    }
}