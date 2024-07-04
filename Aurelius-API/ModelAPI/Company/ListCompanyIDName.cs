using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Aurelius_API.ModelAPI.Company
{
    public class ListCompanyIDName
    {
        public List<DetailCompanyIDName> CompanyIDName { get; set; }

    }
    public class DetailCompanyIDName
    {
        public string CompanyID { get; set; }
        public string CompanyName { get; set; }

    }
}