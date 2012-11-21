using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace Yandex.Mathematics
{
    internal sealed class Program
    {
        private const int MAX_LINES = 50000;

        static void Main(string[] args)
        {
            string path = Environment.CurrentDirectory + @"\..\..\..\dataset\train";

            Dictionary<string, Session> sessions = new Dictionary<string, Session>();

            int linesReaded = 0;
            using (var fs = new FileStream(path, FileMode.Open))
            using (var sr = new StreamReader(fs))
            {
                while (linesReaded < MAX_LINES)
                {
                    string line = sr.ReadLine();
                    string[] tokens = line.Split('\t');

                    if (tokens[2].Equals("M"))
                    {
                        if (!sessions.ContainsKey(tokens[0]))
                            sessions.Add(tokens[0], Session.Create(tokens));
                        else
                            Debug.Fail("unexpected");
                    }
                    if (tokens[2].Equals("Q"))
                    {
                        if (sessions.ContainsKey(tokens[0]))
                        {
                            var query = Query.Create(tokens);
                            sessions[tokens[0]].AddQuery(query);
                        }
                        else
                            Debug.Fail("unexpected");
                    }
                    if (tokens[2].Equals("C"))
                    {
                        if (sessions.ContainsKey(tokens[0]))
                        {
                            var click = Click.Create(tokens);
                            sessions[tokens[0]].AddClick(click);
                        }
                        else
                            Debug.Fail("unexpected");
                    }
                    if (tokens[2].Equals("S"))
                    {
                        if (sessions.ContainsKey(tokens[0]))
                            sessions[tokens[0]].AddSwitch(Switch.Create(tokens));
                        else
                            Debug.Fail("unexpected");
                    }
                    linesReaded++;
                }
            }

            var sess = sessions.Values.ToList<Session>(); //.FindAll(p => p.Switch != Session.SwitchType.No);
            var detected = sess.Count(p => p.Switch != Session.SwitchType.No);
            NeuroNet nn = new NeuroNet();
            IEnumerable<double> errors = nn.Train(sess);

            int correct = 0;
            foreach (Session s in sess)
            {
                bool prediction = nn.DetectSwitch(s);
                if (s.Switch == Session.SwitchType.No && !prediction)
                    correct++;
                if (s.Switch != Session.SwitchType.No && prediction)
                    correct++;                         
            }
        }
    }
}
