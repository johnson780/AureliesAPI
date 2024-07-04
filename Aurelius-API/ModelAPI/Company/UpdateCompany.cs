using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Aurelius_API.ModelAPI.Company
{
    public class UpdateCompany
    {
        public string CompanyID { get; set; }
        public string CompanyName { get; set; }
        public string Status { get; set; }

    }
}
