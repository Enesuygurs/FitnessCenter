using FitnessCenter.Data;
using FitnessCenter.Models;
using FitnessCenter.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Controllers
{
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

        // GET: /Admin
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

        #region Gym Management

        // GET: /Admin/Gyms
        public async Task<IActionResult> Gyms()
        {
            var gyms = await _context.Gyms.ToListAsync();
            return View(gyms);
        }

        // GET: /Admin/CreateGym
        public IActionResult CreateGym()
        {
            return View();
        }

        // POST: /Admin/CreateGym
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

        // GET: /Admin/EditGym/5
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

        // POST: /Admin/EditGym/5
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

        // POST: /Admin/DeleteGym/5
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

        #region Service Management

        // GET: /Admin/Services
        public async Task<IActionResult> Services()
        {
            var services = await _context.Services
                .Include(s => s.Gym)
                .ToListAsync();
            return View(services);
        }

        // GET: /Admin/CreateService
        public async Task<IActionResult> CreateService()
        {
            ViewBag.Gyms = await _context.Gyms.Where(g => g.IsActive).ToListAsync();
            return View();
        }

        // POST: /Admin/CreateService
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

        // GET: /Admin/EditService/5
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

        // POST: /Admin/EditService/5
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

        // POST: /Admin/DeleteService/5
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

        #region Trainer Management

        // GET: /Admin/Trainers
        public async Task<IActionResult> Trainers()
        {
            var trainers = await _context.Trainers
                .Include(t => t.Gym)
                .Include(t => t.TrainerServices)
                    .ThenInclude(ts => ts.Service)
                .ToListAsync();
            return View(trainers);
        }

        // GET: /Admin/CreateTrainer
        public async Task<IActionResult> CreateTrainer()
        {
            ViewBag.Gyms = await _context.Gyms.Where(g => g.IsActive).ToListAsync();
            ViewBag.Services = await _context.Services.Where(s => s.IsActive).ToListAsync();
            return View();
        }

        // POST: /Admin/CreateTrainer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTrainer(Trainer trainer, int[] selectedServices)
        {
            if (ModelState.IsValid)
            {
                _context.Trainers.Add(trainer);
                await _context.SaveChangesAsync();

                // Add trainer services
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

        // GET: /Admin/EditTrainer/5
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

        // POST: /Admin/EditTrainer/5
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

                    // Update trainer services
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

        // POST: /Admin/DeleteTrainer/5
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

        #region Appointment Management

        // GET: /Admin/Appointments
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

        // POST: /Admin/ConfirmAppointment/5
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
            return RedirectToAction(nameof(Appointments));
        }

        // POST: /Admin/CancelAppointment/5
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
            return RedirectToAction(nameof(Appointments));
        }

        // POST: /Admin/CompleteAppointment/5
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
            return RedirectToAction(nameof(Appointments));
        }

        #endregion

        #region Member Management

        // GET: /Admin/Members
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

        // POST: /Admin/DeleteMember
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMember(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
                TempData["Success"] = "Üye başarıyla silindi.";
            }
            return RedirectToAction(nameof(Members));
        }

        #endregion
    }
}
