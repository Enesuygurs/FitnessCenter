using System.ComponentModel.DataAnnotations;

namespace FitnessCenter.Models
{
    public enum AppointmentStatus
    {
        [Display(Name = "Beklemede")]
        Pending = 0,
        
        [Display(Name = "Onaylandı")]
        Confirmed = 1,
        
        [Display(Name = "İptal Edildi")]
        Cancelled = 2,
        
        [Display(Name = "Tamamlandı")]
        Completed = 3
    }

    public class Appointment
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Randevu tarihi zorunludur")]
        [Display(Name = "Randevu Tarihi")]
        [DataType(DataType.Date)]
        public DateTime AppointmentDate { get; set; }

        [Required(ErrorMessage = "Randevu saati zorunludur")]
        [Display(Name = "Randevu Saati")]
        [DataType(DataType.Time)]
        public TimeSpan AppointmentTime { get; set; }

        [Display(Name = "Bitiş Saati")]
        [DataType(DataType.Time)]
        public TimeSpan EndTime { get; set; }

        [Display(Name = "Durum")]
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

        [Display(Name = "Notlar")]
        [StringLength(1000, ErrorMessage = "Notlar en fazla 1000 karakter olabilir")]
        public string? Notes { get; set; }

        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Toplam Ücret")]
        [DataType(DataType.Currency)]
        public decimal TotalPrice { get; set; }

        // Yabancı anahtarlar
        [Required]
        [Display(Name = "Üye")]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Antrenör")]
        public int TrainerId { get; set; }

        [Required]
        [Display(Name = "Hizmet")]
        public int ServiceId { get; set; }

        // İlişkili tablolar
        public virtual ApplicationUser? User { get; set; }
        public virtual Trainer? Trainer { get; set; }
        public virtual Service? Service { get; set; }

        [Display(Name = "Randevu Tarihi ve Saati")]
        public DateTime FullDateTime => AppointmentDate.Date.Add(AppointmentTime);
    }
}
