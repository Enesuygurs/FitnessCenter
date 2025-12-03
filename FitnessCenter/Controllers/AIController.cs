using FitnessCenter.Models.ViewModels;
using FitnessCenter.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitnessCenter.Controllers
{
    public class AIController : Controller
    {
        private readonly IAIService _aiService;

        public AIController(IAIService aiService)
        {
            _aiService = aiService;
        }

        // GET: /AI
        public IActionResult Index()
        {
            return View(new AIRecommendationViewModel());
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
