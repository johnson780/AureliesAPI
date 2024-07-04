using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Aurelius_API.ModelAPI.User
{
    public class GetByUserID
    {
        public string ID { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
    }
}