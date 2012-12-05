﻿using System;
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
        private const int MAX_USERS = 100000;

        static void Main(string[] args)
        {
            string data = Environment.CurrentDirectory + @"\..\..\..\dataset\train";
            string prepared = Environment.CurrentDirectory + @"\..\..\..\dataset\prepared_train";

            PrepareDataHDD(data, prepared);
        }

        private static void PrepareDataHDD(string dataPath, string preparedDataPath)
        {
            HashSet<string> oldUsers = new HashSet<string>();

            int totalUsers = 0;
            int newUsers = 0;

            Dictionary<string, User> users = new Dictionary<string, User>();
            while (totalUsers < 20)
            {
                Dictionary<string, Session> sessions = new Dictionary<string, Session>();

                var keys = users.Keys.ToList();
                foreach (var key in keys)
                {
                    oldUsers.Add(key);
                }
                users = new Dictionary<string, User>();

                totalUsers++;
                newUsers = 0;

                int linesReaded = 0;
                using (var fs = new FileStream(dataPath, FileMode.Open))
                using (var sr = new StreamReader(fs))
                {
                    string line = null;
                    //while (linesReaded < MAX_LINES)
                    while ((line = sr.ReadLine()) != null)
                    {
                        //line = sr.ReadLine();
                        string[] tokens = line.Split('\t');

                        if (tokens[2].Equals("M"))
                        {
                            User user;
                            if (users.ContainsKey(tokens[3])) // userid                            
                                user = users[tokens[3]];
                            else
                            {
                                if (oldUsers.Contains(tokens[3]))
                                    continue;
                                if (newUsers < MAX_USERS)
                                {
                                    user = new User() { UserID = UInt64.Parse(tokens[3]) };
                                    users.Add(tokens[3], user);
                                    newUsers++;
                                }
                                else
                                    continue;
                            }

                            var session = Session.Create(user, tokens);
                            user.Sessions.Add(session);

                            if (!sessions.ContainsKey(tokens[0]))
                                sessions.Add(tokens[0], session);
                            //else
                            //    Debug.Fail("unexpected");
                        }
                        if (tokens[2].Equals("Q"))
                        {
                            if (sessions.ContainsKey(tokens[0]))
                            {
                                var query = Query.Create(tokens);
                                sessions[tokens[0]].AddQuery(query);
                            }
                            //else
                            //    Debug.Fail("unexpected");
                        }
                        if (tokens[2].Equals("C"))
                        {
                            if (sessions.ContainsKey(tokens[0]))
                            {
                                var click = Click.Create(tokens);
                                sessions[tokens[0]].AddClick(click);
                            }
                            //else
                            //    Debug.Fail("unexpected");
                        }
                        if (tokens[2].Equals("S"))
                        {
                            if (sessions.ContainsKey(tokens[0]))
                                sessions[tokens[0]].AddSwitch(Switch.Create(tokens));
                            //else
                            //    Debug.Fail("unexpected");
                        }
                        linesReaded++;
                        if (linesReaded % 1000 == 0)
                            Console.WriteLine(linesReaded);
                    }
                }

                var sess = sessions.Values.ToList<Session>(); //.FindAll(p => p.Switch != Session.SwitchType.No);            

                WriteData(preparedDataPath, sess);
            }
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

        private static void WriteData(string path, List<Session> sessions)
        {
            double[][] data = new double[sessions.Count][];
            for (int i = 0; i < sessions.Count; i++)
            {
                data[i] = new double[20];
                var input = CreateInput(sessions[i]);
                var output = CreateOutput(sessions[i]);
                for (int k = 0; k < 18; k++)
                    data[i][k] = input[k];
                data[i][18] = output[0];
                data[i][19] = output[1];
            }
            
            using (StreamWriter sw = new StreamWriter(path, true))
            {
                for (int m = 0; m < sessions.Count; m++)
                {
                    string s = "";
                    for (int j = 0; j < 20; j++)
                        s += data[m][j] + " ";
                    sw.WriteLine(s.Trim());
                }
            }
        }

        private static double[] CreateInput(Session session)
        {
            double[] input = new double[18];
            input[0] = session.FirstClickTime;
            input[1] = session.TotalTimes;
            input[2] = session.MinTimeBetweenQueries;
            input[3] = session.MaxTimeBetweenQueries;
            input[4] = session.AvgMinTimeBetweenClicksInSERP;
            input[5] = session.AvgMaxTimeBetweenClicksInSERP;
            input[6] = session.TotalClicks;
            input[7] = session.TotalQueries;
            input[8] = session.FirstClickPageDuration;
            input[9] = session.FirstClickResultIndex;
            input[10] = session.User.SwitchFreq;
            input[11] = session.User.AvgTimeBeforeFirstSwitch;
            input[12] = session.User.AvgQueriesBeforeFirstSwitch;
            input[13] = session.User.AvgClicksBeforeFirstSwitch;
            input[14] = session.AvgTimeBetweenClicksInSERP;
            input[15] = session.AvgClicksPerQuery;
            input[16] = session.AvgFirstClickResultIndexPerQuery;
            input[17] = session.AvgFirstClickTimePerQuery;

            return input;
        }
        
        private static double[] CreateOutput(Session session)
        {
            double[] output = new double[2];
            output[0] = session.Switch == Session.SwitchType.No ? 1 : 0;
            output[1] = session.Switch != Session.SwitchType.No ? 1 : 0;
            return output;
        }
    }
}
