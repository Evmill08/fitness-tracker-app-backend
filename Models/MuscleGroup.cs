using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FitnessApi.Models;


namespace TestApp.Models
{
    public class MuscleGroup
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MuscleGroupID { get; set; }

        [Required]
        public string MuscleGroupName { get; set; }

    }
}
