using Aurelius_API.ModelAPI;
using Aurelius_API.ModelAPI.Company;
using Aurelius_API.ModelAPI.Login;
using Aurelius_API.ModelPortal.Login;
using Microsoft.Ajax.Utilities;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Odbc;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;


namespace Aurelius_API.Controllers
{
    [RoutePrefix("api/authentication")]
    public class AuthenticationController : ApiController
    {
        private readonly string _connectionString;

        public AuthenticationController()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }

        [Route("Login")]
        [HttpPost]
        public async Task<GeneralAPIResponse<LoginResponse>> Login(UserLoginDto model) { 
            var response = new GeneralAPIResponse<LoginResponse>();
            decimal faudtdate = 0;
            string faudttime = "";

            try
            {
                using (var connection = new OdbcConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    string query = "SELECT * FROM TBLUSER WHERE \"FUSERNAME\" = ? AND \"FPASSWORD\" = ? AND \"FACTIVE\" = 1";
                    UserResponseDto user = new UserResponseDto();

                    using (var command = new OdbcCommand(query, connection))
                    {

                        command.Parameters.Add(new OdbcParameter("@username", model.Username.Trim()));
                        command.Parameters.Add(new OdbcParameter("@password", model.Password.Trim()));

                        using (OdbcDataReader reader = (OdbcDataReader)await command.ExecuteReaderAsync())
                        { 
                            if (await reader.ReadAsync())
                            {
                                user.FID = reader["FID"].ToString();
                                user.FUSERNAME = reader["FUSERNAME"].ToString();
                                user.FFULLNAME = reader["FFULLNAME"].ToString();
                                user.FROLE = reader["FROLE"].ToString();
                                user.FTEMPPWD = reader["FTEMPPWD"].ToString();
                                user.FCOMPID = reader["FCOMPID"].ToString();
                                faudtdate = Convert.ToDecimal(reader["FAUDTDATE"].ToString());
                                faudttime = reader["FAUDTTIME"].ToString();
                            }
                            else
                            {
                                response.Success = false;
                                response.Message = "Invalid Username/Password/Company";
                                return response;    
                            }   
                            reader.Close();
                        }
                    }

                    if (user.FROLE.ToLower() == "user" || user.FROLE.ToLower() == "admin")
                    {
                        var compIds = user.FCOMPID.ToLower().Split(',');

                        // Check if model.Company is in the compIds array
                        if (!compIds.Contains(model.Company.ToLower()))
                        {
                            response.Success = false;
                            response.Message = "Invalid Username/Password/Company";
                            return response;
                        }
                    }

                    if (Convert.ToInt32(user.FTEMPPWD) == 1)
                    {
                        // Get the created date and time
                        DateTime Cdatetime = DateTime.ParseExact(faudtdate + " " + faudttime, "yyyyMMdd HHmmss", null);

                        // Calculate the date and time 24 hours from now
                        DateTime futureDateTime = Cdatetime.AddHours(24);

                        // Get the current date and time
                        DateTime now = DateTime.Now;
                        string currentDateTimeFormatted = now.ToString("yyyyMMdd HHmmss");
                        DateTime Cnow = DateTime.ParseExact(currentDateTimeFormatted, "yyyyMMdd HHmmss", null);

                        // Calculate the difference between the current date and the created date
                        TimeSpan difference = Cnow - Cdatetime;

                        // Check if the difference is less than 24 hours
                        if (difference.TotalHours < 24)
                        {
                            // Authentication successful
                           
                           var authToken = JwtToken.GenerateJwtToken(model.Company, user.FID);
                            var encryptedToken = JwtToken.AesEncryption.EncryptToken(authToken);

                            LoginResponse responseData = new LoginResponse
                            {
                                AuthToken = encryptedToken,
                                //RefreshToken = refreshToken.Token,
                                User = user,
                            };

                            int totalComp = 0, totalUser = 0, totalDoctor=0, totalTransDay = 0, totalTransMonth = 0;
                            //here for retreive the dashboard details
                            //if role==admin || user else super....

                            GetDashboardDetailsDto userDashboard = new GetDashboardDetailsDto();
                            userDashboard.TotalDoctor = totalDoctor;
                            userDashboard.TotalCompany = totalComp;
                            userDashboard.TotalUser = totalUser;
                            userDashboard.TotalTransactionDay = totalTransDay;
                            userDashboard.TotalTransactionMonth = totalTransMonth;

                            responseData.DashboardDetails = userDashboard;

                            response.Data = responseData;
                            response.Success = true;
                            response.Message = "Authentication successful";

                        }
                        else
                        {
                            response.Success = false;
                            response.Message = "Temporary password expired";
                            return response;
                        }
                    }
                    else
                    {
                        // Authentication successful for temppwd 0
                       var authToken = JwtToken.GenerateJwtToken(model.Company,user.FID);
                        var encryptedToken = JwtToken.AesEncryption.EncryptToken(authToken);

                        LoginResponse responseData = new LoginResponse
                        {
                            AuthToken = encryptedToken,
                            //RefreshToken = refreshToken.Token,
                            User = user,
                        };
                        int totalComp = 0, totalUser = 0, totalDoctor = 0, totalTransDay = 0, totalTransMonth = 0;
                        //here for retreive the dashboard details
                        //if role==admin || user else super
                        //
                        GetDashboardDetailsDto userDashboard = new GetDashboardDetailsDto();
                        userDashboard.TotalDoctor = totalDoctor;
                        userDashboard.TotalCompany = totalComp;
                        userDashboard.TotalUser = totalUser;
                        userDashboard.TotalTransactionDay = totalTransDay;
                        userDashboard.TotalTransactionMonth = totalTransMonth;

                        responseData.DashboardDetails = userDashboard;

                        response.Data = responseData;
                        response.Success = true;
                        response.Message = "Authentication successful";

                    }

                }
            }
            catch (Exception ex)
            {
                response.Success = false; 
                response.Message = ex.Message;
                return response;
            }

            return response;

        }


        [Route("GetCompany")]
        [HttpGet]
        public async Task<List<GetCompany>> FetchCompany()
        {
            var compList = new List<GetCompany>();
            var CompanyID = HttpContext.Current.Items["CompanyID"]?.ToString();
            var username = HttpContext.Current.Items["UserID"]?.ToString();
            try
            {
                using (var connection = new OdbcConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    string query = "SELECT * FROM TBLCOMPANY";

                    using (var command = new OdbcCommand(query, connection))
                    using (OdbcDataReader reader = (OdbcDataReader)await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var company = new GetCompany
                            {
                                fcompid = reader["FCOMPID"].ToString(),
                                fcompname = reader["FCOMPNAME"].ToString()
                            };
                            compList.Add(company);
                        }
                    }
                }
            }
            catch (Exception ex)
            {   
                Console.WriteLine(ex.Message);
            }
            return compList;
        }
    }
}
