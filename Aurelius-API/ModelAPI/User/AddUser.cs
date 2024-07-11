using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Aurelius_API.ModelAPI.User
{
    public class AddUser
    {
        public string FROLE { get; set; }
        public string FUSERNAME { get; set; }
        public string FFULLNAME { get; set; }
        public List<string> FCOMPID { get; set; }
        public string FACTIVE { get; set; }


    }
}