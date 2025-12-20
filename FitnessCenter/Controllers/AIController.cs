using FitnessCenter.Data;
using FitnessCenter.Models;
using FitnessCenter.Models.ViewModels;
using FitnessCenter.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FitnessCenter.Controllers
{
    // AI fitness önerileri controller sınıfı
    public class AIController : Controller
    {
        private readonly IAIService _aiService;
        private readonly UserManager<ApplicationUser> _userManager;

        public AIController(IAIService aiService, UserManager<ApplicationUser> userManager)
        {
            _aiService = aiService;
            _userManager = userManager;
        }

        // AI öneri sayfası
        public async Task<IActionResult> Index()
        {
            var model = new AIRecommendationViewModel();

            // Giriş yapmış kullanıcının profil bilgilerini doldur
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

        // AI'dan öneri al
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
                // Fotoğraf varsa Base64'e çevir (görüntüleme için)
                if (model.Photo != null && model.Photo.Length > 0)
                {
                    using (var ms = new MemoryStream())
                    {
                        await model.Photo.CopyToAsync(ms);
                        var fileBytes = ms.ToArray();
                        model.UploadedImageBase64 = Convert.ToBase64String(fileBytes);
                        
                        // Stream pozisyonunu başa al (servis de kullanabilsin diye)
                        model.Photo.OpenReadStream().Position = 0;
                    }
                }

                // Fotoğraf ve metin önerisi al
                var (textRecommendation, imageUrl) = await _aiService.GetFitnessRecommendationAsync(model);
                
                model.Recommendation = textRecommendation;
                model.GeneratedImageUrl = imageUrl;
            }
            catch (Exception)
            {
                TempData["Error"] = "Öneri alınırken bir hata oluştu. Lütfen daha sonra tekrar deneyin.";
            }

            return View("Index", model);
        }
    }
}
