using Aurelius_API.ModelAPI;
using Aurelius_API.ModelAPI.Company;
using Aurelius_API.ModelPortal.Company;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Configuration;
using System.Data.Odbc;
using System.Globalization;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace Aurelius_API.Controllers.Company
{
    // POST http://<your-api-domain>/api/company/AddCompany
    [RoutePrefix("api/company")]
    public class CompanyController : ApiController
    {
        private readonly string _connectionString;

        public CompanyController()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }

        [HttpPost]
        [Route("AddCompany")]
        public async Task<GeneralAPIResponse<AddCompanyPortal>> CompanyInfo(AddCompanyPortal model)
        {
            var userID = HttpContext.Current.Items["UserID"]?.ToString();
            string userid=Convert.ToString(userID.Trim());
            if(string.IsNullOrEmpty(userid.Trim()))
            {
                userid = "";
            }
            //var CompanyID = HttpContext.Items["CompanyID"]?.ToString();
            //var username = HttpContext.Items["UserID"]?.ToString();
            //var password = HttpContext.Items["Password"]?.ToString();
            //var token = HttpContext.Items["EncryptedToken"]?.ToString();
            var response = new GeneralAPIResponse<AddCompanyPortal>();

            try
            {
                // Calculate the current date in the format yyyyMMdd
                string currentDate = DateTime.Now.ToString("yyyyMMdd");

                // Convert the formatted date string to a decimal
                decimal audtDate = decimal.Parse(currentDate.Trim());
                using (OdbcConnection connection = new OdbcConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Check if company with the same FCOMPID already exists
                    var companyExistsQuery = "SELECT COUNT(*) FROM TBLCOMPANY WHERE FCOMPID = ?";
                    using (OdbcCommand cmdExists = new OdbcCommand(companyExistsQuery, connection))
                    {
                        cmdExists.Parameters.AddWithValue("@FCOMPID", model.FCOMPID.Trim());
                        var result = await cmdExists.ExecuteScalarAsync();
                        int companyCount = Convert.ToInt32(result);

                        if (companyCount > 0)
                        {
                            response.Success = false;
                            response.Message = "Company with this ID already exists.";
                            return response;
                        }
                    }

                    // If company does not exist, proceed with insertion
                    var insertQuery = @"
                        INSERT INTO TBLCOMPANY (""FCOMPID"", ""FCOMPNAME"", ""FACTIVE"", ""FAUDTUSER"", ""FAUDTDATE"", ""FAUDTTIME"")
                        VALUES (?, ?, ?, ?, ?, ?);
                    ";
                   

                    using (OdbcCommand cmd = new OdbcCommand(insertQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@FCOMPID", model.FCOMPID.Trim());
                        cmd.Parameters.AddWithValue("@FCOMPNAME", model.FCOMPNAME.Trim());
                        cmd.Parameters.AddWithValue("@FACTIVE", model.FACTIVE.Trim());
                        cmd.Parameters.AddWithValue("@FAUDTUSER", userid.Trim());
                        cmd.Parameters.AddWithValue("@FAUDTDATE", audtDate);
                        cmd.Parameters.AddWithValue("@FAUDTTIME", DateTime.Now.ToString("HHmmss"));

                        //cmd.Parameters.Add(new OdbcParameter { Value = model.FCOMPID });
                        //cmd.Parameters.Add(new OdbcParameter { Value = model.FCOMPNAME });
                        //cmd.Parameters.Add(new OdbcParameter { Value = model.FACTIVE });
                        //cmd.Parameters.Add(new OdbcParameter { Value = userID });
                        //cmd.Parameters.Add(new OdbcParameter { Value = Convert.ToDecimal(DateTime.Now.ToString("yyyyMMdd")) });
                        //cmd.Parameters.Add(new OdbcParameter { Value = DateTime.Now.ToString("HHmmss") });
                        await cmd.ExecuteNonQueryAsync();
                    }
                    // Create a response object with all added data
                    var addedData = new AddCompanyPortal
                    {
                        FCOMPID = model.FCOMPID.Trim(),
                        FCOMPNAME = model.FCOMPNAME.Trim(),
                        FACTIVE = model.FACTIVE.Trim(),
                        FAUDTUSER = userid.Trim(),
                        FAUDTDATE = audtDate.ToString().Trim(),
                        FAUDTTIME = DateTime.Now.ToString("HHmmss")
                    };

                    response.Message = "Company Created successfully.";
                    response.Success = true;
                    response.Data = addedData; // Set the entire model to the Data property
                    return response;
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                if (ex.Message.Contains("duplicate key value"))
                {
                    response.Message = "Company already exists!";
                }
                else
                {
                    response.Message = ex.InnerException?.Message ?? ex.Message;
                }
                // Optionally log the exception
            }

            return response;
        }//


        [HttpPost]
        [Route("ListAllSearchCompany")]
        public async Task<IHttpActionResult> ListAllSearchCompany(
    
     [FromUri] string company = null,
     [FromUri] string active = null
     //[FromUri] string userId = null,
     //[FromUri] decimal? audtDate = null,
     //[FromUri] string audtTime = null
 )
        {
            var response = new GeneralAPIResponse<List<GetAllSearchCompany>>();

            try
            {
                using (OdbcConnection connection = new OdbcConnection(_connectionString))
                {
                    await connection.OpenAsync(); // Asynchronously open the connection

                    // Prepare the base query
                    //string query = $@"SELECT ""FCOMPID"", ""FCOMPNAME"", ""FACTIVE"", ""FAUDTUSER"", ""FAUDTDATE"", ""FAUDTTIME"" FROM ""TBLCOMPANY"" WHERE 1=1";
                    // Prepare the base query with JOIN to TBLUSER
                    string query = $@"
                SELECT TC.""FCOMPID"", TC.""FCOMPNAME"", TC.""FACTIVE"", TC.""FAUDTUSER"", TC.""FAUDTDATE"", TC.""FAUDTTIME"", TU.""FFULLNAME""
                FROM ""TBLCOMPANY"" TC
                LEFT JOIN ""DFMAIN"".""TBLUSER"" TU ON TC.""FAUDTUSER"" = TU.""FID""
                WHERE 1=1";

                    // Build parameters dynamically based on provided inputs
                    List<OdbcParameter> parameters = new List<OdbcParameter>();

                    if (!string.IsNullOrEmpty(company.Trim()))
                    {
                        query += @" AND (TC.""FCOMPID"" LIKE ? OR TC.""FCOMPNAME"" LIKE ?)";
                        parameters.Add(new OdbcParameter("@FCOMPID", $"%{company.Trim()}%"));
                        parameters.Add(new OdbcParameter("@FCOMPNAME", $"%{company.Trim()}%"));
                    }


                    if (!string.IsNullOrEmpty(active.Trim()))
                    {
                        query += @" AND TC.""FACTIVE"" = ?";
                        parameters.Add(new OdbcParameter("@FACTIVE", active.Trim()));
                    }
                    //if (!string.IsNullOrEmpty(userId))
                    //{
                    //    query += " AND FAUDTUSER = ?";
                    //    parameters.Add(new OdbcParameter("@FAUDTUSER", userId));
                    //}
                    //if (audtDate.HasValue)
                    //{
                    //    query += " AND FAUDTDATE = ?";
                    //    parameters.Add(new OdbcParameter("@FAUDTDATE", audtDate));
                    //}
                    //if (!string.IsNullOrEmpty(audtTime))
                    //{
                    //    query += " AND FAUDTTIME = ?";
                    //    parameters.Add(new OdbcParameter("@FAUDTTIME", audtTime));
                    //}

                    // Execute the query asynchronously
                    using (OdbcCommand command = new OdbcCommand(query, connection))
                    {
                        foreach (var param in parameters)
                        {
                            command.Parameters.Add(param);
                        }

                        using (OdbcDataReader reader =  command.ExecuteReader())
                        {
                            var companies = new List<GetAllSearchCompany>();

                            while (await reader.ReadAsync())
                            {
                                var companyList = new GetAllSearchCompany
                                {
                                    FCOMPID = reader["FCOMPID"].ToString().Trim(),
                                    FCOMPNAME = reader["FCOMPNAME"].ToString(),
                                    FACTIVE = reader["FACTIVE"].ToString(),
                                    //FAUDTUSER = reader["FAUDTUSER"].ToString(),
                                    FAUDTUSER = reader["FFULLNAME"] == DBNull.Value ? string.Empty : reader["FFULLNAME"].ToString(),
                                    FAUDTDATE = reader["FAUDTDATE"] == DBNull.Value ? string.Empty : DateTime.ParseExact(reader["FAUDTDATE"].ToString(), "yyyyMMdd", CultureInfo.InvariantCulture).ToString("dd/MM/yyyy"),
                                    //FAUDTTIME = reader["FAUDTTIME"] == DBNull.Value ? string.Empty : TimeSpan.ParseExact(reader["FAUDTTIME"].ToString(), "HHmmss", CultureInfo.InvariantCulture).ToString(@"HH\:mm\:ss"),
                                    FAUDTTIME = reader["FAUDTTIME"] == DBNull.Value? string.Empty : DateTime.ParseExact(reader["FAUDTTIME"].ToString(), "HHmmss", CultureInfo.InvariantCulture).ToString("hh:mm tt", CultureInfo.InvariantCulture),

                                };
                                companies.Add(companyList);
                            }

                            response.Data = companies;
                            response.Success = true;
                            response.Message = $"{companies.Count} companies found.";
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

            return Ok(response); // Use IHttpActionResult for return
        }



        [HttpPost]
        [Route("DeleteCompany")]
        public IHttpActionResult DeleteCompany([FromUri] string companyId)
        {
            var response = new GeneralAPIResponse<object>();

            try
            {
                //if (string.IsNullOrEmpty(companyId))
                //{
                //    response.Success = false;
                //    response.Message = "CompanyId cannot be null or empty.";
                //    return BadRequest(response);
                //}

                using (OdbcConnection connection = new OdbcConnection(_connectionString))
                {
                    connection.Open();

                    // Prepare the delete query
                    string deleteQuery = $@"DELETE FROM ""TBLCOMPANY"" WHERE ""FCOMPID"" = ?";

                    // Execute the query
                    using (OdbcCommand command = new OdbcCommand(deleteQuery, connection))
                    {
                        command.Parameters.AddWithValue("@FCOMPID", companyId.Trim());

                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            response.Success = true;
                            response.Message = $"Company with ID '{companyId.Trim()}' deleted successfully.";
                        }
                        else
                        {
                            response.Success = false;
                            response.Message = $"No company found with ID '{companyId.Trim()}'.";
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


        [HttpPut]
        [Route("UpdateCompanyInfo")]
        public async Task<IHttpActionResult> UpdateCompanyInfo(UpdateCompany model)
        {
            var response = new GeneralAPIResponse<UpdateCompany>();

            try
            {
                var userID = HttpContext.Current.Items["UserID"]?.ToString();
                string userid = Convert.ToString(userID.Trim());
                if (string.IsNullOrEmpty(userid.Trim()))
                {
                    userid = "";
                }

                using (OdbcConnection connection = new OdbcConnection(_connectionString.Trim()))
                {
                    await connection.OpenAsync();

                    // Check if company with the specified FCOMPID exists
                    var companyExistsQuery = @"SELECT COUNT(*) FROM ""TBLCOMPANY"" WHERE ""FCOMPID"" = ?";
                    using (OdbcCommand cmdExists = new OdbcCommand(companyExistsQuery.Trim(), connection))
                    {
                        cmdExists.Parameters.AddWithValue("@FCOMPID", model.FCOMPID);
                        var result = await cmdExists.ExecuteScalarAsync();
                        int companyCount = Convert.ToInt32(result);

                        if (companyCount == 0)
                        {
                            response.Success = false;
                            response.Message = "Company with this ID does not exist.";
                            return Ok(response);
                        }
                    }

                    // Proceed with updating the company
                    var updateQuery = @"
                UPDATE ""TBLCOMPANY""
                SET ""FCOMPNAME"" = ?,
                    ""FACTIVE"" = ?,
                    ""FAUDTUSER"" = ?,
                    ""FAUDTDATE"" = ?,
                    ""FAUDTTIME"" = ?
                WHERE ""FCOMPID"" = ?
            ";

                    using (OdbcCommand cmd = new OdbcCommand(updateQuery.Trim(), connection))
                    {
                        cmd.Parameters.AddWithValue("@FCOMPNAME", model.FCOMPNAME.Trim());
                        cmd.Parameters.AddWithValue("@FACTIVE", model.FACTIVE.Trim());
                        cmd.Parameters.AddWithValue("@FAUDTUSER", userid.Trim());
                        cmd.Parameters.AddWithValue("@FAUDTDATE", decimal.Parse(DateTime.Now.ToString("yyyyMMdd")));
                        cmd.Parameters.AddWithValue("@FAUDTTIME", DateTime.Now.ToString("HHmmss"));
                        cmd.Parameters.AddWithValue("@FCOMPID", model.FCOMPID.Trim());

                        int rowsAffected = await cmd.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            response.Success = true;
                            response.Message = $"Company with ID '{model.FCOMPID.Trim()}' updated successfully.";
                        }
                        else
                        {
                            response.Success = false;
                            response.Message = $"Failed to update company with ID '{model.FCOMPID.Trim()}'.";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.InnerException?.Message ?? ex.Message;
                // Optionally log the exception
            }

            return Ok(response);
        }

        [HttpGet]
        [Route("GetCompanyInfo")]
        public IHttpActionResult GetCompanyInfo(string companyId)
        {
            var response = new GeneralAPIResponse<ViewByCompanyID>();

            try
            {
                using (OdbcConnection connection = new OdbcConnection(_connectionString))
                {
                    connection.Open();

                    // Prepare the query to fetch FCOMPNAME and FACTIVE based on FCOMPID
                    string query = @"
                SELECT ""FCOMPNAME"", ""FACTIVE""
                FROM ""TBLCOMPANY""
                WHERE ""FCOMPID"" = ?
            ";

                    using (OdbcCommand command = new OdbcCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@FCOMPID", companyId.Trim());

                        using (OdbcDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var companyInfo = new ViewByCompanyID
                                {
                                    FCOMPID = companyId,
                                    FCOMPNAME = reader["FCOMPNAME"].ToString(),
                                    FACTIVE = reader["FACTIVE"].ToString()
                                };

                                response.Data = companyInfo;
                                response.Success = true;
                                response.Message = "Company information retrieved successfully.";
                            }
                            else
                            {
                                response.Success = false;
                                response.Message = $"No company found with ID '{companyId}'.";
                            }
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


    }

}
