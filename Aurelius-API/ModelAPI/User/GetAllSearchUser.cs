﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Aurelius_API.ModelAPI.User
{

    public class ListUserModel
    {
        public string FID { get; set; }
        public string FUSERNAME { get; set; }
        public string FFULLNAME { get; set; }
        public string FROLE { get; set; }
        public string FACTIVE { get; set; }
        public string FAUDTUSER { get; set; }
        public string FAUDTDATE { get; set; }
        public string FAUDTTIME { get; set; }
    }

}