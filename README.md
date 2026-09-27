# Cổng thông tin điện tử VIU (Trường Đại học Công nghiệp Việt - Hung)

Hệ thống ASP.NET Core MVC + Entity Framework Core + SQL Server + Identity.
**Phase 1-3 đã hoàn thành**: nền tảng, Database, Authentication/Authorization, Trang chủ, Tin tức, Thông báo, Sự kiện (cả Public site và Admin CMS).

---

## 1. Yêu cầu cài đặt trước

Cài đặt các phần mềm sau (theo đúng thứ tự):

### 1.1. .NET 8 SDK
Tải và cài tại: https://dotnet.microsoft.com/download/dotnet/8.0

Kiểm tra sau khi cài:
```bash
dotnet --version
# Kết quả mong đợi: 8.0.x
```

### 1.2. SQL Server
Chọn 1 trong 2 cách:

**Cách A - Đơn giản nhất (khuyến nghị cho máy cá nhân):**
Khi cài Visual Studio 2022 (bản Community miễn phí), tick chọn workload
".NET desktop development" hoặc "ASP.NET and web development" — SQL Server LocalDB
sẽ được cài kèm tự động. Không cần làm gì thêm.

**Cách B - Cài SQL Server riêng:**
Tải SQL Server Express (miễn phí): https://www.microsoft.com/sql-server/sql-server-downloads
Cài thêm SQL Server Management Studio (SSMS) để xem database bằng giao diện:
https://learn.microsoft.com/sql/ssms/download-sql-server-management-studio-ssms

### 1.3. EF Core CLI Tools
```bash
dotnet tool install --global dotnet-ef
```
Kiểm tra:
```bash
dotnet ef --version
```

### 1.4. (Tùy chọn) Visual Studio 2022 hoặc VS Code
- Visual Studio 2022 Community: https://visualstudio.microsoft.com/vs/community/
- Hoặc VS Code + extension C# Dev Kit: https://code.visualstudio.com/

---

## 2. Cấu hình Connection String

Mở file `appsettings.json`, kiểm tra chuỗi kết nối phù hợp với máy bạn:

**Nếu dùng LocalDB (Cách A ở trên) — mặc định đã đúng, không cần sửa:**
```json
"DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=VIUUniversityDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
```

**Nếu dùng SQL Server Express (Cách B):**
```json
"DefaultConnection": "Server=.\\SQLEXPRESS;Database=VIUUniversityDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
```

**Nếu dùng SQL Server với tài khoản sa/mật khẩu (ví dụ chạy trong Docker):**
```json
"DefaultConnection": "Server=localhost,1433;Database=VIUUniversityDB;User Id=sa;Password=YourPassword123!;TrustServerCertificate=True"
```

---

## 3. Khôi phục package, tạo Migration và Database

Mở terminal tại thư mục gốc của project (nơi có file `VIUUniversity.csproj`), chạy lần lượt:

```bash
# Bước 1: Tải các package NuGet
dotnet restore

# Bước 2: Build thử để chắc chắn code không lỗi cú pháp
dotnet build

# Bước 3: Tạo migration đầu tiên (sinh ra file định nghĩa database từ Models)
dotnet ef migrations add InitialCreate

# Bước 4: Áp dụng migration - EF Core sẽ TỰ TẠO database VIUUniversityDB trên SQL Server
dotnet ef database update
```

Nếu bước 3 báo lỗi "No executable found matching command dotnet-ef", nghĩa là bạn quên bước 1.3 ở trên (cài `dotnet-ef` global tool) — chạy lại lệnh đó rồi thử lại.

Sau bước 4, mở SSMS (hoặc Server Explorer trong Visual Studio), kết nối tới SQL Server của bạn, bạn sẽ thấy database **VIUUniversityDB** với đầy đủ các bảng: `AspNetUsers`, `AspNetRoles`, `Departments`, `Majors`, `Subjects`, `Lecturers`, `News`, `NewsCategories`, `Announcements`, `Events`, `Campuses`, `Partners`, `Banners`, `AuditLogs`...

---

## 4. Chạy ứng dụng

```bash
dotnet run
```

Terminal sẽ hiện dòng tương tự:
```
Now listening on: https://localhost:5001
Now listening on: http://localhost:5000
```

Mở trình duyệt vào địa chỉ đó. Lần chạy đầu tiên, hệ thống sẽ **tự động seed dữ liệu mẫu**:
- 5 roles: Admin, Editor, Lecturer, Student, Guest
- 1 tài khoản Admin
- 3 khoa, 4 ngành đào tạo mẫu
- Tin tức, thông báo, sự kiện mẫu
- 3 cơ sở đào tạo thực tế của VIU (Tùng Thiện, Tây Phương, Thanh Xuân)

---

## 5. Tài khoản đăng nhập mặc định

| Vai trò | Email | Mật khẩu |
|---|---|---|
| Admin | admin@viu.edu.vn | Admin@123456 |

Đổi email/mật khẩu mặc định trong mục `SeedAdmin` của `appsettings.json` **trước lần chạy đầu tiên** nếu muốn.

Truy cập trang quản trị tại: `https://localhost:5001/admin`

---

## 6. Cấu trúc thư mục

```
VIUUniversity/
├── Controllers/          → Controller cho website công khai (Home, News, Announcements, Events, Account)
├── Areas/Admin/          → Toàn bộ khu vực quản trị (CMS)
│   ├── Controllers/
│   └── Views/
├── Models/                → Entity Framework entities (bảng database)
├── ViewModels/            → Dữ liệu truyền riêng cho View, tách khỏi Entity
├── Data/
│   ├── ApplicationDbContext.cs  → Khai báo DbSet + cấu hình quan hệ
│   └── SeedData.cs              → Dữ liệu khởi tạo
├── Services/              → Toàn bộ business logic + truy vấn EF Core (Service Layer)
│   └── Interfaces/
├── Views/                 → Razor views cho website công khai
├── wwwroot/               → CSS, JS, ảnh, file upload
├── Migrations/            → Sinh ra sau khi chạy `dotnet ef migrations add`
└── Program.cs             → Cấu hình DI, Identity, routing
```

Luồng dữ liệu tuân thủ đúng kiến trúc:
```
SQL Server → EF Core (DbContext) → Service → Controller → ViewModel → View
```

---

## 7. Những gì đã có (Phase 1-3)

- ✅ Database Model đầy đủ quan hệ (FK, unique index, soft delete, audit fields)
- ✅ ASP.NET Core Identity: đăng ký / đăng nhập / đăng xuất, 5 roles
- ✅ Phân quyền Admin/Editor cho khu vực quản trị
- ✅ Trang chủ lấy toàn bộ dữ liệu từ database (banner, ngành học, tin tức, thông báo, sự kiện, đối tác, cơ sở)
- ✅ Module Tin tức: public (list + search + pagination + chi tiết + đếm view) và admin (CRUD + publish/unpublish)
- ✅ Module Thông báo: public + admin CRUD, đánh dấu "quan trọng"
- ✅ Module Sự kiện: public + admin CRUD
- ✅ Quản lý Banner trang chủ (CRUD + bật/tắt)
- ✅ Admin Dashboard với thống kê realtime + biểu đồ tin theo tháng
- ✅ Giao diện responsive, thương hiệu riêng VIU (navy + gold), không sao chép HaUI

## 8. Lộ trình tiếp theo (Phase 4-10)

Theo đúng roadmap ban đầu, các phase sau sẽ được bổ sung ở các lượt phát triển kế tiếp:

- **Phase 4**: Departments/Majors/Subjects/Lecturers — trang public chi tiết từng khoa/ngành/giảng viên
- **Phase 5**: Module Tuyển sinh (AdmissionMethods, AdmissionScores, công cụ tra cứu điểm chuẩn)
- **Phase 6**: Student Portal (điểm, thời khóa biểu, học phí) + Lecturer Portal (nhập điểm, lớp phụ trách)
- **Phase 7**: Mở rộng Admin CRUD cho Departments/Majors/Students/Lecturers
- **Phase 8**: Thư viện, Nghiên cứu khoa học, Đối tác, Gallery
- **Phase 9**: REST API công khai, Global Search, Chatbot cơ bản
- **Phase 10**: Testing, bảo mật nâng cao, SEO, tối ưu hiệu năng, hướng dẫn deploy production

---

## 9. Xử lý lỗi thường gặp

**Lỗi "Cannot open database VIUUniversityDB"**
→ Bạn chưa chạy `dotnet ef database update`, hoặc connection string sai tên server.

**Lỗi "A network-related or instance-specific error..."**
→ SQL Server chưa chạy, hoặc sai tên instance trong connection string. Mở SQL Server Configuration Manager kiểm tra dịch vụ đang chạy.

**Lỗi "Certificate error" khi chạy `dotnet run`**
→ Chạy `dotnet dev-certs https --trust` một lần rồi thử lại.

**Muốn xóa hết dữ liệu và seed lại từ đầu**
→ Chạy `dotnet ef database drop` rồi `dotnet ef database update` lại.
