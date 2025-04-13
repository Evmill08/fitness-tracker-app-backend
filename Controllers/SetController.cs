using FitnessApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Data;
using System.Runtime.CompilerServices;
using TestApp.Services;

namespace TestApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SetController : Controller
    {
        public readonly IConfiguration _configuration;
        public readonly IMuscleGroupService _muscleGroupService;
        public readonly IExSetService _exSetService;

        public SetController(IConfiguration configuration, IMuscleGroupService muscleGroupService, IExSetService exSetService)
        {
            _configuration = configuration;
            _muscleGroupService = muscleGroupService;
            _exSetService = exSetService;
        }

        [HttpGet]
        [Route("GetSetByID")]
        public async Task<string> GetSetByID (int setID)
        {
            Response response = new Response();
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();
                    string query = "SELECT * FROM ExerciseSets WHERE ExSetID = @ExSetID";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ExSetID", setID);

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (!reader.HasRows)
                            {
                                response.StatusCode = 500;
                                response.ErrorMessage = "No set found for this ID";
                                return JsonConvert.SerializeObject(response);
                            }
                            await reader.ReadAsync();
                            ExSet set = new ExSet
                            {
                                ExSetID = reader.GetInt32(reader.GetOrdinal("ExSetID")),
                                Weight = Convert.ToSingle(reader.GetValue(reader.GetOrdinal("Weight"))),
                                Reps = reader.GetInt32(reader.GetOrdinal("Reps")),
                                DateStarted = reader.GetDateTime(reader.GetOrdinal("DateStarted")),
                                RPE = reader.GetInt32(reader.GetOrdinal("RPE")),
                                ExerciseID = reader.GetInt32(reader.GetOrdinal("ExerciseID")),
                            };
                            return JsonConvert.SerializeObject(set);
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
        [Route("AddSetToExercise")]
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
            }catch (Exception ex)
            {
                response.StatusCode = 500;
                response.ErrorMessage = ex.Message;
            }
            return JsonConvert.SerializeObject(response);
        }

        [HttpPost]
        [Route("DeleteSetFromExercise")]
        public async Task<string> DeleteSetFromExercise(int ExSetID, int ExerciseID)
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

        [HttpGet]
        [Route("GetAllSets")]
        public async Task<string> GetAllSets()
        {
            Response response = new Response();
            List<ExSet> exSetList = new List<ExSet>();
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();
                    string query = "SELECT * FROM ExerciseSets";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (!reader.HasRows)
                            {
                                response.StatusCode = 200;
                                response.ErrorMessage = "No sets found";
                                return JsonConvert.SerializeObject(response);
                            }

                            while (await reader.ReadAsync())
                            {
                                ExSet set = new ExSet
                                {
                                    ExSetID = reader.GetInt32(reader.GetOrdinal("ExSetID")),
                                    Weight = Convert.ToSingle(reader.GetValue(reader.GetOrdinal("Weight"))),
                                    Reps = reader.GetInt32(reader.GetOrdinal("Reps")),
                                    DateStarted = reader.GetDateTime(reader.GetOrdinal("DateStarted")),
                                    RPE = reader.GetInt32(reader.GetOrdinal("RPE")),
                                    ExerciseID = reader.GetInt32(reader.GetOrdinal("ExerciseID")),
                                };
                                exSetList.Add(set);
                            }
                            return JsonConvert.SerializeObject(exSetList);
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
        [Route("UpdateSet")]
        public async Task<string> updateSet(ExSet set)
        {
            Response response = new Response();
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();
                    string query = "UPDATE ExerciseSets SET Weight = @Weight, Reps = @Reps, DateStarted = @DateStarted, RPE = @RPE, ExerciseID = @ExerciseID WHERE ExSetID = @ExSetID";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Weight", set.Weight);
                        cmd.Parameters.AddWithValue("@Reps", set.Reps);
                        cmd.Parameters.AddWithValue("@RPE", set.RPE);
                        cmd.Parameters.AddWithValue("@DateStarted", set.DateStarted);
                        cmd.Parameters.AddWithValue("@ExerciseID", set.ExerciseID);
                        cmd.Parameters.AddWithValue("@ExSetID", set.ExSetID);

                        await cmd.ExecuteNonQueryAsync();
                        return JsonConvert.SerializeObject(set);
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

        [HttpGet]
        [Route("GetExerciseByExSetID")]
        public async Task<string> GetExerciseByExSetID(int ExSetID)
        {
            Response response = new Response();
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefualtConnection")))
                {
                    await conn.OpenAsync();
                    string query = @"SELECT e.* FROM Exercises e INNER JOIN ExerciseSets ex on e.ExerciseID = ex.ExerciseID WHERE ex.ExSetID = @ExSetID";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ExSetID", ExSetID);

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (!reader.HasRows)
                            {
                                response.StatusCode = 500;
                                response.ErrorMessage = "Exercise not found";
                                return JsonConvert.SerializeObject(response);
                            }

                            await reader.ReadAsync();
                            Exercise exercise = new Exercise
                            {
                                ExerciseID = reader.GetInt32(reader.GetOrdinal("ExerciseID")),
                                ExerciseName = reader.GetString(reader.GetOrdinal("ExerciseName")),
                                WorkoutID = reader.GetInt32(reader.GetOrdinal("WorkoutID")),
                                RestTime = Convert.ToSingle(reader.GetValue(reader.GetOrdinal("RestTime"))),
                                ExerciseType = reader.GetString(reader.GetOrdinal("ExerciseType")),

                            };
                            exercise.MuscleGroups = await _muscleGroupService.GetGroupNamesAsync(exercise.ExerciseID);
                            exercise.Sets = await _exSetService.GetExSetsAsync(exercise.ExerciseID);
                            return JsonConvert.SerializeObject(exercise);
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
    }
}
