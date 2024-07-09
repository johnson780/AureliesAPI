using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Aurelius_API.ModelAPI.Doctor
{
    public class GetAllDoctorSearch
    {
        public class AddFacilityCharges
        {
            public string FDOCCODE { get; set; }
            public string FDOCNAME { get; set; }
            public string FCONTRACT { get; set; }
            public string FSTARTDATE { get; set; }
            public string FENDDATE { get; set; }
            public string GMIAmount { get; set; }
            public string AdministrativeChargesRate { get; set; }
            public string FacilityCharges { get; set; }
            public string FAUDTUSER { get; set; }
            public string FAUDTTIME { get; set; }

        }
    }
}