using FitnessCenter.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Controllers
{
    // Antrenör controller sınıfı
    public class TrainerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TrainerController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Antrenör listesi
        public async Task<IActionResult> Index()
        {
            var trainers = await _context.Trainers
                .Include(t => t.Gym)
                .Include(t => t.TrainerServices)
                    .ThenInclude(ts => ts.Service)
                .Where(t => t.IsActive)
                .ToListAsync();

            return View(trainers);
        }

        // Antrenör detayı
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var trainer = await _context.Trainers
                .Include(t => t.Gym)
                .Include(t => t.TrainerServices)
                    .ThenInclude(ts => ts.Service)
                .Include(t => t.Availabilities)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (trainer == null)
            {
                return NotFound();
            }

            return View(trainer);
        }
    }
}
