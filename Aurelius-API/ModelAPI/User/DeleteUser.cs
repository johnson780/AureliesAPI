using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Aurelius_API.ModelAPI.User
{
    public class DeleteUser
    {
        public string ID { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
    }
}