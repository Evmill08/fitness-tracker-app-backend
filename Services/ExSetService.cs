using FitnessApi.Models;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;

namespace TestApp.Services
{

    public interface IExSetService
    {
        Task<List<ExSet>> GetExSetsAsync(int exerciseID);
        Task<string> RemoveSetFromExercise(int ExSetID, int exerciseID);
        Task<string> AddSetToExercise(ExSet set, int exerciseID);
    }
    public class ExSetService : IExSetService
    {
        private readonly IConfiguration _configuration;

        public ExSetService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<List<ExSet>> GetExSetsAsync(int exerciseID)
        {
            List<ExSet> exerciseSets = new List<ExSet>();

            try
            {
                string query = @"SELECT es.* FROM ExerciseSets es INNER JOIN Exercises e ON es.ExerciseID = e.ExerciseID WHERE e.ExerciseID = @ExerciseID";

                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ExerciseID", exerciseID);

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                ExSet exSet = new ExSet
                                {
                                    ExSetID = reader.GetInt32(reader.GetOrdinal("ExSetID")),
                                    Weight = Convert.ToSingle(reader.GetValue(reader.GetOrdinal("Weight"))),
                                    Reps = reader.GetInt32(reader.GetOrdinal("Reps")),
                                    DateStarted = reader.GetDateTime(reader.GetOrdinal("DateStarted")),
                                    RPE = reader.GetInt32(reader.GetOrdinal("RPE")),
                                    ExerciseID = reader.GetInt32(reader.GetOrdinal("ExerciseID"))

                                };
                                exerciseSets.Add(exSet);
                            }

                        }
                    }
                    return exerciseSets;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving sets: {ex.Message}");
            }
        }

        public async Task<string> RemoveSetFromExercise(int ExSetID, int ExerciseID)
        {
            Response response = new Response();
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    string query = "DELETE FROM ExerciseSets WHERE ExSetID = @ExSetID AND ExerciseID = @ExerciseID";
                    await conn.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ExSetID", ExSetID);
                        cmd.Parameters.AddWithValue("@ExerciseID", ExerciseID);
                        await cmd.ExecuteNonQueryAsync();
                    }
                    response.StatusCode = 200;
                    response.ErrorMessage = "Set removed successfully";
                    return JsonConvert.SerializeObject(response);
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.ErrorMessage = ex.Message;
            }
            return JsonConvert.SerializeObject(response);
        }

        public async Task<string> AddSetToExercise(ExSet set, int exerciseID)
        {
            Response response = new Response();
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    string query = @"INSERT INTO ExerciseSets (ExerciseID, Weight, Reps, RPE, DateStarted)
                                     VALUES (@ExerciseID, @Weight, @Reps, @RPE, @DateStarted);
                                     SELECT SCOPE_IDENTITY();";
                    await conn.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ExerciseID", set.ExerciseID);
                        cmd.Parameters.AddWithValue("@Weight", set.Weight);
                        cmd.Parameters.AddWithValue("@Reps", set.Reps);
                        cmd.Parameters.AddWithValue("@RPE", set.RPE);
                        cmd.Parameters.AddWithValue("@DateStarted", set.DateStarted);
                        set.ExSetID = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    }

                    return JsonConvert.SerializeObject(set);
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.ErrorMessage = ex.Message;
            }
            return JsonConvert.SerializeObject(response);
        }
    }
}

