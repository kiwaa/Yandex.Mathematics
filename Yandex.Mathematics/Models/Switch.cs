using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yandex.Mathematics
{
    internal class Switch : IUserAction
    {
        //SessionID TimePassed TypeOfRecord
        public long Time { get; private set; }

        internal static Switch Create(string[] tokens)
        {
            if (!tokens[2].Equals("S"))
                throw new ArgumentException("unexpected type");
            Switch s = new Switch();

            s.Time = long.Parse(tokens[1]);
            return s;
        }
    }
}
