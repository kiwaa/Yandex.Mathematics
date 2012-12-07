using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yandex.Mathematics
{
    class User
    {
        private bool _estimated;

        public UInt64 UserID { get; set; }
        public List<Session> Sessions { get; private set; }

        //estimated properties        
        public double SwitchFreq { get; set; }
        public double AvgTimeBeforeFirstSwitch { get; set; }
        public double AvgClicksBeforeFirstSwitch { get; set; }
        public double AvgQueriesBeforeFirstSwitch { get; set; }

        public User()
        {
            Sessions = new List<Session>();
        }

        public void Estimate()
        {
            if (Sessions.Count == 0)
                return;
                //throw new InvalidOperationException("sessions not set");
            if (_estimated)
                return;

            double switchCount = Sessions.Count(p => p.Switch != Session.SwitchType.No);
            SwitchFreq = (switchCount / Sessions.Count);
            AvgTimeBeforeFirstSwitch = Sessions.Average(p => p.FirstSwitchTime);
            AvgClicksBeforeFirstSwitch = Sessions.Average(p => p.TotalClicksBeforeFirstSwitch);
            AvgQueriesBeforeFirstSwitch = Sessions.Average(p => p.TotalQueriesBeforeFirstSwitch);
            _estimated = true;
        }
    }
}
