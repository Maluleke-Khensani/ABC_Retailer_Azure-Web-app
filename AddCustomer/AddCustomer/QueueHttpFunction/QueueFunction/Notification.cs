using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;

namespace ABC_Retails_Functions.QueueHttpFunction.QueueFunction
{
    public class Notification : ITableEntity
    {
        public string Id => RowKey;

        public string Message { get; set; } = string.Empty;

        public string? Category { get; set; }

        public string ProductName
        {
            get => RowKey;
            set => RowKey = value;
        }

        public int? NewStock { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;


        public string PartitionKey { get; set; } = "Notifications";
        public string RowKey { get; set; } 

        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}