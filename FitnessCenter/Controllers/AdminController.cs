using FitnessCenter.Data;
using FitnessCenter.Models;
using FitnessCenter.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Controllers
{
    // Admin panel controller sınıfı
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Admin ana sayfa / Dashboard
        public async Task<IActionResult> Index()
        {
            var totalMembers = await _userManager.Users.CountAsync();
            var totalTrainers = await _context.Trainers.CountAsync();
            var totalAppointments = await _context.Appointments.CountAsync();
            var pendingAppointments = await _context.Appointments
                .Where(a => a.Status == AppointmentStatus.Pending)
                .CountAsync();
            var todayAppointments = await _context.Appointments
                .Where(a => a.AppointmentDate.Date == DateTime.Today)
                .CountAsync();
            var totalRevenue = await _context.Appointments
                .Where(a => a.Status == AppointmentStatus.Completed)
                .SumAsync(a => a.TotalPrice);
            var monthlyRevenue = await _context.Appointments
                .Where(a => a.Status == AppointmentStatus.Completed && 
                           a.AppointmentDate.Month == DateTime.Now.Month &&
                           a.AppointmentDate.Year == DateTime.Now.Year)
                .SumAsync(a => a.TotalPrice);

            var recentAppointments = await _context.Appointments
                .Include(a => a.User)
                .Include(a => a.Trainer)
                .Include(a => a.Service)
                .OrderByDescending(a => a.CreatedAt)
                .Take(10)
                .ToListAsync();

            var model = new DashboardViewModel
            {
                TotalMembers = totalMembers,
                TotalTrainers = totalTrainers,
                TotalAppointments = totalAppointments,
                PendingAppointments = pendingAppointments,
                TodayAppointments = todayAppointments,
                TotalRevenue = totalRevenue,
                MonthlyRevenue = monthlyRevenue,
                RecentAppointments = recentAppointments
            };

            return View(model);
        }

        #region Spor Salonu Yönetimi

        // Spor salonu listesi
        public async Task<IActionResult> Gyms()
        {
            var gyms = await _context.Gyms.ToListAsync();
            return View(gyms);
        }

        // Spor salonu oluşturma sayfası
        public IActionResult CreateGym()
        {
            return View();
        }

        // Spor salonu oluşturma işlemi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateGym(Gym gym)
        {
            if (ModelState.IsValid)
            {
                _context.Gyms.Add(gym);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Spor salonu başarıyla eklendi.";
                return RedirectToAction(nameof(Gyms));
            }
            return View(gym);
        }

        // Spor salonu düzenleme sayfası
        public async Task<IActionResult> EditGym(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gym = await _context.Gyms.FindAsync(id);
            if (gym == null)
            {
                return NotFound();
            }
            return View(gym);
        }

        // Spor salonu düzenleme işlemi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditGym(int id, Gym gym)
        {
            if (id != gym.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(gym);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Spor salonu başarıyla güncellendi.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Gyms.AnyAsync(e => e.Id == id))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return RedirectToAction(nameof(Gyms));
            }
            return View(gym);
        }

        // Spor salonu silme işlemi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGym(int id)
        {
            var gym = await _context.Gyms.FindAsync(id);
            if (gym != null)
            {
                _context.Gyms.Remove(gym);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Spor salonu başarıyla silindi.";
            }
            return RedirectToAction(nameof(Gyms));
        }

        #endregion

        #region Hizmet Yönetimi

        // Hizmet listesi
        public async Task<IActionResult> Services()
        {
            var services = await _context.Services
                .Include(s => s.Gym)
                .ToListAsync();
            return View(services);
        }

        // Hizmet oluşturma sayfası
        public async Task<IActionResult> CreateService()
        {
            ViewBag.Gyms = await _context.Gyms.Where(g => g.IsActive).ToListAsync();
            return View();
        }

        // Hizmet oluşturma işlemi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateService(Service service)
        {
            if (ModelState.IsValid)
            {
                _context.Services.Add(service);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Hizmet başarıyla eklendi.";
                return RedirectToAction(nameof(Services));
            }
            ViewBag.Gyms = await _context.Gyms.Where(g => g.IsActive).ToListAsync();
            return View(service);
        }

        // Hizmet düzenleme sayfası
        public async Task<IActionResult> EditService(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var service = await _context.Services.FindAsync(id);
            if (service == null)
            {
                return NotFound();
            }
            ViewBag.Gyms = await _context.Gyms.Where(g => g.IsActive).ToListAsync();
            return View(service);
        }

        // Hizmet düzenleme işlemi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditService(int id, Service service)
        {
            if (id != service.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(service);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Hizmet başarıyla güncellendi.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Services.AnyAsync(e => e.Id == id))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return RedirectToAction(nameof(Services));
            }
            ViewBag.Gyms = await _context.Gyms.Where(g => g.IsActive).ToListAsync();
            return View(service);
        }

        // Hizmet silme işlemi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteService(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service != null)
            {
                _context.Services.Remove(service);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Hizmet başarıyla silindi.";
            }
            return RedirectToAction(nameof(Services));
        }

        #endregion

        #region Antrenör Yönetimi

        // Antrenör listesi
        public async Task<IActionResult> Trainers()
        {
            var trainers = await _context.Trainers
                .Include(t => t.Gym)
                .Include(t => t.TrainerServices)
                    .ThenInclude(ts => ts.Service)
                .ToListAsync();
            return View(trainers);
        }

        // Antrenör oluşturma sayfası
        public async Task<IActionResult> CreateTrainer()
        {
            ViewBag.Gyms = await _context.Gyms.Where(g => g.IsActive).ToListAsync();
            ViewBag.Services = await _context.Services.Where(s => s.IsActive).ToListAsync();
            return View();
        }

        // Antrenör oluşturma işlemi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTrainer(Trainer trainer, int[] selectedServices)
        {
            if (ModelState.IsValid)
            {
                _context.Trainers.Add(trainer);
                await _context.SaveChangesAsync();

                // Antrenör hizmetlerini ekle
                if (selectedServices != null)
                {
                    foreach (var serviceId in selectedServices)
                    {
                        _context.TrainerServices.Add(new TrainerService
                        {
                            TrainerId = trainer.Id,
                            ServiceId = serviceId
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                TempData["Success"] = "Antrenör başarıyla eklendi.";
                return RedirectToAction(nameof(Trainers));
            }
            ViewBag.Gyms = await _context.Gyms.Where(g => g.IsActive).ToListAsync();
            ViewBag.Services = await _context.Services.Where(s => s.IsActive).ToListAsync();
            return View(trainer);
        }

        // Antrenör düzenleme sayfası
        public async Task<IActionResult> EditTrainer(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var trainer = await _context.Trainers
                .Include(t => t.TrainerServices)
                .FirstOrDefaultAsync(t => t.Id == id);
            
            if (trainer == null)
            {
                return NotFound();
            }

            ViewBag.Gyms = await _context.Gyms.Where(g => g.IsActive).ToListAsync();
            ViewBag.Services = await _context.Services.Where(s => s.IsActive).ToListAsync();
            ViewBag.SelectedServices = trainer.TrainerServices.Select(ts => ts.ServiceId).ToList();
            return View(trainer);
        }

        // Antrenör düzenleme işlemi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTrainer(int id, Trainer trainer, int[] selectedServices)
        {
            if (id != trainer.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(trainer);

                    // Antrenör hizmetlerini güncelle
                    var existingServices = await _context.TrainerServices
                        .Where(ts => ts.TrainerId == id)
                        .ToListAsync();
                    _context.TrainerServices.RemoveRange(existingServices);

                    if (selectedServices != null)
                    {
                        foreach (var serviceId in selectedServices)
                        {
                            _context.TrainerServices.Add(new TrainerService
                            {
                                TrainerId = trainer.Id,
                                ServiceId = serviceId
                            });
                        }
                    }

                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Antrenör başarıyla güncellendi.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Trainers.AnyAsync(e => e.Id == id))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return RedirectToAction(nameof(Trainers));
            }
            ViewBag.Gyms = await _context.Gyms.Where(g => g.IsActive).ToListAsync();
            ViewBag.Services = await _context.Services.Where(s => s.IsActive).ToListAsync();
            return View(trainer);
        }

        // Antrenör silme işlemi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTrainer(int id)
        {
            var trainer = await _context.Trainers.FindAsync(id);
            if (trainer != null)
            {
                _context.Trainers.Remove(trainer);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Antrenör başarıyla silindi.";
            }
            return RedirectToAction(nameof(Trainers));
        }

        #endregion

        #region Randevu Yönetimi

        // Randevu listesi
        public async Task<IActionResult> Appointments(AppointmentStatus? status = null)
        {
            var query = _context.Appointments
                .Include(a => a.User)
                .Include(a => a.Trainer)
                .Include(a => a.Service)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(a => a.Status == status.Value);
            }

            var appointments = await query
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.AppointmentTime)
                .ToListAsync();

            ViewBag.CurrentStatus = status;
            return View(appointments);
        }

        // Randevu onaylama işlemi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmAppointment(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                appointment.Status = AppointmentStatus.Confirmed;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Randevu onaylandı.";
            }
            return RedirectToLocalRefererOrAppointments();
        }

        // Randevu iptal işlemi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelAppointment(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                appointment.Status = AppointmentStatus.Cancelled;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Randevu iptal edildi.";
            }
            return RedirectToLocalRefererOrAppointments();
        }

        // Randevu tamamlama işlemi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteAppointment(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                appointment.Status = AppointmentStatus.Completed;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Randevu tamamlandı olarak işaretlendi.";
            }
            return RedirectToLocalRefererOrAppointments();
        }

        // Randevuyu beklemede durumuna geri al
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevertAppointmentToPending(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null && appointment.Status == AppointmentStatus.Confirmed)
            {
                appointment.Status = AppointmentStatus.Pending;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Randevu beklemede olarak işaretlendi.";
            }
            return RedirectToLocalRefererOrAppointments();
        }

        // Randevuyu onaylı durumuna geri al
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevertAppointmentToConfirmed(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null && appointment.Status == AppointmentStatus.Completed)
            {
                appointment.Status = AppointmentStatus.Confirmed;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Randevu onaylandı olarak geri alındı.";
            }
            return RedirectToLocalRefererOrAppointments();
        }

        // İptal edilen randevuyu beklemede durumuna geri al
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevertCancelledToPending(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null && appointment.Status == AppointmentStatus.Cancelled)
            {
                appointment.Status = AppointmentStatus.Pending;
                await _context.SaveChangesAsync();
                TempData["Success"] = "İptal edilen randevu beklemede olarak geri alındı.";
            }
            return RedirectToLocalRefererOrAppointments();
        }

        // Yönlendirme yardımcı metodu
        private IActionResult RedirectToLocalRefererOrAppointments()
        {
            var referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referer) && Uri.TryCreate(referer, UriKind.Absolute, out var uri))
            {
                var localPath = uri.PathAndQuery;
                if (Url.IsLocalUrl(localPath))
                {
                    return Redirect(localPath);
                }
            }

            return RedirectToAction(nameof(Appointments));
        }

        #endregion

        #region Üye Yönetimi

        // Üye listesi
        public async Task<IActionResult> Members()
        {
            var members = await _userManager.Users.ToListAsync();
            var memberList = new List<(ApplicationUser User, IList<string> Roles)>();

            foreach (var member in members)
            {
                var roles = await _userManager.GetRolesAsync(member);
                memberList.Add((member, roles));
            }

            return View(memberList);
        }

        // Üye silme işlemi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMember(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                // Önce üyenin tüm randevularını sil
                var appointments = await _context.Appointments
                    .Where(a => a.UserId == id)
                    .ToListAsync();
                
                if (appointments.Any())
                {
                    _context.Appointments.RemoveRange(appointments);
                    await _context.SaveChangesAsync();
                }

                // Sonra üyeyi sil
                var result = await _userManager.DeleteAsync(user);
                
                if (result.Succeeded)
                {
                    TempData["Success"] = "Üye ve ilişkili tüm randevuları başarıyla silindi.";
                }
                else
                {
                    TempData["Error"] = "Üye silinirken bir hata oluştu.";
                }
            }
            return RedirectToAction(nameof(Members));
        }

        #endregion
    }
}
