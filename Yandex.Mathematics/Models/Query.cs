using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Yandex.Mathematics
{
    internal class Query : IUserAction
    {
        //SessionID TimePassed TypeOfRecord SERPID QueryID ListOfURLs
        public long Time { get; private set; }
        public UInt64 SERPID { get; private set; }
        public UInt64 QueryID { get; private set; }
        public List<UInt64> URLs { get; private set; }

        public List<Click> Clicks { get; private set; }

        private Query()
        {
            Clicks = new List<Click>();
        }

        internal static Query Create(string[] tokens)
        {
            if (!tokens[2].Equals("Q"))
                throw new ArgumentException("unexpected type");
            Query query = new Query();

            query.Time = long.Parse(tokens[1]);
            query.SERPID = UInt64.Parse(tokens[3]);
            query.QueryID = UInt64.Parse(tokens[4]);

            query.URLs = new List<ulong>();
            for (int i = 5; i < tokens.Length; i++)
            {
                var url = UInt64.Parse(tokens[i]);
                query.URLs.Add(url);
            }
            return query;
        }

        public double AvgTimeBetweenClicks
        {
            get
            {
                long lastTime = 0;
                double avg = 0;
                foreach (Click c in Clicks)
                {
                    avg += c.Time - lastTime;
                    lastTime = c.Time;
                }
                avg /= Clicks.Count;
                if (Clicks.Count == 0)
                    avg = 0;
                return avg;
            }
        }

        public long MaxTimeBetweenClicks
        {
            get
            {
                long lastTime = 0;
                List<long> cliks = new List<long>();
                foreach (Click c in Clicks)
                {
                    cliks.Add(c.Time - lastTime);
                    lastTime = c.Time;
                }
                if (cliks.Count() > 0)
                    return cliks.Max();
                return 0;
            }
        }

        public long MinTimeBetweenClicks
        {
            get
            {
                long lastTime = 0;
                List<long> cliks = new List<long>();
                foreach (Click c in Clicks)
                {
                    cliks.Add(c.Time - lastTime);
                    lastTime = c.Time;
                }
                if (cliks.Count() > 0)
                    return cliks.Min();
                return 0;
            }
        }

        public long FirstClickTime
        {
            get
            {
                if (Clicks.Count > 0)
                    return Clicks[0].Time;
                return 0;
            }
        }

        public long FirstClickResultIndex
        {
            get
            {
                if (Clicks.Count > 0)
                {
                    var url = Clicks[0].URLID;
                    return URLs.IndexOf(url);
                }
                //Debug.Fail("bad :(");
                return 10;
            }
        }
    }
}
