using System.ComponentModel.DataAnnotations;

namespace FitnessCenter.Models
{
    public class Gym
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Salon adı zorunludur")]
        [StringLength(100, ErrorMessage = "Salon adı en fazla 100 karakter olabilir")]
        [Display(Name = "Salon Adı")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Adres zorunludur")]
        [StringLength(500, ErrorMessage = "Adres en fazla 500 karakter olabilir")]
        [Display(Name = "Adres")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Telefon zorunludur")]
        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz")]
        [Display(Name = "Telefon")]
        public string Phone { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
        [Display(Name = "E-posta")]
        public string? Email { get; set; }

        [Display(Name = "Açıklama")]
        [StringLength(1000, ErrorMessage = "Açıklama en fazla 1000 karakter olabilir")]
        public string? Description { get; set; }

        [Display(Name = "Resim URL")]
        public string? ImageUrl { get; set; }

        [Required(ErrorMessage = "Açılış saati zorunludur")]
        [Display(Name = "Açılış Saati")]
        public TimeSpan OpeningTime { get; set; }

        [Required(ErrorMessage = "Kapanış saati zorunludur")]
        [Display(Name = "Kapanış Saati")]
        public TimeSpan ClosingTime { get; set; }

        [Display(Name = "Aktif")]
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<Service> Services { get; set; } = new List<Service>();
        public virtual ICollection<Trainer> Trainers { get; set; } = new List<Trainer>();
    }
}
