using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static FitnessApi.Models.Exercise;

namespace FitnessApi.Models
{
    public class Workout
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int WorkoutID { get; set; }

        [Required]
        public string WorkoutName { get; set; }

        [Required]
        public float Duration { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public List<Exercise> Exercises { get; set; } = new List<Exercise>();

        [Required]
        [ForeignKey("UserID")]
        public int UserID { get; set; }
    }
}
