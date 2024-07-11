using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Aurelius_API.ModelAPI.Doctor
{
    public class GetAllDoctorSearch
    {
            public string FDOCCODE { get; set; }
            public string FDOCNAME { get; set; }
            public string CONTRACTSTARTDATE { get; set; }
            public string CONTRACTENDDATE { get; set; }
            public string FCONTRACT { get; set; }
            public string GMISTARTDATE { get; set; }
            public string GMIENDDATE { get; set; }
            public string GMIAMOUNT { get; set; }
            public string ADMINCHARGE { get; set; }
            public string FACILITYCHARGE { get; set; }
            public string FAUDTUSER { get; set; }
            public string FAUDTTIME { get; set; }
            public string FAUDTDATE{ get; set; }
    }
    
}