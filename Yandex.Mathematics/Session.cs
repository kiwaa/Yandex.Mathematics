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

        public static Session Create(string[] tokens)
        {
            if (!tokens[2].Equals("M"))
                throw new ArgumentException("unexpected type");
            Session session = new Session();

            session.SessionID = UInt64.Parse(tokens[0]);
            session.Day = Byte.Parse(tokens[1]);
            session.UserID = UInt64.Parse(tokens[3]);
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
        public UInt64 UserID { get; private set; }
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
                return long.MaxValue;
            }
        }

        public long FirstClickTime
        {
            get
            {
                var firstClick = Actions.Find(p => p.GetType() == typeof(Click));
                if (firstClick != null)
                    return firstClick.Time;
                return long.MaxValue;
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
                return long.MaxValue;
            }
        }

        public long MaxTimeBetweenClicks
        {
            get
            {
                var times = GetTimeBetweenClicks();
                if (times.Count() > 0)
                    return times.Min();
                return long.MaxValue;
            }
        }

        public long MinTimeBetweenClicks
        {
            get
            {
                var times = GetTimeBetweenClicks();
                if (times.Count() > 0)
                    return times.Min();
                return long.MaxValue;
            }
        }

        public long MaxTimeBetweenQueries
        {
            get
            {
                var times = GetTimeBetweenQueries();
                if (times.Count() > 0)
                    return times.Max();
                return long.MaxValue;
            }
        }

        public long MinTimeBetweenQueries
        {
            get
            {
                var times = GetTimeBetweenQueries();
                if (times.Count() > 0)
                    return times.Min();
                return long.MaxValue;
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
