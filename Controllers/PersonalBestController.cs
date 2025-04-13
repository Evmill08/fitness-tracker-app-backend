using Microsoft.AspNetCore.Mvc;
using FitnessApi.Models;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc.Razor;


namespace TestApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PersonalBestController : Controller
    {
        public readonly IConfiguration _configuration;

        public PersonalBestController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        [Route("GetUserPBs")]
        public async Task<string> GetUsersPBs(int userID)
        {
            Response response = new Response();
            List<PersonalBest> UserPBs = new List<PersonalBest>();

            try
            {
                string query = "SELECT * FROM PersonalBests WHERE UserID = @UserID";

                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserID", userID);

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (!reader.HasRows)
                            {
                                response.StatusCode = 404;
                                response.ErrorMessage = "User Personal Bests Not Found";
                                return JsonConvert.SerializeObject(response);
                            }

                            while (await reader.ReadAsync())
                            {
                                PersonalBest PB = new PersonalBest
                                {
                                    ExerciseID = reader.GetInt32(reader.GetOrdinal("ExerciseID")),
                                    ExerciseName = reader.GetString(reader.GetOrdinal("ExerciseName")),
                                    Weight = Convert.ToSingle(reader.GetValue(reader.GetOrdinal("Weight"))),
                                    Reps = reader.GetInt32(reader.GetOrdinal("Reps")),
                                    DateSet = reader.GetDateTime(reader.GetOrdinal("Date")),
                                    UserID = reader.GetInt32(reader.GetOrdinal("UserID"))
                                };

                                UserPBs.Add(PB);
                            }
                            return JsonConvert.SerializeObject(UserPBs);
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

        [HttpGet]
        [Route("GetExercisePB")]
        public async Task<string> GetExercisePB(string exerciseName, int userID)
        {
            Response response = new Response();

            try
            {
                string query = "SELECT * FROM PersonalBests WHERE ExerciseName = @ExerciseName AND UserID = @UserID";

                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ExerciseName", exerciseName);
                        cmd.Parameters.AddWithValue("@UserID", userID);


                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (!reader.HasRows)
                            {
                                response.StatusCode = 404;
                                response.ErrorMessage = "Personal Best Not found for this exercise";
                                return JsonConvert.SerializeObject(response);
                            }

                            await reader.ReadAsync();
                            PersonalBest PB = new PersonalBest
                            {
                                ExerciseID = reader.GetInt32(reader.GetOrdinal("ExerciseID")),
                                ExerciseName = reader.GetString(reader.GetOrdinal("ExerciseName")),
                                Weight = Convert.ToSingle(reader.GetValue(reader.GetOrdinal("Weight"))),
                                Reps = reader.GetInt32(reader.GetOrdinal("Reps")),
                                DateSet = reader.GetDateTime(reader.GetOrdinal("Date")),
                                UserID = reader.GetInt32(reader.GetOrdinal("UserID"))
                            };
                            return JsonConvert.SerializeObject(PB);

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
        [Route("AddPersonalBest")]
        public async Task<string> AddPersonalBest(PersonalBest personalBest)
        {
            Response response = new Response();
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();

                    // Check if a personal best already exists for this exercise and user
                    string checkQuery = "SELECT * FROM PersonalBests WHERE ExerciseName = @ExerciseName AND UserID = @UserID";
                    PersonalBest existingPB = null;

                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@ExerciseName", personalBest.ExerciseName);
                        checkCmd.Parameters.AddWithValue("@UserID", personalBest.UserID);

                        using (SqlDataReader reader = await checkCmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                existingPB = new PersonalBest
                                {
                                    ExerciseID = reader.GetInt32(reader.GetOrdinal("ExerciseID")),
                                    ExerciseName = reader.GetString(reader.GetOrdinal("ExerciseName")),
                                    Weight = Convert.ToSingle(reader.GetValue(reader.GetOrdinal("Weight"))),
                                    Reps = reader.GetInt32(reader.GetOrdinal("Reps")),
                                    DateSet = reader.GetDateTime(reader.GetOrdinal("Date")),
                                    UserID = reader.GetInt32(reader.GetOrdinal("UserID"))
                                };
                            }
                        }
                    }

                    // If record exists, return existing record instead of throwing an error
                    if (existingPB != null)
                    {
                        return await UpdatePersonalBest(personalBest);
                        
                    }

                    // Insert new personal best
                    string insertQuery = @"INSERT INTO PersonalBests (ExerciseID, ExerciseName, Weight, Reps, Date, UserID)
                    VALUES (@ExerciseID, @ExerciseName, @Weight, @Reps, @Date, @UserID)";
                    
                    using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@ExerciseID", personalBest.ExerciseID);
                        cmd.Parameters.AddWithValue("@ExerciseName", personalBest.ExerciseName);
                        cmd.Parameters.AddWithValue("@Weight", personalBest.Weight);
                        cmd.Parameters.AddWithValue("@Reps", personalBest.Reps);
                        cmd.Parameters.AddWithValue("@Date", personalBest.DateSet);
                        cmd.Parameters.AddWithValue("@UserID", personalBest.UserID);

                        await cmd.ExecuteNonQueryAsync();
                    }

                    return JsonConvert.SerializeObject(personalBest);
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
        [Route("UpdatePersonalBest")]
        public async Task<string> UpdatePersonalBest(PersonalBest personalBest)
        {
            Response response = new Response();

            try
            {
                string query = @"UPDATE PersonalBests SET ExerciseID = @ExerciseID, Weight = @Weight, Reps = @Reps, Date = @Date WHERE ExerciseName = @ExerciseName AND UserID = @UserID";

                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ExerciseID", personalBest.ExerciseID);
                        cmd.Parameters.AddWithValue("@ExerciseName", personalBest.ExerciseName);
                        cmd.Parameters.AddWithValue("@Weight", personalBest.Weight);
                        cmd.Parameters.AddWithValue("@Reps", personalBest.Reps);
                        cmd.Parameters.AddWithValue("@Date", personalBest.DateSet);
                        cmd.Parameters.AddWithValue("@UserID", personalBest.UserID);

                        int rowsAffected = await cmd.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            return JsonConvert.SerializeObject(personalBest);
                        } else
                        {
                            response.StatusCode = 404;
                            response.ErrorMessage = "No matching personal best found to update";
                            return JsonConvert.SerializeObject(response);
                        }
                        
                    }
                }

            } catch (Exception ex)
            {
                response.StatusCode = 500;
                response.ErrorMessage = ex.Message;
            }
            return JsonConvert.SerializeObject(response);

        }

        [HttpPost]
        [Route("DeletePersonalBest")]
        public async Task<string> DeletePersonalBest(int exerciseID)
        {
            Response response = new Response();

            try
            {
                string query = "DELETE FROM PersonalBests WHERE ExerciseID = @ExerciseID";

                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ExerciseID", exerciseID);

                        await cmd.ExecuteNonQueryAsync();

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
    }
}
