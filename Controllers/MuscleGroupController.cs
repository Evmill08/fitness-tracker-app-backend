using FitnessApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Data;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TestApp.Models;
using TestApp.Services;

namespace TestApp.Controllers

{
    [Route("api/[controller]")]
    [ApiController]
    public class MuscleGroupController : Controller
    {
        public readonly IConfiguration _configuration;
        public MuscleGroupController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        [Route("GetAllMuscleGroups")]
        public async Task<string> GetAllMuscleGroups()
        {
            Response response = new Response();
            List<MuscleGroup> muscleGroups = new List<MuscleGroup>();
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();
                    string query = "SELECT MuscleGroupID, MuscleGroupName FROM MuscleGroups ORDER BY MuscleGroupName";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                MuscleGroup group = new MuscleGroup
                                {
                                    MuscleGroupID = reader.GetInt32(reader.GetOrdinal("MuscleGroupID")),
                                    MuscleGroupName = reader.GetString(reader.GetOrdinal("MuscleGroupName"))
                                };
                                muscleGroups.Add(group);

                            }
                            return JsonConvert.SerializeObject(muscleGroups);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.ErrorMessage = ex.Message;
            }
            return JsonConvert.SerializeObject(response);
        }

        [HttpGet]
        [Route("GetMuscleGroupsByExerciseID")]
        public async Task<string> GetMuscleGroupsByExerciseID(int exerciseID)
        {
            Response response = new Response();
            List<MuscleGroup> muscleGroups = new List<MuscleGroup>();
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();
                    string query = @"SELECT mg.MuscleGroupID, mg.MuscleGroupName FROM MuscleGroups mg JOIN ExerciseMuscleGroups emg ON mg.MuscleGroupID = emg.MuscleGroupID WHERE emg.ExerciseID = @ExerciseID";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ExerciseID", exerciseID);

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (!reader.HasRows)
                            {
                                response.StatusCode = 300;
                                response.ErrorMessage = "No sets found for this exercise";
                                return JsonConvert.SerializeObject(response);
                            }

                            while (await reader.ReadAsync())
                            {
                                MuscleGroup group = new MuscleGroup
                                {
                                    MuscleGroupID = reader.GetInt32(reader.GetOrdinal("MuscleGroupID")),
                                    MuscleGroupName = reader.GetString(reader.GetOrdinal("MuscleGroupName"))
                                };
                                muscleGroups.Add(group);
                            }
                            return JsonConvert.SerializeObject(muscleGroups);
                        }
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

        [HttpPost]
        [Route("AddMuscleGroup")]
        public async Task<string> AddMuscleGroup(string muscleGroupName)
        {
            Response response = new Response();
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();
                    string query = "SELECT COUNT(*) FROM MuscleGroups WHERE MuscleGroupName = @MuscleGroupName";
                    int count = 0;

                    using (SqlCommand checkCmd = new SqlCommand(query, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@MuscleGroupName", muscleGroupName);
                        count = (int)await checkCmd.ExecuteScalarAsync();
                    }

                    if (count > 0)
                    {
                        response.StatusCode = 500;
                        response.ErrorMessage = "Muscle Group already exists";
                        return JsonConvert.SerializeObject(response);
                    }

                    string insertQuery = @"INSERT INTO MuscleGroups (MuscleGroupName) VALUES (@MuscleGroupName)";

                    using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@MuscleGroupName", muscleGroupName);
                        var newID = await cmd.ExecuteScalarAsync();

                        MuscleGroup muscleGroup = new MuscleGroup
                        {
                            MuscleGroupID = Convert.ToInt32(newID),
                            MuscleGroupName = muscleGroupName
                        };
                        return JsonConvert.SerializeObject(muscleGroup);
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

        [HttpPost]
        [Route("DeleteMuscleGroup")]
        public async Task<string> DeleteMuscleGroup(int muscleGroupID)
        {
            Response response = new Response();
            try
            {
                string query = "DELETE FROM MuscleGroups WHERE MuscleGroupID = @MuscleGroupID";

                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@MuscleGroupID", muscleGroupID);

                        await cmd.ExecuteNonQueryAsync();
                    }

                    response.StatusCode = 200;
                    response.ErrorMessage = $"Muscle Group {muscleGroupID} deleted successfully";
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

        [HttpPost]
        [Route("AddMuscleGroupToExercise")]
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

        [HttpPost]
        [Route("RemoveMuscleGroupFromExercise")]
        public async Task<string> RemoveMuscleGroupFromExercise(int muscleGroupID, int exerciseID)
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
                        cmd.Parameters.AddWithValue("@ExerciseID", exerciseID);
                        cmd.Parameters.AddWithValue("@MuscleGroupID", muscleGroupID);
                        await cmd.ExecuteNonQueryAsync();
                    }
                    response.StatusCode=200;
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
