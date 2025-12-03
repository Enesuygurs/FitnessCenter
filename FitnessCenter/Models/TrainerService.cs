using System.ComponentModel.DataAnnotations;

namespace FitnessCenter.Models
{
    // Many-to-Many relationship between Trainer and Service
    public class TrainerService
    {
        public int Id { get; set; }

        [Required]
        public int TrainerId { get; set; }

        [Required]
        public int ServiceId { get; set; }

        // Navigation properties
        public virtual Trainer? Trainer { get; set; }
        public virtual Service? Service { get; set; }
    }
}
