using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Aurelius_API.ModelAPI.Configuration
{
    public class ConfigurationModel
    {
        public string FID { get; set; }
        public string FMAILSERVER { get; set; }
        public string FUSERNAME { get; set; }
        public string FUSERPWD { get; set; }
        public string FPORT { get; set; }
        public string FSENDER { get; set; }
        public string FUSESSL { get; set; }
      
    }
    public class sendEmailModel
    {
        public string userEmail { get; set; }
    }
    
}