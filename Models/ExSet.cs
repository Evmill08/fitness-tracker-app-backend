using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FitnessApi.Models
{
    public class ExSet
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ExSetID { get; set; }

        [Required]
        public float Weight { get; set; }

        [Required]
        public int Reps { get; set; }

        [Required]
        public DateTime DateStarted { get; set; }

        [Required]
        public int RPE { get; set; }

        [Required]
        [ForeignKey("ExerciseID")]
        public int ExerciseID { get; set; }
    }
}
