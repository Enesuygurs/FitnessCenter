using System.ComponentModel.DataAnnotations;

namespace FitnessCenter.Models
{
    // Antrenör-Hizmet çoka çok ilişki tablosu
    public class TrainerService
    {
        public int Id { get; set; }

        [Required]
        public int TrainerId { get; set; }

        [Required]
        public int ServiceId { get; set; }

        // İlişkili tablolar
        public virtual Trainer? Trainer { get; set; }
        public virtual Service? Service { get; set; }
    }
}
