# 🏋️ Fitness Center Management System

Modern bir spor salonu yönetim sistemi. ASP.NET Core MVC ile geliştirilmiştir.

## ✨ Özellikler

- **Kullanıcı Yönetimi:** Kayıt, giriş, profil düzenleme
- **Randevu Sistemi:** Online randevu oluşturma ve yönetimi
- **Trainer Yönetimi:** Eğitmen listesi ve detayları
- **Hizmet Kataloğu:** Fitness hizmetlerinin listelenmesi
- **Admin Paneli:** Trainer, servis ve randevu CRUD işlemleri
- **AI Önerileri:** Gemini API ile kişiselleştirilmiş fitness/diyet önerileri

## 🛠️ Teknolojiler

| Teknoloji | Versiyon |
|-----------|----------|
| .NET | 9.0 |
| ASP.NET Core MVC | 9.0 |
| Entity Framework Core | 9.0 |
| Bootstrap | 5.3 |
| Google Gemini API | 2.0 |

## 🚀 Kurulum

### Gereksinimler
- .NET 9 SDK
- SQL Server LocalDB

### Adımlar

```bash
# 1. Projeyi klonlayın
git clone https://github.com/Enesuygurs/FitnessCenter.git
cd web/FitnessCenter

# 2. Veritabanını oluşturun
dotnet ef database update

# 3. Uygulamayı çalıştırın
dotnet run
```

## 📁 Proje Yapısı

```
FitnessCenter/
├── Controllers/     # MVC Controller'lar
├── Models/          # Veritabanı modelleri
├── Views/           # Razor view'ları
├── Data/            # DbContext ve Seed data
├── Services/        # AI servisi
├── wwwroot/         # Statik dosyalar (CSS, JS)
└── Migrations/      # EF Core migration'ları
```

## 👥 Kullanıcı Rolleri

| Rol | Yetkiler |
|-----|----------|
| **Admin** | Tüm CRUD işlemleri, kullanıcı yönetimi |
| **Kullanıcı** | Randevu oluşturma, profil düzenleme |

## ⚙️ Yapılandırma

`appsettings.json` dosyasında:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=FitnessCenterDb;..."
  },
  "Gemini": {
    "ApiKey": "API_KEY"
  }
}
```

## 📸 Ekran Görüntüleri

- Ana Sayfa
- Trainer Listesi
- Randevu Oluşturma
- Admin Paneli
- AI Önerileri

## 📝 Lisans

Bu proje eğitim amaçlı geliştirilmiştir.
