using FitnessApi.Models;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;


namespace TestApp.Services

{
    public interface IPbService
    {
        Task<List<PersonalBest>> GetUserPersonalBestsAsync(int userID);
    }
    public class PbService : IPbService
    {
        private readonly IConfiguration _configuration;
        

        public PbService(IConfiguration configuration) {
            _configuration = configuration;
        }

        public async Task<List<PersonalBest>> GetUserPersonalBestsAsync(int userID)
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
                                return UserPBs; // Empty list
                            }

                            while (await reader.ReadAsync())
                            {
                                PersonalBest PB = new PersonalBest
                                {
                                    ExerciseID = reader.GetInt32(reader.GetOrdinal("ExerciseID")),
                                    ExerciseName = reader.GetString(reader.GetOrdinal("ExerciseName")),
                                    Weight = Convert.ToSingle(reader.GetOrdinal("Weight")),
                                    Reps = reader.GetInt32(reader.GetOrdinal("Reps")),
                                    DateSet = reader.GetDateTime(reader.GetOrdinal("Date")),
                                    UserID = reader.GetInt32(reader.GetOrdinal("UserID"))
                                };

                                UserPBs.Add(PB);
                            }
                            return UserPBs;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching User Personal Bests");
            }
        }
    }
}
