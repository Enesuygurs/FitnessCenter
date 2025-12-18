using FitnessCenter.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Controllers
{
    // Hizmet controller sınıfı
    public class ServiceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ServiceController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Hizmet listesi
        public async Task<IActionResult> Index()
        {
            var services = await _context.Services
                .Include(s => s.Gym)
                .Include(s => s.TrainerServices)
                    .ThenInclude(ts => ts.Trainer)
                .Where(s => s.IsActive)
                .ToListAsync();

            return View(services);
        }

        // Hizmet detayı
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var service = await _context.Services
                .Include(s => s.Gym)
                .Include(s => s.TrainerServices)
                    .ThenInclude(ts => ts.Trainer)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (service == null)
            {
                return NotFound();
            }

            return View(service);
        }
    }
}
