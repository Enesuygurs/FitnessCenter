# 🏋️ FitLife Fitness Center - Proje Dokümantasyonu

## 📋 İçindekiler
1. [Kullanılan Teknolojiler](#1-kullanılan-teknolojiler)
2. [Veritabanı Yapısı](#2-veritabanı-yapısı)
3. [Spor Salonu Tanımlamaları](#3-spor-salonu-tanımlamaları)
4. [Antrenör Yönetimi](#4-antrenör-yönetimi)
5. [Üye ve Randevu Sistemi](#5-üye-ve-randevu-sistemi)
6. [REST API ve LINQ Sorguları](#6-rest-api-ve-linq-sorguları)
7. [Yapay Zeka Entegrasyonu](#7-yapay-zeka-entegrasyonu)
8. [Yetkilendirme ve Güvenlik](#8-yetkilendirme-ve-güvenlik)
9. [Data Validation](#9-data-validation)
10. [CRUD İşlemleri](#10-crud-i̇şlemleri)

---

## 1. Kullanılan Teknolojiler

### 📂 Dosya: `FitnessCenter.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <!-- Entity Framework Core - SQL Server -->
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="9.0.0" />
    
    <!-- Entity Framework Core Tools -->
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="10.0.0" />
    
    <!-- ASP.NET Core Identity -->
    <PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="9.0.0" />
    
    <!-- .env dosyası okuma (API Key için) -->
    <PackageReference Include="DotNetEnv" Version="3.1.1" />
  </ItemGroup>
</Project>
```

| Teknoloji | Kullanım Yeri |
|-----------|---------------|
| **ASP.NET Core 9.0 MVC** | Tüm proje yapısı |
| **C#** | Backend kodları |
| **SQL Server** | Veritabanı |
| **Entity Framework Core** | ORM (Object-Relational Mapping) |
| **LINQ** | API sorgularında |
| **ASP.NET Core Identity** | Kullanıcı yönetimi |
| **Bootstrap 5.3.2** | UI Framework |
| **jQuery 3.7.1** | JavaScript kütüphanesi |
| **jQuery Validation** | Form doğrulama |

---

## 2. Veritabanı Yapısı

### 📂 Dosya: `Data/ApplicationDbContext.cs`

```csharp
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Veritabanı Tabloları (DbSet)
    public DbSet<Gym> Gyms { get; set; }                           // Spor Salonları
    public DbSet<Service> Services { get; set; }                   // Hizmetler
    public DbSet<Trainer> Trainers { get; set; }                   // Antrenörler
    public DbSet<TrainerService> TrainerServices { get; set; }     // Antrenör-Hizmet İlişkisi (Many-to-Many)
    public DbSet<TrainerAvailability> TrainerAvailabilities { get; set; } // Antrenör Müsaitlik
    public DbSet<Appointment> Appointments { get; set; }           // Randevular

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Gym configuration - İlişki tanımlamaları
        builder.Entity<Gym>(entity =>
        {
            entity.HasKey(g => g.Id);
            entity.Property(g => g.Name).IsRequired().HasMaxLength(100);
            entity.Property(g => g.Address).IsRequired().HasMaxLength(500);
        });

        // Service - Gym ilişkisi (One-to-Many)
        builder.Entity<Service>(entity =>
        {
            entity.HasOne(s => s.Gym)
                  .WithMany(g => g.Services)
                  .HasForeignKey(s => s.GymId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // TrainerService - Many-to-Many ilişki
        builder.Entity<TrainerService>(entity =>
        {
            entity.HasOne(ts => ts.Trainer)
                  .WithMany(t => t.TrainerServices)
                  .HasForeignKey(ts => ts.TrainerId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(ts => ts.Service)
                  .WithMany(s => s.TrainerServices)
                  .HasForeignKey(ts => ts.ServiceId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
```

### 📂 Dosya: `Program.cs` - Veritabanı Bağlantısı

```csharp
// SQL Server bağlantısı
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Uygulama başlarken veritabanını oluştur ve seed data ekle
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await DbInitializer.InitializeAsync(services);
}
```

### 📂 Dosya: `appsettings.json` - Bağlantı Dizesi

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=FitnessCenterDb;Trusted_Connection=True;"
  }
}
```

---

## 3. Spor Salonu Tanımlamaları

### 📂 Dosya: `Models/Gym.cs`

```csharp
public class Gym
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Salon adı zorunludur")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Adres zorunludur")]
    [StringLength(500)]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "Telefon zorunludur")]
    [Phone]
    public string Phone { get; set; } = string.Empty;

    [EmailAddress]
    public string? Email { get; set; }

    public string? Description { get; set; }

    // ⏰ Çalışma Saatleri
    [Required(ErrorMessage = "Açılış saati zorunludur")]
    public TimeSpan OpeningTime { get; set; }  // Örn: 06:00

    [Required(ErrorMessage = "Kapanış saati zorunludur")]
    public TimeSpan ClosingTime { get; set; }  // Örn: 23:00

    public bool IsActive { get; set; } = true;

    // Navigation - Salonun Hizmetleri ve Antrenörleri
    public virtual ICollection<Service> Services { get; set; }
    public virtual ICollection<Trainer> Trainers { get; set; }
}
```

### 📂 Dosya: `Models/Service.cs` - Hizmet Tanımlamaları

```csharp
public class Service
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Hizmet adı zorunludur")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    // ⏱️ Süre (dakika) - 15 ile 480 dakika arası
    [Required(ErrorMessage = "Süre zorunludur")]
    [Range(15, 480, ErrorMessage = "Süre 15-480 dakika arasında olmalıdır")]
    public int DurationMinutes { get; set; }

    // 💰 Ücret - 0 ile 10000 TL arası
    [Required(ErrorMessage = "Ücret zorunludur")]
    [Range(0, 10000, ErrorMessage = "Ücret 0-10000 TL arasında olmalıdır")]
    public decimal Price { get; set; }

    // 📁 Kategori (Fitness, Yoga, Pilates, Kişisel Antrenman)
    public string? Category { get; set; }

    public bool IsActive { get; set; } = true;

    // Foreign Key - Hangi salona ait
    public int GymId { get; set; }
    public virtual Gym? Gym { get; set; }
}
```

### 📂 Dosya: `Data/DbInitializer.cs` - Seed Data

```csharp
private static async Task SeedGymDataAsync(ApplicationDbContext context)
{
    if (await context.Gyms.AnyAsync())
        return;

    // Spor Salonu Oluştur
    var gym = new Gym
    {
        Name = "FitLife Spor Merkezi",
        Address = "Sakarya Üniversitesi Kampüsü, Esentepe",
        Phone = "0264 295 00 00",
        Email = "info@fitlife.com",
        Description = "Modern ekipmanları ve uzman kadrosuyla...",
        OpeningTime = new TimeSpan(6, 0, 0),   // 06:00
        ClosingTime = new TimeSpan(23, 0, 0),  // 23:00
        IsActive = true
    };
    context.Gyms.Add(gym);
    await context.SaveChangesAsync();

    // Hizmetler Oluştur
    var services = new List<Service>
    {
        new Service
        {
            Name = "Fitness",
            Description = "Kişiye özel fitness programları",
            DurationMinutes = 60,
            Price = 250,
            Category = "Fitness",
            GymId = gym.Id
        },
        new Service
        {
            Name = "Yoga",
            DurationMinutes = 60,
            Price = 200,
            Category = "Yoga",
            GymId = gym.Id
        },
        // ... diğer hizmetler
    };
    context.Services.AddRange(services);
    await context.SaveChangesAsync();
}
```

---

## 4. Antrenör Yönetimi

### 📂 Dosya: `Models/Trainer.cs`

```csharp
public class Trainer
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Ad zorunludur")]
    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Soyad zorunludur")]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta zorunludur")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Telefon zorunludur")]
    [Phone]
    public string Phone { get; set; } = string.Empty;

    // 🎯 Uzmanlık Alanları (Virgülle ayrılmış: "Fitness, Yoga, Pilates")
    [StringLength(500)]
    public string? Specializations { get; set; }

    public string? Biography { get; set; }

    // ⏰ Çalışma Saatleri
    [Required]
    public TimeSpan WorkStartTime { get; set; }  // Örn: 09:00

    [Required]
    public TimeSpan WorkEndTime { get; set; }    // Örn: 18:00

    public bool IsActive { get; set; } = true;

    [Range(0, 50)]
    public int? ExperienceYears { get; set; }

    // Foreign Key
    public int GymId { get; set; }
    public virtual Gym? Gym { get; set; }

    // Navigation - Antrenörün Hizmetleri, Randevuları, Müsaitlikleri
    public virtual ICollection<TrainerService> TrainerServices { get; set; }
    public virtual ICollection<Appointment> Appointments { get; set; }
    public virtual ICollection<TrainerAvailability> Availabilities { get; set; }

    // Computed Property
    public string FullName => $"{FirstName} {LastName}";
}
```

### 📂 Dosya: `Models/TrainerAvailability.cs` - Müsaitlik Takvimi

```csharp
public class TrainerAvailability
{
    public int Id { get; set; }

    public int TrainerId { get; set; }
    public virtual Trainer? Trainer { get; set; }

    // 📅 Hangi gün müsait
    public DayOfWeek DayOfWeek { get; set; }  // Monday, Tuesday, ...

    // ⏰ Müsait olduğu saat aralığı
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    public bool IsAvailable { get; set; } = true;
}
```

### 📂 Dosya: `Controllers/AdminController.cs` - Antrenör CRUD

```csharp
[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    // GET: /Admin/Trainers - Tüm antrenörleri listele
    public async Task<IActionResult> Trainers()
    {
        var trainers = await _context.Trainers
            .Include(t => t.Gym)
            .Include(t => t.TrainerServices)
                .ThenInclude(ts => ts.Service)
            .ToListAsync();
        return View(trainers);
    }

    // GET: /Admin/CreateTrainer - Antrenör ekleme formu
    public async Task<IActionResult> CreateTrainer()
    {
        ViewBag.Gyms = await _context.Gyms.Where(g => g.IsActive).ToListAsync();
        ViewBag.Services = await _context.Services.Where(s => s.IsActive).ToListAsync();
        return View();
    }

    // POST: /Admin/CreateTrainer - Yeni antrenör kaydet
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTrainer(Trainer trainer, int[] selectedServices)
    {
        if (ModelState.IsValid)
        {
            _context.Trainers.Add(trainer);
            await _context.SaveChangesAsync();

            // Seçilen hizmetleri antrenöre ata
            foreach (var serviceId in selectedServices)
            {
                _context.TrainerServices.Add(new TrainerService
                {
                    TrainerId = trainer.Id,
                    ServiceId = serviceId
                });
            }
            await _context.SaveChangesAsync();

            TempData["Success"] = "Antrenör başarıyla eklendi.";
            return RedirectToAction(nameof(Trainers));
        }
        return View(trainer);
    }

    // GET: /Admin/EditTrainer/5 - Düzenleme formu
    public async Task<IActionResult> EditTrainer(int? id) { ... }

    // POST: /Admin/EditTrainer/5 - Güncelle
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditTrainer(int id, Trainer trainer) { ... }

    // POST: /Admin/DeleteTrainer/5 - Sil
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTrainer(int id) { ... }
}
```

---

## 5. Üye ve Randevu Sistemi

### 📂 Dosya: `Models/Appointment.cs`

```csharp
// 📌 Randevu Durumları
public enum AppointmentStatus
{
    [Display(Name = "Beklemede")]
    Pending = 0,      // Yeni oluşturulan randevu
    
    [Display(Name = "Onaylandı")]
    Confirmed = 1,    // Admin tarafından onaylanan
    
    [Display(Name = "İptal Edildi")]
    Cancelled = 2,    // İptal edilen
    
    [Display(Name = "Tamamlandı")]
    Completed = 3     // Gerçekleştirilen
}

public class Appointment
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Randevu tarihi zorunludur")]
    [DataType(DataType.Date)]
    public DateTime AppointmentDate { get; set; }

    [Required(ErrorMessage = "Randevu saati zorunludur")]
    [DataType(DataType.Time)]
    public TimeSpan AppointmentTime { get; set; }

    [DataType(DataType.Time)]
    public TimeSpan EndTime { get; set; }  // Otomatik hesaplanır

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

    [StringLength(1000)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public decimal TotalPrice { get; set; }

    // Foreign Keys
    public string UserId { get; set; } = string.Empty;      // Üye
    public int TrainerId { get; set; }                       // Antrenör
    public int ServiceId { get; set; }                       // Hizmet

    // Navigation
    public virtual ApplicationUser? User { get; set; }
    public virtual Trainer? Trainer { get; set; }
    public virtual Service? Service { get; set; }
}
```

### 📂 Dosya: `Controllers/AppointmentController.cs` - Randevu Oluşturma

```csharp
[HttpPost]
[Authorize]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(AppointmentCreateViewModel model)
{
    // 1️⃣ Geçmiş tarih kontrolü
    if (model.AppointmentDate.Date < DateTime.Today)
    {
        ModelState.AddModelError("AppointmentDate", "Randevu tarihi geçmişte olamaz.");
        return View(model);
    }

    // 2️⃣ Hizmet bilgisini al
    var service = await _context.Services.FindAsync(model.ServiceId);
    
    // 3️⃣ Antrenörün bu hizmeti sunup sunmadığını kontrol et
    var trainerProvidesService = await _context.TrainerServices
        .AnyAsync(ts => ts.TrainerId == model.TrainerId && ts.ServiceId == model.ServiceId);
    
    if (!trainerProvidesService)
    {
        ModelState.AddModelError("ServiceId", "Seçilen antrenör bu hizmeti sunmamaktadır.");
        return View(model);
    }

    // 4️⃣ Bitiş saatini hesapla
    var endTime = model.AppointmentTime.Add(TimeSpan.FromMinutes(service.DurationMinutes));

    // 5️⃣ ⚠️ RANDEVU ÇAKIŞMA KONTROLÜ ⚠️
    var hasConflict = await _context.Appointments
        .Where(a => a.TrainerId == model.TrainerId &&
                   a.AppointmentDate.Date == model.AppointmentDate.Date &&
                   a.Status != AppointmentStatus.Cancelled &&
                   // Çakışma senaryoları:
                   ((model.AppointmentTime >= a.AppointmentTime && model.AppointmentTime < a.EndTime) ||
                    (endTime > a.AppointmentTime && endTime <= a.EndTime) ||
                    (model.AppointmentTime <= a.AppointmentTime && endTime >= a.EndTime)))
        .AnyAsync();

    if (hasConflict)
    {
        ModelState.AddModelError("", "Seçilen saat diliminde antrenörün başka bir randevusu bulunmaktadır.");
        return View(model);
    }

    // 6️⃣ Randevuyu oluştur
    var appointment = new Appointment
    {
        UserId = user.Id,
        TrainerId = model.TrainerId,
        ServiceId = model.ServiceId,
        AppointmentDate = model.AppointmentDate,
        AppointmentTime = model.AppointmentTime,
        EndTime = endTime,
        TotalPrice = service.Price,
        Status = AppointmentStatus.Pending,  // Varsayılan: Beklemede
        Notes = model.Notes,
        CreatedAt = DateTime.Now
    };

    _context.Appointments.Add(appointment);
    await _context.SaveChangesAsync();

    TempData["Success"] = "Randevunuz başarıyla oluşturuldu.";
    return RedirectToAction(nameof(Index));
}
```

### 📂 Dosya: `Controllers/AdminController.cs` - Randevu Onay Mekanizması

```csharp
// POST: /Admin/UpdateAppointmentStatus
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> UpdateAppointmentStatus(int id, AppointmentStatus status)
{
    var appointment = await _context.Appointments.FindAsync(id);
    if (appointment != null)
    {
        appointment.Status = status;  // Durumu güncelle
        await _context.SaveChangesAsync();
        
        var statusText = status switch
        {
            AppointmentStatus.Confirmed => "onaylandı",
            AppointmentStatus.Cancelled => "iptal edildi",
            AppointmentStatus.Completed => "tamamlandı",
            _ => "güncellendi"
        };
        
        TempData["Success"] = $"Randevu durumu {statusText}.";
    }
    return RedirectToAction(nameof(Appointments));
}
```

---

## 6. REST API ve LINQ Sorguları

### 📂 Dosya: `Controllers/Api/FitnessApiController.cs`

```csharp
[Route("api/[controller]")]
[ApiController]
public class FitnessApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public FitnessApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    // ═══════════════════════════════════════════════════════════
    // 🔷 LINQ SORGUSU 1: Tüm Antrenörleri Getir
    // GET: api/FitnessApi/trainers
    // ═══════════════════════════════════════════════════════════
    [HttpGet("trainers")]
    public async Task<ActionResult<IEnumerable<object>>> GetTrainers()
    {
        var trainers = await _context.Trainers
            .Include(t => t.Gym)                    // JOIN Gym tablosu
            .Include(t => t.TrainerServices)        // JOIN TrainerServices
                .ThenInclude(ts => ts.Service)      // JOIN Services
            .Where(t => t.IsActive)                 // WHERE IsActive = true
            .Select(t => new                        // SELECT (projection)
            {
                t.Id,
                t.FirstName,
                t.LastName,
                FullName = t.FirstName + " " + t.LastName,
                t.Specializations,
                t.ExperienceYears,
                WorkingHours = new
                {
                    Start = t.WorkStartTime.ToString(@"hh\:mm"),
                    End = t.WorkEndTime.ToString(@"hh\:mm")
                },
                Gym = new { t.Gym!.Id, t.Gym.Name },
                Services = t.TrainerServices.Select(ts => new
                {
                    ts.Service!.Id,
                    ts.Service.Name,
                    ts.Service.Price
                })
            })
            .ToListAsync();

        return Ok(trainers);
    }

    // ═══════════════════════════════════════════════════════════
    // 🔷 LINQ SORGUSU 2: ID'ye Göre Antrenör Detayı
    // GET: api/FitnessApi/trainers/5
    // ═══════════════════════════════════════════════════════════
    [HttpGet("trainers/{id}")]
    public async Task<ActionResult<object>> GetTrainer(int id)
    {
        var trainer = await _context.Trainers
            .Include(t => t.Gym)
            .Include(t => t.TrainerServices).ThenInclude(ts => ts.Service)
            .Include(t => t.Availabilities)
            .Where(t => t.Id == id)                  // WHERE Id = @id
            .Select(t => new { ... })
            .FirstOrDefaultAsync();                  // İlk kaydı getir

        if (trainer == null)
            return NotFound(new { message = "Antrenör bulunamadı" });

        return Ok(trainer);
    }

    // ═══════════════════════════════════════════════════════════
    // 🔷 LINQ SORGUSU 3: Belirli Tarihte Uygun Antrenörler
    // GET: api/FitnessApi/trainers/available?date=2024-01-15&serviceId=1
    // ═══════════════════════════════════════════════════════════
    [HttpGet("trainers/available")]
    public async Task<ActionResult<IEnumerable<object>>> GetAvailableTrainers(
        [FromQuery] DateTime date, 
        [FromQuery] int? serviceId = null)
    {
        var dayOfWeek = date.DayOfWeek;

        var query = _context.Trainers
            .Include(t => t.Gym)
            .Include(t => t.TrainerServices).ThenInclude(ts => ts.Service)
            .Include(t => t.Availabilities)
            .Where(t => t.IsActive && 
                       t.Availabilities.Any(a => a.DayOfWeek == dayOfWeek && a.IsAvailable));

        // Opsiyonel: Hizmete göre filtrele
        if (serviceId.HasValue)
        {
            query = query.Where(t => t.TrainerServices.Any(ts => ts.ServiceId == serviceId.Value));
        }

        var trainers = await query.Select(t => new { ... }).ToListAsync();

        return Ok(new
        {
            Date = date.ToString("yyyy-MM-dd"),
            DayOfWeek = dayOfWeek.ToString(),
            AvailableTrainers = trainers
        });
    }

    // ═══════════════════════════════════════════════════════════
    // 🔷 LINQ SORGUSU 4: Uzmanlık Alanına Göre Arama
    // GET: api/FitnessApi/trainers/search?specialization=yoga&minExperience=3
    // ═══════════════════════════════════════════════════════════
    [HttpGet("trainers/search")]
    public async Task<ActionResult<IEnumerable<object>>> SearchTrainers(
        [FromQuery] string? specialization = null,
        [FromQuery] int? minExperience = null)
    {
        var query = _context.Trainers
            .Include(t => t.Gym)
            .Where(t => t.IsActive);

        // Uzmanlık alanına göre filtrele
        if (!string.IsNullOrEmpty(specialization))
        {
            query = query.Where(t => t.Specializations != null && 
                                    t.Specializations.ToLower().Contains(specialization.ToLower()));
        }

        // Minimum deneyime göre filtrele
        if (minExperience.HasValue)
        {
            query = query.Where(t => t.ExperienceYears >= minExperience.Value);
        }

        var trainers = await query.Select(t => new { ... }).ToListAsync();
        return Ok(trainers);
    }

    // ═══════════════════════════════════════════════════════════
    // 🔷 LINQ SORGUSU 5: Üyenin Randevuları
    // GET: api/FitnessApi/appointments/member/{userId}
    // ═══════════════════════════════════════════════════════════
    [HttpGet("appointments/member/{userId}")]
    public async Task<ActionResult<IEnumerable<object>>> GetMemberAppointments(string userId)
    {
        var appointments = await _context.Appointments
            .Include(a => a.Trainer)
            .Include(a => a.Service).ThenInclude(s => s!.Gym)
            .Where(a => a.UserId == userId)                    // WHERE UserId = @userId
            .OrderByDescending(a => a.AppointmentDate)         // ORDER BY DESC
            .ThenByDescending(a => a.AppointmentTime)
            .Select(a => new { ... })
            .ToListAsync();

        return Ok(appointments);
    }

    // ═══════════════════════════════════════════════════════════
    // 🔷 LINQ SORGUSU 6: Tarihe Göre Randevular
    // GET: api/FitnessApi/appointments/date/2024-01-15
    // ═══════════════════════════════════════════════════════════
    [HttpGet("appointments/date/{date}")]
    public async Task<ActionResult<IEnumerable<object>>> GetAppointmentsByDate(DateTime date)
    {
        var appointments = await _context.Appointments
            .Include(a => a.User)
            .Include(a => a.Trainer)
            .Include(a => a.Service)
            .Where(a => a.AppointmentDate.Date == date.Date)   // WHERE Date = @date
            .OrderBy(a => a.AppointmentTime)                    // ORDER BY Time ASC
            .Select(a => new { ... })
            .ToListAsync();

        return Ok(new
        {
            Date = date.ToString("yyyy-MM-dd"),
            TotalAppointments = appointments.Count,
            Appointments = appointments
        });
    }

    // ═══════════════════════════════════════════════════════════
    // 🔷 LINQ SORGUSU 7: Antrenörün Randevuları (Tarih Aralığı)
    // GET: api/FitnessApi/appointments/trainer/5?startDate=2024-01-01&endDate=2024-01-31
    // ═══════════════════════════════════════════════════════════
    [HttpGet("appointments/trainer/{trainerId}")]
    public async Task<ActionResult<IEnumerable<object>>> GetTrainerAppointments(
        int trainerId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var query = _context.Appointments
            .Include(a => a.User)
            .Include(a => a.Service)
            .Where(a => a.TrainerId == trainerId);

        if (startDate.HasValue)
            query = query.Where(a => a.AppointmentDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(a => a.AppointmentDate <= endDate.Value);

        var appointments = await query
            .OrderByDescending(a => a.AppointmentDate)
            .Select(a => new { ... })
            .ToListAsync();

        return Ok(appointments);
    }

    // ═══════════════════════════════════════════════════════════
    // 🔷 LINQ SORGUSU 8: Tüm Hizmetler
    // GET: api/FitnessApi/services
    // ═══════════════════════════════════════════════════════════
    [HttpGet("services")]
    public async Task<ActionResult<IEnumerable<object>>> GetServices()
    {
        var services = await _context.Services
            .Include(s => s.Gym)
            .Include(s => s.TrainerServices).ThenInclude(ts => ts.Trainer)
            .Where(s => s.IsActive)
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.Description,
                s.DurationMinutes,
                s.Price,
                s.Category,
                Gym = s.Gym!.Name,
                AvailableTrainers = s.TrainerServices
                    .Where(ts => ts.Trainer!.IsActive)
                    .Select(ts => new
                    {
                        ts.Trainer!.Id,
                        FullName = ts.Trainer.FirstName + " " + ts.Trainer.LastName
                    })
            })
            .ToListAsync();

        return Ok(services);
    }

    // ═══════════════════════════════════════════════════════════
    // 🔷 LINQ SORGUSU 9: Kategoriye Göre Hizmetler
    // GET: api/FitnessApi/services/category/yoga
    // ═══════════════════════════════════════════════════════════
    [HttpGet("services/category/{category}")]
    public async Task<ActionResult<IEnumerable<object>>> GetServicesByCategory(string category)
    {
        var services = await _context.Services
            .Include(s => s.Gym)
            .Where(s => s.IsActive && 
                       s.Category != null && 
                       s.Category.ToLower().Contains(category.ToLower()))
            .Select(s => new { ... })
            .ToListAsync();

        return Ok(services);
    }

    // ═══════════════════════════════════════════════════════════
    // 🔷 LINQ SORGUSU 10: İstatistikler (Aggregation)
    // GET: api/FitnessApi/stats
    // ═══════════════════════════════════════════════════════════
    [HttpGet("stats")]
    public async Task<ActionResult<object>> GetStats()
    {
        var stats = new
        {
            // COUNT sorguları
            TotalTrainers = await _context.Trainers.CountAsync(t => t.IsActive),
            TotalServices = await _context.Services.CountAsync(s => s.IsActive),
            TotalAppointments = await _context.Appointments.CountAsync(),
            
            TodayAppointments = await _context.Appointments
                .CountAsync(a => a.AppointmentDate.Date == DateTime.Today),
            
            PendingAppointments = await _context.Appointments
                .CountAsync(a => a.Status == AppointmentStatus.Pending),
            
            CompletedAppointments = await _context.Appointments
                .CountAsync(a => a.Status == AppointmentStatus.Completed),
            
            // SUM sorguları
            TotalRevenue = await _context.Appointments
                .Where(a => a.Status == AppointmentStatus.Completed)
                .SumAsync(a => a.TotalPrice),
            
            MonthlyRevenue = await _context.Appointments
                .Where(a => a.Status == AppointmentStatus.Completed &&
                           a.AppointmentDate.Month == DateTime.Now.Month &&
                           a.AppointmentDate.Year == DateTime.Now.Year)
                .SumAsync(a => a.TotalPrice)
        };

        return Ok(stats);
    }
}
```

### 📊 API Endpoint Özeti

| Endpoint | HTTP | LINQ Operasyonları |
|----------|------|-------------------|
| `/api/FitnessApi/trainers` | GET | Include, Where, Select, ToListAsync |
| `/api/FitnessApi/trainers/{id}` | GET | Include, Where, Select, FirstOrDefaultAsync |
| `/api/FitnessApi/trainers/available` | GET | Include, Where, Any, Select |
| `/api/FitnessApi/trainers/search` | GET | Where, Contains, Select |
| `/api/FitnessApi/appointments/member/{userId}` | GET | Include, Where, OrderByDescending |
| `/api/FitnessApi/appointments/date/{date}` | GET | Include, Where, OrderBy |
| `/api/FitnessApi/appointments/trainer/{id}` | GET | Include, Where, OrderByDescending |
| `/api/FitnessApi/services` | GET | Include, Where, Select |
| `/api/FitnessApi/services/category/{cat}` | GET | Where, Contains |
| `/api/FitnessApi/stats` | GET | CountAsync, SumAsync |

---

## 7. Yapay Zeka Entegrasyonu

### 📂 Dosya: `Services/AIService.cs`

```csharp
public interface IAIService
{
    Task<string> GetFitnessRecommendationAsync(AIRecommendationViewModel model);
}

public class AIService : IAIService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AIService> _logger;

    public AIService(HttpClient httpClient, IConfiguration configuration, ILogger<AIService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> GetFitnessRecommendationAsync(AIRecommendationViewModel model)
    {
        // .env dosyasından API key'i al
        var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        
        if (string.IsNullOrEmpty(apiKey))
        {
            return GenerateDemoRecommendation(model);
        }

        try
        {
            var prompt = BuildPrompt(model);

            // 🤖 Google Gemini API İsteği
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { 
                                text = "Sen bir profesyonel fitness ve beslenme danışmanısın. " +
                                       "Türkçe yanıt ver. Kullanıcının verdiği bilgilere göre " +
                                       "kişiselleştirilmiş egzersiz ve diyet önerileri sun.\n\n" + prompt 
                            }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.7,
                    maxOutputTokens = 4096
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // 📡 API Çağrısı - Gemini 2.0 Flash modeli
            var response = await _httpClient.PostAsync(
                $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={apiKey}", 
                content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonDocument.Parse(responseContent);
                
                // JSON'dan öneriyi çıkar
                var recommendation = result.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                return recommendation ?? "Öneri alınamadı.";
            }
            else
            {
                return GenerateDemoRecommendation(model);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Gemini API");
            return GenerateDemoRecommendation(model);
        }
    }

    // 📝 Prompt Oluşturma
    private string BuildPrompt(AIRecommendationViewModel model)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Kullanıcı bilgileri:");
        
        if (model.Height.HasValue)
            sb.AppendLine($"- Boy: {model.Height} cm");
        
        if (model.Weight.HasValue)
            sb.AppendLine($"- Kilo: {model.Weight} kg");
        
        if (model.Age.HasValue)
            sb.AppendLine($"- Yaş: {model.Age}");
        
        if (!string.IsNullOrEmpty(model.Gender))
            sb.AppendLine($"- Cinsiyet: {model.Gender}");
        
        if (!string.IsNullOrEmpty(model.BodyType))
            sb.AppendLine($"- Vücut Tipi: {model.BodyType}");
        
        if (!string.IsNullOrEmpty(model.FitnessGoal))
            sb.AppendLine($"- Hedef: {model.FitnessGoal}");
        
        if (!string.IsNullOrEmpty(model.ActivityLevel))
            sb.AppendLine($"- Aktivite Seviyesi: {model.ActivityLevel}");

        sb.AppendLine();
        sb.AppendLine("Bu bilgilere göre kullanıcıya:");
        sb.AppendLine("1. Haftalık egzersiz programı öner");
        sb.AppendLine("2. Günlük diyet önerileri ver");
        sb.AppendLine("3. Hangi hizmetleri tercih etmesi gerektiğini öner");
        sb.AppendLine("4. Genel sağlık ve fitness tavsiyeleri ver");

        return sb.ToString();
    }
}
```

### 📂 Dosya: `Models/ViewModels/OtherViewModels.cs` - AI Input Model

```csharp
public class AIRecommendationViewModel
{
    [Display(Name = "Boy (cm)")]
    [Range(100, 250, ErrorMessage = "Boy 100-250 cm arasında olmalıdır")]
    public int? Height { get; set; }

    [Display(Name = "Kilo (kg)")]
    [Range(30, 300, ErrorMessage = "Kilo 30-300 kg arasında olmalıdır")]
    public double? Weight { get; set; }

    [Display(Name = "Yaş")]
    [Range(10, 100, ErrorMessage = "Yaş 10-100 arasında olmalıdır")]
    public int? Age { get; set; }

    [Display(Name = "Cinsiyet")]
    public string? Gender { get; set; }

    [Display(Name = "Vücut Tipi")]
    public string? BodyType { get; set; }

    [Display(Name = "Fitness Hedefi")]
    public string? FitnessGoal { get; set; }

    [Display(Name = "Mevcut Aktivite Seviyesi")]
    public string? ActivityLevel { get; set; }

    [Display(Name = "Sağlık Durumu / Kısıtlamalar")]
    public string? HealthConditions { get; set; }

    // Sonuç
    public string? Recommendation { get; set; }
}
```

### 📂 Dosya: `Program.cs` - Dependency Injection

```csharp
// AI Service'i HttpClient ile kaydet
builder.Services.AddHttpClient<IAIService, AIService>();
```

### 📂 Dosya: `.env` - API Key (Güvenli Saklama)

```env
GEMINI_API_KEY=your_api_key_here
```

---

## 8. Yetkilendirme ve Güvenlik

### 📂 Dosya: `Program.cs` - Identity Konfigürasyonu

```csharp
// ASP.NET Core Identity ekle
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Şifre gereksinimleri
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 3;  // Minimum 3 karakter (sau için)
    
    // Kullanıcı ayarları
    options.User.RequireUniqueEmail = true;
    
    // E-posta onayı kapalı
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Cookie ayarları
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";           // Giriş sayfası
    options.LogoutPath = "/Account/Logout";         // Çıkış
    options.AccessDeniedPath = "/Account/AccessDenied";  // Erişim engeli
    options.ExpireTimeSpan = TimeSpan.FromDays(7);  // Cookie süresi
    options.SlidingExpiration = true;
});

// Middleware sırası
app.UseAuthentication();  // Önce kimlik doğrulama
app.UseAuthorization();   // Sonra yetkilendirme
```

### 📂 Dosya: `Data/DbInitializer.cs` - Roller ve Admin Kullanıcısı

```csharp
// 👤 Rolleri Oluştur
private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
{
    string[] roles = { "Admin", "Member" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}

// 👑 Admin Kullanıcısını Oluştur
private static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager)
{
    // Format: ogrencinumarasi@sakarya.edu.tr / sau
    var adminEmail = "g231210302@sakarya.edu.tr";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FirstName = "Admin",
            LastName = "Kullanıcı",
            EmailConfirmed = true,
            RegistrationDate = DateTime.Now
        };

        var result = await userManager.CreateAsync(adminUser, "sau");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
}
```

### 📂 Controller'larda Authorization Kullanımı

```csharp
// 🔒 AdminController.cs - Sadece Admin erişebilir
[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    // Tüm metodlar sadece Admin rolü için
}

// 🔒 AppointmentController.cs - Giriş yapmış kullanıcılar
public class AppointmentController : Controller
{
    [Authorize]  // Sadece giriş yapmış kullanıcılar
    public async Task<IActionResult> Index() { ... }

    [Authorize]
    public async Task<IActionResult> Create() { ... }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]  // CSRF koruması
    public async Task<IActionResult> Create(AppointmentCreateViewModel model) { ... }
}

// 🔒 AccountController.cs - Herkese açık ve korumalı metodlar
public class AccountController : Controller
{
    // Herkese açık
    public IActionResult Login() { ... }
    public IActionResult Register() { ... }

    [Authorize]  // Sadece giriş yapmış
    public async Task<IActionResult> Profile() { ... }

    [Authorize]
    public async Task<IActionResult> MyAppointments() { ... }
}
```

---

## 9. Data Validation

### Server-Side Validation (Model Attributes)

```csharp
// Models/Gym.cs
[Required(ErrorMessage = "Salon adı zorunludur")]
[StringLength(100, ErrorMessage = "Salon adı en fazla 100 karakter olabilir")]
public string Name { get; set; }

// Models/Service.cs
[Required(ErrorMessage = "Süre zorunludur")]
[Range(15, 480, ErrorMessage = "Süre 15-480 dakika arasında olmalıdır")]
public int DurationMinutes { get; set; }

[Required(ErrorMessage = "Ücret zorunludur")]
[Range(0, 10000, ErrorMessage = "Ücret 0-10000 TL arasında olmalıdır")]
public decimal Price { get; set; }

// Models/Trainer.cs
[Required(ErrorMessage = "E-posta zorunludur")]
[EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
public string Email { get; set; }

[Required(ErrorMessage = "Telefon zorunludur")]
[Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz")]
public string Phone { get; set; }
```

### Client-Side Validation (Razor Views)

```html
<!-- Views/Admin/CreateGym.cshtml -->
<form asp-action="CreateGym" method="post">
    @Html.AntiForgeryToken()
    
    <div class="form-group">
        <label asp-for="Name"></label>
        <input asp-for="Name" class="form-control" 
               data-val-required="Salon adı zorunludur" />
        <span asp-validation-for="Name" class="text-danger"></span>
    </div>
    
    <button type="submit" class="btn btn-primary">Kaydet</button>
</form>

@section Scripts {
    <partial name="_ValidationScriptsPartial" />
}
```

### 📂 Dosya: `Views/Shared/_ValidationScriptsPartial.cshtml`

```html
<script src="~/lib/jquery-validation/dist/jquery.validate.min.js"></script>
<script src="~/lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.min.js"></script>
```

---

## 10. CRUD İşlemleri

### Gym CRUD Özeti

| İşlem | HTTP | Endpoint | Dosya |
|-------|------|----------|-------|
| **Create** | GET | `/Admin/CreateGym` | AdminController.cs:78 |
| **Create** | POST | `/Admin/CreateGym` | AdminController.cs:84 |
| **Read** | GET | `/Admin/Gyms` | AdminController.cs:69 |
| **Update** | GET | `/Admin/EditGym/5` | AdminController.cs:95 |
| **Update** | POST | `/Admin/EditGym/5` | AdminController.cs:108 |
| **Delete** | POST | `/Admin/DeleteGym/5` | AdminController.cs:131 |

### Service CRUD Özeti

| İşlem | HTTP | Endpoint | Dosya |
|-------|------|----------|-------|
| **Create** | GET | `/Admin/CreateService` | AdminController.cs:171 |
| **Create** | POST | `/Admin/CreateService` | AdminController.cs:177 |
| **Read** | GET | `/Admin/Services` | AdminController.cs:162 |
| **Update** | GET | `/Admin/EditService/5` | AdminController.cs:191 |
| **Update** | POST | `/Admin/EditService/5` | AdminController.cs:204 |
| **Delete** | POST | `/Admin/DeleteService/5` | AdminController.cs:227 |

### Trainer CRUD Özeti

| İşlem | HTTP | Endpoint | Dosya |
|-------|------|----------|-------|
| **Create** | GET | `/Admin/CreateTrainer` | AdminController.cs:260 |
| **Create** | POST | `/Admin/CreateTrainer` | AdminController.cs:268 |
| **Read** | GET | `/Admin/Trainers` | AdminController.cs:248 |
| **Update** | GET | `/Admin/EditTrainer/5` | AdminController.cs:298 |
| **Update** | POST | `/Admin/EditTrainer/5` | AdminController.cs:316 |
| **Delete** | POST | `/Admin/DeleteTrainer/5` | AdminController.cs:354 |

### Appointment CRUD Özeti

| İşlem | HTTP | Endpoint | Dosya |
|-------|------|----------|-------|
| **Create** | GET | `/Appointment/Create` | AppointmentController.cs:45 |
| **Create** | POST | `/Appointment/Create` | AppointmentController.cs:73 |
| **Read** | GET | `/Appointment` | AppointmentController.cs:22 |
| **Read** | GET | `/Appointment/Details/5` | AppointmentController.cs:170 |
| **Update** | POST | `/Admin/UpdateAppointmentStatus` | AdminController.cs:430 |
| **Delete** | POST | `/Appointment/Cancel/5` | AppointmentController.cs:195 |

---

## 📊 Özet Tablo

| Gereksinim | Uygulama | Dosya(lar) |
|------------|----------|------------|
| ASP.NET Core MVC | ✅ .NET 9.0 | FitnessCenter.csproj |
| SQL Server + EF Core | ✅ | Program.cs, ApplicationDbContext.cs |
| LINQ Sorguları | ✅ 10+ sorgu | FitnessApiController.cs |
| Bootstrap 5 | ✅ 5.3.2 | _Layout.cshtml |
| Spor Salonu Yönetimi | ✅ CRUD | AdminController.cs, Gym.cs |
| Hizmet Yönetimi | ✅ CRUD | AdminController.cs, Service.cs |
| Antrenör Yönetimi | ✅ CRUD + Müsaitlik | AdminController.cs, Trainer.cs |
| Randevu Sistemi | ✅ Çakışma Kontrolü | AppointmentController.cs |
| Randevu Onay Mekanizması | ✅ Status Enum | AdminController.cs:430 |
| REST API | ✅ 10 endpoint | FitnessApiController.cs |
| Yapay Zeka | ✅ Gemini 2.0 | AIService.cs |
| Rol Bazlı Yetkilendirme | ✅ Admin + Member | DbInitializer.cs, Controllers |
| Admin Kullanıcısı | ✅ g231210302@sakarya.edu.tr / sau | DbInitializer.cs:44 |
| Data Validation | ✅ Client + Server | Model sınıfları, Views |
| CSRF Koruması | ✅ ValidateAntiForgeryToken | Tüm POST metodları |

---

## 🎓 Proje Bilgileri

- **Öğrenci No:** G231210302
- **Üniversite:** Sakarya Üniversitesi
- **Ders:** Web Programlama
- **Teknoloji:** ASP.NET Core 9.0 MVC
- **Veritabanı:** SQL Server (LocalDB)
- **AI API:** Google Gemini 2.0 Flash
