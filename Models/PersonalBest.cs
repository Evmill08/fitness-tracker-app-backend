using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FitnessApi.Models
{
    public class PersonalBest
    {
        [Key]
        [Required]
        public int ExerciseID { get; set; }

        [Required]
        public string ExerciseName { get; set; }

        [Required]
        public float Weight { get; set; }

        [Required]
        public int Reps { get; set; }

        [Required]
        public DateTime DateSet { get; set; }

        [Required]
        [ForeignKey("UserID")]
        public int UserID { get; set; }
    }
}
