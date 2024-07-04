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
            public string DoctorCode { get; set; }
            public string DoctorName { get; set; }
            public string Contract { get; set; }
            public string ContractPeriod { get; set; }
            public string GMIPeriod { get; set; }
            public string GMIAmount { get; set; }
            public string AdministrativeChargesRate { get; set; }
            public string FacilityCharges { get; set; }
            public string UpdatedBy { get; set; }
            public string UpdatedDateTime { get; set; }

        }
    }
}