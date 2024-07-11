using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Aurelius_API.ModelPortal.Login
{
    public class UserLoginDto
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Company {  get; set; }
    }
}