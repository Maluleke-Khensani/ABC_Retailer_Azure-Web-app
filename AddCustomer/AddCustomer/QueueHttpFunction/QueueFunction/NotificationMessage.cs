using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABC_Retails_Functions.QueueHttpFunction.QueueFunction
{
    public class NotificationMessage
    {
        public string Message { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int NewStock { get; set; }
    }
}
