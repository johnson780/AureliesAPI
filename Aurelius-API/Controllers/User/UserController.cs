using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
//using System.Net.Mail;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Configuration;
using System.Data.Odbc;
using System.Globalization;
using Aurelius_API.ModelAPI;
using Aurelius_API.ModelAPI.User;
using Aurelius_API.ModelPortal.Company;
using MailKit.Net.Smtp;
using MimeKit;
using Org.BouncyCastle.Asn1.Tsp;

namespace Aurelius_API.Controllers.User
{
    [RoutePrefix("api/user")]
    public class UserController : ApiController
    {
        private readonly string _connectionString;

        public string FID { get; set; }
        public UserController()
        {

            _connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }

        [HttpPost]
        [Route("UserInfo")]

        public async Task<GeneralAPIResponse<AddUser>> UserInfo(AddUser model)
        {

            var userID = HttpContext.Current.Items["UserID"]?.ToString();
            string userid = Convert.ToString(userID);
            if (string.IsNullOrEmpty(userid))
            {
                userid = "";
            }
            var response = new GeneralAPIResponse<AddUser>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    MailSetting mailSetting = new MailSetting();
                    //Encryption.Encryption encrypt = new Encryption.Encryption();

                    // Check if username already exists
                    bool usernameExists = checkFUSERNAMEExists(model.FUSERNAME);
                    if (usernameExists)
                    {
                        response.Success = false;
                        response.Message = "Username already exists.";
                        return response;
                    }

                    // Generate temporary password
                    string temporaryPassword = mailSetting.GenerateTemporaryPassword();
                    //string encryptedPassword = encrypt.Encrypt("YourEncryptionKey", temporaryPassword);


                    if (model.FROLE.ToUpper().Contains("SUPER-ADMIN"))
                    {
                        FID = "SA";
                    }
                    if (model.FROLE.ToUpper().Contains("ADMIN") && model.FROLE.ToUpper().Contains("USER"))
                    {
                        FID = "US";
                    }

                    GetLastId classAInstance = new GetLastId();
                    int lastId = classAInstance.GetLastIdFromTable(_connectionString);
                    int usId = (lastId + 1);
                    string formattedNumber = usId.ToString("D4");
                    string ID = FID + formattedNumber;

                    // Example of inserting into TBLUSER
                    SqlCommand cmd = new SqlCommand(@"INSERT INTO ""TBLUSER"" (""FID"",""FUSERNAME"", ""FFULLNAME"", ""FPASSWORD"", ""FROLE"", ""FACTIVE"", ""FAUDTUSER"", ""FAUDTDATE"", ""FAUDTTIME"", ""FTEMPPWD"", ""FCOMPID"") " +
                                                                  "VALUES (@FID,@FUSERNAME, @FFULLNAME, @FPASSWORD, @FROLE, @FACTIVE ,@FAUDTUSER, @FAUDTDATE, @FAUDTTIME, @FTEMPPWD, @FCOMPID)", connection);
                    cmd.Parameters.AddWithValue("@FID", ID.Trim());
                    cmd.Parameters.AddWithValue("@FUSERNAME", model.FUSERNAME.Trim());
                    cmd.Parameters.AddWithValue("@FFULLNAME", model.FFULLNAME.Trim());
                    cmd.Parameters.AddWithValue("@FPASSWORD", temporaryPassword.Trim());
                    cmd.Parameters.AddWithValue("@FROLE", model.FROLE.Trim());
                    cmd.Parameters.AddWithValue("@FACTIVE", model.FACTIVE.Trim());
                    cmd.Parameters.AddWithValue("@FAUDTUSER", userID.Trim());
                    cmd.Parameters.AddWithValue("@FAUDTDATE", Convert.ToDecimal(DateTime.Now.ToString("yyyyMMdd")));
                    cmd.Parameters.AddWithValue("@FAUDTTIME", DateTime.Now.ToString("HHmmss"));
                    cmd.Parameters.AddWithValue("@FTEMPPWD", 1);
                    cmd.Parameters.AddWithValue("@FCOMPID", string.Join(",", model.FCOMPID));

                    await cmd.ExecuteNonQueryAsync();


                    emailUser(model.FUSERNAME, temporaryPassword, response);

                    response.Success = true;
                    response.Message = "User Created successfully.";
                    response.Data = null; // Optional: return some data if needed


                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                if (ex.InnerException.Message.Contains("duplicate key"))
                {
                    response.Message = "Username already exists!";
                }
                else
                {
                    response.Message = ex.InnerException.Message;
                }
                // Optionally log the exception
            }

            return response;
        }


        [HttpGet]
        [Route("ListSearchUsers")]
        public async Task<IHttpActionResult> SearchUsers(
            [FromUri] string USER = null,
            [FromUri] string FROLE = null,
          [FromUri] string FACTIVE = null)
        {
            
            var response = new GeneralAPIResponse<List<ListUserModel>>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Prepare the base query with JOIN to TBLUSER
                    string query = @"
                SELECT TU.""FID"", TU.""FUSERNAME"", TU.""FFULLNAME"", TU.""FROLE"", TU.""FACTIVE"", 
                       U.""FFULLNAME"" AS updatedUserName, TU.""FAUDTDATE"", TU.""FAUDTTIME""
                FROM ""TBLUSER"" TU  LEFT JOIN ""DFMAIN"".""TBLUSER"" U ON TU.""FAUDTUSER"" = U.""FID""
                WHERE 1=1";

                    // Build parameters dynamically based on provided inputs
                    List<SqlParameter> parameters = new List<SqlParameter>();

                    if (!string.IsNullOrEmpty(USER.Trim()))
                    {
                        query += @" AND (TU.""FUSERNAME"" LIKE ? OR TC.""FFULLNAME"" LIKE ?)";
                        parameters.Add(new SqlParameter("@FUSERNAME", $"%{USER.Trim()} %"));
                        parameters.Add(new SqlParameter("@FFULLNAME", $"%{USER.Trim()} %"));

                    }
                    if (!string.IsNullOrEmpty(FROLE))
                    {
                        query += @" AND TU.""FROLE"" = @FROLE";
                        parameters.Add(new SqlParameter("@FROLE", FROLE.Trim()));
                    }

                    if (!string.IsNullOrEmpty(FACTIVE))
                    {
                        query += @" AND TU.""FACTIVE"" = @FACTIVE";
                        parameters.Add(new SqlParameter("@FACTIVE", FACTIVE.Trim()));
                    }

                    // Execute the query asynchronously
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddRange(parameters.ToArray());

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            var users = new List<ListUserModel>();

                            while (await reader.ReadAsync())
                            {
                                var user = new ListUserModel
                                {
                                    FID = reader["FID"].ToString(),
                                    FUSERNAME = reader["FUSERNAME"].ToString(),
                                    FFULLNAME = reader["FFULLNAME"].ToString(),
                                    FROLE = reader["FROLE"].ToString(),
                                    FACTIVE = reader["FACTIVE"].ToString(),
                                    FAUDTUSER = reader["updatedUserName"].ToString(),
                                     FAUDTDATE= reader["FAUDTDATE"].ToString(),
                                    FAUDTTIME = reader["FAUDTTIME"].ToString(),
                                };
                                users.Add(user);

                            }

                            response.Data = users;
                            response.Success = true;
                            response.Message = $"{users.Count} users found.";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
                // Log the exception
            }

            return Ok(response);
        }











        public class MailSetting
        {
            
            public string GenerateTemporaryPassword()
            {
                Random random = new Random();
                const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
                return new string(Enumerable.Repeat(chars, 8).Select(s => s[random.Next(s.Length)]).ToArray());
            }
        }

            public class GetLastId
            {
                public int GetLastIdFromTable(string connectionString)
                {
                    int lastId = 0; // Default value in case no records are found

                    // Construct the SQL query to get the maximum value of the ID column
                    string sqlQuery = $@"SELECT MAX(""FID"") FROM ""TBLUSER""";

                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        using (SqlCommand command = new SqlCommand(sqlQuery, connection))
                        {
                            connection.Open();
                            // Execute the command and get the scalar result
                            object result = command.ExecuteScalar();
                            if (result != DBNull.Value && result != null)
                            {
                                result = result.ToString().Replace("US", "").Replace("SA", "");
                                // If a valid result is returned, convert it to an integer
                                lastId = Convert.ToInt32(result);
                            }
                        }
                    }

                    return lastId;
                }
            }

            public bool checkFUSERNAMEExists(string username)
            {
                bool usernameExists = false;
                try
                {
                    using (var connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();

                        string sqlQuery = @"SELECT COUNT(*) FROM TBLCONFIG WHERE FUSERNAME = @Username";

                        using (var command = new SqlCommand(sqlQuery, connection))
                        {
                            command.Parameters.AddWithValue("@Username", username);

                            // ExecuteScalar returns the first column of the first row
                            // in the result set returned by the query, in this case, the count.
                            int count = (int)command.ExecuteScalar();

                            // If count is greater than zero, the username exists
                            if (count > 0)
                            {
                                usernameExists = true;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Handle exception, log, or throw as needed
                    throw new ApplicationException("Error checking username existence.", ex);
                }

                return usernameExists;
            }


            private void emailUser(string username, string temporaryPassword, GeneralAPIResponse<AddUser> response)
            {
                try
                {
                    using (var connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();

                        // Fetch configuration details from TBLCONFIG
                        string sqlQuery = "SELECT * FROM TBLCONFIG";
                        SqlCommand command = new SqlCommand(sqlQuery, connection);
                        SqlDataReader reader = command.ExecuteReader();

                        string sEmail = "", sPaswd = "", server = "";
                        int port = 0;

                        // Read configuration details
                        while (reader.Read())
                        {
                            sEmail = reader["FSENDER"].ToString(); // Assuming FSENDER is the sender email address
                            sPaswd = reader["FUSERPWD"].ToString(); // Assuming FUSERPWD is the sender email password
                            server = reader["FMAILSERVER"].ToString(); // Assuming FMAILSERVER is the mail server
                            int.TryParse(reader["FPORT"].ToString(), out port); // Assuming FPORT is the port number
                        }

                        reader.Close();

                        string subject = "AH Temporary Password"; // Email subject
                        string body = $"Your temporary EZInvois password is: {temporaryPassword}";

                        var email = new MimeMessage();
                        email.From.Add(MailboxAddress.Parse(sEmail));
                        email.To.Add(MailboxAddress.Parse(username));
                        email.Subject = subject;
                        email.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = body };

                        var smtp = new SmtpClient();
                        smtp.AuthenticationMechanisms.Remove("XOAUTH2");
                        smtp.Connect(server, port, MailKit.Security.SecureSocketOptions.StartTls);
                        smtp.Authenticate(sEmail, sPaswd);
                        smtp.Send(email);
                        smtp.Disconnect(true);

                    response.Data = new AddUser { FUSERNAME = username }; // Assigning the username to response.Data
                    response.Message = "User Created successfully.";
                        response.Success = true;
                    }
                }
                catch (Exception ex)
                {
                    response.Success = false;
                    response.Message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                    // Optionally log the exception
                }
            }


    }
}