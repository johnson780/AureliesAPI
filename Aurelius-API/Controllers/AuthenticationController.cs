using Aurelius_API.ModelAPI.Company;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Odbc;
using System.Threading.Tasks;
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

       
        [Route("getcompany")]
        [HttpGet]
        public async Task<List<GetCompany>> FetchCompany()
        {
            var compList = new List<GetCompany>();

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
                throw; // Consider logging or handling the exception appropriately
            }
            return compList;
        }
    }
}
