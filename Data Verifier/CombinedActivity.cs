using System;
using System.Collections.Generic;
using System.Text;

namespace DataVerifier
{
    public class CombinedActivity
    {
        public int ID { get; set; }

        public string ActivityID { get; set; }

        public string User { get; set; }
        
        public TimeSpan Time { get; set; }

        public TimeSpan ProperTime { get; set; }
    }
}
