using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Aurelius_API.ModelAPI.Transaction
{
    public class GetAllSearchTransaction
    {
        public string YearMonth { get; set; }
        public string ProccessedBy { get; set; }
        public string ProcessedDatetime { get; set; }
        public string PostedBy { get; set; }
        public string PostedDatetime { get; set; }
        public string Status { get; set; }

    }
}