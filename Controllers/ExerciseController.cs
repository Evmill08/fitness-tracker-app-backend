 using Microsoft.AspNetCore.Mvc;
using TestApp.Services;
using FitnessApi.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using Newtonsoft.Json;
using TestApp.Models;
using System.Collections.Generic;

namespace TestApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExerciseController : ControllerBase
    {
        public readonly IConfiguration _configuration;
        public readonly IMuscleGroupService _muscleGroupService;
        public readonly IExSetService _exSetService;

        public ExerciseController(IConfiguration configuration, IMuscleGroupService muscleGroupService, IExSetService ExSetService)
        {
            _configuration = configuration;
            _muscleGroupService = muscleGroupService;
            _exSetService = ExSetService;
        }

        [HttpGet]
        [Route("GetAllExercise")]
        public async Task<string> GetExercises()
        {
            Response response = new Response();
            try
            {
                SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection").ToString());
                SqlDataAdapter da = new SqlDataAdapter("SELECT * from Exercises", conn);
                DataTable dt = new DataTable();
                da.Fill(dt);
                List<Exercise> exerciseList = new List<Exercise>();

                if (dt.Rows.Count > 0)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        Exercise exercise = new Exercise();
                        exercise.ExerciseID = Convert.ToInt32(dt.Rows[i]["ExerciseID"]);
                        exercise.ExerciseName = Convert.ToString(dt.Rows[i]["ExerciseName"]);
                        exercise.WorkoutID = Convert.ToInt32(dt.Rows[i]["WorkoutID"]);
                        exercise.RestTime = Convert.ToSingle(dt.Rows[i]["RestTime"]);
                        exercise.ExerciseType = Convert.ToString(dt.Rows[i]["ExerciseType"]);
                        exerciseList.Add(exercise);
                    }
                }

                if (exerciseList.Count > 0)
                {
                    return JsonConvert.SerializeObject(exerciseList);
                } else
                {
                    response.StatusCode = 100;
                    response.ErrorMessage = "No Data Found";
                }
            } catch (Exception ex) {
                response.StatusCode = 500;
                response.ErrorMessage = ex.Message;
            }
            return JsonConvert.SerializeObject(response);
        }

        [HttpPost]
        [Route("AddExercise")]
        public async Task<string> AddExercise(Exercise exercise)
        {
            Response response = new Response();

            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();
                    string query = @"INSERT INTO Exercises (ExerciseName, WorkoutID, RestTime, ExerciseType)
                                     VALUES (@ExerciseName, @WorkoutID, @RestTime, @ExerciseType);
                                     SELECT SCOPE_IDENTITY()";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ExerciseName", exercise.ExerciseName);
                        cmd.Parameters.AddWithValue("@WorkoutID", exercise.WorkoutID);
                        cmd.Parameters.AddWithValue("@RestTime", exercise.RestTime);
                        cmd.Parameters.AddWithValue("@ExerciseType", exercise.ExerciseType);

                        var result = await cmd.ExecuteScalarAsync();
                        exercise.ExerciseID = Convert.ToInt32(result);
                        List<MuscleGroup> muscleGroups = await _muscleGroupService.GetGroupNamesAsync(exercise.ExerciseID);
                        exercise.MuscleGroups.AddRange(muscleGroups);

                        if (exercise.MuscleGroups.Count > 0)
                        {
                            foreach (var mg in exercise.MuscleGroups)
                            {
                                string mgResponse = await _muscleGroupService.AddMuscleGroupToExercise(mg.MuscleGroupID, exercise.ExerciseID);
                            }
                        }
                    }
                    return JsonConvert.SerializeObject(exercise);
                }
                
            } catch (Exception ex)
            {
                response.StatusCode = 500;
                response.ErrorMessage = ex.Message;
            }
            return JsonConvert.SerializeObject(response);
        }

        [HttpPost]
        [Route("UpdateExercise")]
        public async Task<string> UpdateExercise(Exercise exercise)
        {
            Response response = new Response();
            string query = "UPDATE Exercises SET ExerciseName = @ExerciseName, WorkoutID = @WorkoutID, RestTime = @RestTime, ExerciseType = @ExerciseType WHERE ExerciseID = @ExerciseID";

            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ExerciseID", exercise.ExerciseID);
                        cmd.Parameters.AddWithValue("@ExerciseName", exercise.ExerciseName);
                        cmd.Parameters.AddWithValue("@WorkoutID", exercise.WorkoutID);
                        cmd.Parameters.AddWithValue("@RestTime", exercise.RestTime);
                        cmd.Parameters.AddWithValue("@ExerciseType", exercise.ExerciseType);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                List<MuscleGroup> existingMuscleGroups = await _muscleGroupService.GetGroupNamesAsync(exercise.ExerciseID);
                List<ExSet> existingExSets = await _exSetService.GetExSetsAsync(exercise.ExerciseID);

                foreach (var oldMg in existingMuscleGroups)
                {
                    if (!exercise.MuscleGroups.Any(mg => mg.MuscleGroupID == oldMg.MuscleGroupID))
                    {
                        await _muscleGroupService.RemoveMuscleGroupFromExercise(oldMg.MuscleGroupID, exercise.ExerciseID);
                    }
                }

                foreach (var newMg in exercise.MuscleGroups)
                {
                    if (!existingMuscleGroups.Any(mg => mg.MuscleGroupID == newMg.MuscleGroupID))
                    {
                        await _muscleGroupService.AddMuscleGroupToExercise(newMg.MuscleGroupID, exercise.ExerciseID);
                    }
                }

                exercise.MuscleGroups = await _muscleGroupService.GetGroupNamesAsync(exercise.ExerciseID);
                exercise.Sets = await _exSetService.GetExSetsAsync(exercise.ExerciseID);

                return JsonConvert.SerializeObject(exercise);

            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.ErrorMessage = ex.Message;
            }
            return JsonConvert.SerializeObject(response);
        }

        [HttpPost]
        [Route("DeleteExercise")]
        public async Task<string> DeleteExercise(int exerciseID)
        {
            Response response = new Response();
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();

                    string deleteRelatedQuery = "DELETE FROM ExerciseMuscleGroups WHERE ExerciseID = @ExerciseID";

                    using (SqlCommand relatedCmd = new SqlCommand(deleteRelatedQuery, conn))
                    {
                        relatedCmd.Parameters.AddWithValue("@ExerciseID", exerciseID);
                        await relatedCmd.ExecuteNonQueryAsync();
                    }

                    string query = "DELETE FROM Exercises WHERE ExerciseID = @ExerciseID";
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

        [HttpGet]
        [Route("GetExerciseByID")]
        public async Task<string> GetExerciseByID(int exerciseID)
        {
            Response response = new Response();
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();
                    string query = "SELECT * FROM Exercises WHERE ExerciseID = @ExerciseID";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ExerciseID", exerciseID);

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (!reader.HasRows)
                            {
                                response.StatusCode = 500;
                                response.ErrorMessage = "Excercise Not Found";
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
                            exercise.MuscleGroups = await _muscleGroupService.GetGroupNamesAsync(exerciseID);
                            exercise.Sets = await _exSetService.GetExSetsAsync(exerciseID);
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

        [HttpGet]
        [Route("GetExercisesByWorkout")]
        public async Task<string> GetExercisesByWorkout(int workoutID)
        {
            Response response = new Response();
            try
            {
                string query = "SELECT * FROM Exercises WHERE WorkoutID = @WorkoutID";
                List<Exercise> exercises = new List<Exercise>();

                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@WorkoutID", workoutID);
                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {

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

                                exercises.Add(exercise);
                            }

                            // If we have exercises, add the related data
                            if (exercises.Count > 0)
                            {
                                foreach (var exercise in exercises)
                                {
                                    List<MuscleGroup> muscleGroups = await _muscleGroupService.GetGroupNamesAsync(exercise.ExerciseID);
                                    exercise.MuscleGroups.AddRange(muscleGroups);
                                    List<ExSet> exerciseSets = await _exSetService.GetExSetsAsync(exercise.ExerciseID);
                                    exercise.Sets.AddRange(exerciseSets);
                                }
                            }

                            // Always return the exercises list, even if it's empty
                            return JsonConvert.SerializeObject(exercises);
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
        [Route("GetUserExercises")]
        public async Task<string> GetUserExercises(int userID)
        {
            Response response = new Response();

            try
            {
                string query = @"SELECT e.* FROM Exercises e INNER JOIN Workouts w ON e.WorkoutID = w.WorkoutID WHERE w.UserID = @UserID ORDER BY e.ExerciseName";

                List<Exercise> exercises = new List<Exercise>();

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
                                response.StatusCode = 500;
                                response.ErrorMessage = "No exercises found";
                                return JsonConvert.SerializeObject(response);
                            }

                            while (await reader.ReadAsync())
                            {
                                Exercise exercise = new Exercise
                                {
                                    ExerciseID = reader.GetInt32(reader.GetOrdinal("ExerciseID")),
                                    ExerciseName = reader.GetString(reader.GetOrdinal("ExerciseName")),
                                    WorkoutID = reader.GetInt32(reader.GetOrdinal("WorkoutID")),
                                };

                                if (!reader.IsDBNull(reader.GetOrdinal("ExerciseType")))
                                {
                                    exercise.ExerciseType = reader.GetString(reader.GetOrdinal("ExerciseType"));
                                }

                                if (!reader.IsDBNull(reader.GetOrdinal("RestTime")))
                                {
                                    exercise.RestTime = Convert.ToSingle(reader.GetValue(reader.GetOrdinal("RestTime")));

                                }

                                exercises.Add(exercise);
                            }
                        }
                    }


                    foreach (var exercise in exercises)
                    {
                        List<MuscleGroup> mucleGroups = await _muscleGroupService.GetGroupNamesAsync(exercise.ExerciseID);
                        exercise.MuscleGroups.AddRange(mucleGroups);
                        List<ExSet> exerciseSets = await _exSetService.GetExSetsAsync(exercise.ExerciseID);
                        exercise.Sets.AddRange(exerciseSets);
                    }
                }
                

                return JsonConvert.SerializeObject(exercises);

            } catch (Exception ex) {
                response.StatusCode = 500;
                response.ErrorMessage = ex.Message;
                return JsonConvert.SerializeObject(response);
            }
        }

        [HttpGet]
        [Route("GetUserExerciseCards")]
        public async Task<string> GetUserExerciseCards(int userID)
        {
            Response response = new Response();

            try
            {
                string query = @"SELECT DISTINCT e.* FROM Exercises e INNER JOIN Workouts w ON e.WorkoutID = w.WorkoutID WHERE w.UserID = @UserID ORDER BY e.ExerciseName";

                List<Exercise> uniqueExercises = new List<Exercise>();

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
                                return JsonConvert.SerializeObject(uniqueExercises);
                            }

                            while (await reader.ReadAsync())
                            {
                                Exercise exercise = new Exercise
                                {
                                    ExerciseID = reader.GetInt32(reader.GetOrdinal("ExerciseID")),
                                    ExerciseName = reader.GetString(reader.GetOrdinal("ExerciseName")),
                                    ExerciseType = reader.GetString(reader.GetOrdinal("ExerciseType")),
                                    WorkoutID = reader.GetInt32(reader.GetOrdinal("WorkoutID")),
                                };

                                List<MuscleGroup> mucleGroups = await _muscleGroupService.GetGroupNamesAsync(exercise.ExerciseID);
                                exercise.MuscleGroups.AddRange(mucleGroups);

                                if (!uniqueExercises.Any(e => (e.ExerciseName.ToLower() == exercise.ExerciseName.ToLower())))
                                {
                                    uniqueExercises.Add(exercise);
                                }
                            }
                        }
                    }
                }

                return JsonConvert.SerializeObject(uniqueExercises);

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
