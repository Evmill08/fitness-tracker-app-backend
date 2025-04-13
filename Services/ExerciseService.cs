using Azure;
using FitnessApi.Models;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using TestApp.Models;


namespace TestApp.Services
{

    public interface IExerciseService
    {
        Task<List<Exercise>> GetExerciseListAsync(int workoutID);
    }
    public class ExerciseService : IExerciseService
    {
        private readonly IConfiguration _configuration;
        private readonly IMuscleGroupService _groupService;
        private readonly IExSetService _exSetService;

        public ExerciseService(IConfiguration configuration, IMuscleGroupService groupService, IExSetService exSetService)
        {
            _configuration = configuration;
            _groupService = groupService;
            _exSetService = exSetService;
        }

        public async Task<List<Exercise>> GetExerciseListAsync(int workoutID)
        {
            List<Exercise> exercises = new List<Exercise>();

            try
            {
                string query = "SELECT * FROM Exercises WHERE WorkoutID = @WorkoutID";

                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@WorkoutID", workoutID);

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (!reader.HasRows)
                            {
                                return exercises;
                            }

                            while (await reader.ReadAsync())
                            {
                                Exercise exercise = new Exercise
                                {
                                    ExerciseID = reader.GetInt32(reader.GetOrdinal("ExerciseID")),
                                    ExerciseName = reader.GetString(reader.GetOrdinal("ExerciseName")),
                                    WorkoutID = reader.GetInt32(reader.GetOrdinal("WorkoutID")),
                                    RestTime = Convert.ToSingle(reader.GetValue(reader.GetOrdinal("RestTime"))),
                                    ExerciseType = reader.GetString(reader.GetOrdinal("ExerciseType")),
                                };
                                List<MuscleGroup> muscleGroups = await _groupService.GetGroupNamesAsync(exercise.ExerciseID);
                                exercise.MuscleGroups.AddRange(muscleGroups);
                                List<ExSet> exSets = await _exSetService.GetExSetsAsync(exercise.ExerciseID);
                                exercise.Sets.AddRange(exSets);
                                exercises.Add(exercise);
                                
                            }
                            return exercises;
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                return new List<Exercise>();
            }
        }
    }
}
