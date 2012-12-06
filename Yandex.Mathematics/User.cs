using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yandex.Mathematics
{
    class User
    {
        public UInt64 UserID { get; set; }
        public List<Session> Sessions { get; set; }

        public User()
        {
            Sessions = new List<Session>();
        }

        public double SwitchFreq
        {
            get
            {
                if (Sessions.Count == 0)
                    return 0;
                double @switch = Sessions.Count(p => p.Switch != Session.SwitchType.No);
                return (@switch / Sessions.Count);
            }
        }

        public double AvgTimeBeforeFirstSwitch
        {
            get
            {
                if (Sessions.Count == 0)
                    return 0;
                return Sessions.Average(p => p.FirstSwitchTime);
            }
        }

        public double AvgClicksBeforeFirstSwitch
        {
            get
            {
                if (Sessions.Count == 0)
                    return 0;
                return Sessions.Average(p => p.TotalClicksBeforeFirstSwitch);
            }
        }

        public double AvgQueriesBeforeFirstSwitch
        {
            get
            {
                if (Sessions.Count == 0)
                    return 0;
                return Sessions.Average(p => p.TotalQueriesBeforeFirstSwitch);
            }
        }
    }
}
