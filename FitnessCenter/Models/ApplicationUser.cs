using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace FitnessCenter.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required(ErrorMessage = "Ad alanı zorunludur")]
        [StringLength(50, ErrorMessage = "Ad en fazla 50 karakter olabilir")]
        [Display(Name = "Ad")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Soyad alanı zorunludur")]
        [StringLength(50, ErrorMessage = "Soyad en fazla 50 karakter olabilir")]
        [Display(Name = "Soyad")]
        public string LastName { get; set; } = string.Empty;

        [Display(Name = "Kayıt Tarihi")]
        public DateTime RegistrationDate { get; set; } = DateTime.Now;

        [Display(Name = "Profil Fotoğrafı")]
        public string? ProfileImageUrl { get; set; }

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

        [Display(Name = "Ad Soyad")]
        public string FullName => $"{FirstName} {LastName}";

        // Navigation properties
        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}
