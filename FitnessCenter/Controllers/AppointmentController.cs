using FitnessCenter.Data;
using FitnessCenter.Models;
using FitnessCenter.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Controllers
{
    // Randevu controller sınıfı
    public class AppointmentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AppointmentController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Randevu listesi
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var appointments = await _context.Appointments
                .Include(a => a.Trainer)
                    .ThenInclude(t => t!.Gym)
                .Include(a => a.Service)
                .Where(a => a.UserId == user.Id)
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.AppointmentTime)
                .ToListAsync();

            return View(appointments);
        }

        // Randevu oluşturma sayfası
        [Authorize]
        public async Task<IActionResult> Create(int? trainerId = null, int? serviceId = null)
        {
            var trainers = await _context.Trainers
                .Include(t => t.TrainerServices)
                    .ThenInclude(ts => ts.Service)
                .Where(t => t.IsActive)
                .ToListAsync();

            var services = await _context.Services
                .Where(s => s.IsActive)
                .ToListAsync();

            ViewBag.Trainers = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(trainers, "Id", "FullName", trainerId);
            ViewBag.Services = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(services, "Id", "Name", serviceId);

            var model = new AppointmentCreateViewModel
            {
                Trainers = trainers,
                Services = services,
                TrainerId = trainerId ?? 0,
                ServiceId = serviceId ?? 0,
                AppointmentDate = DateTime.Today.AddDays(1)
            };

            return View(model);
        }

        // Randevu oluşturma işlemi
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AppointmentCreateViewModel model)
        {
            var trainers = await _context.Trainers
                .Include(t => t.TrainerServices)
                    .ThenInclude(ts => ts.Service)
                .Where(t => t.IsActive)
                .ToListAsync();

            var services = await _context.Services
                .Where(s => s.IsActive)
                .ToListAsync();

            model.Trainers = trainers;
            model.Services = services;

            ViewBag.Trainers = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(trainers, "Id", "FullName", model.TrainerId);
            ViewBag.Services = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(services, "Id", "Name", model.ServiceId);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Geçmiş tarih kontrolü
            if (model.AppointmentDate.Date < DateTime.Today)
            {
                ModelState.AddModelError("AppointmentDate", "Randevu tarihi geçmişte olamaz.");
                return View(model);
            }

            // Hizmeti bul ve bitiş saati/fiyat hesapla
            var service = await _context.Services.FindAsync(model.ServiceId);
            if (service == null)
            {
                ModelState.AddModelError("ServiceId", "Seçilen hizmet bulunamadı.");
                return View(model);
            }

            // Antrenörün bu hizmeti sunup sunmadığını kontrol et
            var trainerProvidesService = await _context.TrainerServices
                .AnyAsync(ts => ts.TrainerId == model.TrainerId && ts.ServiceId == model.ServiceId);
            
            if (!trainerProvidesService)
            {
                ModelState.AddModelError("ServiceId", "Seçilen antrenör bu hizmeti sunmamaktadır.");
                return View(model);
            }

            var endTime = model.AppointmentTime.Add(TimeSpan.FromMinutes(service.DurationMinutes));

            // Randevu çakışması kontrolü
            var hasConflict = await _context.Appointments
                .Where(a => a.TrainerId == model.TrainerId &&
                           a.AppointmentDate.Date == model.AppointmentDate.Date &&
                           a.Status != AppointmentStatus.Cancelled &&
                           ((model.AppointmentTime >= a.AppointmentTime && model.AppointmentTime < a.EndTime) ||
                            (endTime > a.AppointmentTime && endTime <= a.EndTime) ||
                            (model.AppointmentTime <= a.AppointmentTime && endTime >= a.EndTime)))
                .AnyAsync();

            if (hasConflict)
            {
                ModelState.AddModelError("", "Seçilen saat diliminde antrenörün başka bir randevusu bulunmaktadır. Lütfen farklı bir saat seçin.");
                return View(model);
            }

            // Antrenör müsaitlik kontrolü
            var dayOfWeek = model.AppointmentDate.DayOfWeek;
            
            // Önce WorkingDays kontrolü yap
            var trainer = await _context.Trainers.FindAsync(model.TrainerId);
            if (trainer != null && !string.IsNullOrEmpty(trainer.WorkingDays))
            {
                var workingDaysList = trainer.WorkingDays.Split(',', StringSplitOptions.RemoveEmptyEntries);
                var dayName = dayOfWeek.ToString();
                
                if (!workingDaysList.Contains(dayName))
                {
                    string dayNameTr = dayOfWeek switch
                    {
                        DayOfWeek.Monday => "Pazartesi",
                        DayOfWeek.Tuesday => "Salı",
                        DayOfWeek.Wednesday => "Çarşamba",
                        DayOfWeek.Thursday => "Perşembe",
                        DayOfWeek.Friday => "Cuma",
                        DayOfWeek.Saturday => "Cumartesi",
                        DayOfWeek.Sunday => "Pazar",
                        _ => dayName
                    };
                    ModelState.AddModelError("AppointmentDate", $"Antrenör {dayNameTr} günü çalışmamaktadır. Lütfen çalıştığı günlerden birini seçin.");
                    return View(model);
                }
            }
            
            var hasAvailabilityRecords = await _context.TrainerAvailabilities
                .Where(ta => ta.TrainerId == model.TrainerId)
                .AnyAsync();

            if (hasAvailabilityRecords)
            {
                var isAvailable = await _context.TrainerAvailabilities
                    .Where(ta => ta.TrainerId == model.TrainerId &&
                                ta.DayOfWeek == dayOfWeek &&
                                ta.IsAvailable &&
                                ta.StartTime <= model.AppointmentTime &&
                                ta.EndTime >= endTime)
                    .AnyAsync();

                if (!isAvailable)
                {
                    ModelState.AddModelError("", "Seçilen tarih ve saatte antrenör müsait değildir.");
                    return View(model);
                }
            }

            // Randevuyu oluştur
            var appointment = new Appointment
            {
                UserId = user.Id,
                TrainerId = model.TrainerId,
                ServiceId = model.ServiceId,
                AppointmentDate = model.AppointmentDate.Date,
                AppointmentTime = model.AppointmentTime,
                EndTime = endTime,
                TotalPrice = service.Price,
                Notes = model.Notes,
                Status = AppointmentStatus.Pending,
                CreatedAt = DateTime.Now
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Randevunuz başarıyla oluşturuldu. Onay bekleniyor.";
            return RedirectToAction("MyAppointments", "Account");
        }

        // Randevu detayı
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointments
                .Include(a => a.User)
                .Include(a => a.Trainer)
                .Include(a => a.Service)
                    .ThenInclude(s => s!.Gym)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
            {
                return NotFound();
            }

            // Yetki kontrolü
            var user = await _userManager.GetUserAsync(User);
            if (user != null && appointment.UserId != user.Id && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            return View(appointment);
        }

        // Randevu iptal işlemi
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
            {
                return NotFound();
            }

            // Yetki kontrolü
            if (appointment.UserId != user.Id && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            // Sadece bekleyen veya onaylı randevular iptal edilebilir
            if (appointment.Status == AppointmentStatus.Completed ||
                appointment.Status == AppointmentStatus.Cancelled)
            {
                TempData["Error"] = "Bu randevu iptal edilemez.";
                return RedirectToAction("MyAppointments", "Account");
            }

            appointment.Status = AppointmentStatus.Cancelled;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Randevunuz başarıyla iptal edildi.";
            return RedirectToAction("MyAppointments", "Account");
        }

        // Müsait zaman dilimleri
        [HttpGet]
        public async Task<IActionResult> GetAvailableSlots(int trainerId, int serviceId, DateTime date)
        {
            var service = await _context.Services.FindAsync(serviceId);
            if (service == null)
            {
                return Json(new { error = "Hizmet bulunamadı" });
            }

            var dayOfWeek = date.DayOfWeek;
            var availability = await _context.TrainerAvailabilities
                .Where(ta => ta.TrainerId == trainerId && ta.DayOfWeek == dayOfWeek && ta.IsAvailable)
                .FirstOrDefaultAsync();

            if (availability == null)
            {
                return Json(new { error = "Antrenör bu gün çalışmıyor" });
            }

            // Bu tarihte mevcut randevuları getir
            var existingAppointments = await _context.Appointments
                .Where(a => a.TrainerId == trainerId &&
                           a.AppointmentDate.Date == date.Date &&
                           a.Status != AppointmentStatus.Cancelled)
                .Select(a => new { a.AppointmentTime, a.EndTime })
                .ToListAsync();

            var slots = new List<AvailableSlotViewModel>();
            var slotDuration = TimeSpan.FromMinutes(service.DurationMinutes);
            var currentTime = availability.StartTime;

            while (currentTime.Add(slotDuration) <= availability.EndTime)
            {
                var slotEnd = currentTime.Add(slotDuration);
                var isAvailable = !existingAppointments.Any(a =>
                    (currentTime >= a.AppointmentTime && currentTime < a.EndTime) ||
                    (slotEnd > a.AppointmentTime && slotEnd <= a.EndTime) ||
                    (currentTime <= a.AppointmentTime && slotEnd >= a.EndTime));

                // Bugün için geçmiş saatleri gösterme
                if (date.Date == DateTime.Today && currentTime < DateTime.Now.TimeOfDay)
                {
                    isAvailable = false;
                }

                slots.Add(new AvailableSlotViewModel
                {
                    StartTime = currentTime,
                    EndTime = slotEnd,
                    IsAvailable = isAvailable
                });

                currentTime = currentTime.Add(TimeSpan.FromMinutes(30));
            }

            return Json(slots);
        }

        // Antrenörün sunduğu hizmetler
        [HttpGet]
        public async Task<IActionResult> GetTrainerServices(int trainerId)
        {
            var trainerServices = await _context.TrainerServices
                .Include(ts => ts.Service)
                .Where(ts => ts.TrainerId == trainerId && ts.Service!.IsActive)
                .Select(ts => new
                {
                    ts.Service!.Id,
                    ts.Service.Name,
                    ts.Service.DurationMinutes,
                    ts.Service.Price
                })
                .ToListAsync();

            return Json(trainerServices);
        }
    }
}
