using Azure;
using FitnessApi.Models;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;


namespace TestApp.Services
{

    public interface IWorkoutService
    {
        Task<List<Workout>> GetUserWorkoutsAsync(int userID);
    }
    public class WorkoutService : IWorkoutService
    {
        private readonly IConfiguration _configuration;
        private readonly IExerciseService _exerciseService;

        public WorkoutService(IConfiguration configuration, IExerciseService exerciseService)
        {
            _configuration = configuration;
            _exerciseService = exerciseService;
        }

        public async Task<List<Workout>> GetUserWorkoutsAsync(int userID)
        {

            List<Workout> workouts = new List<Workout>();

            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();
                    string query = "SELECT * FROM Workouts WHERE UserID = @UserID";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserID", userID);

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {


                            while (await reader.ReadAsync())
                            {
                                Workout workout = new Workout
                                {
                                    WorkoutID = reader.GetInt32(reader.GetOrdinal("WorkoutID")),
                                    WorkoutName = reader.GetString(reader.GetOrdinal("WorkoutName")),
                                    Duration = Convert.ToSingle(reader.GetValue(reader.GetOrdinal("Duration"))),
                                    Date = reader.GetDateTime(reader.GetOrdinal("WorkoutDate")),
                                    UserID = reader.GetInt32(reader.GetOrdinal("UserID")),
                                };
                                workouts.Add(workout);
                                workout.Exercises = await _exerciseService.GetExerciseListAsync(workout.WorkoutID);
                            }
                            return workouts;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return workouts;
            }
        }
    }
}
