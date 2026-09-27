using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VIUUniversity.Data;
using VIUUniversity.Models;
using VIUUniversity.Services;
using VIUUniversity.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// ---- Database ----
var connString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=VIUUniversity.db";
if (connString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase) || connString.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(connString));
}
else
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connString));
}

// ---- Identity (Authentication + Authorization) ----
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/tai-khoan/dang-nhap";
    options.AccessDeniedPath = "/tai-khoan/khong-co-quyen";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
});

// ---- Application services (Service Layer, all injected via interfaces) ----
builder.Services.AddScoped<INewsService, NewsService>();
builder.Services.AddScoped<IAnnouncementService, AnnouncementService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IHomeService, HomeService>();
builder.Services.AddScoped<IAcademicService, AcademicService>();
builder.Services.AddScoped<IAdmissionService, AdmissionService>();

// ---- MVC + Areas (Admin CMS) ----
builder.Services.AddControllersWithViews();

var app = builder.Build();

// ---- Seed database on startup (roles, admin user, sample content) ----
using (var scope = app.Services.CreateScope())
{
    await SeedData.InitializeAsync(scope.ServiceProvider, app.Configuration);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Admin area must be mapped before the default route.
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
