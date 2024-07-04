using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Aurelius_API.ModelAPI.Doctor
{
    public class EditDoctorContract
    {
        public string DoctorCode { get; set; }
        public string DoctorName { get; set; }
        public string Contract { get; set; }
        public string ContractStart { get; set; }
        public string ContractEnd { get; set; }

    }
}