using System;
using Azure;
using Azure.Data.Tables;

namespace GoogleFitOnFhir.Models
{
    public class QueueMessage
    {
      public QueueMessage()
      {
      }

      public string UserId { get; set; }
    }
}
