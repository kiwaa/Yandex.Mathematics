using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Yandex.Mathematics
{
    internal class Session
    {
        public enum SwitchType
        {
            Bar, //тулбар = B
            Search, //поисковая выдача = P
            Hybrid, // оба типа = H
            No //не удалось отследить переходы = N
        }

        #region Creation

        public static Session Create(User user, string[] tokens)
        {
            if (!tokens[2].Equals("M"))
                throw new ArgumentException("unexpected type");
            Session session = new Session();

            session.SessionID = UInt64.Parse(tokens[0]);
            session.Day = Byte.Parse(tokens[1]);
            session.User = user;
            switch (tokens[4])
            {
                case "B":
                    session.Switch = SwitchType.Bar;
                    break;
                case "P":
                    session.Switch = SwitchType.Search;
                    break;
                case "H":
                    session.Switch = SwitchType.Hybrid;
                    break;
                case "N":
                    session.Switch = SwitchType.No;
                    break;
                default:
                    Debug.Fail("unexpected switch type: " + tokens[4]);
                    break;
            }
            session.Actions = new List<IUserAction>();
            return session;
        }

        #endregion //Creation

        #region Properties

        public UInt64 SessionID { get; private set; }
        public Byte Day { get; private set; }
        public User User { get; private set; }
        public SwitchType Switch { get; private set; }
        public List<IUserAction> Actions { get; private set; }

        #endregion //Properties        

        #region Constructor

        private Session()
        {
        }

        #endregion //Constructor

        public long FirstSwitchTime
        {
            get
            {
                var firstSwitch = Actions.Find(p => p.GetType() == typeof(Switch));
                if (firstSwitch != null)
                    return firstSwitch.Time;
                ////if don't have switch, take last action time
                //if (Actions.Count > 0)
                //    return Actions[Actions.Count - 1].Time;
                return 0;
            }
        }

        public long FirstClickTime
        {
            get
            {
                var firstClick = Actions.Find(p => p.GetType() == typeof(Click));
                if (firstClick != null)
                    return firstClick.Time;
                return 0;
            }
        }

        public long FirstClickPageDuration
        {
            get
            {
                var firstClickIndex = Actions.FindIndex(p => p.GetType() == typeof(Click));
                if (firstClickIndex > 0 && Actions.Count > firstClickIndex + 1)
                    return Actions[firstClickIndex+1].Time - Actions[firstClickIndex].Time;
                return 0;
            }
        }

        public long FirstClickResultIndex
        {
            get
            {
                var firstClick = (Actions.Find(p => p.GetType() == typeof(Click)) as Click);
                if (firstClick != null)
                {
                    var url = firstClick.URLID;                    
                    var query = (Actions.Find(p => p.GetType() == typeof(Query) && (p as Query).SERPID == firstClick.SERPID) as Query);
                    return query.URLs.IndexOf(url);
                }
                return 0;
            }
        }
        
        public int TotalQueriesBeforeFirstSwitch
        {
            get
            {
                var actions = Actions.TakeWhile(p => p.GetType() != typeof(Switch));
                return actions.Count(p => p.GetType() == typeof(Query));
            }
        }

        public int TotalClicksBeforeFirstSwitch
        {
            get
            {
                var actions = Actions.TakeWhile(p => p.GetType() != typeof(Switch));
                return actions.Count(p => p.GetType() == typeof(Click));
            }
        }

        public int TotalQueries
        {
            get
            {
                return Actions.Count(p => p.GetType() == typeof(Query));
            }
        }

        public int TotalClicks
        {
            get
            {
                return Actions.Count(p => p.GetType() == typeof(Click));
            }
        }

        public long TotalTimes
        {
            get
            {
                if (Actions.Count > 0)
                    return Actions[Actions.Count - 1].Time;
                return 0;
            }
        }

        public double AvgTimeBetweenClicksInSERP
        {
            get
            {
                return Actions.FindAll(p => p.GetType() == typeof(Query)).Select(p => p as Query).Average(p => p.AvgTimeBetweenClicks);
            }
        }

        public double AvgMaxTimeBetweenClicksInSERP
        {
            get
            {
                return Actions.FindAll(p => p.GetType() == typeof(Query)).Select(p => p as Query).Average(p => p.MaxTimeBetweenClicks);
            }
        }

        public double AvgMinTimeBetweenClicksInSERP
        {
            get
            {
                return Actions.FindAll(p => p.GetType() == typeof(Query)).Select(p => p as Query).Average(p => p.MinTimeBetweenClicks);
            }
        }

        public double AvgClicksPerQuery
        {
            get
            {
                return Actions.FindAll(p => p.GetType() == typeof(Query)).Select(p => p as Query).Average(p => p.Clicks.Count);
            }
        }

        public double AvgFirstClickTimePerQuery
        {
            get
            {
                return Actions.FindAll(p => p.GetType() == typeof(Query)).Select(p => p as Query).Average(p => p.FirstClickTime);
            }
        }

        public double AvgFirstClickResultIndexPerQuery
        {
            get
            {
                return Actions.FindAll(p => p.GetType() == typeof(Query)).Select(p => p as Query).Average(p => p.FirstClickResultIndex);
            }
        }

        public long MaxTimeBetweenQueries
        {
            get
            {
                var times = GetTimeBetweenQueries();
                if (times.Count() > 0)
                    return times.Max();
                return 0;
            }
        }

        public long MinTimeBetweenQueries
        {
            get
            {
                var times = GetTimeBetweenQueries();
                if (times.Count() > 0)
                    return times.Min();
                return 0;
            }
        }

        #region Public Methods

        public void AddQuery(Query query)
        {
            Actions.Add(query);
        }

        public void AddSwitch(Switch @switch)
        {
            Actions.Add(@switch);
        }

        public void AddClick(Click click)
        {
            Actions.Add(click);

            var query = Actions.Find(p => p.GetType() == typeof(Query) && (p as Query).SERPID == click.SERPID);
            Query q = query as Query;
            if (q != null)
                q.Clicks.Add(click);            
        }

        #endregion //Public Methods

        private IEnumerable<long> GetTimeBetweenClicks()
        {
            List<long> times = new List<long>();
            var clicks = Actions.FindAll(p => p.GetType() == typeof(Click));
            for (int i = 0; i < clicks.Count - 1; i++)
            {
                times.Add(clicks[i + 1].Time - clicks[i].Time);
            }
            return times;
        }

        private IEnumerable<long> GetTimeBetweenQueries()
        {
            List<long> times = new List<long>();
            var queries = Actions.FindAll(p => p.GetType() == typeof(Query));
            for (int i = 0; i < queries.Count - 1; i++)
            {
                times.Add(queries[i + 1].Time - queries[i].Time);
            }
            return times;
        }
    }
}
