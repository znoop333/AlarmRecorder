using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMQWonderwareAdapter
{
    class RmqCommandMessage
    {
        public String Command { get; set; }
        public String TagName { get; set; }
        public String Value { get; set; }
        public Boolean Once { get; set; }
        public String RequesterIP { get; set; }
        public String RequesterName { get; set; }

        public String Timestamp { get; set; }
        public string CorrelationId { get; set; }
    }

    class RmqResponseMessage
    {
        public String Command { get; set; }
        public String TagName { get; set; }
        public String Value { get; set; }
        public Boolean Once { get; set; }
        public string CorrelationId { get; set; }
        public String Timestamp { get; set; }
        public String DataType { get; set; }
    }
}
