using DotNetEnv;
using FitnessCenter.Data;
using FitnessCenter.Models;
using FitnessCenter.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

// Ortam değişkenlerini yükle
Env.Load();

var builder = WebApplication.CreateBuilder(args);

// MVC servislerini ekle
builder.Services.AddControllersWithViews();

// Veritabanı bağlantısını yapılandır
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity sistemini yapılandır
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Şifre gereksinimleri
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 3;
    
    // Kullanıcı ayarları
    options.User.RequireUniqueEmail = true;
    
    // Giriş ayarları
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Çerez ayarlarını yapılandır
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
});

// AI servisi için HttpClient ekle
builder.Services.AddHttpClient<IAIService, AIService>();

var app = builder.Build();

// Veritabanını başlat ve seed verilerini ekle
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await DbInitializer.InitializeAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Veritabanı başlatılırken hata oluştu.");
    }
}

// HTTP pipeline yapılandırması
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();

// Kimlik doğrulama ve yetkilendirme
app.UseAuthentication();
app.UseAuthorization();

// Route tanımlamaları
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
