using FitnessApi.Models;
using static TestApp.Controllers.WorkoutController;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Data;
using Newtonsoft.Json;
using Microsoft.Data.SqlClient;
using TestApp.Services;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Identity;

namespace FitnessApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        public readonly IConfiguration _configuration;
        private readonly IWorkoutService _workoutService;
        private readonly IPbService _pbService;

        public UserController(IConfiguration configuration, IWorkoutService workoutService, IPbService pbService)
        {
            _configuration = configuration;
            _workoutService = workoutService;
            _pbService = pbService;
        }

        [HttpGet]
        [Route("GetAllUsers")]
        public string GetUsers()
        {
            SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection").ToString());
            SqlDataAdapter da = new SqlDataAdapter("SELECT * from Users", conn);
            DataTable dt = new DataTable();
            da.Fill(dt);
            List<User> usersList = new List<User>();
            Response response = new Response();

            if (dt.Rows.Count > 0)
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    User user = new User();
                    user.UserID = Convert.ToInt32(dt.Rows[i]["UserID"]);
                    user.Username = Convert.ToString(dt.Rows[i]["Username"]);
                    user.Password = Convert.ToString(dt.Rows[i]["UserPassword"]);
                    user.Email = Convert.ToString(dt.Rows[i]["Email"]);
                    user.Weight = Convert.ToSingle(dt.Rows[i]["UserWeight"]);
                    user.Height = Convert.ToSingle(dt.Rows[i]["UserHeight"]);
                    usersList.Add(user);
                }
            }

            if (usersList.Count > 0)
            {
                return JsonConvert.SerializeObject(usersList);
            }
            else
            {
                response.StatusCode = 100;
                response.ErrorMessage = "No Data Found";
                return JsonConvert.SerializeObject(response);
            }


        }

        [HttpPost]
        [Route("AddUser")]
        public async Task<string> AddUser(User user)
        {
            Response response = new Response();
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();
                    string query = @"INSERT INTO Users ( Username, UserPassword, Email, UserWeight, UserHeight) 
                           VALUES  (@Username, @Password, @Email, @Weight, @Height);
                           SELECT SCOPE_IDENTITY();";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Username", user.Username);
                        cmd.Parameters.AddWithValue("@Password", user.Password);
                        cmd.Parameters.AddWithValue("@Email", user.Email);
                        cmd.Parameters.AddWithValue("@Weight", user.Weight);
                        cmd.Parameters.AddWithValue("@Height", user.Height);
                        
                        var result = await cmd.ExecuteScalarAsync();

                        user.UserID = Convert.ToInt32(result);
                    }
                    return JsonConvert.SerializeObject(user);

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
        [Route("UpdateUser")]
        public async Task<string> UpdateUser(User user)
        {

            Response response = new Response();
            string query = "UPDATE Users SET Username = @Username, Email = @Email, UserPassword = @UserPassword, UserHeight = @UserHeight, UserWeight = @UserWeight WHERE UserID = @UserID";
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();
                    using (SqlCommand command = new SqlCommand(query, conn))
                    {
                        command.Parameters.AddWithValue("@UserID", user.UserID);
                        command.Parameters.AddWithValue("@Username", user.Username);
                        command.Parameters.AddWithValue("@UserPassword", user.Password);
                        command.Parameters.AddWithValue("@Email", user.Email);
                        command.Parameters.AddWithValue("@UserWeight", user.Weight);
                        command.Parameters.AddWithValue("@UserHeight", user.Height);

                        await command.ExecuteNonQueryAsync();
                        return JsonConvert.SerializeObject(user);
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
        [Route("DeleteUser")]
        public async Task<string> DeleteUser(int userID)
        {
            Response response = new Response();
            string query = "DELETE FROM Users WHERE UserID = @UserID";

            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserID", userID);
                        await cmd.ExecuteNonQueryAsync();
                    }
                    response.StatusCode = 200;
                    response.ErrorMessage = $"User {userID} deleted successfully";
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
        [Route("GetUserByID")]
        public async Task<string> GetUserByID(int userID)
        {
            Response response = new Response();

            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();
                    string query = "SELECT * FROM Users WHERE UserID = @UserID";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserID", userID);

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (!reader.HasRows)
                            {
                                response.StatusCode = 200;
                                response.ErrorMessage = "User not found";
                                return JsonConvert.SerializeObject(response);
                            }

                            await reader.ReadAsync();
                            User user = new User
                            {
                                UserID = reader.GetInt32(reader.GetOrdinal("UserID")),
                                Username = reader.GetString(reader.GetOrdinal("Username")),
                                Password = reader.GetString(reader.GetOrdinal("UserPassword")),
                                Email = reader.GetString(reader.GetOrdinal("Email")),
                                Weight = Convert.ToSingle(reader.GetValue(reader.GetOrdinal("UserWeight"))),
                                Height = Convert.ToSingle(reader.GetValue(reader.GetOrdinal("UserHeight"))),
                                WorkoutHistory = new List<Workout>(),
                                PersonalBests = new List<PersonalBest>(),
                            };



                            List<Workout> userWorkouts = await _workoutService.GetUserWorkoutsAsync(userID);
                            List<PersonalBest> userPBs = await _pbService.GetUserPersonalBestsAsync(userID);
                            user.WorkoutHistory.AddRange(userWorkouts);
                            user.PersonalBests.AddRange(userPBs);

                            return JsonConvert.SerializeObject(user);
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
        [Route("ValidateUserLogin")]
        public async Task<string> ValidateUserLogin(string username, string password)
        {
            Response response = new Response();
            try
            {
                string query = "SELECT * FROM Users WHERE Username = @Username AND UserPassword = @Password";
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Username", username);
                        cmd.Parameters.AddWithValue("@Password", password);

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (!reader.HasRows)
                            {
                                throw new Exception("User Not Found");
                            }

                            await reader.ReadAsync();
                            int userID = reader.GetInt32(reader.GetOrdinal("UserID"));
                            string userJSON = await GetUserByID(userID);
                            if (userJSON ==  null)
                            {
                                response.StatusCode = 500;
                                response.ErrorMessage = "User not found";
                                return JsonConvert.SerializeObject(response);
                            }

                            return userJSON;
                        }
                    }
                }
            } catch (Exception ex)
            {
                response.StatusCode = 500;
                response.ErrorMessage = ex.Message;
                return JsonConvert.SerializeObject(response);
            }
        }
    }
}


