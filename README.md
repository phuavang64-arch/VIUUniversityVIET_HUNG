# Cổng thông tin điện tử VIU (Trường Đại học Công nghiệp Việt - Hung)

Hệ thống ASP.NET Core MVC + Entity Framework Core + SQLite + ASP.NET Core Identity.

---

## 1. Yêu cầu cài đặt

| Phần mềm | Phiên bản | Ghi chú |
|---|---|---|
| .NET SDK | **10.0 trở lên** | Kiểm tra: `dotnet --version` |
| EF Core CLI | 8.0.31 | `dotnet tool install --global dotnet-ef --version 8.0.31` |

Không cần cài SQL Server. Mặc định ứng dụng dùng **SQLite** với file `VIUUniversity.db`
đặt cạnh project.

> Lưu ý: dự án nhắm `net10.0` nhưng dùng thư viện EF Core 8.0.31 (bản patch mới nhất của
> nhánh 8). Nâng lên .NET 8.x sẽ không chạy được vì `TargetFramework` là `net10.0`.

---

## 2. Cấu hình

### 2.1. Tài khoản quản trị

Mật khẩu **không** còn nằm trong `appsettings.json`. Đặt qua user-secrets:

```bash
dotnet user-secrets set "SeedAdmin:Email" "admin@viu.edu.vn"
dotnet user-secrets set "SeedAdmin:Password" "MatKhau@MatKhau1"
dotnet user-secrets set "SeedAdmin:FullName" "Quản trị hệ thống"
```

- Lần chạy đầu tiên, tài khoản admin sẽ được tạo từ các giá trị này.
- Nếu **không** đặt mật khẩu:
  - ở `Development`: hệ thống tự sinh một mật khẩu ngẫu nhiên và in ra terminal (mỗi lần
    tạo mới một lần, không bao giờ commit vào repo).
  - ở môi trường thật: ứng dụng **từ chối khởi động** cho tới khi bạn cấu hình.
- Chính sách mật khẩu: tối thiểu 10 ký tự, cần chữ hoa, chữ thường, số và ký tự đặc biệt.
- Khoá tài khoản sau 5 lần sai trong 10 phút.

### 2.2. Đổi cơ sở dữ liệu (tuỳ chọn)

Trong `appsettings.json`, `ConnectionStrings:DefaultConnection`:

```json
"Data Source=VIUUniversity.db"     // SQLite (mặc định)
"Server=localhost,1433;Database=VIUUniversityDB;User Id=sa;Password=...;TrustServerCertificate=True"   // SQL Server
```

Ứng dụng tự chọn provider dựa trên chuỗi kết nối (có `Data Source=` / đuôi `.db` → SQLite,
ngược lại → SQL Server).

---

## 3. Chạy ứng dụng

```bash
dotnet restore
dotnet run
```

Mở trình duyệt tại địa chỉ in ra terminal (mặc định `http://localhost:5000`).

Lần chạy đầu tiên hệ thống sẽ:

1. Áp dụng các **EF Core migration** (tạo database nếu chưa có).
2. Seed dữ liệu mẫu: 5 roles, tài khoản admin, 3 khoa, 7 ngành, môn học, giảng viên,
   tin tức, thông báo, sự kiện, banner, cơ sở, thư viện ảnh, phương thức & điểm chuẩn tuyển sinh.
3. Seed các **cài đặt website** (hotline, email, địa chỉ, các con số thống kê).

Trang quản trị: `/admin`.

### Chạy HTTPS ở local

```bash
dotnet dev-certs https --trust
```

Cookie đăng nhập chỉ được đánh dấu `Secure` ở môi trường Production. Ở Development nó dùng
`SameAsRequest` để đăng nhập vẫn hoạt động khi bạn chạy plain HTTP.

---

## 4. Cơ sở dữ liệu & migration

Schema được quản lý bằng **EF Core migrations** thật, nằm trong `Migrations/`.

```bash
# Thêm migration sau khi sửa Models
dotnet ef migrations add TenMigration
dotnet ef database update
```

`DatabaseInitializer` chạy tự động khi khởi động ứng dụng: áp migration còn thiếu rồi seed.

### Cơ sở dữ liệu tạo từ trước khi có migration

Database cũ được tạo bằng `EnsureCreated()` nên không có bảng `__EFMigrationsHistory`.
`DatabaseInitializer` nhận ra điều này và **đánh dấu migration baseline đã chạy**, rồi chỉ áp
migration mới từ đó trở đi — **không mất dữ liệu**.

### Xoá sạch và seed lại từ đầu

```bash
rm VIUUniversity.db        # macOS/Linux
# hoặc: dotnet ef database drop --force
dotnet run
```

---

## 5. Kiểm thử

```bash
dotnet test
```

57 test bao phủ: sanitizer XSS, slug trùng, view count, soft delete, tìm kiếm không phân biệt
hoa thường, kiểm tra magic-byte khi upload, thứ tự biểu đồ tháng, slug tiếng Việt, và
sitemap (loại nội dung nào được phép xuất hiện trong sitemap.xml).

> Cần mở đúng solution. Trước đây thư mục gốc không có file solution, nên `dotnet test`
> chạy từ thư mục gốc **không tìm thấy project kiểm thử và kết thúc im lặng** — dễ
> tưởng là đã pass. Nay đã thêm `VIUUniversity.slnx` (định dạng solution XML mặc định
> của .NET 10), nên `dotnet build` và `dotnet test` chạy đúng từ thư mục gốc.
>
> Nếu dùng SDK cũ chưa hiểu `.slnx`, tạo bản `.sln` cũ:
> `dotnet new sln -n VIUUniversity --format sln`

---

## 6. Cấu trúc thư mục

```
VIUUniversity/
├── Controllers/          → Website công khai (Home, News, Events, Admissions, Account, Sitemap, Ai)
├── Areas/Admin/          → CMS quản trị
│   ├── Controllers/
│   └── Views/
├── Models/               → EF Core entities
├── ViewModels/           → Dữ liệu truyền riêng cho View
├── Data/                 → DbContext, migration initializer, SeedData
│   └── Migrations/       → EF Core migrations
├── Services/             → Business logic + truy vấn EF Core
│   └── Interfaces/
├── Views/                → Razor views
├── tools/                → script import ảnh (import-images.py)
├── wwwroot/              → css, js, ảnh, upload
├── Program.cs            → DI, Identity, rate limiting, authorization
├── VIUUniversity.slnx
└── VIUUniversity.Tests/  → Unit tests (xUnit)
```

Luồng dữ liệu: `Database → DbContext → Service → Controller → ViewModel → View`

---

## 7. Bảo mật

Đã áp dụng:

| Biện pháp | Chi tiết |
|---|---|
| **Fallback authorization** | Mọi controller mặc định yêu cầu đăng nhập; controller công khai phải ghi rõ `[AllowAnonymous]`. |
| **Chống XSS khi ghi** | `ContentSanitizer` khử `<script>`, event handler, `javascript:` URL và iframe không tin cậy **trước khi lưu**. Đây là điều kiện để `@Html.Raw` trong view chi tiết là an toàn. |
| **XSS phía client** | Ô tìm kiếm và chatbot dựng DOM bằng `textContent` / API DOM, không nội suy chuỗi vào `innerHTML`. Link chỉ chấp nhận đường dẫn nội bộ. |
| **Rate limiting** | Form công khai 5 req/phút/IP, chatbot 20 req/phút/IP, toàn site 120 req/phút/IP. Trả `429` khi vượt. |
| **Upload** | Kiểm phần mở rộng, dung lượng tối đa 5 MB và **magic bytes** (chặn file HTML/SVG đổi tên `.jpg`). Tên file sinh từ `Guid`. |
| **Upload PDF** | `DocumentUploadHelper` chỉ nhận PDF thật (magic byte `%PDF-`), tối đa 20 MB. Cần thiết cho văn bản học phí: tệp HTML đổi tên `.pdf` sẽ được phục vụ ngay trên origin của site và chạy script trong trình duyệt người đọc. |
| **CSRF** | `[ValidateAntiForgeryToken]` trên mọi POST. |
| **Slug trùng** | Tự thêm hậu tố `-2`, `-3` thay vì để vi phạm UNIQUE index gây lỗi 500. |
| **Cookie** | `HttpOnly`, `SameSite=Lax`, `Secure` ở Production. |

### Trước khi đưa lên production

- [ ] Đặt `SeedAdmin:*` qua biến môi trường hoặc secret manager.
- [ ] Đổi `AllowedHosts` từ `*` sang tên miền thật.
- [ ] Đặt connection string qua biến môi trường, không commit.
- [ ] Bật HTTPS + HSTS (đã tự bật khi `ASPNETCORE_ENVIRONMENT=Production`).
- [ ] Rà lại toàn bộ nội dung trong **Admin → Cài đặt website**: các ô trống sẽ không
      hiển thị. Đây là chỗ chứa các con số quảng bá (số giảng viên, tỷ lệ việc làm, quỹ học
      bổng...) — **chỉ điền số liệu đã được xác minh.**

---

## 8. Bảng màu & hệ thiết kế

Toàn bộ token nằm ở đầu `wwwroot/css/site.css` (`:root`). Dưới đây là các token
**thực sự đang dùng**; nhóm `--viu-primary`, `--viu-link`, `--viu-blue`… chỉ là
tên cũ còn lại cho các view inline cũ, đều trỏ về token thật.

### 8.1. Chỉ ba màu: xanh `#00A6D6`, đen, trắng

Yêu cầu thiết kế: **màu xanh phải đúng màu của logo trường (`#00A6D6`), còn lại là
đen và trắng.** Không dùng màu khác — không navy, không xám làm chữ, không vàng,
không đỏ. Phân cấp thị giác bằng **cỡ chữ và độ đậm**, không bằng màu.

| Token | Mã | Vai trò |
|---|---|---|
| `--viu-navy` | `#00A6D6` | tiêu đề, link, nút, đường kẽ, nhãn, icon — **màu xanh duy nhất** |
| `--viu-navy-hover` | `#008fc0` | trạng thái hover |
| `--viu-navy-soft` | `#e6f6fc` | nền xanh rất nhạt (badge, hover) |
| `--viu-navy-line` | `#b3e5f5` | đường kẽ xanh nhạt |
| `--ink` / `--ink-body` / `--ink-muted` / `--ink-faint` | `#000000` | toàn bộ chữ |
| `--surface` | `#ffffff` | nền |
| `--surface-alt` | `#f5f5f5` | dải xen kẽ |

**Lưu ý về độ tương phản:** `#00A6D6` trên nền trắng đạt **2.58:1**, dưới ngưỡng
WCAG AA (3:1) cho chữ lớn. Vì vậy `#00A6D6` chỉ dùng cho tiêu đề, link, nút và đường
kẽ — những thứ người đọc vẫn nhận ra được nhờ hình dạng, không nhờ màu. Mọi chữ đều
đen nên không có chữ nào mất khả năng đọc. Chữ đặt **trên** nền xanh (nút, nhãn, đầu
bảng) dùng màu **đen** — đen trên `#00A6D6` đạt 8.6:1, trong khi trắng chỉ 2.58:1.

Khu vực tối (thanh tiện ích trên cùng, đầu trang con, chân trang, sidebar quản trị)
dùng nền **đen** với chữ **trắng** — vừa đạt tương phản, vừa phân biệt rõ khu vực
quản trị với website công khai.

### 8.2. Mega menu có ảnh

Menu **Giới thiệu** và **Tuyển sinh** là mega menu 2 cột, mỗi mục gắn một ảnh thật
của nhà trường (từ `wwwroot/images/viu/`) kèm mô tả ngắn. Lý do: các nhóm này có
4–5 mục, chỉ thấy dòng chữ thì người đọc phải đọc từng dòng để biết nội dung; ảnh giúp
nhận ra ngay và trông hiện đại hơn một danh sách rỗng.

## 9. Ảnh

### 9.1. Nguyên tắc: chỉ dùng ảnh chụp thật của nhà trường

> **Không được dùng ảnh của cơ sở khác.** Ảnh chụp từ trường khác nhìn rất giống
> nhưng là thông tin sai — người đọc sẽ tưởng đó là cơ sở của VIU. Ảnh dùng trên
> site phải là ảnh chụp thật của Trường Đại học Công nghiệp Việt - Hung.

Nguồn ảnh gốc nằm ở thư mục `~/Documents` của máy làm việc (khoảng 36 tệp, phần
lớn do nhà trường cung cấp). Ảnh nào còn dùng được thì giữ lại `~/Documents`; **không
di chuyển, không xoá tệp nguồn** — mỗi khi dàn trang mới sẽ lấy lại từ đó.

### 9.2. Quy ước tệp

`Html.Img()` dựng `<picture>` trỏ tới **hai bản WebP** đặt cạnh bản gốc:

```
wwwroot/images/<nhóm>/<tên>.jpg        bản gốc, làm <img src> dự phòng
wwwroot/images/<nhóm>/<tên>.webp       rộng tối đa 1600px
wwwroot/images/<nhóm>/<tên>@sm.webp     rộng 640px, cho điện thoại
```

Thiếu file `.webp` thì helper âm thầm bỏ `<picture>` và chỉ xuất `<img>` — thêm
ảnh mới mà quên tạo bản WebP vẫn chạy, chỉ nặng hơn một chút.

### 9.3. Các nhóm ảnh

| Nhóm | Nội dung | Nguồn |
|---|---|---|
| `images/viu/` | 30 ảnh trang trí: cơ sở, sự kiện, sinh viên, khoa | import từ `~/Documents` |
| `images/site/` | 5 ảnh nền cơ sở (toà nhà, sân, cổng) | ảnh sẵn có của dự án |
| `images/real/` | 13 ảnh do Admin/Banner/SeedData trỏ tới | ảnh sẵn có của dự án |
| `images/departments/` | ảnh đại diện khoa | ảnh sẵn có của dự án |
| `images/majors/` | ảnh đại diện 21 ngành K50 | cổng tuyển sinh chính thức (`media.viu.edu.vn`) |

Ảnh khoa và tin tức lấy từ database (`Department.Image`, `News.Thumbnail`…) nên
đường dẫn nằm trong `SeedData.cs`, không khai báo trong view.

### 9.4. Ảnh đại diện 21 ngành

Mỗi ngành có một ảnh riêng, lấy từ **chính trang giới thiệu ngành đó** trên cổng tuyển
sinh (máy chủ `media.viu.edu.vn` của Trường) — không lấy ảnh trường khác, không sinh ảnh
AI, không dùng chung một ảnh cho nhiều ngành.

```bash
python3 tools/import-majors.py     # scrape ảnh + nội dung 21 ngành
python3 tools/import-majors.py --gen   # sinh lại Data/MajorCatalogK50.cs
```

Script chỉ đọc trang `tuyensinh.viu.edu.vn`, tải ảnh về `wwwroot/images/majors/` (sinh
cả `.webp` 1600px và `@sm.webp` 640px) và ghi lại URL nguồn vào `Major.SourceUrl` để
còn tra ngược. Nếu một ngành không tìm được ảnh, script báo rõ mã ngành đó thay vì
lấy ảnh khác thay thế.

### 9.5. Thêm ảnh mới

Đặt ảnh gốc vào `~/Documents`, sau đó chạy lại script import với cặp
`tên đích → tệp nguồn` tương ứng:

```bash
pip install Pillow
python3 tools/import-images.py
```

Script đặt lại tên theo nội dung, resize về rộng tối đa 1600px và sinh hai bản
WebP. Script chỉ **đọc** `~/Documents`, không di chuyển hay xoá tệp nguồn.
Bỏ qua mấy tệp dạng poster chữ hoặc giấy tờ trong `~/Documents`
(`305-tb-bgd-*.jpg`, `aac16a91-*.jpeg`) — chúng không phải ảnh cơ sở.

### 9.6. Thành phần ảnh trong CSS

| Lớp | Dùng ở đâu |
|---|---|
| `.viu-hero__media` | ảnh tràn ngang đầu trang chủ |
| `.viu-hero__facts` | dải thông tin ngắn dưới nút ở hero |
| `.viu-pagehead--media` | đầu trang con; ảnh truyền qua `--pagehead-photo: url(...)` |
| `.viu-photo-band` | dải ảnh giữa hai section, có lớp phủ navy + chú thích |
| `.viu-photo-grid` | lưới ảnh 4 cột; `--3` = 3 cột; `--wide` / `--tall` dựng mosaic |
| `.viu-photo-grid figcaption` | nhãn trắng đè lên đáy ảnh |
| `.viu-feature-card` | thẻ "Thế mạnh chương trình": ảnh trên, chữ dưới |
| `.viu-media-split`, `.viu-media-collage` | ảnh bên cạnh khối chữ, kiểu collage |
| `.viu-sidebar-photo` | ảnh nhỏ trong thanh sidebar |

Lưới ảnh dùng `grid-auto-rows` **cố định** thay vì để `aspect-ratio` quyết định
chiều cao ô. Nếu không, ô cao 2 hàng sẽ kéo dài hàng chứa nó và các ô cạnh bên
rơi lệch nhịp — nhìn rõ ở khối mosaic.

---


## 10. Tính năng hiện có

**Website công khai**
- Trang chủ tổng hợp từ database (banner, ngành học, tin tức, thông báo, sự kiện, đối tác, cơ sở, thư viện ảnh)
- Tin tức / Thông báo / Sự kiện: danh sách, tìm kiếm, phân trang, chi tiết, đếm lượt xem
- **Bốn chuyên mục bài viết** dùng chung bảng `News`, phân biệt bằng `News.Section`:
  Tin tức (`/tin-tuc`), Nghiên cứu khoa học (`/nghien-cuu`), Hợp tác quốc tế và doanh
  nghiệp (`/hop-tac`), Học phí (`/hoc-phi`). Cùng một bộ CRUD trong `/admin`, cùng
  phân trang/tìm kiếm/đếm lượt đọc — xem [mục 10.1](#101-bốn-chuyên-mục-bài-viết)
- **Học phí không phải bảng giá cố định**: mỗi bài là một văn bản/thông báo do Ban
  Tuyển sinh đăng, kèm số ký hiệu, ngày ban hành, năm học áp dụng và tệp PDF đính
  kèm. Khi có văn bản mới, trang cập nhật ngay mà không phải sửa mã nguồn
- **21 ngành đại học chính quy khóa 50** trích từ cổng tuyển sinh chính thức
  (`tuyensinh.viu.edu.vn/21nganhk50.html`), mỗi ngành có ảnh đại diện riêng lấy từ
  chính trang giới thiệu ngành đó, kèm giới thiệu / kiến thức & kỹ năng / cơ hội nghề
  nghiệp / thời gian đào tạo. Học phí, điểm chuẩn, chỉ tiêu **cố tình để trống** — xem
  [mục 10.2](#102-21-ngành-đại-học-chính-quy-khóa-50)
- Đào tạo: khoa, ngành, khung môn học, đội ngũ giảng viên (lọc theo khoa)
- Tuyển sinh: phương thức xét tuyển, tra cứu điểm chuẩn theo năm, công cụ tính điểm & gợi ý ngành
- Tìm kiếm toàn site (tin tức, thông báo, sự kiện, ngành học)
- Ảnh phủ ở đầu mọi trang con, dải ảnh toàn ngang, lưới ảnh mosaic ở các trang
  nội dung tĩnh — **chỉ dùng ảnh chụp thật của nhà trường**, xem [mục 9](#9-ảnh)
- `sitemap.xml` sinh động từ database + `robots.txt` trỏ đúng sitemap của host đang chạy
- Trang 404 riêng, trả **mã 404 thật** (không phải chuyển hướng sang trang đăng nhập)
- Trợ lý Tuyển sinh (tra cứu dữ liệu + tùy chọn mô hình AI) + form đăng ký tư vấn + form liên hệ

### 10.1. Bốn chuyên mục bài viết

Tin tức, Nghiên cứu khoa học, Hợp tác và Học phí **dùng chung một bảng `News`** và chỉ
khác nhau ở cột `News.Section` (`ContentSection`). Lý do: tách thành bốn bảng sẽ phải
nhân đôn bốn lần controller / service / view / quyền, trong khi phần lớn hành vi (phân
trang, tìm kiếm, nháp/đăng, đếm lượt đọc, xem trước) giống hệt nhau.

| Chuyên mục | Đường dẫn | Quản trị | Ghi chú |
|---|---|---|---|
| Tin tức | `/tin-tuc` | `/admin/news` | Đã có từ trước |
| Nghiên cứu khoa học | `/nghien-cuu` | `/admin/nghien-cuu` | mới |
| Hợp tác quốc tế và doanh nghiệp | `/hop-tac` | `/admin/hop-tac` | mới; nội dung giới thiệu cũ giữ nguyên |
| Học phí | `/hoc-phi` | `/admin/hoc-phi` | mới; có thêm trường riêng |

Trường riêng của chuyên mục Học phí (đều nullable, các chuyên mục khác không dùng):

- `DocumentNumber` — số / ký hiệu văn bản
- `IssuedDate` — ngày ban hành (khác với ngày đăng lên web)
- `AcademicYear` — năm học / khóa áp dụng
- `AttachmentUrl` / `AttachmentName` — tệp PDF đính kèm

`DocumentUploadHelper` chỉ nhận PDF thật: kiểm tra magic byte `%PDF-` trước khi ghi đĩa
(chặn tệp HTML/JS đổi tên `.pdf` — tệp đó sẽ được phục vụ ngay trên origin của site),
giới hạn 20 MB.

### 10.2. 21 ngành đại học chính quy khóa 50

Danh mục nằm ở `Data/MajorCatalogK50.cs` (sinh tự động bởi `tools/import-majors.py`
từ dữ liệu scrape của cổng tuyển sinh chính thức). Mã ngành, tên ngành, ảnh đại diện
và nội dung đều lấy từ `tuyensinh.viu.edu.vn` — không tự đặt.

**Cố tình để trống:** học phí, điểm chuẩn, chỉ tiêu. Các số này đổi theo từng năm tuyển
sinh; hiển thị một con số cố định trên trang ngành sẽ bị đọc là thông tin chính thức của
Trường. Muốn biết mức học phí, người đọc xem chuyên mục [Học phí](#101-bốn-chuyên-mục-bài-viết).

**Khoa quản lý:** cổng tuyển sinh không công bố, nên danh mục phân nhóm theo tính chất
ngành vào 3 khoa đang có (`CNTT` / `DIEN` / `KT`). Sửa trong `MajorCatalogK50` khi
Trường công bố.

**Dữ liệu cũ:** ngành `7220201 Ngôn ngữ Anh` có trong cơ sở dữ liệu từ bản seed mẫu
nhưng không có trong danh sách K50. Không xoá (để không mất dữ liệu đã nhập), nhưng
trang `/dao-tao` ghi rõ nó nằm ngoài danh sách chính thức. Mã `7480103 Kỹ thuật phần
mềm` (cũng không có trong K50) đã được sửa tại chỗ thành `7480101 Khoa học máy tính`
cùng migration, kèm theo `AdmissionMajors` tham chiếu tới nó.

### Xử lý lỗi HTTP

`Program.cs` sửa một lỗi dễ bị bỏ qua: **fallback authorization policy sẽ biến URL hỏng
thành trang đăng nhập.** Trước khi sửa, mọi URL không khớp route trả `302` về
`/tai-khoan/dang-nhp`, và `GET` lên một form chỉ nhận `POST` cũng vậy. Ba nguyên nhân
tách biệt, đã xử lý trước `UseAuthorization`:

| Tình huống | Trước | Nay |
|---|---|---|
| URL không khớp route (`/khong-co-trang`) | `302` → đăng nhập | `404` + trang 404 có header/footer |
| Sai HTTP method (`GET /tuyen-sinh/dang-ky`) | `302` → đăng nhập | `405` |
| `HEAD` lên trang `GET` | `302` → đăng nhập | `200` (Kestrel tự bỏ body) |

`HEAD` được xử lý bằng cách đổi method sang `GET` **trước** `UseRouting`, vì MVC chỉ
khai báo route `[HttpGet]`. Routing thay thế endpoint bằng một stub không metadata khi
sai method; stub đó không mang `[AllowAnonymous]` nên bị fallback policy challenge — đó
chính là lý do phải chặn trước `UseAuthorization`.

---

### Về `sitemap.xml` và `robots.txt`

Cả hai **sinh động từ database**, không phải file viết tay trong `wwwroot`:

- `SitemapService` chỉ liệt kê tin tức / thông báo / sự kiện đã publish và chưa bị xoá
  mềm, cộng khoa và ngành. Bản nháp không bao giờ lọt vào sitemap.
- `robots.txt` gắn đường dẫn sitemap theo host đang phục vụ. Nếu để là file tĩnh trong
  `wwwroot`, dòng `Sitemap:` sẽ khóa cứng `localhost` và âm thầm bảo crawler rằng
  sitemap nằm trên máy của lập trình viên.

**Admin CMS**
- Đăng nhập / đăng xuất, phân quyền `Admin` và `Editor`
- CRUD Tin tức, Thông báo, Sự kiện, Banner, Thư viện ảnh (upload nhiều ảnh)
- Quản lý hồ sơ thí sinh & tin nhắn liên hệ (đánh dấu đã xử lý / đã đọc)
- Dashboard thống kê + biểu đồ theo tháng
- **Cài đặt website**: sửa hotline, email, địa chỉ và các con số chatbot dùng

---

## 11. Trợ lý Tuyển sinh (AI)

Trợ lý chat ở góc phải màn hình hoạt động theo hai tầng:

1. **Tra cứu dữ liệu** — luôn bật. Trợ lý tự động lấy số liệu thật từ database
   (điểm chuẩn, học phí, ngành/khoa, phương thức xét tuyển, cơ sở, tin tức) và ghi rõ
   nguồn của câu trả lời.
2. **Mô hình ngôn ngữ** — tùy chọn. Nếu cấu hình API key, trợ lý còn diễn giải câu hỏi
   tự do bằng mô hình, **chỉ dựa trên dữ liệu được cung cấp** và được lệnh buộc nói rõ
   "chưa có thông tin" thay vì bịa học phí/điểm chuẩn.

Nếu **chưa** cấu hình, giao diện hiển thị *"Chế độ tra cứu dữ liệu"* và trợ lý nói thẳng là
AI chưa được cấu hình — không giả lập câu trả lời mẫu.

### Cấu hình

Key **chỉ** được đặt ở phía máy chủ, không bao giờ nhúng vào JavaScript, HTML hay
commit vào git. Thứ tự đọc: biến môi trường `OPENAI_API_KEY` → `Chat:ApiKey`.

```bash
# ===== OpenAI Responses API (SDK chính thức, khuyến nghị) =====
export OPENAI_API_KEY="sk-..."          # ưu tiên biến môi trường
dotnet user-secrets set "Chat:Provider" "openai"
dotnet user-secrets set "Chat:Model" "gpt-4o-mini"

# ===== Hoặc endpoint tương thích OpenAI (Azure OpenAI, OpenRouter, vLLM...) =====
dotnet user-secrets set "Chat:Provider" "openai-compatible"
dotnet user-secrets set "Chat:ApiKey" "<key của bạn>"
dotnet user-secrets set "Chat:BaseUrl" "https://api.openai.com/v1"

# ===== Hoặc Anthropic =====
dotnet user-secrets set "Chat:Provider" "anthropic"
dotnet user-secrets set "Chat:ApiKey" "<key của bạn>"
dotnet user-secrets set "Chat:BaseUrl" "https://api.anthropic.com/v1"
dotnet user-secrets set "Chat:Model" "claude-sonnet-4-5"

# Cho phép trả lời ngoài phạm vi dữ liệu trường (vẫn được ghi nhãn "không phải
# thông tin chính thức"). Để false = chỉ trả lời dựa trên dữ liệu trường.
dotnet user-secrets set "Chat:AllowGeneralKnowledge" "false"

# Kiểm tra trạng thái sau khi cấu hình
curl -s http://localhost:5000/api/chat/status   # {"configured":true,...}
```

Tương đương bằng biến môi trường (dùng khi triển khai):
`Chat__Provider`, `Chat__ApiKey`, `Chat__BaseUrl`, `Chat__Model`, `Chat__AllowGeneralKnowledge`.

> **Lưu ý về model:** ví dụ trong đề bài dùng `gpt-6-astra` — đây không phải tên model
> hợp lệ của OpenAI. Mặc định dự án đặt `gpt-4o-mini`; ghi đè bằng `Chat__Model`
> nếu tài khoản của bạn được cấp quyền model khác.

> **Lưu ý SDK:** SDK đánh dấu Responses API là *evaluation only* (chẩn đoán
> `OPENAI001`). Chẩn đoán này được tắt trong `.csproj` kèm chú thích; hãy kiểm tra
> lại khi nâng phiên bản `OpenAI` package.

### Hành vi khi thiếu cấu hình

| Tình huống | Kết quả |
|---|---|
| Không có key | `{"configured":false}`; trợ lý vẫn tra cứu dữ liệu thật trong DB và nói rõ AI chưa cấu hình |
| Key sai / hết hạn | Trả lời thân thiện cho người dùng; server log `HTTP 401` **không chứa key** |
| Vượt hạn mức | Log `HTTP 429`; người dùng thấy "tạm thời không phản hồi" |
| Timeout | Giới hạn theo `Chat__TimeoutSeconds` (mặc định 25s) |

### An toàn

- Mô hình **không** được tự chạy SQL. Dữ liệu do `KnowledgeService` truy vấn bằng truy vấn
  EF Core cố định trên các bảng công khai; dữ liệu quản trị (hồ sơ thí sinh, tin nhắn,
  log, tài khoản) không bao giờ đưa vào ngữ cảnh.
- Câu trả lời của mô hình bị `HtmlSanitizer` lọc trước khi trả về trình duyệt.
- Endpoint `POST /api/chat` bị giới hạn 20 req/phút/IP.
- Server log chỉ ghi mã trạng thái lỗi, **không ghi nội dung hội thoại hay API key**.
- Giới hạn độ dài câu hỏi (500 ký tự) và số lượt lịch sử gửi lên mô hình (6 lượt).

---

## 12. Xử lý lỗi thường gặp

**`no such table: SiteSettings` hoặc `table X already exists` khi khởi động**
→ Database cũ đã được "đánh dấu" migration nhưng thiếu bảng mới. Xoá `VIUUniversity.db` và
chạy lại (dữ liệu sẽ được seed lại), hoặc chạy `dotnet ef database update`.

**`Login trả về nhưng không vào được trang Admin`**
→ Đang chạy Production qua HTTP thì cookie `Secure` bị trình duyệt chặn. Dùng HTTPS.

**Lỗi khi đăng ký tài khoản mới**
→ Chính sách mật khẩu nay yêu cầu tối thiểu 10 ký tự kèm hoa/thường/số/ký tự đặc biệt.

**`dotnet ef` báo không tìm thấy lệnh**
→ `dotnet tool install --global dotnet-ef --version 8.0.31`