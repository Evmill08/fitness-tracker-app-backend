using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TestApp.Models;
using static FitnessApi.Models.ExSet;

namespace FitnessApi.Models
{
    public class Exercise
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ExerciseID { get; set; }

        [Required]
        public string ExerciseName { get; set; }

        public string ExerciseType { get; set; }

        [Required]
        public List<MuscleGroup> MuscleGroups { get; set; } = new List<MuscleGroup>();
        [Required]
        public List<ExSet> Sets { get; set; } = new List<ExSet>();

        [Required]
        public float RestTime { get; set; }

        [Required]
        [ForeignKey("WorkoutID")]
        public int WorkoutID { get; set; }

    }
}
