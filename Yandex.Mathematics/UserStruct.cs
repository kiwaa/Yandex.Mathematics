using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yandex.Mathematics
{
    struct UserStruct
    {
        UInt64 UserID { get; set; }
        public double SwitchFreq  { get; set; }
        public double AvgTimeBeforeFirstSwitch  { get; set; }
        public double AvgClicksBeforeFirstSwitch  { get; set; }
        public double AvgQueriesBeforeFirstSwitch { get; set; }
    }
}
