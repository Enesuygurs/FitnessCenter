using System.ComponentModel.DataAnnotations;

namespace FitnessCenter.Models
{
    public class Trainer
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Ad zorunludur")]
        [StringLength(50, ErrorMessage = "Ad en fazla 50 karakter olabilir")]
        [Display(Name = "Ad")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Soyad zorunludur")]
        [StringLength(50, ErrorMessage = "Soyad en fazla 50 karakter olabilir")]
        [Display(Name = "Soyad")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-posta zorunludur")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
        [Display(Name = "E-posta")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Telefon zorunludur")]
        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz")]
        [Display(Name = "Telefon")]
        public string Phone { get; set; } = string.Empty;

        [Display(Name = "Uzmanlık Alanları")]
        [StringLength(500, ErrorMessage = "Uzmanlık alanları en fazla 500 karakter olabilir")]
        public string? Specializations { get; set; }

        [Display(Name = "Biyografi")]
        [StringLength(2000, ErrorMessage = "Biyografi en fazla 2000 karakter olabilir")]
        public string? Biography { get; set; }

        [Display(Name = "Profil Fotoğrafı")]
        public string? ProfileImageUrl { get; set; }

        [Required(ErrorMessage = "Çalışma başlangıç saati zorunludur")]
        [Display(Name = "Çalışma Başlangıç Saati")]
        public TimeSpan WorkStartTime { get; set; }

        [Required(ErrorMessage = "Çalışma bitiş saati zorunludur")]
        [Display(Name = "Çalışma Bitiş Saati")]
        public TimeSpan WorkEndTime { get; set; }

        [Display(Name = "Aktif")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Deneyim (Yıl)")]
        [Range(0, 50, ErrorMessage = "Deneyim 0-50 yıl arasında olmalıdır")]
        public int? ExperienceYears { get; set; }

        // Yabancı anahtar
        [Required]
        [Display(Name = "Spor Salonu")]
        public int GymId { get; set; }

        // İlişkili tablolar
        public virtual Gym? Gym { get; set; }
        public virtual ICollection<TrainerService> TrainerServices { get; set; } = new List<TrainerService>();
        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public virtual ICollection<TrainerAvailability> Availabilities { get; set; } = new List<TrainerAvailability>();

        [Display(Name = "Ad Soyad")]
        public string FullName => $"{FirstName} {LastName}";
    }
}
