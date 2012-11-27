using System;
using System.Windows; // the root WPF namespace
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using DebugVisualization;
using System.Threading;
using AForge.Neuro;

namespace Yandex.Mathematics
{
    internal sealed class Program
    {
        private const int MAX_LINES = 200000;

        static void Main(string[] args)
        {
            string path = Environment.CurrentDirectory + @"\..\..\..\dataset\train";

            Dictionary<string, User> users = new Dictionary<string, User>();
            Dictionary<string, Session> sessions = new Dictionary<string, Session>();

            int linesReaded = 0;
            using (var fs = new FileStream(path, FileMode.Open))
            using (var sr = new StreamReader(fs))
            {
                string line = null;
                while (linesReaded < MAX_LINES)
                //while ((line = sr.ReadLine()) != null)
                {
                    line = sr.ReadLine();
                    string[] tokens = line.Split('\t');

                    if (tokens[2].Equals("M"))
                    {
                        User user;
                        if (users.ContainsKey(tokens[3])) // userid                        
                            user = users[tokens[3]];
                        else
                        {
                            user = new User() { UserID = UInt64.Parse(tokens[3]) };
                            users.Add(tokens[3], user);
                        }
                        
                        var session = Session.Create(user, tokens);
                        user.Sessions.Add(session);

                        if (!sessions.ContainsKey(tokens[0]))
                            sessions.Add(tokens[0], session);
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
                    if (linesReaded % 1000 == 0)
                        Console.WriteLine(linesReaded);
                }
            }

            var sess = sessions.Values.ToList<Session>(); //.FindAll(p => p.Switch != Session.SwitchType.No);            
            List<double> trainErrors;
            List<double> cvErrors;
            //Train(sess, out trainErrors, out cvErrors);

            //Visualize(trainErrors, cvErrors);

            //TrainLambda(sess, out trainErrors, out cvErrors);
            //Visualize(trainErrors, cvErrors);

            var trainSize = (int)(0.7 * sess.Count);
            var cvSize = sess.Count - trainSize;
            var train = sess.GetRange(0, trainSize);
            train = train.FindAll(p => p.User.Sessions.Count > 5);
            var cv = sess.GetRange(trainSize, cvSize);

            Console.WriteLine("Start neuro");
            NeuroNet nn = new NeuroNet();
            List<double> tErr, cvErr;
            nn.Train(train, cv, out tErr, out cvErr);

            //Visualize(tErr, cvErr);

            Console.WriteLine();
            Console.WriteLine("Train data metrics");
            //EstimateMetrics(train, nn);
            Console.WriteLine();
            Console.WriteLine("CV data metrics");
            EstimateMetrics(cv, nn);

            Console.ReadKey();
        }

        private static void TrainLambda(List<Session> sess, out List<double> trainErrors, out List<double> cvErrors)
        {
            trainErrors = new List<double>();
            cvErrors = new List<double>();
            for (double alpha = 0.1; alpha < 2; alpha += 0.1)
            {
                var trainSize = (int)(0.7 * sess.Count);
                var cvSize = sess.Count - trainSize;
                var train = sess.GetRange(0, trainSize);
                var cv = sess.GetRange(trainSize, cvSize);
                
                NeuroNet nn = new NeuroNet();
                List<double> tErr, cvErr;
                nn.Train(train, cv, out tErr, out cvErr, new SigmoidFunction(alpha));
                trainErrors.Add(tErr.Last());
                cvErrors.Add(cvErr.Last());
                Console.WriteLine("Trained alpha: {0}", alpha);
            }
        }

        private static void EstimateMetrics(List<Session> data, NeuroNet nn)
        {
            int truePositive = 0;
            int trueNegative = 0;
            int falsePositive = 0;
            int falseNegative = 0;
            foreach (Session s in data)
            {
                bool detected = nn.DetectSwitch(s);
                if (s.Switch == Session.SwitchType.No)
                {
                    if (detected)
                        falsePositive++;
                    else
                        trueNegative++;
                }
                if (s.Switch != Session.SwitchType.No)
                {
                    if (detected)
                        truePositive++;
                    else
                        falseNegative++;
                }
            }
            double precision = ((double)truePositive) / (truePositive + falsePositive);
            double recall = ((double)truePositive) / (truePositive + falseNegative);
            double f1 = 2 * precision * recall / (precision + recall);
            double missclassificationError = ((double)(falseNegative + falsePositive)) / data.Count;

            Console.WriteLine("Precision: {0}", precision);
            Console.WriteLine("Recall: {0}", recall);
            Console.WriteLine("F1: {0}", f1);
            Console.WriteLine("Missclassification: {0}", missclassificationError);
        }

        private static void Train(List<Session> sess, out List<double> trainErrors, out List<double> cvErrors)
        {
            trainErrors = new List<double>();
            cvErrors = new List<double>();
            for (int size = sess.Count % 1000; size < sess.Count; size += 1000)
            {
                var trainSize = (int)(0.8 * size);
                var cvSize = size - trainSize;
                var train = sess.GetRange(0, trainSize);
                var cv = sess.GetRange(trainSize, cvSize);

                var detectedTrain = train.Count(p => p.Switch != Session.SwitchType.No);
                var detectedCV = cv.Count(p => p.Switch != Session.SwitchType.No);

                NeuroNet nn = new NeuroNet();
                List<double> tErr, cvErr;
                nn.Train(train, cv, out tErr, out cvErr);
                trainErrors.Add(tErr.Last());
                cvErrors.Add(cvErr.Last());
                Console.WriteLine("Trained size: {0}/{1}", size, sess.Count);
            }
        }

        private static void OpenWindow(List<double> trainErrors, List<double> cvErrors)
        {
            //Exception(Cannot create more than one System.Windows.Application instance in the same AppDomain.)
            //is thrown at the second iteration.
            var app = new System.Windows.Application();
            var window = new MainWindow(trainErrors, cvErrors);
            app.Run(window);
            //User  closes the opened window manually.
        }

        private static void Visualize(List<double> trainErrors, List<double> cvErrors)
        {
            var thread = new Thread(() =>
            {
                OpenWindow(trainErrors, cvErrors);
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();
        }
    }
}
