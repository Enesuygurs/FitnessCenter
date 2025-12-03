using System.ComponentModel.DataAnnotations;

namespace FitnessCenter.Models
{
    public class Service
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Hizmet adı zorunludur")]
        [StringLength(100, ErrorMessage = "Hizmet adı en fazla 100 karakter olabilir")]
        [Display(Name = "Hizmet Adı")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Açıklama")]
        [StringLength(1000, ErrorMessage = "Açıklama en fazla 1000 karakter olabilir")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Süre zorunludur")]
        [Range(15, 480, ErrorMessage = "Süre 15-480 dakika arasında olmalıdır")]
        [Display(Name = "Süre (Dakika)")]
        public int DurationMinutes { get; set; }

        [Required(ErrorMessage = "Ücret zorunludur")]
        [Range(0, 10000, ErrorMessage = "Ücret 0-10000 TL arasında olmalıdır")]
        [Display(Name = "Ücret (TL)")]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        [Display(Name = "Kategori")]
        public string? Category { get; set; }

        [Display(Name = "Resim URL")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Aktif")]
        public bool IsActive { get; set; } = true;

        // Foreign Key
        [Required]
        [Display(Name = "Spor Salonu")]
        public int GymId { get; set; }

        // Navigation properties
        public virtual Gym? Gym { get; set; }
        public virtual ICollection<TrainerService> TrainerServices { get; set; } = new List<TrainerService>();
        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}
