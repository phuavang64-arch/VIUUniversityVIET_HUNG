using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql;
using VIUUniversity.Data;
using VIUUniversity.Models;
using VIUUniversity.Services;
using VIUUniversity.Services.Chat;
using VIUUniversity.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Ensure user-secrets are loaded even when running without ASPNETCORE_ENVIRONMENT=Development
builder.Configuration.AddUserSecrets<Program>(optional: true);

// ---- Database ----
// MySQL is the database provider for this project (Pomelo). The connection string in
// appsettings.json (or user-secrets) points at a MySQL server; there is no
// zero-config file-based fallback anymore.
var connString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=localhost;Port=3306;Database=VIUUniversityDB;Uid=root;Pwd=;";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connString, new MySqlServerVersion(new Version(8, 0, 36))));

// ---- Identity (Authentication + Authorization) ----
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 10;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;

        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);

        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/tai-khoan/dang-nhap";
    options.AccessDeniedPath = "/tai-khoan/khong-co-quyen";
    options.LogoutPath = "/tai-khoan/dang-xuat";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;

    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.HttpOnly = true;

    // Secure-only in production. Forcing it in development would silently break login,
    // because `dotnet run` serves plain HTTP unless you configure an HTTPS profile.
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});

// ---- Authorization fallback ----
// Without a fallback policy, a newly added controller that forgets [Authorize] is
// silently public. Public controllers must opt out explicitly with [AllowAnonymous].
builder.Services.AddAuthorization(options =>
{
    // Bound to Identity's application cookie so authenticated users are recognised.
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder(
        IdentityConstants.ApplicationScheme)
        .RequireAuthenticatedUser()
        .Build();
});

// ---- Rate limiting ----
// The public forms (contact, admission lead) and the chat endpoint are unauthenticated
// and write to the database, so they are the obvious spam targets.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy(RateLimitPolicies.PublicForms, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.AddPolicy(RateLimitPolicies.Chat, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
        context => RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// ---- Application services (Service Layer, all injected via interfaces) ----
builder.Services.AddScoped<INewsService, NewsService>();
builder.Services.AddScoped<IAnnouncementService, AnnouncementService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IHomeService, HomeService>();
builder.Services.AddScoped<IAcademicService, AcademicService>();
builder.Services.AddScoped<IAdmissionService, AdmissionService>();
builder.Services.AddScoped<ISiteSettingService, SiteSettingService>();
builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<IGalleryService, GalleryService>();
builder.Services.AddScoped<ISitemapService, SitemapService>();

// Stateless and thread-safe, so a single shared instance is enough.
builder.Services.AddSingleton<IContentSanitizer, ContentSanitizer>();

// ---- Admission assistant (AI) ----
// Provider is chosen once at startup from configuration. The API key lives in
// user-secrets / environment variables and is never sent to the browser - the
// view component only ever learns whether a provider is configured.
builder.Services.Configure<ChatOptions>(builder.Configuration.GetSection(ChatOptions.SectionName));

var chatOptions = builder.Configuration.GetSection(ChatOptions.SectionName).Get<ChatOptions>() ?? new ChatOptions();

// Fallback: allow configuring Gemini via environment variables without appsettings/user-secrets.
if (string.IsNullOrWhiteSpace(chatOptions.ApiKey))
{
    var geminiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
        ?? Environment.GetEnvironmentVariable("GOOGLE_API_KEY");
    if (!string.IsNullOrWhiteSpace(geminiKey))
    {
        chatOptions.ApiKey = geminiKey;
        if (string.IsNullOrWhiteSpace(chatOptions.Provider) || chatOptions.Provider.Equals("none", StringComparison.OrdinalIgnoreCase))
            chatOptions.Provider = "gemini";
    }
}

// Gemini convenience: map provider "gemini" to the OpenAI-compatible endpoint.
if (chatOptions.Provider.Equals("gemini", StringComparison.OrdinalIgnoreCase))
{
    if (string.IsNullOrWhiteSpace(chatOptions.BaseUrl))
        chatOptions.BaseUrl = "https://generativelanguage.googleapis.com/v1beta/openai";
    if (string.IsNullOrWhiteSpace(chatOptions.Model))
        chatOptions.Model = "gemini-3.5-flash-lite";
}

// KnowledgeService is needed on both paths (it is what grounds the answers), and it
// depends on the scoped DbContext, so it must always be registered as scoped.
builder.Services.AddScoped<KnowledgeService>();

if (!chatOptions.IsConfigured)
{
    // Không có key: trợ lý vẫn tra cứu được dữ liệu thật trong hệ thống và nói thẳng
    // là phần AI chưa được cấu hình. Không bao giờ giả lập câu trả lời mẫu.
    builder.Services.AddScoped<IChatService, UnavailableChatService>();
}
else if (chatOptions.Provider.Equals("openai", StringComparison.OrdinalIgnoreCase))
{
    // OpenAI Responses API qua SDK chính thức.
    builder.Services.AddSingleton<IChatProvider, OpenAiResponsesProvider>();
    builder.Services.AddScoped<IChatService, ChatService>();
}
else
{
    // Các provider tương thích khác (endpoint OpenAI-style, Anthropic...).
    Type? chatProvider = chatOptions.Provider.ToLowerInvariant() switch
    {
        "openai-compatible" => typeof(OpenAiCompatibleProvider),
        "gemini" => typeof(OpenAiCompatibleProvider),
        "anthropic" => typeof(AnthropicProvider),
        _ => null
    };

    if (chatProvider is null)
    {
        builder.Services.AddScoped<IChatService, UnavailableChatService>();
    }
    else
    {
        // AddSingleton(Type) đăng ký chính kiểu concrete, nên phải chỉ rõ service type là
        // IChatProvider — nếu không ChatService sẽ không resolve được.
        builder.Services.AddSingleton(typeof(IChatProvider), chatProvider);
        builder.Services.AddScoped<IChatService, ChatService>();

        builder.Services.AddHttpClient("openai-compatible", c =>
            c.Timeout = TimeSpan.FromSeconds(chatOptions.TimeoutSeconds));
        builder.Services.AddHttpClient("anthropic", c =>
            c.Timeout = TimeSpan.FromSeconds(chatOptions.TimeoutSeconds));
    }
}

// ---- MVC + Areas (Admin CMS) ----
builder.Services.AddControllersWithViews();

var app = builder.Build();

// ---- Database migration + seed ----
using (var scope = app.Services.CreateScope())
{
    await DatabaseInitializer.InitializeAsync(scope.ServiceProvider, app.Configuration);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// HEAD has to be answered by the GET route. MVC declares [HttpGet] routes only, so
// without this a HEAD request matches no endpoint and is reported as 405 - which breaks
// every link checker, uptime probe and CDN revalidation. Kestrel discards the body of a
// HEAD response on its own, so running the GET pipeline and dropping the content is both
// correct and cheaper than maintaining a parallel HEAD route for every action.
app.Use(async (context, next) =>
{
    if (HttpMethods.IsHead(context.Request.Method))
    {
        context.Request.Method = HttpMethods.Get;
    }

    await next();
});

// ---- Routes ----
// Mapping endpoints is pure registration: it does not depend on, and does not affect, the
// order of the middleware below.
app.MapControllers();

// Admin area must be mapped before the default route.
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.UseRouting();

app.UseRateLimiter();

// Give every 404 a real, branded page instead of an empty body. This covers the URLs that
// match no route at all as well as the ones a controller returns with NotFound().
// The middleware below must run first for the former to reach here at all.
app.UseStatusCodePagesWithReExecute("/Home/NotFound", "?code={0}");

// Routing failures must answer 404 or 405, never a login redirect.
//
// Two distinct failures reach this point, and both have to sit above UseAuthorization -
// once the challenge has been sent the status is already a redirect:
//   1. No route matched at all, so GetEndpoint() is null.
//   2. The route matched but not this verb. Routing substitutes a synthetic endpoint
//      with no RequestDelegate and no metadata at all. The fallback authorization
//      policy reads that bare endpoint as protected and challenges it, which is why
//      GET on a POST-only form used to answer 302 -> /tai-khoan/dang-nhap.
app.Use(async (context, next) =>
{
    var endpoint = context.GetEndpoint();

    if (endpoint is null)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    // Every routable action endpoint carries metadata - route, verb, filters,
    // authorization attributes. A metadata-free endpoint can only be the stub routing
    // substitutes for a verb mismatch, so it is identified by that rather than by its
    // display name, which is free to change between framework versions.
    if (endpoint.Metadata.Count == 0)
    {
        context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
        return;
    }

    await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.Run();

/// <summary>Names of the rate-limiting policies referenced by controllers.</summary>
public static class RateLimitPolicies
{
    public const string PublicForms = "PublicForms";
    public const string Chat = "Chat";
}