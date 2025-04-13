using Microsoft.Data.SqlClient;
using FitnessApi.Models;
using TestApp.Models;
using Newtonsoft.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Security.Cryptography.Xml;
using System.Text.RegularExpressions;

namespace TestApp.Services
{

    public interface IMuscleGroupService
    {
        Task<List<MuscleGroup>> GetGroupNamesAsync(int exerciseID);
        Task<string> AddMuscleGroupToExercise(int muscleGroupID,  int ExerciseID);
        Task<string> RemoveMuscleGroupFromExercise(int muscleGroupID, int ExerciseID);
    }
    public class MuscleGroupService : IMuscleGroupService
    {
        private readonly IConfiguration _configuration;

        public MuscleGroupService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<List<MuscleGroup>> GetGroupNamesAsync(int exerciseID)
        {
            List<MuscleGroup> muscleGroups = new List<MuscleGroup>();
            Response response = new Response();

            try
            {
                string query = @"SELECT mg.MuscleGroupID, mg.MuscleGroupName FROM MuscleGroups mg JOIN ExerciseMuscleGroups emg ON mg.MuscleGroupID = emg.MuscleGroupID WHERE emg.ExerciseID = @ExerciseID";

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
                                MuscleGroup muscleGroup = new MuscleGroup
                                {
                                    MuscleGroupID = reader.GetInt32(reader.GetOrdinal("MuscleGroupID")),
                                    MuscleGroupName = reader.GetString(reader.GetOrdinal("MuscleGroupName"))
                                };
                                muscleGroups.Add(muscleGroup);
                            }
                        }
                    }
                    return muscleGroups;
                }

            }
            catch (Exception ex)
            {
                return muscleGroups;
            }
        }

        public async Task<string> AddMuscleGroupToExercise(int muscleGroupID, int ExerciseID)
        {
            Response response = new Response();
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();
                    string checkQuery = @"SELECT COUNT(*) FROM ExerciseMuscleGroups WHERE ExerciseID = @ExerciseID AND MuscleGroupID = @MuscleGroupID";

                    int count = 0;

                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@ExerciseID", ExerciseID);
                        checkCmd.Parameters.AddWithValue("@MuscleGroupID", muscleGroupID);
                        count = (int)await checkCmd.ExecuteScalarAsync();
                    }
                    if (count > 0)
                    {
                        response.StatusCode = 500;
                        response.ErrorMessage = "Muscle Group already assigned to this exercise";
                        return JsonConvert.SerializeObject(response);
                    }

                    string insertQuery = @"INSERT INTO ExerciseMuscleGroups (ExerciseID, MuscleGroupID) VALUES (@ExerciseID, @MuscleGroupID)";

                    using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@ExerciseID", ExerciseID);
                        cmd.Parameters.AddWithValue("@MuscleGroupID", muscleGroupID);
                        await cmd.ExecuteNonQueryAsync();
                    }
                    string returnQuery = "SELECT * FROM MuscleGroups WHERE MuscleGroupID = @MuscleGroupID";

                    using (SqlCommand returnCmd = new SqlCommand(returnQuery, conn))
                    {
                        returnCmd.Parameters.AddWithValue("@MuscleGroupID", muscleGroupID);
                        using (SqlDataReader reader = await returnCmd.ExecuteReaderAsync())
                        {
                            if (!reader.HasRows)
                            {
                                response.StatusCode = 500;
                                response.ErrorMessage = "Muscle Group Not Found";
                                return JsonConvert.SerializeObject(response);
                            }

                            while (await reader.ReadAsync())
                            {
                                MuscleGroup muscleGroup = new MuscleGroup
                                {
                                    MuscleGroupID = reader.GetInt32(reader.GetOrdinal("MuscleGroupID")),
                                    MuscleGroupName = reader.GetString(reader.GetOrdinal("MuscleGroupName"))
                                };
                                return JsonConvert.SerializeObject(muscleGroup);

                            }

                        }
                        return JsonConvert.SerializeObject(muscleGroupID);

                    }
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.ErrorMessage = ex.Message;
                return JsonConvert.SerializeObject(response);
            }

        }

        public async Task<string> RemoveMuscleGroupFromExercise(int muscleGroupID, int ExerciseID)
        {
            Response response = new Response();
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();
                    string query = "DELETE FROM ExerciseMuscleGroups WHERE ExerciseID = @ExerciseID AND MuscleGroupID = @MuscleGroupID";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ExerciseID", ExerciseID);
                        cmd.Parameters.AddWithValue("@MuscleGroupID", muscleGroupID);
                        await cmd.ExecuteNonQueryAsync();
                    }
                    response.StatusCode = 200;
                    response.ErrorMessage = "Muscle Group deleted successfully";
                    return JsonConvert.SerializeObject(response);
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.ErrorMessage = ex.Message;
                return JsonConvert.SerializeObject(response);
            }
        }
    }
}
