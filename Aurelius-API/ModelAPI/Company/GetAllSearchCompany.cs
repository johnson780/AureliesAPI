﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Aurelius_API.ModelAPI.Company
{
    public class GetAllSearchCompany
    {
        
        public string FCOMPID { get; set; }
        public string FCOMPNAME { get; set; }
        public string FACTIVE { get; set; }
        public string FAUDTUSER { get; set; }
        //public string FUSERNAME { get; set; }

        public string FAUDTDATE { get; set; }
        public string FAUDTTIME { get; set; }
    }

    public class GetCompany
    {
        public string fcompid { get; set; }
        public string fcompname { get; set; }
        public string factive { get; set; }
        public string faudtuser { get; set; }
        public string faudtdate { get; set; }
        public string faudttime { get; set; }

    }
}