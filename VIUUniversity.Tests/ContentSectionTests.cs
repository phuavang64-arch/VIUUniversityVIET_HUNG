using Microsoft.EntityFrameworkCore;
using VIUUniversity.Models;
using VIUUniversity.Services;

namespace VIUUniversity.Tests;

/// <summary>
/// Bốn chuyên mục (Tin tức, Nghiên cứu khoa học, Hợp tác, Học phí) dùng chung bảng
/// <see cref="News"/> và chỉ khác nhau ở <see cref="News.Section"/>. Nếu lọc theo chuyên
/// mục bị hỏng thì bài học phí sẽ lọt vào trang Tin tức — lỗi khó phát hiện bằng
/// mắt vì cả hai trang đều "có nội dung".
/// </summary>
public class ContentSectionTests : DatabaseTestBase
{
    private NewsService CreateService() => new(Db, new ContentSanitizer());

    private News NewNews(string slug, string section, bool published = true,
                          bool deleted = false) => new()
    {
        Title = slug,
        Slug = slug,
        Content = "<p>Nội dung</p>",
        Section = section,
        IsPublished = published,
        IsDeleted = deleted
    };

    [Fact]
    public async Task GetPublishedBySectionAsync_ChiTraVeDungChuyenMuc()
    {
        Db.News.AddRange(
            NewNews("tin-tuc-a", ContentSection.TinTuc),
            NewNews("nghien-cuu-a", ContentSection.NghiênCứu),
            NewNews("hop-tac-a", ContentSection.HopTac),
            NewNews("hoc-phi-a", ContentSection.HocPhi));
        await Db.SaveChangesAsync();

        var tinTuc = await CreateService().GetPublishedBySectionAsync(
            ContentSection.TinTuc, 1, 10);
        var nghienCuu = await CreateService().GetPublishedBySectionAsync(
            ContentSection.NghiênCứu, 1, 10);
        var hopTac = await CreateService().GetPublishedBySectionAsync(
            ContentSection.HopTac, 1, 10);
        var hocPhi = await CreateService().GetPublishedBySectionAsync(
            ContentSection.HocPhi, 1, 10);

        Assert.Equal(1, tinTuc.TotalCount);
        Assert.Equal("tin-tuc-a", tinTuc.Items[0].Slug);
        Assert.Equal(1, nghienCuu.TotalCount);
        Assert.Equal("nghien-cuu-a", nghienCuu.Items[0].Slug);
        Assert.Equal(1, hopTac.TotalCount);
        Assert.Equal("hop-tac-a", hopTac.Items[0].Slug);
        Assert.Equal(1, hocPhi.TotalCount);
        Assert.Equal("hoc-phi-a", hocPhi.Items[0].Slug);
    }

    [Fact]
    public async Task GetPublishedBySectionAsync_BoQuaBanNhapVaBanDaXoa()
    {
        Db.News.AddRange(
            NewNews("ban-nhap", ContentSection.HocPhi, published: false),
            NewNews("ban-da-xoa", ContentSection.HocPhi, deleted: true),
            NewNews("ban-dang", ContentSection.HocPhi));
        await Db.SaveChangesAsync();

        var result = await CreateService().GetPublishedBySectionAsync(
            ContentSection.HocPhi, 1, 10);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("ban-dang", result.Items[0].Slug);
    }

    [Fact]
    public async Task GetBySlugInSectionAsync_KhongLayBaiCuaChuyenMucKhac()
    {
        // News.Slug có UNIQUE index nên hai bài không thể dùng chung slug; điều đó
        // vẫn đúng ý đồ: slug là định danh toàn site. Kiểm tra ở đây là truy vấn
        // theo chuyên mục không bao giờ trả về bài của chuyên mục khác.
        Db.News.Add(NewNews("chi-o-tin-tuc", ContentSection.TinTuc));
        await Db.SaveChangesAsync();

        var tinTuc = await CreateService().GetBySlugInSectionAsync(
            "chi-o-tin-tuc", ContentSection.TinTuc);
        var hocPhi = await CreateService().GetBySlugInSectionAsync(
            "chi-o-tin-tuc", ContentSection.HocPhi);

        Assert.NotNull(tinTuc);
        Assert.Equal(ContentSection.TinTuc, tinTuc.Section);
        Assert.Null(hocPhi);
    }

    [Fact]
    public async Task EnsureUniqueSlugAsync_ThemHauToKhiSlugDaCo()
    {
        // Hai bài cùng tiêu đề ở hai chuyên mục sẽ sinh cùng slug. Nếu không thêm
        // hậu tố thì lưu bài thứ hai vi phạm UNIQUE và trả 500.
        Db.News.Add(NewNews("cung-tieu-de", ContentSection.TinTuc));
        await Db.SaveChangesAsync();

        var thuHai = await CreateService().CreateAsync(
            NewNews("cung-tieu-de", ContentSection.HocPhi));

        Assert.NotEqual("cung-tieu-de", thuHai.Slug);
        Assert.StartsWith("cung-tieu-de-", thuHai.Slug);
    }

    [Fact]
    public async Task GetRelatedInSectionAsync_KhongLayBaiCuaChuyenMucKhac()
    {
        Db.News.AddRange(
            NewNews("lien-quan-tin", ContentSection.TinTuc),
            NewNews("lien-quan-hoc-phi", ContentSection.HocPhi));
        await Db.SaveChangesAsync();

        var related = await CreateService().GetRelatedInSectionAsync(
            ContentSection.HocPhi, 0, 5);

        Assert.Single(related);
        Assert.Equal("lien-quan-hoc-phi", related[0].Slug);
    }

    [Fact]
    public async Task GetAllForAdminBySectionAsync_GomCaBanNhap()
    {
        Db.News.AddRange(
            NewNews("nhap", ContentSection.NghiênCứu, published: false),
            NewNews("dang", ContentSection.NghiênCứu),
            NewNews("cua-tin-tuc", ContentSection.TinTuc));
        await Db.SaveChangesAsync();

        var result = await CreateService().GetAllForAdminBySectionAsync(
            ContentSection.NghiênCứu, 1, 10);

        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task CreateAsync_GanDungChuyenMuc()
    {
        var news = await CreateService().CreateAsync(
            NewNews("bai-moi", ContentSection.HocPhi));

        Assert.Equal(ContentSection.HocPhi, news.Section);
    }

    [Fact]
    public async Task CreateAsync_SectionRacThiVeTinTuc()
    {
        // Dữ liệu cũ hoặc request giả mạo có thể gửi Section bất kỳ giá trị nào.
        // Đổi về Tin tức thay vì lưu lại, nếu không bài sẽ biến mất khỏi mọi trang.
        var news = await CreateService().CreateAsync(
            NewNews("bai-rac", "ChuyenMucKhongTonTai"));

        Assert.Equal(ContentSection.TinTuc, news.Section);
    }

    [Fact]
    public async Task UpdateAsync_GiuNguyenTruongRiengCuaHocPhi()
    {
        var news = await CreateService().CreateAsync(new News
        {
            Title = "van-ban",
            Slug = "van-ban",
            Content = "<p>Nội dung</p>",
            Section = ContentSection.HocPhi,
            DocumentNumber = "1234/TB-ĐHCN-VHD",
            IssuedDate = new DateTime(2026, 6, 1),
            AcademicYear = "2026 - 2027",
            AttachmentUrl = "/uploads/tuition/x.pdf",
            AttachmentName = "van-ban.pdf"
        });

        news.Title = "Văn bản (sửa)";
        await CreateService().UpdateAsync(news);

        var saved = await Db.News.AsNoTracking().FirstAsync(n => n.Id == news.Id);
        Assert.Equal("1234/TB-ĐHCN-VHD", saved.DocumentNumber);
        Assert.Equal(new DateTime(2026, 6, 1), saved.IssuedDate);
        Assert.Equal("2026 - 2027", saved.AcademicYear);
        Assert.Equal("/uploads/tuition/x.pdf", saved.AttachmentUrl);
        Assert.Equal("van-ban.pdf", saved.AttachmentName);
    }

    [Fact]
    public async Task UpdateAsync_KhongGhiDeCategoryIdCuaBanKhac()
    {
        // CategoryId đã nullable. Nếu UpdateAsync gán 0 thay vì null thì lưu sẽ
        // vi phạm khoá ngoại và trả 500 — lỗi này đã từng xảy ra.
        var news = await CreateService().CreateAsync(
            NewNews("khong-chuyen-muc", ContentSection.HocPhi));

        Assert.Null(news.CategoryId);

        news.Title = "Vẫn không chuyên mục";
        await CreateService().UpdateAsync(news);

        var saved = await Db.News.AsNoTracking().FirstAsync(n => n.Id == news.Id);
        Assert.Null(saved.CategoryId);
    }

    [Fact]
    public void ContentSection_Normalise_TuChuyenMucKhongTonTai()
    {
        Assert.Equal(ContentSection.TinTuc, ContentSection.Normalise(null));
        Assert.Equal(ContentSection.TinTuc, ContentSection.Normalise(""));
        Assert.Equal(ContentSection.TinTuc, ContentSection.Normalise("rac"));
        Assert.Equal(ContentSection.HocPhi, ContentSection.Normalise(ContentSection.HocPhi));
    }

    [Fact]
    public void ContentSection_Label_DungTenHienThi()
    {
        Assert.Equal("Tin tức", ContentSection.Label(ContentSection.TinTuc));
        Assert.Equal("Nghiên cứu khoa học", ContentSection.Label(ContentSection.NghiênCứu));
        Assert.Equal("Hợp tác", ContentSection.Label(ContentSection.HopTac));
        Assert.Equal("Học phí", ContentSection.Label(ContentSection.HocPhi));
        Assert.Equal("Tin tức", ContentSection.Label("khong-co"));
    }
}