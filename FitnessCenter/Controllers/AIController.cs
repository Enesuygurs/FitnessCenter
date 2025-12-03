using FitnessCenter.Data;
using FitnessCenter.Models;
using FitnessCenter.Models.ViewModels;
using FitnessCenter.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FitnessCenter.Controllers
{
    public class AIController : Controller
    {
        private readonly IAIService _aiService;
        private readonly UserManager<ApplicationUser> _userManager;

        public AIController(IAIService aiService, UserManager<ApplicationUser> userManager)
        {
            _aiService = aiService;
            _userManager = userManager;
        }

        // GET: /AI
        public async Task<IActionResult> Index()
        {
            var model = new AIRecommendationViewModel();

            // Kullanıcı giriş yapmışsa profil bilgilerini otomatik doldur
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    model.Height = user.Height;
                    model.Weight = user.Weight;
                    model.Age = user.Age;
                    model.BodyType = user.BodyType;
                    model.Gender = user.Gender;
                }
            }

            return View(model);
        }

        // POST: /AI/GetRecommendation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetRecommendation(AIRecommendationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            try
            {
                var recommendation = await _aiService.GetFitnessRecommendationAsync(model);
                model.Recommendation = recommendation;
            }
            catch (Exception)
            {
                TempData["Error"] = "Öneri alınırken bir hata oluştu. Lütfen daha sonra tekrar deneyin.";
            }

            return View("Index", model);
        }
    }
}
