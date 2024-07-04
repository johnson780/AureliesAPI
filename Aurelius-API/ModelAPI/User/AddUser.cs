using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Aurelius_API.ModelAPI.User
{
    public class AddUser
    {
        public string FRole { get; set; }
        public string Username { get; set; }
        public string Fullname { get; set; }
        public string Company { get; set; }
        public string Status { get; set; }


    }
}