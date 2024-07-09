using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Aurelius_API.ModelAPI.Doctor
{
    public class DoctorContract
    {
        public string FID { get; set; }
        public string FDOCCODE { get; set; }
        public string FCONTRACT { get; set; }
        public string FSTARTDATE { get; set; }
        public string FENDDATE { get; set; }
        public string FAUDTUSER { get; set; }
        public string FAUDTDATE { get; set; }
        public string FAUDTTIME { get; set; }
    }
}