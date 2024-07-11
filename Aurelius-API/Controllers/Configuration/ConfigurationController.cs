using Aurelius_API.ModelAPI.User;
using Aurelius_API.ModelAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Aurelius_API.ModelAPI.Configuration;
using static Aurelius_API.Controllers.User.UserController;
using System.Data.SqlClient;
using System.Security.Cryptography;
using static Org.BouncyCastle.Math.EC.ECCurve;
using Aurelius_API.ModelAPI.Company;
using System.Globalization;
using MimeKit;
using System.Data;
using Org.BouncyCastle.Asn1.Cmp;
using MailKit.Net.Smtp;
using EncryptDecrypt;

namespace Aurelius_API.Controllers.Configuration
{
    [RoutePrefix("api/Configuration")]
    public class ConfigurationController : ApiController
    {
        private readonly string _connectionString;

        [HttpPost]
        [Route("UserInfo")]
        public async Task<GeneralAPIResponse<ConfigurationModel>> AddEditConfiguration(ConfigurationModel model)
        {
            Encryption encrypt = new Encryption();
            Decryption decrypt = new Decryption();

            var userID = HttpContext.Current.Items["UserID"]?.ToString();
            string userid = Convert.ToString(userID);
            if (string.IsNullOrEmpty(userid))
            {
                userid = "";
            }

            var response = new GeneralAPIResponse<ConfigurationModel>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    if (string.IsNullOrEmpty(model.FID))
                    {
                        // Insert logic
                        //GetLastId classAInstance = new GetLastId();
                        int lastId = GetLastIdFromTable(_connectionString);
                        int usId = (lastId + 1);
                        string ID = usId.ToString("D4");

                        SqlCommand cmd = new SqlCommand(@"INSERT INTO ""TBLCONFIG"" (""FID"", ""FMAILSERVER"", ""FUSERNAME"", ""FUSERPWD"", ""FPORT"", ""FSENDER"", ""FUSESSL"") " +
                                                      "VALUES (@FID, @FMAILSERVER, @FUSERNAME, @FUSERPWD, @FPORT, @FSENDER, @FUSESSL)", connection);
                        cmd.Parameters.AddWithValue("@FID", ID);
                        cmd.Parameters.AddWithValue("@FMAILSERVER", model.FMAILSERVER.Trim());
                        cmd.Parameters.AddWithValue("@FUSERNAME", model.FUSERNAME.Trim());
                        cmd.Parameters.AddWithValue("@FUSERPWD", encrypt.Encrypt(model.FUSERPWD.Trim()));
                        cmd.Parameters.AddWithValue("@FPORT", model.FPORT.Trim());
                        cmd.Parameters.AddWithValue("@FSENDER", model.FSENDER.Trim());
                        cmd.Parameters.AddWithValue("@FUSESSL", model.FUSESSL.Trim());

                        await cmd.ExecuteNonQueryAsync();
                        var configuration = new ConfigurationModel
                        {
                            FID = ID,
                            FMAILSERVER = model.FMAILSERVER.Trim(),
                            FUSERNAME = model.FUSERNAME.Trim(),
                            FUSERPWD = encrypt.Encrypt(model.FUSERPWD.Trim()),
                            FPORT = model.FPORT.Trim(),
                            FSENDER = model.FSENDER.Trim(),
                            FUSESSL = model.FUSESSL.Trim()
                        };
                        response.Message = "Configuration added successfully.";
                        response.Success = true;
                        response.Data = configuration; // Set the entire model to the Data property
                        return response;

                    }
                    else
                    {
                        // Update logic
                        SqlCommand cmd = new SqlCommand(@"UPDATE ""TBLCONFIG"" SET ""FMAILSERVER"" 
                       = @FMAILSERVER, ""FUSERNAME"" = @FUSERNAME, ""FUSERPWD"" = @FUSERPWD, ""FPORT"" = @FPORT, ""FSENDER"" = @FSENDER,
                        ""FUSESSL"" = @FUSESSL WHERE ""FID"" = @FID", connection);
                        cmd.Parameters.AddWithValue("@FID", model.FID);

                        cmd.Parameters.AddWithValue("@FMAILSERVER", model.FMAILSERVER.Trim());
                        cmd.Parameters.AddWithValue("@FUSERNAME", model.FUSERNAME.Trim());
                        cmd.Parameters.AddWithValue("@FUSERPWD", encrypt.Encrypt(model.FUSERPWD.Trim()));
                        cmd.Parameters.AddWithValue("@FPORT", model.FPORT.Trim());
                        cmd.Parameters.AddWithValue("@FSENDER", model.FSENDER.Trim());
                        cmd.Parameters.AddWithValue("@FUSESSL", model.FUSESSL.Trim());

                        int rowsAffected = await cmd.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {

                            var configuration = new ConfigurationModel
                            {
                                FID = model.FID,
                                FMAILSERVER = model.FMAILSERVER.Trim(),
                                FUSERNAME = model.FUSERNAME.Trim(),
                                FUSERPWD = encrypt.Encrypt(model.FUSERPWD.Trim()),
                                FPORT = model.FPORT.Trim(),
                                FSENDER = model.FSENDER.Trim(),
                                FUSESSL = model.FUSESSL.Trim()
                            };
                            response.Message = "Configuration added successfully.";
                            response.Success = true;
                            response.Data = configuration; // Set the entire model to the Data property
                            return response;

                        }
                        else
                        {
                            response.Data = model;
                            response.Success = false;
                            response.Message = "No configuration found with the provided FID.";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error: {ex.Message}";
            }

            return response;
        }

        [HttpGet]
        [Route("ListConfiguration")]
        public async Task<IHttpActionResult> ListUsers()
        {

            var response = new GeneralAPIResponse<List<ConfigurationModel>>();
            Decryption decrypt = new Decryption();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string sqlQuery = @"SELECT ""FID"", ""FMAILSERVER"", ""FUSERNAME"", ""FUSERPWD"", ""FPORT"", ""FSENDER"", ""FUSESSL"" FROM ""TBLCONFIG""";

                    using (var command = new SqlCommand(sqlQuery, connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            var configuration = new List<ConfigurationModel>();

                            while (await reader.ReadAsync())
                            {
                                var user = new ConfigurationModel
                                {
                                    FID = reader["FID"].ToString(),
                                    FMAILSERVER = reader["FMAILSERVER"].ToString(),
                                    FUSERNAME = reader["FUSERNAME"].ToString(),
                                    FUSERPWD = decrypt.Decrypt(reader["FUSERPWD"].ToString()),
                                    FPORT = reader["FPORT"].ToString(),
                                    FSENDER = reader["FSENDER"].ToString(),
                                    FUSESSL = reader["FUSESSL"].ToString()
                                };

                                configuration.Add(user);
                                response.Data = configuration;
                                response.Success = true;
                                response.Message = $"{configuration.Count} configuration found.";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
                // Handle exceptions here
                //throw new ApplicationException("Error retrieving users from database.", ex);
            }

            return Ok(response); // Use IHttpActionResult for return
        }



        [HttpGet]
        [Route("SendEmail")]

        public async Task<GeneralAPIResponse<string>> SendEmail(sendEmailModel model)
        {
            var response = new GeneralAPIResponse<string>();
            //MailSetting mailSetting = new MailSetting();
            //Encryption.Encryption encrypt = new Encryption.Encryption();

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

                    string subject = "AH Configuration Email"; // Email subject
                    string body = $"Your test AH email";
                    try
                    {
                        //mailSetting.SendEmailConfig(sEmail, sPaswd, server, port, model.Email, subject, body);
                        string senderEml = sEmail; // Sender's email address
                        string senderPaswd = sPaswd; // Sender's email password

                        var email = new MimeMessage();
                        email.From.Add(MailboxAddress.Parse(sEmail));
                        email.To.Add(MailboxAddress.Parse(model.userEmail));
                        email.Subject = subject;
                        email.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = body };

                        var smtp = new SmtpClient();
                        smtp.AuthenticationMechanisms.Remove("XOAUTH2");
                        smtp.Connect(server, port, MailKit.Security.SecureSocketOptions.StartTls);
                        smtp.Authenticate(sEmail, sPaswd);
                        smtp.Send(email);
                        smtp.Disconnect(true);
                        response.Message = "Email send successfully.";
                        response.Success = true;
                    }
                    catch (Exception ex)
                    {
                        response.Message = ex.Message;
                        response.Success = false;
                    }
                    //response.Data = configuration;

                    return response;
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
                // Optionally log the exception
            }

            return response;

        }




        public int GetLastIdFromTable(string connectionString)
        {
            int lastId = 0; // Default value in case no records are found

            // Construct the SQL query to get the maximum value of the ID column
            string sqlQuery = $@"SELECT MAX(""FID"") FROM ""TBLCONFIG""";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand(sqlQuery, connection))
                {
                    connection.Open();
                    // Execute the command and get the scalar result
                    object result = command.ExecuteScalar();
                    if (result != DBNull.Value && result != null)
                    {
                        result = result.ToString();
                        // If a valid result is returned, convert it to an integer
                        lastId = Convert.ToInt32(result);
                    }
                }
            }

            return lastId;
        }



    }
}