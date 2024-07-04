﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Aurelius_API.ModelAPI.Doctor
{
    public class EditFacilityCharges
    {
        public string DoctorCode { get; set; }
        public string DoctorName { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public decimal Amount { get; set; }

    }
}