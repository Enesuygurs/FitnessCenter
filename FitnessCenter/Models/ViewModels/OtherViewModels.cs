using System.ComponentModel.DataAnnotations;

namespace FitnessCenter.Models.ViewModels
{
    public class AIRecommendationViewModel
    {
        [Display(Name = "Boy (cm)")]
        [Range(100, 250, ErrorMessage = "Boy 100-250 cm arasında olmalıdır")]
        public int? Height { get; set; }

        [Display(Name = "Kilo (kg)")]
        [Range(30, 300, ErrorMessage = "Kilo 30-300 kg arasında olmalıdır")]
        public double? Weight { get; set; }

        [Display(Name = "Yaş")]
        [Range(10, 100, ErrorMessage = "Yaş 10-100 arasında olmalıdır")]
        public int? Age { get; set; }

        [Display(Name = "Cinsiyet")]
        public string? Gender { get; set; }

        [Display(Name = "Vücut Tipi")]
        public string? BodyType { get; set; }

        [Display(Name = "Fitness Hedefi")]
        [StringLength(500)]
        public string? FitnessGoal { get; set; }

        [Display(Name = "Mevcut Aktivite Seviyesi")]
        public string? ActivityLevel { get; set; }

        [Display(Name = "Sağlık Durumu / Kısıtlamalar")]
        [StringLength(1000)]
        public string? HealthConditions { get; set; }

        // Result
        public string? Recommendation { get; set; }
        public bool HasRecommendation => !string.IsNullOrEmpty(Recommendation);
    }

    public class DashboardViewModel
    {
        public int TotalMembers { get; set; }
        public int TotalTrainers { get; set; }
        public int TotalAppointments { get; set; }
        public int PendingAppointments { get; set; }
        public int TodayAppointments { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public List<Appointment>? RecentAppointments { get; set; }
        public List<Trainer>? TopTrainers { get; set; }
    }

    public class HomeViewModel
    {
        public List<Service>? FeaturedServices { get; set; }
        public List<Trainer>? FeaturedTrainers { get; set; }
        public Gym? Gym { get; set; }
        public int TotalMembers { get; set; }
        public int TotalTrainers { get; set; }
    }
}
