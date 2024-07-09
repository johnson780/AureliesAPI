using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Aurelius_API.ModelAPI.Login
{
    public class LoginResponse
    {
        public string AuthToken { get; set; }

        public string RefreshToken { get; set; }

        public UserResponseDto User { get; set; }
        public GetDashboardDetailsDto DashboardDetails { get; set; }
       
    }

    public class UserResponseDto
    {
        public string FID { get; set; }
        public string FUSERNAME { get; set; }
        public string FFULLNAME { get; set; }
        public string FROLE { get; set; }
        public string FTEMPPWD { get; set; }
        public string FCOMPID { get; set; }
    }

    public class GetDashboardDetailsDto
    {
        public int TotalDoctor { get; set; }
        public int TotalCompany { get; set; }
        public int TotalUser { get; set; }
        public int TotalTransactionDay { get; set; }
        public int TotalTransactionMonth { get; set; }

    }
}