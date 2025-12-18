using System.ComponentModel.DataAnnotations;

namespace FitnessCenter.Models
{
    public class TrainerAvailability
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Antrenör")]
        public int TrainerId { get; set; }

        [Required]
        [Display(Name = "Gün")]
        public DayOfWeek DayOfWeek { get; set; }

        [Required]
        [Display(Name = "Başlangıç Saati")]
        public TimeSpan StartTime { get; set; }

        [Required]
        [Display(Name = "Bitiş Saati")]
        public TimeSpan EndTime { get; set; }

        [Display(Name = "Müsait")]
        public bool IsAvailable { get; set; } = true;

        // İlişkili tablo
        public virtual Trainer? Trainer { get; set; }
    }
}
