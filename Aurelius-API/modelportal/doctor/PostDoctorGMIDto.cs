using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Aurelius_API.ModelPortal.Doctor
{
    public class PostDoctorGMIDto
    {
        public string fid { get; set; }
        public string fdoccode { get; set; }
        public string fdocname { get; set; }
        public decimal famount { get; set; }
        public decimal fstartdate { get; set; }
        public decimal fenddate { get; set; }
    }
}