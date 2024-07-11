using Aurelius_API.ModelAPI;
using Aurelius_API.ModelAPI.Company;
using Aurelius_API.ModelAPI.Doctor;
using Aurelius_API.ModelPortal.Doctor;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Odbc;
using System.Data.SqlClient;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace Aurelius_API.Controllers.Doctor
{
    [RoutePrefix("api/doctor")]
    public class DoctorController : ApiController
    {
        private readonly string _connectionString;

        public DoctorController()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }

        [Route("GetDoctorList")]
        [HttpGet]
        public async Task<GeneralAPIResponse<List<GetAllDoctorSearch>>> SearchDoctor(
[FromUri] string doctor = null,
[FromUri] string contract = null,
[FromUri] decimal? dateFrom = null,
[FromUri] decimal? dateTo = null)
        {
            var CompanyID = HttpContext.Current.Items["CompanyID"]?.ToString();
            var username = HttpContext.Current.Items["UserID"]?.ToString();
            var response = new GeneralAPIResponse<List<GetAllDoctorSearch>>();
            decimal currentDate = Convert.ToDecimal(DateTime.Now.ToString("yyyyMMdd"));

            try
            {

                using (var connection = new OdbcConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"
    WITH DocContract AS (
        SELECT 
            ""FDOCCODE"", ""FCONTRACT"", ""FSTARTDATE"", ""FENDDATE"",
            ROW_NUMBER() OVER (PARTITION BY ""FDOCCODE"" ORDER BY ""FSTARTDATE"" DESC) AS rn
        FROM 
            ""TBLDOCCONTRACT""
        WHERE 
            ? BETWEEN ""FSTARTDATE"" AND ""FENDDATE""
    ),
    DocAdminCharge AS (
        SELECT 
            ""FDOCCODE"", ""FAMOUNT"",
            ROW_NUMBER() OVER (PARTITION BY ""FDOCCODE"" ORDER BY ""FSTARTDATE"" DESC) AS rn
        FROM 
            ""TBLDOCADMINCHARGE""
        WHERE 
            ? BETWEEN ""FSTARTDATE"" AND ""FENDDATE""
    ),
    DocFacCharge AS (
        SELECT 
            ""FDOCCODE"", ""FAMOUNT"",
            ROW_NUMBER() OVER (PARTITION BY ""FDOCCODE"" ORDER BY ""FSTARTDATE"" DESC) AS rn
        FROM 
            ""TBLDOCFACCHARGE""
        WHERE 
            ? BETWEEN ""FSTARTDATE"" AND ""FENDDATE""
    ),
    DocGmi AS (
        SELECT 
            ""FDOCCODE"", ""FAMOUNT"",""FSTARTDATE"", ""FENDDATE"",
            ROW_NUMBER() OVER (PARTITION BY ""FDOCCODE"" ORDER BY ""FSTARTDATE"" DESC) AS rn
        FROM 
            ""TBLDOCGMI""
        WHERE 
            ? BETWEEN ""FSTARTDATE"" AND ""FENDDATE""
    )
    SELECT 
        doclist.""FDOCCODE"",
        doclist.""FDOCNAME"",
        doclist.""FAUDTDATE"",
        doclist.""FAUDTTIME"",
        doclist.""FAUDTUSER"",
        doccontract.""FCONTRACT"",
        doccontract.""FSTARTDATE"",
        doccontract.""FENDDATE"",
        docadmincharge.""FAMOUNT"" AS AdminChargeAmount,
        docfaccharge.""FAMOUNT"" AS FacChargeAmount,
        docgmi.""FAMOUNT"" AS GmiAmount,
        docgmi.""FSTARTDATE"" As GmiStartDate,
        docgmi.""FENDDATE"" As GmiEndDate
    FROM 
        ""TBLDOCLIST"" doclist
    LEFT JOIN 
        DocContract doccontract 
    ON 
        doclist.""FDOCCODE"" = doccontract.""FDOCCODE"" AND doccontract.rn = 1
    LEFT JOIN 
        DocAdminCharge docadmincharge 
    ON 
        doclist.""FDOCCODE"" = docadmincharge.""FDOCCODE"" AND docadmincharge.rn = 1
    LEFT JOIN 
        DocFacCharge docfaccharge 
    ON 
        doclist.""FDOCCODE"" = docfaccharge.""FDOCCODE"" AND docfaccharge.rn = 1
    LEFT JOIN 
        DocGmi docgmi 
    ON 
        doclist.""FDOCCODE"" = docgmi.""FDOCCODE"" AND docgmi.rn = 1
    WHERE 
        1 = 1 ";

                    // Append conditions for startDate and endDate if both are provided
                    if (dateFrom.HasValue && dateTo.HasValue)
                    {
                        query += " AND doccontract.\"FSTARTDATE\" <= ? AND doccontract.\"FENDDATE\" >= ?";
                    }
                    else if (dateFrom.HasValue)
                    {
                        query += " AND doccontract.\"FENDDATE\" >= ?";
                    }
                    else if (dateTo.HasValue)
                    {
                        query += " AND doccontract.\"FSTARTDATE\" <= ?";
                    }

                    // Append condition for searchText if provided
                    if (!string.IsNullOrEmpty(doctor))
                    {
                        query += " AND (doclist.\"FDOCCODE\" LIKE ? OR doclist.\"FDOCNAME\" LIKE ?)";
                    }

                    // Append condition for contractType if provided
                    if (!string.IsNullOrEmpty(contract))
                    {
                        query += " AND doccontract.\"FCONTRACT\" = ?";
                    }

                    query += @"
    GROUP BY 
        doclist.""FDOCCODE"", 
        doclist.""FDOCNAME"", 
        doclist.""FAUDTDATE"", 
        doclist.""FAUDTTIME"", 
        doclist.""FAUDTUSER"", 
        doccontract.""FCONTRACT"", 
        doccontract.""FSTARTDATE"", 
        doccontract.""FENDDATE"", 
        docadmincharge.""FAMOUNT"", 
        docfaccharge.""FAMOUNT"", 
        docgmi.""FAMOUNT"",
         docgmi.""FSTARTDATE"",
         docgmi.""FENDDATE""
ORDER BY 
doccontract.""FENDDATE"" DESC;";

                    using (var command = new OdbcCommand(query, connection))
                    {
                        int paramIndex = 0; // Counter for parameter indexing

                        command.Parameters.Add(new OdbcParameter($"@currentdate1", currentDate));
                        command.Parameters.Add(new OdbcParameter($"@currentdate2", currentDate));
                        command.Parameters.Add(new OdbcParameter($"@currentdate3", currentDate));
                        command.Parameters.Add(new OdbcParameter($"@currentdate4", currentDate));

                        // Add parameters for startDate and endDate if both are provided
                        if (dateTo != null && dateFrom != null)
                        {
                            command.Parameters.Add(new OdbcParameter($"@startDate{paramIndex}", dateTo));
                            command.Parameters.Add(new OdbcParameter($"@endDate{paramIndex++}", dateFrom));
                        }
                        else if (dateFrom != null)
                        {
                            command.Parameters.Add(new OdbcParameter($"@endDate{paramIndex++}", dateFrom));
                        }
                        else if (dateTo != null)
                        {
                            command.Parameters.Add(new OdbcParameter($"@startDate{paramIndex++}", dateTo));
                        }

                        // Add parameters for searchText if provided
                        if (!string.IsNullOrEmpty(doctor))
                        {
                            command.Parameters.Add(new OdbcParameter($"@docCode{paramIndex++}", $"%{doctor}%"));
                            command.Parameters.Add(new OdbcParameter($"@docName{paramIndex++}", $"%{doctor}%"));
                        }

                        // Add parameter for contractType if provided
                        if (!string.IsNullOrEmpty(contract))
                        {
                            command.Parameters.Add(new OdbcParameter($"@contract{paramIndex++}", contract));
                        }

                        int doctorCount = 0;

                        using (OdbcDataReader reader = (OdbcDataReader)await command.ExecuteReaderAsync())
                        {
                            if (reader.HasRows)
                            {
                                var searchResponseList = new List<GetAllDoctorSearch>();

                                while (reader.Read())
                                {

                                    var searchResponse = new GetAllDoctorSearch
                                    {
                                        FDOCCODE = reader["FDOCCODE"].ToString(),
                                        FDOCNAME = reader["FDOCNAME"].ToString(),
                                        FAUDTDATE = reader["FAUDTDATE"] == DBNull.Value ? string.Empty : DateTime.ParseExact(reader["FAUDTDATE"].ToString(), "yyyyMMdd", CultureInfo.InvariantCulture).ToString("dd/MM/yyyy"),
                                        FAUDTTIME = reader["FAUDTTIME"] == DBNull.Value ? string.Empty : ConvertTo12HourFormat(reader["FAUDTTIME"].ToString()),
                                        FAUDTUSER = reader["FAUDTUSER"].ToString(),
                                        FCONTRACT = reader["FCONTRACT"].ToString(),
                                        CONTRACTSTARTDATE = reader["FSTARTDATE"] == DBNull.Value ? string.Empty : DateTime.ParseExact(reader["FSTARTDATE"].ToString(), "yyyyMMdd", CultureInfo.InvariantCulture).ToString("dd/MM/yyyy"),
                                        CONTRACTENDDATE = reader["FENDDATE"] == DBNull.Value ? string.Empty : DateTime.ParseExact(reader["FENDDATE"].ToString(), "yyyyMMdd", CultureInfo.InvariantCulture).ToString("dd/MM/yyyy"),
                                        GMISTARTDATE = reader["GmiStartDate"] == DBNull.Value ? string.Empty : DateTime.ParseExact(reader["GmiStartDate"].ToString(), "yyyyMMdd", CultureInfo.InvariantCulture).ToString("dd/MM/yyyy"),
                                        GMIENDDATE = reader["GmiEndDate"] == DBNull.Value ? string.Empty : DateTime.ParseExact(reader["GmiEndDate"].ToString(), "yyyyMMdd", CultureInfo.InvariantCulture).ToString("dd/MM/yyyy"),
                                        GMIAMOUNT = reader["GmiAmount"] == DBNull.Value ? "0.00" : Convert.ToDecimal(reader["GmiAmount"]).ToString("N2", CultureInfo.InvariantCulture),
                                       ADMINCHARGE = reader["AdminChargeAmount"] == DBNull.Value ? "0.00" : Convert.ToDecimal(reader["AdminChargeAmount"]).ToString("N2", CultureInfo.InvariantCulture),
                                        FACILITYCHARGE = reader["FacChargeAmount"] == DBNull.Value ? "0.00" : Convert.ToDecimal(reader["AdminChargeAmount"]).ToString("N2", CultureInfo.InvariantCulture),
                         
                                    };

                                    searchResponseList.Add(searchResponse);
                                }
                              

                                doctorCount = searchResponseList.Count();

                                if (doctorCount <= 0)
                                {
                                    response.Success = false;
                                    response.Message = "No doctor found";
                                }
                                else
                                {
                                    response.Data = searchResponseList;
                                    response.Success = true;
                                    response.Message = doctorCount + " Doctor(s) found.";
                                }


                            }
                            else
                            {
                                response.Success = false;
                                response.Message = "No doctor found";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }

            return response;
        }

        [Route("GetDoctorInfo")]
        [HttpGet]
        public async Task<DoctorInfo> GetDoctorInfo([FromUri]string doctorCode)
        {
            var doctor = new DoctorInfo();
            var CompanyID = HttpContext.Current.Items["CompanyID"]?.ToString();
            var userid = HttpContext.Current.Items["UserID"]?.ToString();

            try
            {
                using (var connection = new OdbcConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"
        SELECT TBLDOCLIST.*, TBLUSER.""FFULLNAME""
        FROM TBLDOCLIST
        LEFT JOIN TBLUSER ON TBLDOCLIST.""FAUDTUSER""= TBLUSER.""FID""
        WHERE TBLDOCLIST.""FDOCCODE"" = ?";

                    using (var command = new OdbcCommand(query, connection)) {
                        command.Parameters.Add(new OdbcParameter("@FDOCCODE", doctorCode));

                        using (OdbcDataReader reader = (OdbcDataReader)await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                               doctor = new DoctorInfo
                                {
                                    FDOCCODE = reader["FDOCCODE"].ToString(),
                                    FDOCNAME = reader["FDOCNAME"].ToString(),
                                    FAUDTUSER = reader["FFULLNAME"].ToString(),
                                    FAUDTDATE = reader["FAUDTDATE"] == DBNull.Value ? string.Empty : DateTime.ParseExact(reader["FAUDTDATE"].ToString(), "yyyyMMdd", CultureInfo.InvariantCulture).ToString("dd/MM/yyyy"),
                                    FAUDTTIME = reader["FAUDTTIME"] == DBNull.Value ? string.Empty : ConvertTo12HourFormat(reader["FAUDTTIME"].ToString()),
                                };
                            }
                            else
                            {
                                doctor = new DoctorInfo
                                {
                                    FDOCCODE = "",
                                    FDOCNAME = "",
                                    FAUDTUSER ="",
                                    FAUDTDATE ="",
                                    FAUDTTIME =""
                                };
                            }
                            reader.Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return doctor;
        }

        [Route("GetDoctorContract")]
        [HttpGet]
        public async Task<List<DoctorContract>> GetDoctorContract([FromUri] string doctorCode)
        {
            var doctorContract = new List<DoctorContract>();
            var CompanyID = HttpContext.Current.Items["CompanyID"]?.ToString();
            var userid = HttpContext.Current.Items["UserID"]?.ToString();

            try
            {
                using (var connection = new OdbcConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"
        SELECT TBLDOCCONTRACT.*, TBLUSER.""FFULLNAME""
        FROM TBLDOCCONTRACT
        LEFT JOIN TBLUSER ON TBLDOCCONTRACT.""FAUDTUSER""= TBLUSER.""FID""
        WHERE TBLDOCCONTRACT.""FDOCCODE"" = ?";

                    using (var command = new OdbcCommand(query, connection))
                    {
                        command.Parameters.Add(new OdbcParameter("@FDOCCODE", doctorCode));

                        using (OdbcDataReader reader = (OdbcDataReader)await command.ExecuteReaderAsync())
                        {
                            var contract = new DoctorContract();

                           while (await reader.ReadAsync())
                            {
                                contract = new DoctorContract
                                {
                                    FID = reader["FID"].ToString(),
                                    FCONTRACT = reader["FCONTRACT"].ToString(),
                                    FDOCCODE = reader["FDOCCODE"].ToString(),
                                    FSTARTDATE = reader["FSTARTDATE"] == DBNull.Value ? string.Empty : DateTime.ParseExact(reader["FSTARTDATE"].ToString(), "yyyyMMdd", CultureInfo.InvariantCulture).ToString("dd/MM/yyyy"),
                                    FENDDATE = reader["FENDDATE"] == DBNull.Value ? string.Empty : DateTime.ParseExact(reader["FENDDATE"].ToString(), "yyyyMMdd", CultureInfo.InvariantCulture).ToString("dd/MM/yyyy"),
                                    FAUDTUSER = reader["FFULLNAME"].ToString(),
                                    FAUDTDATE = reader["FAUDTDATE"] == DBNull.Value ? string.Empty : DateTime.ParseExact(reader["FAUDTDATE"].ToString(), "yyyyMMdd", CultureInfo.InvariantCulture).ToString("dd/MM/yyyy"),                                  
                                    FAUDTTIME = reader["FAUDTTIME"] == DBNull.Value ? string.Empty : ConvertTo12HourFormat(reader["FAUDTTIME"].ToString()),
                                };
                                doctorContract.Add(contract);
                            }
                   
                            reader.Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return doctorContract;
        }

        [Route("GetDoctorGMI")]
        [HttpGet]
        public async Task<List<DoctorGMI>> GetDoctorGMI([FromUri] string doctorCode)
        {
            var doctorGMI = new List<DoctorGMI>();
            var CompanyID = HttpContext.Current.Items["CompanyID"]?.ToString();
            var userid = HttpContext.Current.Items["UserID"]?.ToString();

            try
            {
                using (var connection = new OdbcConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"
        SELECT TBLDOCGMI.*, TBLUSER.""FFULLNAME""
        FROM TBLDOCGMI
        LEFT JOIN TBLUSER ON TBLDOCGMI.""FAUDTUSER""= TBLUSER.""FID""
        WHERE TBLDOCGMI.""FDOCCODE"" = ?";

                    using (var command = new OdbcCommand(query, connection))
                    {
                        command.Parameters.Add(new OdbcParameter("@FDOCCODE", doctorCode));

                        using (OdbcDataReader reader = (OdbcDataReader)await command.ExecuteReaderAsync())
                        {
                            var gmi = new DoctorGMI();

                            while (await reader.ReadAsync())
                            {
                                gmi = new DoctorGMI
                                {
                                    FID = reader["FID"].ToString(),
                                    FAMOUNT = reader["FCONTRACT"].ToString(),
                                    FDOCCODE = reader["FDOCCODE"].ToString(),
                                    FSTARTDATE = reader["FSTARTDATE"]==DBNull.Value ? string.Empty : DateTime.ParseExact(reader["FSTARTDATE"].ToString(), "yyyyMMdd", CultureInfo.InvariantCulture).ToString("dd/MM/yyyy"),
                                    FENDDATE = reader["FENDDATE"]==DBNull.Value ? string.Empty : DateTime.ParseExact(reader["FENDDATE"].ToString(), "yyyyMMdd", CultureInfo.InvariantCulture).ToString("dd/MM/yyyy"),
                                    FAUDTUSER = reader["FFULLNAME"].ToString(),
                                    FAUDTDATE = reader["FAUDTDATE"] == DBNull.Value ? string.Empty : DateTime.ParseExact(reader["FAUDTDATE"].ToString(), "yyyyMMdd", CultureInfo.InvariantCulture).ToString("dd/MM/yyyy"),
                                    FAUDTTIME = reader["FAUDTTIME"] == DBNull.Value ? string.Empty : ConvertTo12HourFormat(reader["FAUDTTIME"].ToString()),
                                };
                                doctorGMI.Add(gmi);
                            }

                            reader.Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return doctorGMI;
        }

        [Route("GetDoctorAdminCharges")]
        [HttpGet]
        public async Task<List<DoctorAdminCharges>> GetDoctorAdminCharges([FromUri] string doctorCode)
        {
            var doctorAdminCharges = new List<DoctorAdminCharges>();
            var CompanyID = HttpContext.Current.Items["CompanyID"]?.ToString();
            var userid = HttpContext.Current.Items["UserID"]?.ToString();

            try
            {
                using (var connection = new OdbcConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"
        SELECT TBLDOCADMINCHARGE.*, TBLUSER.""FFULLNAME""
        FROM TBLDOCADMINCHARGE
        LEFT JOIN TBLUSER ON TBLDOCADMINCHARGE.""FAUDTUSER""= TBLUSER.""FID""
        WHERE TBLDOCADMINCHARGE.""FDOCCODE"" = ?";

                    using (var command = new OdbcCommand(query, connection))
                    {
                        command.Parameters.Add(new OdbcParameter("@FDOCCODE", doctorCode));

                        using (OdbcDataReader reader = (OdbcDataReader)await command.ExecuteReaderAsync())
                        {
                            var adminCharges = new DoctorAdminCharges();

                            while (await reader.ReadAsync())
                            {
                                adminCharges = new DoctorAdminCharges
                                {
                                    FID = reader["FID"].ToString(),
                                    FAMOUNT = reader["FCONTRACT"].ToString(),
                                    FCHARGERATE = reader["FCHARGERATE"].ToString(),
                                    FDISCOUNTRATE = reader["FDISCOUNTRATE"].ToString(),
                                    FDOCCODE = reader["FDOCCODE"].ToString(),
                                    FSTARTDATE = reader["FSTARTDATE"] == DBNull.Value ? string.Empty : DateTime.ParseExact(reader["FSTARTDATE"].ToString(), "yyyyMMdd", CultureInfo.InvariantCulture).ToString("dd/MM/yyyy"),
                                    FENDDATE = reader["FENDDATE"] == DBNull.Value ? string.Empty : DateTime.ParseExact(reader["FENDDATE"].ToString(), "yyyyMMdd", CultureInfo.InvariantCulture).ToString("dd/MM/yyyy"),
                                    FAUDTUSER = reader["FFULLNAME"].ToString(),
                                    FAUDTDATE = reader["FAUDTDATE"] == DBNull.Value ? string.Empty : DateTime.ParseExact(reader["FAUDTDATE"].ToString(), "yyyyMMdd", CultureInfo.InvariantCulture).ToString("dd/MM/yyyy"),
                                    FAUDTTIME = reader["FAUDTTIME"] == DBNull.Value ? string.Empty : ConvertTo12HourFormat(reader["FAUDTTIME"].ToString()),
                                };
                                doctorAdminCharges.Add(adminCharges);
                            }

                            reader.Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return doctorAdminCharges;
        }

        [Route("GetDoctorFacilityCharges")]
        [HttpGet]
        public async Task<List<DoctorFacilityCharges>> GetDoctorFacilityCharges([FromUri] string doctorCode)
        {
            var doctorFacilityCharges = new List<DoctorFacilityCharges>();
            var CompanyID = HttpContext.Current.Items["CompanyID"]?.ToString();
            var userid = HttpContext.Current.Items["UserID"]?.ToString();

            try
            {
                using (var connection = new OdbcConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"
        SELECT TBLDOCFACCHARGE.*, TBLUSER.""FFULLNAME""
        FROM TBLDOCFACCHARGE
        LEFT JOIN TBLUSER ON TBLDOCFACCHARGE.""FAUDTUSER""= TBLUSER.""FID""
        WHERE TBLDOCFACCHARGE.""FDOCCODE"" = ?";

                    using (var command = new OdbcCommand(query, connection))
                    {
                        command.Parameters.Add(new OdbcParameter("@FDOCCODE", doctorCode));

                        using (OdbcDataReader reader = (OdbcDataReader)await command.ExecuteReaderAsync())
                        {
                            var facilityCharges = new DoctorFacilityCharges();

                            while (await reader.ReadAsync())
                            {
                                facilityCharges = new DoctorFacilityCharges
                                {
                                    FID = reader["FID"].ToString(),
                                    FAMOUNT = reader["FCONTRACT"].ToString(),                                
                                    FDOCCODE = reader["FDOCCODE"].ToString(),
                                    FSTARTDATE = reader["FSTARTDATE"] == DBNull.Value ? string.Empty : DateTime.ParseExact(reader["FSTARTDATE"].ToString(), "yyyyMMdd", CultureInfo.InvariantCulture).ToString("dd/MM/yyyy"),
                                    FENDDATE = reader["FENDDATE"] == DBNull.Value ? string.Empty : DateTime.ParseExact(reader["FENDDATE"].ToString(), "yyyyMMdd", CultureInfo.InvariantCulture).ToString("dd/MM/yyyy"),
                                    FAUDTUSER = reader["FFULLNAME"].ToString(),
                                    FAUDTDATE = reader["FAUDTDATE"] == DBNull.Value ? string.Empty : DateTime.ParseExact(reader["FAUDTDATE"].ToString(), "yyyyMMdd", CultureInfo.InvariantCulture).ToString("dd/MM/yyyy"),
                                    FAUDTTIME = reader["FAUDTTIME"] == DBNull.Value ? string.Empty : ConvertTo12HourFormat(reader["FAUDTTIME"].ToString()),
                                };
                                doctorFacilityCharges.Add(facilityCharges);
                            }

                            reader.Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return doctorFacilityCharges;
        }

        [Route("DeleteDoctorContract")]
        [HttpDelete]
        public async Task <IHttpActionResult> DeleteDoctorContract([FromUri] string rowID, [FromUri] string doctorCode)
        {
            var response = new GeneralAPIResponse<string>();
            var CompanyID = HttpContext.Current.Items["CompanyID"]?.ToString();
            var userid = HttpContext.Current.Items["UserID"]?.ToString();

            try
            {
                using (var connection = new OdbcConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"
        DELETE
        FROM TBLDOCCONTRACT
        WHERE TBLDOCCONTRACT.""FID"" = ? AND TBLDOCCONTRACT.""FDOCCODE"" = ? ";

                    using (var command = new OdbcCommand(query, connection))
                    {
                        command.Parameters.Add(new OdbcParameter("@FID", rowID));
                        command.Parameters.Add(new OdbcParameter("@FDOCCODE", doctorCode));

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        if (rowsAffected > 0)
                        {
                            response.Success = true;
                            response.Message = "Doctor contract deleted successfully.";
                        }
                        else
                        {
                            response.Success = false;
                            response.Message = "No matching record found.";
                           
                        }
                      
                    }
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error : {ex.Message}";
          
            }
            return Ok(response);
        }

        [Route("DeleteDoctorGMI")]
        [HttpDelete]
        public async Task<IHttpActionResult> DeleteDoctorGMI([FromUri] string rowID, [FromUri] string doctorCode)
        {
            var response = new GeneralAPIResponse<string>();
            var CompanyID = HttpContext.Current.Items["CompanyID"]?.ToString();
            var userid = HttpContext.Current.Items["UserID"]?.ToString();

            try
            {
                using (var connection = new OdbcConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"
        DELETE
        FROM TBLDOCGMI
        WHERE TBLDOCGMI.""FID"" = ? AND TBLDOCGMI.""FDOCCODE"" = ? ";

                    using (var command = new OdbcCommand(query, connection))
                    {
                        command.Parameters.Add(new OdbcParameter("@FID", rowID));
                        command.Parameters.Add(new OdbcParameter("@FDOCCODE", doctorCode));

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        if (rowsAffected > 0)
                        {
                            response.Success = true;
                            response.Message = "Doctor GMI deleted successfully.";
                        }
                        else
                        {
                            response.Success = false;
                            response.Message = "No matching record found.";

                        }

                    }
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error : {ex.Message}";

            }
            return Ok(response);
        }

        [Route("DeleteDoctorAdminCharges")]
        [HttpDelete]
        public async Task<IHttpActionResult> DeleteDoctorAdminCharges([FromUri] string rowID, [FromUri] string doctorCode)
        {
            var response = new GeneralAPIResponse<string>();
            var CompanyID = HttpContext.Current.Items["CompanyID"]?.ToString();
            var userid = HttpContext.Current.Items["UserID"]?.ToString();

            try
            {
                using (var connection = new OdbcConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"
        DELETE
        FROM TBLDOCADMINCHARGE
        WHERE TBLDOCADMINCHARGE.""FID"" = ? AND TBLDOCADMINCHARGE.""FDOCCODE"" = ? ";

                    using (var command = new OdbcCommand(query, connection))
                    {
                        command.Parameters.Add(new OdbcParameter("@FID", rowID));
                        command.Parameters.Add(new OdbcParameter("@FDOCCODE", doctorCode));

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        if (rowsAffected > 0)
                        {
                            response.Success = true;
                            response.Message = "Doctor Administrative Charge deleted successfully.";
                        }
                        else
                        {
                            response.Success = false;
                            response.Message = "No matching record found.";

                        }

                    }
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error : {ex.Message}";

            }
            return Ok(response);
        }

        [Route("DeleteDoctorFacilityCharges")]
        [HttpDelete]
        public async Task<IHttpActionResult> DeleteDoctorFacilityCharges([FromUri] string rowID, [FromUri] string doctorCode)
        {
            var response = new GeneralAPIResponse<string>();
            var CompanyID = HttpContext.Current.Items["CompanyID"]?.ToString();
            var userid = HttpContext.Current.Items["UserID"]?.ToString();

            try
            {
                using (var connection = new OdbcConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"
        DELETE
        FROM TBLDOCFACCHARGE
        WHERE TBLDOCFACCHARGE.""FID"" = ? AND TBLDOCFACCHARGE.""FDOCCODE"" = ? ";

                    using (var command = new OdbcCommand(query, connection))
                    {
                        command.Parameters.Add(new OdbcParameter("@FID", rowID));
                        command.Parameters.Add(new OdbcParameter("@FDOCCODE", doctorCode));

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        if (rowsAffected > 0)
                        {
                            response.Success = true;
                            response.Message = "Doctor Facility Charge deleted successfully.";
                        }
                        else
                        {
                            response.Success = false;
                            response.Message = "No matching record found.";

                        }

                    }
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error : {ex.Message}";

            }
            return Ok(response);
        }

        [Route("SaveDoctorContract")]
        [HttpPost]
        public async Task<IHttpActionResult> SaveDoctorContract(PostDoctorContractDto dto)
        {
            var response = new GeneralAPIResponse<string>();
            var CompanyID = HttpContext.Current.Items["CompanyID"]?.ToString();
            var userid = HttpContext.Current.Items["UserID"]?.ToString();

            decimal currentDate = Convert.ToDecimal(DateTime.Now.ToString("yyyyMMdd"));
            DateTime currentTime = DateTime.Now;
            string formattedTime = currentTime.ToString("HHmmss");

            try
            {
                using (var connection = new OdbcConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Check if fdoccode exists
                    string checkQuery = @"SELECT COUNT(*) FROM TBLDOCLIST WHERE ""FDOCCODE"" = ?";
                    using (var checkCommand = new OdbcCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.Add(new OdbcParameter("@FODCCODE", dto.fdoccode));
                        int count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());

                        if (count > 0)
                        {
                            // Update existing record
                            string updateQuery = @"
                        UPDATE TBLDOCLIST 
                        SET ""FAUDTDATE"" = ?, ""FAUDTTIME"" = ?, ""FAUDTUSER"" = ? 
                        WHERE ""FDOCCODE"" = ?";
                            using (var updateCommand = new OdbcCommand(updateQuery, connection))
                            {
                                updateCommand.Parameters.Add(new OdbcParameter("@FAUDTDATE",currentDate));
                                updateCommand.Parameters.Add(new OdbcParameter("@FAUDTTIME", formattedTime));
                                updateCommand.Parameters.Add(new OdbcParameter("@FAUDTUSER", userid));
                                updateCommand.Parameters.Add(new OdbcParameter("@FDOCCODE", dto.fdoccode));

                                int rowsAffected = await updateCommand.ExecuteNonQueryAsync();
                                response.Success = rowsAffected > 0;
                                response.Message += rowsAffected > 0
                                                            ? $"Doctor {dto.fdoccode} record updated successfully. "
                                                            : $"Doctor {dto.fdoccode} record failed to update. ";
                            }
                        }
                        else
                        {
                            // Insert new record
                            string insertQuery = @"
                        INSERT INTO TBLDOCLIST (""FDOCCODE"", ""FDOCNAME"", ""FAUDTDATE"", ""FAUDTTIME"", ""FAUDTUSER"") 
                        VALUES (?, ?, ?, ?, ?)";
                            using (var insertCommand = new OdbcCommand(insertQuery, connection))
                            {
                                insertCommand.Parameters.Add(new OdbcParameter("@fdoccode", dto.fdoccode));
                                insertCommand.Parameters.Add(new OdbcParameter("@fdocname", dto.fdocname));
                                insertCommand.Parameters.Add(new OdbcParameter("@faudtdate", currentDate));
                                insertCommand.Parameters.Add(new OdbcParameter("@faudttime", formattedTime));
                                insertCommand.Parameters.Add(new OdbcParameter("@faudtuser", userid));

                                int rowsAffected = await insertCommand.ExecuteNonQueryAsync();
                                response.Success = rowsAffected > 0;
                                response.Message += rowsAffected > 0 ? $"Doctor {dto.fdoccode} record inserted successfully. "
                                                            : $"Doctor {dto.fdoccode} record failed to insert. ";
                            }
                        }
                    }

                    // Check if fid exists in tbldoctorcontract
                    string checkContractQuery = @"SELECT COUNT(*) FROM TBLDOCCONTRACT WHERE ""FID"" = ? AND ""FDOCCODE"" = ?";
                    using (var checkContractCommand = new OdbcCommand(checkContractQuery, connection))
                    {
                        checkContractCommand.Parameters.Add(new OdbcParameter("@FID", dto.fid));
                        checkContractCommand.Parameters.Add(new OdbcParameter("@FDOCCODE", dto.fdoccode));

                        int contractCount = Convert.ToInt32(await checkContractCommand.ExecuteScalarAsync());

                        if (contractCount > 0)
                        {
                            // Update existing record in TBLDOCCONTRACT
                            string updateContractQuery = @"
                        UPDATE TBLDOCCONTRACT 
                        SET ""FCONTRACT"" = ? , ""FSTARTDATE"" = ?, ""FENDDATE"" = ?, ""FAUDTUSER"" = ?, ""FAUDTDATE"" = ? , ""FAUDTTIME"" = ?
                        WHERE ""FID"" = ? AND ""FDOCCODE"" = ?";
                            using (var updateContractCommand = new OdbcCommand(updateContractQuery, connection))
                            {
                                updateContractCommand.Parameters.Add(new OdbcParameter("@FCONTRACT", dto.fcontract));
                                updateContractCommand.Parameters.Add(new OdbcParameter("@FSTARTDATE", dto.fstartdate));
                                updateContractCommand.Parameters.Add(new OdbcParameter("@FENDDATE", dto.fenddate));
                                updateContractCommand.Parameters.Add(new OdbcParameter("@FAUDTUSER",userid));
                                updateContractCommand.Parameters.Add(new OdbcParameter("@FAUDTDATE", currentDate));
                                updateContractCommand.Parameters.Add(new OdbcParameter("@FAUDTTIME", formattedTime));
                                updateContractCommand.Parameters.Add(new OdbcParameter("@FID",dto.fid));
                                updateContractCommand.Parameters.Add(new OdbcParameter("@FDOCCODE", dto.fdoccode));

                                int rowsAffected = await updateContractCommand.ExecuteNonQueryAsync();
                                response.Success = rowsAffected > 0;
                                response.Message += rowsAffected > 0 ? " Doctor contract updated successfully." : " Doctor contract failed to update.";
                            }
                        }
                        else
                        {
                            int lastId = GetLastIdFromTable("TBLDOCCONTRACT", "FID");
                            int usId = (lastId + 1);
                            // Convert the number to a string with leading zeros using format specifier
                            string formattedNumber = usId.ToString("D5");

                            // Insert new record into TBLDOCCONTRACT
                            string insertContractQuery = @"
                        INSERT INTO TBLDOCCONTRACT (""FID"", ""FDOCCODE"", ""FCONTRACT"",""FSTARTDATE"", ""FENDDATE"",  ""FAUDTUSER"",""FAUDTDATE"", ""FAUDTTIME"") 
                        VALUES (?, ?, ?, ?, ?, ?, ?, ?)";
                            using (var insertContractCommand = new OdbcCommand(insertContractQuery, connection))
                            {
                                insertContractCommand.Parameters.Add(new OdbcParameter("@FID", formattedNumber));
                                insertContractCommand.Parameters.Add(new OdbcParameter("@FDOCCODE", dto.fdoccode));
                                insertContractCommand.Parameters.Add(new OdbcParameter("@FCONTRACT", dto.fcontract));
                                insertContractCommand.Parameters.Add(new OdbcParameter("@FSTARTDATE", dto.fstartdate));
                                insertContractCommand.Parameters.Add(new OdbcParameter("@FENDDATE", dto.fenddate));
                                insertContractCommand.Parameters.Add(new OdbcParameter("@FAUDTUSER", userid));
                                insertContractCommand.Parameters.Add(new OdbcParameter("@FAUDTDATE", currentDate));
                                insertContractCommand.Parameters.Add(new OdbcParameter("@FAUDTTIME", formattedTime));

                                int rowsAffected = await insertContractCommand.ExecuteNonQueryAsync();
                                response.Success = rowsAffected > 0;
                                response.Message += rowsAffected > 0 ? " Doctor contract inserted successfully." : " Doctor contract failed to insert.";
                            }
                        }
                    }


                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error: {ex.Message}";
            }

            return Ok(response);
        }

        [Route("SaveDoctorGMI")]
        [HttpPost]
        public async Task<IHttpActionResult> SaveDoctorGMI(PostDoctorGMIDto dto)
        {
            var response = new GeneralAPIResponse<string>();
            var CompanyID = HttpContext.Current.Items["CompanyID"]?.ToString();
            var userid = HttpContext.Current.Items["UserID"]?.ToString();

            decimal currentDate = Convert.ToDecimal(DateTime.Now.ToString("yyyyMMdd"));
            DateTime currentTime = DateTime.Now;
            string formattedTime = currentTime.ToString("HHmmss");

            try
            {
                using (var connection = new OdbcConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Check if fdoccode exists
                    string checkQuery = @"SELECT COUNT(*) FROM TBLDOCLIST WHERE ""FDOCCODE"" = ?";
                    using (var checkCommand = new OdbcCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.Add(new OdbcParameter("@FODCCODE", dto.fdoccode));
                        int count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());

                        if (count > 0)
                        {
                            // Update existing record
                            string updateQuery = @"
                        UPDATE TBLDOCLIST 
                        SET ""FAUDTDATE"" = ?, ""FAUDTTIME"" = ?, ""FAUDTUSER"" = ? 
                        WHERE ""FDOCCODE"" = ?";
                            using (var updateCommand = new OdbcCommand(updateQuery, connection))
                            {
                                updateCommand.Parameters.Add(new OdbcParameter("@FAUDTDATE", currentDate));
                                updateCommand.Parameters.Add(new OdbcParameter("@FAUDTTIME", formattedTime));
                                updateCommand.Parameters.Add(new OdbcParameter("@FAUDTUSER", userid));
                                updateCommand.Parameters.Add(new OdbcParameter("@FDOCCODE", dto.fdoccode));

                                int rowsAffected = await updateCommand.ExecuteNonQueryAsync();
                                response.Success = rowsAffected > 0;
                                response.Message += rowsAffected > 0
                                                            ? $"Doctor {dto.fdoccode} record updated successfully. "
                                                            : $"Doctor {dto.fdoccode} record failed to update. ";
                            }
                        }
                        else
                        {
                            // Insert new record
                            string insertQuery = @"
                        INSERT INTO TBLDOCLIST (""FDOCCODE"", ""FDOCNAME"", ""FAUDTDATE"", ""FAUDTTIME"", ""FAUDTUSER"") 
                        VALUES (?, ?, ?, ?, ?)";
                            using (var insertCommand = new OdbcCommand(insertQuery, connection))
                            {
                                insertCommand.Parameters.Add(new OdbcParameter("@fdoccode", dto.fdoccode));
                                insertCommand.Parameters.Add(new OdbcParameter("@fdocname", dto.fdocname));
                                insertCommand.Parameters.Add(new OdbcParameter("@faudtdate", currentDate));
                                insertCommand.Parameters.Add(new OdbcParameter("@faudttime", formattedTime));
                                insertCommand.Parameters.Add(new OdbcParameter("@faudtuser", userid));

                                int rowsAffected = await insertCommand.ExecuteNonQueryAsync();
                                response.Success = rowsAffected > 0;
                                response.Message += rowsAffected > 0 ? $"Doctor {dto.fdoccode} record inserted successfully. "
                                                            : $"Doctor {dto.fdoccode} record failed to insert. ";
                            }
                        }
                    }

                    // Check if fid exists in tbldoctorgmi
                    string checkGMIQuery = @"SELECT COUNT(*) FROM TBLDOCGMI WHERE ""FID"" = ? AND ""FDOCCODE"" = ?";
                    using (var checkGMICommand = new OdbcCommand(checkGMIQuery, connection))
                    {
                        checkGMICommand.Parameters.Add(new OdbcParameter("@FID", dto.fid));
                        checkGMICommand.Parameters.Add(new OdbcParameter("@FDOCCODE", dto.fdoccode));

                        int gmiCount = Convert.ToInt32(await checkGMICommand.ExecuteScalarAsync());

                        if (gmiCount > 0)
                        {
                            // Update existing record in TBLDOCGMI
                            string updateGMIQuery = @"
                        UPDATE TBLDOCGMI
                        SET ""FSTARTDATE"" = ?, ""FENDDATE"" = ?, ""FAMOUNT"" = ? , ""FAUDTUSER"" = ?, ""FAUDTDATE"" = ? , ""FAUDTTIME"" = ?
                        WHERE ""FID"" = ? AND ""FDOCCODE"" = ?";
                            using (var updateGMICommand = new OdbcCommand(updateGMIQuery, connection))
                            {
                                updateGMICommand.Parameters.Add(new OdbcParameter("@FSTARTDATE", dto.fstartdate));
                                updateGMICommand.Parameters.Add(new OdbcParameter("@FENDDATE", dto.fenddate));                                
                                updateGMICommand.Parameters.Add(new OdbcParameter("@FAMOUNT", dto.famount));
                                updateGMICommand.Parameters.Add(new OdbcParameter("@FAUDTUSER", userid));
                                updateGMICommand.Parameters.Add(new OdbcParameter("@FAUDTDATE", currentDate));
                                updateGMICommand.Parameters.Add(new OdbcParameter("@FAUDTTIME", formattedTime));
                                updateGMICommand.Parameters.Add(new OdbcParameter("@FID", dto.fid));
                                updateGMICommand.Parameters.Add(new OdbcParameter("@FDOCCODE", dto.fdoccode));

                                int rowsAffected = await updateGMICommand.ExecuteNonQueryAsync();
                                response.Success = rowsAffected > 0;
                                response.Message += rowsAffected > 0 ? " Doctor GMI updated successfully." : " Doctor GMI failed to update.";
                            }
                        }
                        else
                        {
                            int lastId = GetLastIdFromTable("TBLDOCGMI", "FID");
                            int usId = (lastId + 1);
                            // Convert the number to a string with leading zeros using format specifier
                            string formattedNumber = usId.ToString("D5");

                            // Insert new record into TBLDOCGMI
                            string insertGMIQuery = @"
                        INSERT INTO TBLDOCGMI (""FID"", ""FDOCCODE"", ""FSTARTDATE"", ""FENDDATE"",""FAMOUNT"",  ""FAUDTUSER"",""FAUDTDATE"", ""FAUDTTIME"") 
                        VALUES (?, ?, ?, ?, ?, ?, ?, ?)";
                            using (var insertGMICommand = new OdbcCommand(insertGMIQuery, connection))
                            {
                                insertGMICommand.Parameters.Add(new OdbcParameter("@FID", formattedNumber));
                                insertGMICommand.Parameters.Add(new OdbcParameter("@FDOCCODE", dto.fdoccode));
                                insertGMICommand.Parameters.Add(new OdbcParameter("@FSTARTDATE", dto.fstartdate));
                                insertGMICommand.Parameters.Add(new OdbcParameter("@FENDDATE", dto.fenddate));
                                insertGMICommand.Parameters.Add(new OdbcParameter("@FAMOUNT", dto.famount));
                                insertGMICommand.Parameters.Add(new OdbcParameter("@FAUDTUSER", userid));
                                insertGMICommand.Parameters.Add(new OdbcParameter("@FAUDTDATE", currentDate));
                                insertGMICommand.Parameters.Add(new OdbcParameter("@FAUDTTIME", formattedTime));

                                int rowsAffected = await insertGMICommand.ExecuteNonQueryAsync();
                                response.Success = rowsAffected > 0;
                                response.Message += rowsAffected > 0 ? " Doctor GMI inserted successfully." : " Doctor GMI failed to insert.";
                            }
                        }
                    }


                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error: {ex.Message}";
            }

            return Ok(response);
        }

        [Route("SaveDoctorAdminCharges")]
        [HttpPost]
        public async Task<IHttpActionResult> SaveDoctorAdminCharges(PostDoctorAdminChargesDto dto)
        {
            var response = new GeneralAPIResponse<string>();
            var CompanyID = HttpContext.Current.Items["CompanyID"]?.ToString();
            var userid = HttpContext.Current.Items["UserID"]?.ToString();

            decimal currentDate = Convert.ToDecimal(DateTime.Now.ToString("yyyyMMdd"));
            DateTime currentTime = DateTime.Now;
            string formattedTime = currentTime.ToString("HHmmss");

            try
            {
                using (var connection = new OdbcConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Check if fdoccode exists
                    string checkQuery = @"SELECT COUNT(*) FROM TBLDOCLIST WHERE ""FDOCCODE"" = ?";
                    using (var checkCommand = new OdbcCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.Add(new OdbcParameter("@FODCCODE", dto.fdoccode));
                        int count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());

                        if (count > 0)
                        {
                            // Update existing record
                            string updateQuery = @"
                        UPDATE TBLDOCLIST 
                        SET ""FAUDTDATE"" = ?, ""FAUDTTIME"" = ?, ""FAUDTUSER"" = ? 
                        WHERE ""FDOCCODE"" = ?";
                            using (var updateCommand = new OdbcCommand(updateQuery, connection))
                            {
                                updateCommand.Parameters.Add(new OdbcParameter("@FAUDTDATE", currentDate));
                                updateCommand.Parameters.Add(new OdbcParameter("@FAUDTTIME", formattedTime));
                                updateCommand.Parameters.Add(new OdbcParameter("@FAUDTUSER", userid));
                                updateCommand.Parameters.Add(new OdbcParameter("@FDOCCODE", dto.fdoccode));

                                int rowsAffected = await updateCommand.ExecuteNonQueryAsync();
                                response.Success = rowsAffected > 0;
                                response.Message += rowsAffected > 0
                                                            ? $"Doctor {dto.fdoccode} record updated successfully. "
                                                            : $"Doctor {dto.fdoccode} record failed to update. ";
                            }
                        }
                        else
                        {
                            // Insert new record
                            string insertQuery = @"
                        INSERT INTO TBLDOCLIST (""FDOCCODE"", ""FDOCNAME"", ""FAUDTDATE"", ""FAUDTTIME"", ""FAUDTUSER"") 
                        VALUES (?, ?, ?, ?, ?)";
                            using (var insertCommand = new OdbcCommand(insertQuery, connection))
                            {
                                insertCommand.Parameters.Add(new OdbcParameter("@fdoccode", dto.fdoccode));
                                insertCommand.Parameters.Add(new OdbcParameter("@fdocname", dto.fdocname));
                                insertCommand.Parameters.Add(new OdbcParameter("@faudtdate", currentDate));
                                insertCommand.Parameters.Add(new OdbcParameter("@faudttime", formattedTime));
                                insertCommand.Parameters.Add(new OdbcParameter("@faudtuser", userid));

                                int rowsAffected = await insertCommand.ExecuteNonQueryAsync();
                                response.Success = rowsAffected > 0;
                                response.Message += rowsAffected > 0 ? $"Doctor {dto.fdoccode} record inserted successfully. "
                                                            : $"Doctor {dto.fdoccode} record failed to insert. ";
                            }
                        }
                    }

                    // Check if fid exists in tbldoctoradmincharges
                    string checkDocAdminQuery = @"SELECT COUNT(*) FROM TBLDOCADMINCHARGE WHERE ""FID"" = ? AND ""FDOCCODE"" = ?";
                    using (var checkDocAdminCommand = new OdbcCommand(checkDocAdminQuery, connection))
                    {
                        checkDocAdminCommand.Parameters.Add(new OdbcParameter("@FID", dto.fid));
                        checkDocAdminCommand.Parameters.Add(new OdbcParameter("@FDOCCODE", dto.fdoccode));

                        int docAdminCount = Convert.ToInt32(await checkDocAdminCommand.ExecuteScalarAsync());

                        if (docAdminCount > 0)
                        {
                            // Update existing record in TBLDOCADMINCHARGE
                            string updateDocAdminQuery = @"
                        UPDATE TBLDOCADMINCHARGE
                        SET ""FSTARTDATE"" = ?, ""FENDDATE"" = ?, ""FCHARGERATE"" = ? ,""FDISCOUNTRATE"" = ? ,""FAMOUNT"" = ? , ""FAUDTUSER"" = ?, ""FAUDTDATE"" = ? , ""FAUDTTIME"" = ?
                        WHERE ""FID"" = ? AND ""FDOCCODE"" = ?";
                            using (var updateDocAdminCommand = new OdbcCommand(updateDocAdminQuery, connection))
                            {
                                updateDocAdminCommand.Parameters.Add(new OdbcParameter("@FSTARTDATE", dto.fstartdate));
                                updateDocAdminCommand.Parameters.Add(new OdbcParameter("@FENDDATE", dto.fenddate));
                                updateDocAdminCommand.Parameters.Add(new OdbcParameter("@FCHARGERATE", dto.fchargerate));
                                updateDocAdminCommand.Parameters.Add(new OdbcParameter("@FDISCOUNTRATE", dto.fdiscountrate));
                                updateDocAdminCommand.Parameters.Add(new OdbcParameter("@FAMOUNT", dto.famount));
                                updateDocAdminCommand.Parameters.Add(new OdbcParameter("@FAUDTUSER", userid));
                                updateDocAdminCommand.Parameters.Add(new OdbcParameter("@FAUDTDATE", currentDate));
                                updateDocAdminCommand.Parameters.Add(new OdbcParameter("@FAUDTTIME", formattedTime));
                                updateDocAdminCommand.Parameters.Add(new OdbcParameter("@FID", dto.fid));
                                updateDocAdminCommand.Parameters.Add(new OdbcParameter("@FDOCCODE", dto.fdoccode));

                                int rowsAffected = await updateDocAdminCommand.ExecuteNonQueryAsync();
                                response.Success = rowsAffected > 0;
                                response.Message += rowsAffected > 0 ? " Doctor Administrative Charges updated successfully." : " Doctor Administrative Charges failed to update.";
                            }
                        }
                        else
                        {
                            int lastId = GetLastIdFromTable("TBLDOCADMINCHARGE", "FID");
                            int usId = (lastId + 1);
                            // Convert the number to a string with leading zeros using format specifier
                            string formattedNumber = usId.ToString("D5");

                            // Insert new record into TBLDOCADMINCHARGE
                            string insertDocAdminQuery = @"
                        INSERT INTO TBLDOCADMINCHARGE (""FID"", ""FDOCCODE"", ""FSTARTDATE"", ""FENDDATE"",""FCHARGERATE"", ""FDISCOUNTRATE"", ""FAMOUNT"",  ""FAUDTUSER"",""FAUDTDATE"", ""FAUDTTIME"") 
                        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
                            using (var insertDocAdminCommand = new OdbcCommand(insertDocAdminQuery, connection))
                            {
                                insertDocAdminCommand.Parameters.Add(new OdbcParameter("@FID", formattedNumber));
                                insertDocAdminCommand.Parameters.Add(new OdbcParameter("@FDOCCODE", dto.fdoccode));
                                insertDocAdminCommand.Parameters.Add(new OdbcParameter("@FSTARTDATE", dto.fstartdate));
                                insertDocAdminCommand.Parameters.Add(new OdbcParameter("@FENDDATE", dto.fenddate));
                                insertDocAdminCommand.Parameters.Add(new OdbcParameter("@FCHARGERATE", dto.fchargerate));
                                insertDocAdminCommand.Parameters.Add(new OdbcParameter("@FDISCOUNTRATE", dto.fdiscountrate));
                                insertDocAdminCommand.Parameters.Add(new OdbcParameter("@FAMOUNT", dto.famount));
                                insertDocAdminCommand.Parameters.Add(new OdbcParameter("@FAUDTUSER", userid));
                                insertDocAdminCommand.Parameters.Add(new OdbcParameter("@FAUDTDATE", currentDate));
                                insertDocAdminCommand.Parameters.Add(new OdbcParameter("@FAUDTTIME", formattedTime));

                                int rowsAffected = await insertDocAdminCommand.ExecuteNonQueryAsync();
                                response.Success = rowsAffected > 0;
                                response.Message += rowsAffected > 0 ? " Doctor Administrative Charges inserted successfully." : " Doctor Administrative Charges failed to insert.";
                            }
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error: {ex.Message}";
            }

            return Ok(response);
        }

        [Route("SaveDoctorFacilityCharges")]
        [HttpPost]
        public async Task<IHttpActionResult> SaveDoctorFacilityCharges(PostDoctorFacilityChargesDto dto)
        {
            var response = new GeneralAPIResponse<string>();
            var CompanyID = HttpContext.Current.Items["CompanyID"]?.ToString();
            var userid = HttpContext.Current.Items["UserID"]?.ToString();

            decimal currentDate = Convert.ToDecimal(DateTime.Now.ToString("yyyyMMdd"));
            DateTime currentTime = DateTime.Now;
            string formattedTime = currentTime.ToString("HHmmss");

            try
            {
                using (var connection = new OdbcConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Check if fdoccode exists
                    string checkQuery = @"SELECT COUNT(*) FROM TBLDOCLIST WHERE ""FDOCCODE"" = ?";
                    using (var checkCommand = new OdbcCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.Add(new OdbcParameter("@FODCCODE", dto.fdoccode));
                        int count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());

                        if (count > 0)
                        {
                            // Update existing record
                            string updateQuery = @"
                        UPDATE TBLDOCLIST 
                        SET ""FAUDTDATE"" = ?, ""FAUDTTIME"" = ?, ""FAUDTUSER"" = ? 
                        WHERE ""FDOCCODE"" = ?";
                            using (var updateCommand = new OdbcCommand(updateQuery, connection))
                            {
                                updateCommand.Parameters.Add(new OdbcParameter("@FAUDTDATE", currentDate));
                                updateCommand.Parameters.Add(new OdbcParameter("@FAUDTTIME", formattedTime));
                                updateCommand.Parameters.Add(new OdbcParameter("@FAUDTUSER", userid));
                                updateCommand.Parameters.Add(new OdbcParameter("@FDOCCODE", dto.fdoccode));

                                int rowsAffected = await updateCommand.ExecuteNonQueryAsync();
                                response.Success = rowsAffected > 0;
                                response.Message += rowsAffected > 0
                                                            ? $"Doctor {dto.fdoccode} record updated successfully. "
                                                            : $"Doctor {dto.fdoccode} record failed to update. ";
                            }
                        }
                        else
                        {
                            // Insert new record
                            string insertQuery = @"
                        INSERT INTO TBLDOCLIST (""FDOCCODE"", ""FDOCNAME"", ""FAUDTDATE"", ""FAUDTTIME"", ""FAUDTUSER"") 
                        VALUES (?, ?, ?, ?, ?)";
                            using (var insertCommand = new OdbcCommand(insertQuery, connection))
                            {
                                insertCommand.Parameters.Add(new OdbcParameter("@fdoccode", dto.fdoccode));
                                insertCommand.Parameters.Add(new OdbcParameter("@fdocname", dto.fdocname));
                                insertCommand.Parameters.Add(new OdbcParameter("@faudtdate", currentDate));
                                insertCommand.Parameters.Add(new OdbcParameter("@faudttime", formattedTime));
                                insertCommand.Parameters.Add(new OdbcParameter("@faudtuser", userid));

                                int rowsAffected = await insertCommand.ExecuteNonQueryAsync();
                                response.Success = rowsAffected > 0;
                                response.Message += rowsAffected > 0 ? $"Doctor {dto.fdoccode} record inserted successfully. "
                                                            : $"Doctor {dto.fdoccode} record failed to insert. ";
                            }
                        }
                    }

                    // Check if fid exists in tbldoctorfacilitycharges
                    string checkFacChargeQuery = @"SELECT COUNT(*) FROM TBLDOCFACCHARGE WHERE ""FID"" = ? AND ""FDOCCODE"" = ?";
                    using (var checkFacChargeCommand = new OdbcCommand(checkFacChargeQuery, connection))
                    {
                        checkFacChargeCommand.Parameters.Add(new OdbcParameter("@FID", dto.fid));
                        checkFacChargeCommand.Parameters.Add(new OdbcParameter("@FDOCCODE", dto.fdoccode));

                        int facChargeCount = Convert.ToInt32(await checkFacChargeCommand.ExecuteScalarAsync());

                        if (facChargeCount > 0)
                        {
                            // Update existing record in TBLDOCFACCHARGE
                            string updateFacChargeQuery = @"
                        UPDATE TBLDOCFACCHARGE
                        SET ""FSTARTDATE"" = ?, ""FENDDATE"" = ?, ""FAMOUNT"" = ? , ""FAUDTUSER"" = ?, ""FAUDTDATE"" = ? , ""FAUDTTIME"" = ?
                        WHERE ""FID"" = ? AND ""FDOCCODE"" = ?";
                            using (var updateFacChargeCommand = new OdbcCommand(updateFacChargeQuery, connection))
                            {
                                updateFacChargeCommand.Parameters.Add(new OdbcParameter("@FSTARTDATE", dto.fstartdate));
                                updateFacChargeCommand.Parameters.Add(new OdbcParameter("@FENDDATE", dto.fenddate));
                                updateFacChargeCommand.Parameters.Add(new OdbcParameter("@FAMOUNT", dto.famount));
                                updateFacChargeCommand.Parameters.Add(new OdbcParameter("@FAUDTUSER", userid));
                                updateFacChargeCommand.Parameters.Add(new OdbcParameter("@FAUDTDATE", currentDate));
                                updateFacChargeCommand.Parameters.Add(new OdbcParameter("@FAUDTTIME", formattedTime));
                                updateFacChargeCommand.Parameters.Add(new OdbcParameter("@FID", dto.fid));
                                updateFacChargeCommand.Parameters.Add(new OdbcParameter("@FDOCCODE", dto.fdoccode));

                                int rowsAffected = await updateFacChargeCommand.ExecuteNonQueryAsync();
                                response.Success = rowsAffected > 0;
                                response.Message += rowsAffected > 0 ? " Doctor Facility Charges updated successfully." : " Doctor Facility Charges failed to update.";
                            }
                        }
                        else
                        {
                            int lastId = GetLastIdFromTable("TBLDOCFACCHARGE", "FID");
                            int usId = (lastId + 1);
                            // Convert the number to a string with leading zeros using format specifier
                            string formattedNumber = usId.ToString("D5");

                            // Insert new record into TBLDOCFACCHARGE
                            string insertFacChargeQuery = @"
                        INSERT INTO TBLDOCADMINCHARGE (""FID"", ""FDOCCODE"", ""FSTARTDATE"", ""FENDDATE"", ""FAMOUNT"", ""FAUDTUSER"",""FAUDTDATE"", ""FAUDTTIME"") 
                        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
                            using (var insertFacChargeCommand = new OdbcCommand(insertFacChargeQuery, connection))
                            {
                                insertFacChargeCommand.Parameters.Add(new OdbcParameter("@FID", formattedNumber));
                                insertFacChargeCommand.Parameters.Add(new OdbcParameter("@FDOCCODE", dto.fdoccode));
                                insertFacChargeCommand.Parameters.Add(new OdbcParameter("@FSTARTDATE", dto.fstartdate));
                                insertFacChargeCommand.Parameters.Add(new OdbcParameter("@FENDDATE", dto.fenddate));
                                insertFacChargeCommand.Parameters.Add(new OdbcParameter("@FAMOUNT", dto.famount));
                                insertFacChargeCommand.Parameters.Add(new OdbcParameter("@FAUDTUSER", userid));
                                insertFacChargeCommand.Parameters.Add(new OdbcParameter("@FAUDTDATE", currentDate));
                                insertFacChargeCommand.Parameters.Add(new OdbcParameter("@FAUDTTIME", formattedTime));

                                int rowsAffected = await insertFacChargeCommand.ExecuteNonQueryAsync();
                                response.Success = rowsAffected > 0;
                                response.Message += rowsAffected > 0 ? " Doctor Facility Charges inserted successfully." : " Doctor Facility Charges failed to insert.";
                            }
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error: {ex.Message}";
            }

            return Ok(response);
        }

        private string ConvertTo12HourFormat(string time)
        {
            if (DateTime.TryParseExact(time, "HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateTime))
            {
                return dateTime.ToString("hh:mm tt", CultureInfo.InvariantCulture); // 12-hour format with AM/PM
            }
            return time; 
        }

        private int GetLastIdFromTable(string tableName, string idColumnName)
        {
            int lastId = 0; // Default value in case no records are found

            // Construct the SQL query to get the maximum value of the ID column
            string query = $"SELECT MAX(\"{idColumnName}\") FROM {tableName}";

            using (OdbcConnection connection = new OdbcConnection(_connectionString))
            {
                using (OdbcCommand command = new OdbcCommand(query, connection))
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
