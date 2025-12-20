using FitnessCenter.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Data
{
    // Veritabanı başlatıcı ve seed veri sınıfı
    public static class DbInitializer
    {
        // Ana başlatma metodu
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Bekleyen migration'ları uygula
            await context.Database.MigrateAsync();

            // Rolleri oluştur
            await SeedRolesAsync(roleManager);

            // Admin kullanıcısını oluştur
            await SeedAdminUserAsync(userManager);

            // Spor salonu verilerini oluştur
            await SeedGymDataAsync(context);
        }

        // Rolleri oluştur
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

        // Admin ve demo kullanıcılarını oluştur
        private static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager)
        {
            // Admin kullanıcısı: g231210302@sakarya.edu.tr / sau
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

            // Demo üye kullanıcısı
            var memberEmail = "uye@example.com";
            var memberUser = await userManager.FindByEmailAsync(memberEmail);

            if (memberUser == null)
            {
                memberUser = new ApplicationUser
                {
                    UserName = memberEmail,
                    Email = memberEmail,
                    FirstName = "Demo",
                    LastName = "Üye",
                    EmailConfirmed = true,
                    RegistrationDate = DateTime.Now,
                    Height = 175,
                    Weight = 70,
                    Age = 28,
                    Gender = "Erkek",
                    BodyType = "Normal"
                };

                var result = await userManager.CreateAsync(memberUser, "Uye123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(memberUser, "Member");
                }
            }
        }

        // Spor salonu ve ilgili verileri oluştur
        private static async Task SeedGymDataAsync(ApplicationDbContext context)
        {
            // Veri zaten varsa çık
            if (await context.Gyms.AnyAsync())
                return;

            // Spor salonu oluştur
            var gym = new Gym
            {
                Name = "FitLife Spor Merkezi",
                Address = "Sakarya Üniversitesi Kampüsü, Esentepe, 54187 Serdivan/Sakarya",
                Phone = "0264 295 00 00",
                Email = "info@fitlife.com",
                Description = "Modern ekipmanları ve uzman kadrosuyla Sakarya'nın en kapsamlı fitness merkezine hoş geldiniz. Yoga, pilates, fitness ve kişisel antrenman hizmetleri sunmaktayız.",
                OpeningTime = new TimeSpan(6, 0, 0),
                ClosingTime = new TimeSpan(23, 0, 0),
                IsActive = true,
                ImageUrl = "/images/gym.jpg"
            };

            context.Gyms.Add(gym);
            await context.SaveChangesAsync();

            // Hizmetleri oluştur
            var services = new List<Service>
            {
                new Service
                {
                    Name = "Fitness",
                    Description = "Kişiye özel fitness programları ile kas gelişimi ve genel kondisyon antrenmanları.",
                    DurationMinutes = 60,
                    Price = 250,
                    Category = "Fitness",
                    GymId = gym.Id,
                    IsActive = true,
                    ImageUrl = "/images/services/fitness.jpg"
                },
                new Service
                {
                    Name = "Yoga",
                    Description = "Esneklik, denge ve zihinsel rahatlama için profesyonel yoga dersleri.",
                    DurationMinutes = 60,
                    Price = 200,
                    Category = "Yoga",
                    GymId = gym.Id,
                    IsActive = true,
                    ImageUrl = "/images/services/yoga.jpg"
                },
                new Service
                {
                    Name = "Pilates",
                    Description = "Core kaslarını güçlendiren ve postürü düzelten pilates seansları.",
                    DurationMinutes = 60,
                    Price = 220,
                    Category = "Pilates",
                    GymId = gym.Id,
                    IsActive = true,
                    ImageUrl = "/images/services/pilates.jpg"
                },
                new Service
                {
                    Name = "Kişisel Antrenman",
                    Description = "Birebir kişisel antrenör eşliğinde özel antrenman programı.",
                    DurationMinutes = 60,
                    Price = 400,
                    Category = "Personal Training",
                    GymId = gym.Id,
                    IsActive = true,
                    ImageUrl = "/images/services/personal.jpg"
                },
                new Service
                {
                    Name = "Kilo Verme Programı",
                    Description = "Kilo verme odaklı kardio ve direnç antrenmanları.",
                    DurationMinutes = 90,
                    Price = 350,
                    Category = "Weight Loss",
                    GymId = gym.Id,
                    IsActive = true,
                    ImageUrl = "/images/services/weightloss.jpg"
                },
                new Service
                {
                    Name = "Kas Geliştirme Programı",
                    Description = "Kas kütlesi artırmaya yönelik yoğun antrenman programı.",
                    DurationMinutes = 90,
                    Price = 350,
                    Category = "Muscle Building",
                    GymId = gym.Id,
                    IsActive = true,
                    ImageUrl = "/images/services/muscle.jpg"
                }
            };

            context.Services.AddRange(services);
            await context.SaveChangesAsync();

            // Antrenörleri oluştur
            var trainers = new List<Trainer>
            {
                new Trainer
                {
                    FirstName = "Ahmet",
                    LastName = "Yılmaz",
                    Email = "ahmet.yilmaz@fitlife.com",
                    Phone = "0532 111 22 33",
                    Specializations = "Fitness, Kas Geliştirme, Kuvvet Antrenmanı",
                    Biography = "10 yıllık deneyime sahip, profesyonel vücut geliştirme sporcusu ve sertifikalı fitness antrenörü.",
                    WorkStartTime = new TimeSpan(8, 0, 0),
                    WorkEndTime = new TimeSpan(18, 0, 0),
                    WorkingDays = "Monday,Tuesday,Wednesday,Thursday,Friday",
                    GymId = gym.Id,
                    IsActive = true,
                    ExperienceYears = 10,
                    ProfileImageUrl = "https://images.unsplash.com/photo-1567013127542-490d757e51fc?w=400"
                },
                new Trainer
                {
                    FirstName = "Ayşe",
                    LastName = "Demir",
                    Email = "ayse.demir@fitlife.com",
                    Phone = "0533 222 33 44",
                    Specializations = "Yoga, Pilates, Esneklik",
                    Biography = "Uluslararası sertifikalı yoga eğitmeni. Hindistan'da eğitim almış, 8 yıllık deneyim.",
                    WorkStartTime = new TimeSpan(7, 0, 0),
                    WorkEndTime = new TimeSpan(17, 0, 0),
                    WorkingDays = "Monday,Tuesday,Wednesday,Thursday,Friday,Saturday",
                    GymId = gym.Id,
                    IsActive = true,
                    ExperienceYears = 8,
                    ProfileImageUrl = "https://images.unsplash.com/photo-1594381898411-846e7d193883?w=400"
                },
                new Trainer
                {
                    FirstName = "Mehmet",
                    LastName = "Kaya",
                    Email = "mehmet.kaya@fitlife.com",
                    Phone = "0534 333 44 55",
                    Specializations = "Kilo Verme, Kardio, HIIT",
                    Biography = "Spor bilimleri mezunu, 6 yıllık kilo verme ve kondisyon antrenörlüğü deneyimi.",
                    WorkStartTime = new TimeSpan(10, 0, 0),
                    WorkEndTime = new TimeSpan(20, 0, 0),
                    WorkingDays = "Tuesday,Wednesday,Thursday,Friday,Saturday",
                    GymId = gym.Id,
                    IsActive = true,
                    ExperienceYears = 6,
                    ProfileImageUrl = "https://images.unsplash.com/photo-1571019614242-c5c5dee9f50b?w=400"
                },
                new Trainer
                {
                    FirstName = "Zeynep",
                    LastName = "Şahin",
                    Email = "zeynep.sahin@fitlife.com",
                    Phone = "0535 444 55 66",
                    Specializations = "Pilates, Fonksiyonel Antrenman, Rehabilitasyon",
                    Biography = "Fizyoterapi kökenli pilates eğitmeni. Sakatlanma sonrası rehabilitasyon uzmanı.",
                    WorkStartTime = new TimeSpan(9, 0, 0),
                    WorkEndTime = new TimeSpan(19, 0, 0),
                    WorkingDays = "Monday,Wednesday,Thursday,Friday,Saturday",
                    GymId = gym.Id,
                    IsActive = true,
                    ExperienceYears = 7,
                    ProfileImageUrl = "https://images.unsplash.com/photo-1518611012118-696072aa579a?w=400"
                }
            };

            context.Trainers.AddRange(trainers);
            await context.SaveChangesAsync();

            // Antrenör-Hizmet ilişkilerini oluştur
            var trainerServices = new List<TrainerService>
            {
                // Ahmet - Fitness, Kas Geliştirme, Kişisel Antrenman
                new TrainerService { TrainerId = trainers[0].Id, ServiceId = services[0].Id },
                new TrainerService { TrainerId = trainers[0].Id, ServiceId = services[3].Id },
                new TrainerService { TrainerId = trainers[0].Id, ServiceId = services[5].Id },
                
                // Ayşe - Yoga, Pilates
                new TrainerService { TrainerId = trainers[1].Id, ServiceId = services[1].Id },
                new TrainerService { TrainerId = trainers[1].Id, ServiceId = services[2].Id },
                
                // Mehmet - Fitness, Kilo Verme, Kişisel Antrenman
                new TrainerService { TrainerId = trainers[2].Id, ServiceId = services[0].Id },
                new TrainerService { TrainerId = trainers[2].Id, ServiceId = services[3].Id },
                new TrainerService { TrainerId = trainers[2].Id, ServiceId = services[4].Id },
                
                // Zeynep - Pilates, Kişisel Antrenman
                new TrainerService { TrainerId = trainers[3].Id, ServiceId = services[2].Id },
                new TrainerService { TrainerId = trainers[3].Id, ServiceId = services[3].Id }
            };

            context.TrainerServices.AddRange(trainerServices);
            await context.SaveChangesAsync();

            // Antrenör müsaitlik zamanlarını oluştur - her antrenör için WorkingDays'e göre
            var availabilities = new List<TrainerAvailability>();
            foreach (var trainer in trainers)
            {
                // WorkingDays string'ini parse et
                var workingDaysList = trainer.WorkingDays?.Split(',').Select(d => d.Trim()).ToList() ?? new List<string>();
                
                foreach (var dayName in workingDaysList)
                {
                    // String günü DayOfWeek enum'a çevir
                    if (Enum.TryParse<DayOfWeek>(dayName, out var dayOfWeek))
                    {
                        availabilities.Add(new TrainerAvailability
                        {
                            TrainerId = trainer.Id,
                            DayOfWeek = dayOfWeek,
                            StartTime = trainer.WorkStartTime,
                            EndTime = trainer.WorkEndTime,
                            IsAvailable = true
                        });
                    }
                }
            }

            context.TrainerAvailabilities.AddRange(availabilities);
            await context.SaveChangesAsync();
        }
    }
}
