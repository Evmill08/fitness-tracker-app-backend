using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static FitnessApi.Models.Workout;
using static FitnessApi.Models.PersonalBest;

namespace FitnessApi.Models
{
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserID { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string Email { get; set; }

        public float Weight { get; set; }
        public float Height { get; set; }

        public List<Workout> WorkoutHistory { get; set; } = new List<Workout>();

        public List<PersonalBest> PersonalBests { get; set; } = new List<PersonalBest>();
    }
}
