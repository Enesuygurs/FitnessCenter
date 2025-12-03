using System.Diagnostics;
using FitnessCenter.Data;
using FitnessCenter.Models;
using FitnessCenter.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Controllers;

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

    public async Task<IActionResult> Index()
    {
        var gym = await _context.Gyms.FirstOrDefaultAsync();
        var featuredServices = await _context.Services
            .Where(s => s.IsActive)
            .Take(6)
            .ToListAsync();
        var featuredTrainers = await _context.Trainers
            .Include(t => t.TrainerServices)
                .ThenInclude(ts => ts.Service)
            .Where(t => t.IsActive)
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

    public IActionResult About()
    {
        return View();
    }

    public IActionResult Contact()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
