using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Aurelius_API.ModelAPI.Company
{
    public class GetAllSearchCompany
    {
        public List<GetAllSearchCompany> AllSearchCompany { get; set; }
    }
    public class DetailGetAllSearchUser
    {
        public string CompanyID { get; set; }
        public string CompanyName { get; set; }
        public string UpdatedBy { get; set; }
        public string UpdatedDatetime { get; set; }
        public string Status { get; set; }
    }

    public class GetCompany
    {
        public string fcompid { get; set; }
        public string fcompname { get; set; }
        public string factive { get; set; }
        public string faudtuser { get; set; }
        public string faudtdate { get; set; }
        public string faudttime { get; set; }

    }
}