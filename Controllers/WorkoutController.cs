using FitnessApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Data;
using System.Runtime.CompilerServices;
using TestApp.Services;

namespace TestApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WorkoutController : ControllerBase
    {
        public readonly IConfiguration _configuration;
        private readonly IExerciseService _exerciseService;

        public WorkoutController(IConfiguration configuration, IExerciseService exerciseService)
        {
            _configuration = configuration;
            _exerciseService = exerciseService;
        }

        [HttpGet]
        [Route("GetAllWorkouts")]
        public string GetWorkouts()
        {
            Response response = new Response();
            try
            {
                SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection").ToString());
                SqlDataAdapter da = new SqlDataAdapter("SELECT * from Workouts", conn);
                DataTable dt = new DataTable();
                da.Fill(dt);
                List<Workout> workoutList = new List<Workout>();

                if (dt.Rows.Count > 0)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        Workout workout = new Workout();
                        workout.WorkoutID = Convert.ToInt32(dt.Rows[i]["WorkoutId"]);
                        workout.WorkoutName = Convert.ToString(dt.Rows[i]["WorkoutName"]);
                        workout.Duration = Convert.ToSingle(dt.Rows[i]["Duration"]);
                        workout.Date = Convert.ToDateTime(dt.Rows[i]["WorkoutDate"]);
                        workout.UserID = Convert.ToInt32(dt.Rows[i]["UserID"]);
                        workoutList.Add(workout);
                    }
                }

                if (workoutList.Count > 0)
                {
                    return JsonConvert.SerializeObject(workoutList);
                }
                else
                {
                    response.StatusCode = 100;
                    response.ErrorMessage = "No data Found";
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
        [Route("AddWorkout")]
        public async Task<string> AddWorkout(Workout workout)
        {
            Response response = new Response();

            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();
                    string query = @"INSERT INTO Workouts (WorkoutName, Duration, WorkoutDate, UserID)
                                        VALUES (@WorkoutName, @Duration, @WorkoutDate, @UserID);
                                        SELECT SCOPE_IDENTITY();";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@WorkoutName", workout.WorkoutName);
                        cmd.Parameters.AddWithValue("@Duration", workout.Duration);
                        cmd.Parameters.AddWithValue("@WorkoutDate", workout.Date);
                        cmd.Parameters.AddWithValue("@UserID", workout.UserID);

                        var result = await cmd.ExecuteScalarAsync();
                        workout.WorkoutID = Convert.ToInt32(result);
                    }
                    return JsonConvert.SerializeObject(workout);
                }

            } catch (Exception ex)
            {
                response.StatusCode = 500;
                response.ErrorMessage = ex.Message;
            }
            return JsonConvert.SerializeObject(response);
        }

        [HttpPost]
        [Route("UpdateWorkout")]
        public async Task<string> UpdateWorkout(Workout workout)
        {
            Response response = new Response();
            string query = "UPDATE Workouts SET WorkoutName = @WorkoutName, Duration = @Duration, WorkoutDate = @WorkoutDate WHERE WorkoutID = @WorkoutID";

            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@WorkoutID", workout.WorkoutID);
                        cmd.Parameters.AddWithValue("@WorkoutName", workout.WorkoutName);
                        cmd.Parameters.AddWithValue("@Duration", workout.Duration);
                        cmd.Parameters.AddWithValue("@WorkoutDate", workout.Date);

                        List<Exercise> exercises = await _exerciseService.GetExerciseListAsync(workout.WorkoutID);
                        workout.Exercises = exercises;

                        await cmd.ExecuteNonQueryAsync();
                        return JsonConvert.SerializeObject(workout);
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

        [HttpPost]
        [Route("DeleteWorkout")]
        public async Task<string> DeleteWorkout(int workoutID)
        {
            Response response = new Response();
            try
            {
                string query = "DELETE FROM Workouts WHERE WorkoutID = @WorkoutID";

                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@WorkoutID", workoutID);
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

        [HttpGet]
        [Route("GetWorkoutByID")]
        public async Task<string> GetWorkoutByID(int workoutID)
        {
            Response response = new Response();

            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();
                    string query = "SELECT * FROM Workouts WHERE WorkoutID = @WorkoutID";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@WorkoutID", workoutID);

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (!reader.HasRows)
                            {
                                response.StatusCode = 500;
                                response.ErrorMessage = "Workout Not Found";
                                return JsonConvert.SerializeObject(response);
                            }

                            await reader.ReadAsync();
                            Workout workout = new Workout
                            {
                                WorkoutID = reader.GetInt32(reader.GetOrdinal("WorkoutID")),
                                WorkoutName = reader.GetString(reader.GetOrdinal("WorkoutName")),
                                Duration = Convert.ToSingle(reader.GetValue(reader.GetOrdinal("Duration"))),
                                Date = reader.GetDateTime(reader.GetOrdinal("WorkoutDate")),
                                UserID = reader.GetInt32(reader.GetOrdinal("UserID")),
                                Exercises = new List<Exercise>()

                            };

                            List<Exercise> exercises = await _exerciseService.GetExerciseListAsync(workout.WorkoutID);
                            workout.Exercises.AddRange(exercises);

                            return JsonConvert.SerializeObject(workout);
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
        [Route("GetUserWorkouts")]
        public async Task<string> GetUserWorkouts(int userID)
        {
            Response response = new Response();

            try
            {
                string query = "SELECT * FROM Workouts WHERE UserID = @UserID";
                List<Workout> workouts = new List<Workout>();

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
                                response.StatusCode = 200;
                                response.ErrorMessage = "User Workouts not found";
                                return JsonConvert.SerializeObject(workouts);
                            }


                            while (await reader.ReadAsync())
                            {
                                Workout workout = new Workout
                                {
                                    WorkoutID = reader.GetInt32(reader.GetOrdinal("WorkoutID")),
                                    WorkoutName = reader.GetString(reader.GetOrdinal("WorkoutName")),
                                    Duration = Convert.ToSingle(reader.GetValue(reader.GetOrdinal("Duration"))),
                                    Date = reader.GetDateTime(reader.GetOrdinal("WorkoutDate")),
                                    UserID = reader.GetInt32(reader.GetOrdinal("UserID")),
                                    Exercises = new List<Exercise>()
                                };
                                
                                List<Exercise> exercises = await _exerciseService.GetExerciseListAsync(workout.WorkoutID);
                                workout.Exercises.AddRange(exercises);
                                workouts.Add(workout);

                            }
                            
                            return JsonConvert.SerializeObject(workouts);
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
