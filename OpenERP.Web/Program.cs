/*
 * File: OpenERP.Web/Program.cs
 * Description: Application entry point and service configuration for OpenERP.Web.
 */

using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using OpenERP.Asset.Data;
using OpenERP.BasicData.Data;
using OpenERP.CRM.Data;
using OpenERP.Finance.Data;
using OpenERP.Logistics.Data;
using OpenERP.Office.Data;
using OpenERP.Production.Data;
using OpenERP.Purchasing.Data;
using OpenERP.Sales.Data;
using OpenERP.Service.Data;
using OpenERP.Transport.Data;
using OpenERP.Web.Data.HR;
using OpenERP.Web.Documents;
using OpenERP.Web.Localization;
using OpenERP.Web.Printing;
using OpenERP.Web.Security;
using AssetDbContext = OpenERP.Asset.Data.ApplicationDbContext;
using CRMDbContext = OpenERP.CRM.Data.ApplicationDbContext;
using FinanceDbContext = OpenERP.Finance.Data.ApplicationDbContext;
using LogisticsDbContext = OpenERP.Logistics.Data.ApplicationDbContext;
using OfficeDbContext = OpenERP.Office.Data.ApplicationDbContext;
using ProductionDbContext = OpenERP.Production.Data.ApplicationDbContext;
using PurchasingDbContext = OpenERP.Purchasing.Data.ApplicationDbContext;
using SalesDbContext = OpenERP.Sales.Data.ApplicationDbContext;
using ServiceDbContext = OpenERP.Service.Data.ApplicationDbContext;
using TransportDbContext = OpenERP.Transport.Data.ApplicationDbContext;

var builder = WebApplication.CreateBuilder(args);

// 本地化资源目录（统一存放系统界面文案）。
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// 注册 MVC、视图本地化和统一的数据注解本地化。
builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(options =>
    {
        options.DataAnnotationLocalizerProvider = (_, factory) => factory.Create(typeof(SharedResource));
    });

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "OpenERP.Auth";
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        // Cookie 安全加固（HttpOnly 防脚本读取；SameSite=Lax 防跨站携带；生产环境强制仅 HTTPS 传输）。
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
    });

// 支持的界面语言（简体中文、繁体中文、英文）。
var supportedCultures = new[]
{
    new CultureInfo("zh-Hans"),
    new CultureInfo("zh-Hant"),
    new CultureInfo("en")
};

// 请求语言解析配置（默认简体中文，并支持 QueryString、Cookie、浏览器语言协商）。
var requestLocalizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("zh-Hans"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures,
    ApplyCurrentCultureToResponseHeaders = true
};

requestLocalizationOptions.RequestCultureProviders =
[
    new QueryStringRequestCultureProvider(),
    new CookieRequestCultureProvider(),
    new AcceptLanguageHeaderRequestCultureProvider()
];

builder.Services.AddDbContext<PurchasingDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContext<SalesDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContext<LogisticsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContext<FinanceDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContext<ProductionDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContext<CRMDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContext<ServiceDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContext<TransportDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContext<AssetDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContext<OfficeDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 注册 HR 与基础数据模块仓储。
builder.Services.AddScoped<IHrRepository, HrSqlRepository>();
builder.Services.AddScoped<IBasicDataRepository, BasicDataSqlRepository>();
builder.Services.AddScoped<ICompanyPrintTemplateService, CompanyPrintTemplateService>();
builder.Services.AddScoped<ICommonDocumentService, CommonDocumentService>();

// 登录防暴力破解：内存缓存 + 账号失败锁定服务（连续 5 次失败锁定 15 分钟）。
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<LoginThrottler>();

// 登录接口限流（按客户端 IP 每分钟最多 10 次，超出直接返回 429，配合 [EnableRateLimiting("login")] 使用）。
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

var app = builder.Build();

// 初始化人资模块与基础数据模块的表结构及种子数据。
using (var scope = app.Services.CreateScope())
{
    var hrRepository = scope.ServiceProvider.GetRequiredService<IHrRepository>();
    await hrRepository.InitializeAsync();

    var basicDataRepository = scope.ServiceProvider.GetRequiredService<IBasicDataRepository>();
    await basicDataRepository.InitializeAsync();

    var commonDocumentService = scope.ServiceProvider.GetRequiredService<ICommonDocumentService>();
    await commonDocumentService.InitializeAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRequestLocalization(requestLocalizationOptions);
app.UseRouting();

// 登录接口限流（需在 UseRouting 之后启用，配合控制器上的 [EnableRateLimiting] 特性生效）。
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}")
    .WithStaticAssets();

app.MapGet("/", () => Results.Redirect("/login"));
app.MapGet("/login", () => Results.Redirect("/Account/Login"));
app.MapGet("/home", () => Results.Redirect("/Home/Index"));

app.Run();
