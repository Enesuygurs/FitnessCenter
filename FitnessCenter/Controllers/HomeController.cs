using System.Diagnostics;
using FitnessCenter.Data;
using FitnessCenter.Models;
using FitnessCenter.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Controllers;

// Ana sayfa controller sınıfı
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(
        ILogger<HomeController> logger, 
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
    }

    // Ana sayfa
    public async Task<IActionResult> Index()
    {
        // Aktif spor salonunu getir
        var gym = await _context.Gyms
            .Where(g => g.IsActive)
            .OrderBy(g => g.Id)
            .FirstOrDefaultAsync();

        // Öne çıkan hizmetleri getir
        var featuredServices = await _context.Services
            .Where(s => s.IsActive)
            .OrderBy(s => s.Id)
            .Take(6)
            .ToListAsync();

        // Öne çıkan antrenörleri getir
        var featuredTrainers = await _context.Trainers
            .Include(t => t.TrainerServices)
                .ThenInclude(ts => ts.Service)
            .Where(t => t.IsActive)
            .OrderBy(t => t.Id)
            .Take(4)
            .ToListAsync();

        var model = new HomeViewModel
        {
            Gym = gym,
            FeaturedServices = featuredServices,
            FeaturedTrainers = featuredTrainers,
            TotalMembers = await _userManager.Users.CountAsync(),
            TotalTrainers = await _context.Trainers.CountAsync(t => t.IsActive)
        };

        return View(model);
    }

    // Hakkımızda sayfası
    public IActionResult About()
    {
        return View();
    }

    // İletişim sayfası
    public IActionResult Contact()
    {
        return View();
    }

    // Gizlilik politikası sayfası
    public IActionResult Privacy()
    {
        return View();
    }

    // Hata sayfası
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
