using Aurelius_API.ModelAPI;
using Aurelius_API.ModelAPI.Company;
using Aurelius_API.ModelAPI.Doctor;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Odbc;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
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

        //        [Route("GetDoctorList")]
        //        [HttpGet]
        //        public async Task<GeneralAPIResponse<List<GetAllDoctorSearch>>> SearchDoctor(
        //[FromUri] string doctor = null,
        //[FromUri] string contract = null,
        //[FromUri] decimal? dateFrom = null,
        //[FromUri] decimal? dateTo = null)
        //        {
        //            var CompanyID = HttpContext.Current.Items["CompanyID"]?.ToString();
        //            var username = HttpContext.Current.Items["UserID"]?.ToString();
        //            var response = new GeneralAPIResponse<List<GetAllDoctorSearch>>();

        //            try
        //            {
        //                string userId = _globalService.GetUserId();
        //                string custId = getCustId(userId);
        //                string dbName = getDbName(userId);

        //                string connectionString = _defaultConnectionString;
        //                connectionString = connectionString.Replace("{dbName}", dbName);

        //                var companyResponse = await FetchCompanyTrans();
        //                var companyIds = companyResponse.Data.Select(c => c.FCOMPID).ToList();

        //                using (var connection = new SqlConnection(connectionString))
        //                {
        //                    await connection.OpenAsync();
        //                    string dateFromDecimal = "";
        //                    string dateToDecimal = "";
        //                    string query = @"
        //              SELECT FCOMPID, FEINVNO, FDOCDATE, FMODULE, FBUYERNAME, SUM(FSUBTOTAL) AS FSUBTOTAL, FSTATUS, FDOCSTATUS, FREFETCH, FINVCURRCODE 
        //                FROM TRANSDT 
        //                WHERE FCOMPID IN (" + string.Join(",", companyIds.Select(id => "'" + id + "'")) + ")";

        //                    if (!string.IsNullOrEmpty(companyId))
        //                    {
        //                        query += " AND FCOMPID = @FCOMPID";
        //                    }
        //                    if (!string.IsNullOrEmpty(custVendorName))
        //                    {
        //                        query += " AND FBUYERNAME = @FBUYERNAME";
        //                    }

        //                    if (dateFrom.HasValue)
        //                    {
        //                        query += " AND FDOCDATE >= @DateFrom";
        //                    }
        //                    if (dateTo.HasValue)
        //                    {
        //                        query += " AND FDOCDATE <= @DateTo";
        //                    }

        //                    if (!string.IsNullOrEmpty(documentType))
        //                        query += " AND FMODULE = @FMODULE";
        //                    if (!string.IsNullOrEmpty(documentStatus))
        //                    {
        //                        if (documentStatus.ToLower() == "refetched")
        //                        {
        //                            query += " AND FSTATUS = 'Pending' AND FREFETCH = '1'";
        //                        }
        //                        else
        //                        {
        //                            query += " AND FSTATUS = @FSTATUS AND (FREFETCH != '1' OR FREFETCH IS NULL)";
        //                        }
        //                    }
        //                    //else
        //                    //{
        //                    //    query += " AND (FREFETCH != '1' OR FREFETCH IS NULL)";

        //                    //}



        //                    query += " AND FCONSOLIDATE = 'N'";
        //                    query += " GROUP BY FCOMPID, FEINVNO,FDOCDATE, FMODULE, FBUYERNAME, FSTATUS, FDOCSTATUS, FREFETCH,FINVCURRCODE  ORDER BY FCOMPID, FDOCDATE DESC";
        //                    SqlCommand command = new SqlCommand(query, connection);

        //                    // Add parameters to the query
        //                    if (!string.IsNullOrEmpty(companyId))
        //                        command.Parameters.AddWithValue("@FCOMPID", companyId);
        //                    if (!string.IsNullOrEmpty(custVendorName))
        //                        command.Parameters.AddWithValue("@FBUYERNAME", Encrypt(custId, custVendorName));
        //                    if (dateFrom.HasValue)
        //                        command.Parameters.AddWithValue("@DateFrom", Convert.ToDecimal(dateFrom));
        //                    if (dateTo.HasValue)
        //                        command.Parameters.AddWithValue("@DateTo", Convert.ToDecimal(dateTo));
        //                    if (!string.IsNullOrEmpty(documentType))
        //                        command.Parameters.AddWithValue("@FMODULE", documentType);
        //                    if (!string.IsNullOrEmpty(documentStatus) && documentStatus.ToLower() != "refetched")
        //                        command.Parameters.AddWithValue("@FSTATUS", documentStatus);

        //                    int transactionCount = 0;

        //                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
        //                    {
        //                        if (reader.HasRows)
        //                        {
        //                            var searchResponseList = new List<SearchResponse>();

        //                            while (reader.Read())
        //                            {

        //                                var searchResponse = new SearchResponse
        //                                {
        //                                    FCOMPID = reader["FCOMPID"].ToString(),
        //                                    FEINVNO = Decrypt(custId, reader["FEINVNO"].ToString()),
        //                                    FDOCDATE = reader["FDOCDATE"] == DBNull.Value ? string.Empty : DateTime.ParseExact(reader["FDOCDATE"].ToString(), "yyyyMMdd", CultureInfo.InvariantCulture).ToString("dd/MM/yyyy"),
        //                                    FMODULE = reader["FMODULE"].ToString(),
        //                                    FBUYERNAME = Decrypt(custId, reader["FBUYERNAME"].ToString()),
        //                                    FSUBTOTAL = reader["FSUBTOTAL"] == DBNull.Value ? "0.00" : Convert.ToDecimal(reader["FSUBTOTAL"]).ToString("N2", CultureInfo.InvariantCulture),
        //                                    FSTATUS = reader["FSTATUS"].ToString(),
        //                                    FDOCSTATUS = reader["FDOCSTATUS"].ToString(),
        //                                    FREFETCH = reader["FREFETCH"].ToString(),
        //                                    FINVCURRCODE = Decrypt(custId, reader["FINVCURRCODE"].ToString())
        //                                };

        //                                searchResponseList.Add(searchResponse);
        //                            }
        //                            if (!string.IsNullOrEmpty(documentNo))
        //                            {
        //                                searchResponseList = searchResponseList.Where(r => r.FEINVNO.Contains(documentNo, StringComparison.OrdinalIgnoreCase)).ToList();
        //                            }

        //                            transactionCount = searchResponseList.Count();

        //                            if (transactionCount <= 0)
        //                            {
        //                                response.Success = false;
        //                                response.Message = "No transaction found";
        //                            }
        //                            else
        //                            {
        //                                response.Data = searchResponseList;
        //                                response.Success = true;
        //                                response.Message = transactionCount + " Transaction(s) found.";
        //                            }


        //                        }
        //                        else
        //                        {
        //                            response.Success = false;
        //                            response.Message = "No transaction found";
        //                        }
        //                    }
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                response.Success = false;
        //                response.Message = ex.Message;
        //            }

        //            return response;
        //        }

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

        private string ConvertTo12HourFormat(string time)
        {
            if (DateTime.TryParseExact(time, "HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateTime))
            {
                return dateTime.ToString("hh:mm tt", CultureInfo.InvariantCulture); // 12-hour format with AM/PM
            }
            return time; 
        }

    }
}
