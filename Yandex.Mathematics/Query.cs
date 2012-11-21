using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yandex.Mathematics
{
    internal class Query : IUserAction
    {
        //SessionID TimePassed TypeOfRecord SERPID QueryID ListOfURLs
        public long Time { get; private set; }
        public UInt64 SERPID { get; private set; }
        public UInt64 QueryID { get; private set; }
        public List<UInt64> URLs { get; private set; }

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
    }
}
