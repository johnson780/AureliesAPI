using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Aurelius_API.ModelAPI.Doctor
{
    public class AddAdminCharges
    {
        public string DoctorCode { get; set; }
        public string DoctorName { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string ChargesRate { get; set; }
        public string DiscountRate { get; set; }
        public string AmountExceeded { get; set; }
    }
}