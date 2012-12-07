using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yandex.Mathematics
{
    internal class Click : IUserAction
    {
        //SessionID TimePassed TypeOfRecord SERPID URLID
        public long Time { get; private set; }
        public UInt64 SERPID { get; private set; }
        public UInt64 URLID { get; private set; }

        private Click()
        {
        }

        internal static Click Create(string[] tokens)
        {
            if (!tokens[2].Equals("C"))
                throw new ArgumentException("unexpected type");
            Click click = new Click();

            click.Time = long.Parse(tokens[1]);
            click.SERPID = UInt64.Parse(tokens[3]);
            click.URLID = UInt64.Parse(tokens[4]);

            return click;
        }
    }
}
