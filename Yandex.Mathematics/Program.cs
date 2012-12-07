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
        private const int MAX_USERS = 100000;

        static double[] mean;
        static double[] dev;


        public struct AnswerStruct
        {
            public string SID;
            public double Probability;
            public AnswerStruct(string sid, double probability)
            {
                SID = sid;
                Probability = probability;
            }
        }

        static void Main(string[] args)
        {
            string train = Environment.CurrentDirectory + @"\..\..\..\dataset\train";
            string test = Environment.CurrentDirectory + @"\..\..\..\dataset\test";
            string preparedData = Environment.CurrentDirectory + @"\..\..\..\dataset\data";
            string preparedCV = Environment.CurrentDirectory + @"\..\..\..\dataset\cv";
            string preparedUsers = Environment.CurrentDirectory + @"\..\..\..\dataset\users";
            string preparedCVUsers = Environment.CurrentDirectory + @"\..\..\..\dataset\cvusers";
            string trained = Environment.CurrentDirectory + @"\..\..\..\dataset\neuro.trained";
            string answer = Environment.CurrentDirectory + @"\..\..\..\dataset\answer";

            //Dictionary<string, User> users;
            //List<Session> trainList = PrepareDataInMemory(train, 0, 500000, false);
            //List<Session> cvList = PrepareDataInMemory(train, 500000, 200000, true);

            //foreach (Session s in cvList)
            //{
            //    var sessionWithSameUser = trainList.Find(p => p.User.UserID == s.User.UserID);
            //    if (sessionWithSameUser != null)
            //        s.User = sessionWithSameUser.User;
            //    else
            //        s.User = new User();
            //}

            //PrepareDataHDD(train, 0, 999999999, preparedData, preparedUsers, false);
            //PrepareDataHDD(train, 500000, 20000, preparedCV, preparedCVUsers, true);

            Dictionary<string, UserStruct> trainUsersStruct = GetUsersStructHDD(preparedUsers);
            Dictionary<string, Session> testSessions = PrepareTestData(test);            

            //List<double> trainErrors, cvErrors;

            //NeuroNet nn = new NeuroNet();
            //nn.Load(preparedData, trained);
            ////trainList = trainList.FindAll(p => p.User.Sessions.Count > 5);
            //nn.Train(trainList, cvList, out trainErrors, out cvErrors);
            //nn.Train(preparedData, preparedCV, trainUsersStruct, out trainErrors, out cvErrors);
            //Visualize(trainErrors, cvErrors);

            var answerList = new List<AnswerStruct>();
            Network network = ActivationNetwork.Load(trained);

            //EstimateMetrics(cvList, nn);

            mean = new double[18];
            dev = new double[18];

            int lines = 0;
            var sum = new double[18];
            using (var sr = new StreamReader(preparedData))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    var tokens = line.Split(' ');
                    //int userSessionsCount = int.Parse(tokens[21]);
                    //if (userSessionsCount < 5)
                    //    continue;
                    double[] input = ParseInput(tokens);
                    for (int i = 0; i < 18; i++)
                        sum[i] += input[i];
                    lines++;

                    if (lines % 1000 == 0)
                        Console.WriteLine("mean: " + lines);
                }
            }

            for (int i = 0; i < 18; i++)
            {
                mean[i] = sum[i] / lines;
                sum[i] = 0;
            }

            using (var sr = new StreamReader(preparedData))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    var tokens = line.Split(' ');
                    //int userSessionsCount = int.Parse(tokens[21]);
                    //if (userSessionsCount < 5)
                    //    continue;
                    double[] input = ParseInput(tokens);
                    for (int i = 0; i < 18; i++)
                        sum[i] += (input[i] - mean[i]) * (input[i] - mean[i]);
                    lines++;

                    if (lines % 1000 == 0)
                        Console.WriteLine("dev: " + lines);
                }
            }

            for (int i = 0; i < 18; i++)
            {
                dev[i] = sum[i] / lines;
                sum[i] = 0;
            }

            foreach (var pair in testSessions)
            {
                UserStruct user;
                if (trainUsersStruct.ContainsKey(pair.Value.User.UserID.ToString()))
                    user = trainUsersStruct[pair.Value.User.UserID.ToString()];
                else
                    user = new UserStruct();
                double[] input = CreateTestInput(pair.Value, user);
                double[] output = network.Compute(input);
                answerList.Add(new AnswerStruct(pair.Value.SessionID.ToString(), output[1] / (output[1] + output[0])));
            }
            answerList.Sort((x, y) => -x.Probability.CompareTo(y.Probability));

            using (StreamWriter sw = new StreamWriter(answer))
            {
                for (int m = 0; m < answerList.Count; m++)
                {
                    sw.WriteLine(answerList[m].SID);
                }
            }
            Console.ReadKey();
        }

        private static double[] ParseInput(string[] tokens)
        {
            double[] result = new double[18];
            for (int i = 0; i < 18; i++)
                result[i] = double.Parse(tokens[i]);
            return result;
        }

        private static double[] CreateTestInput(Session session, UserStruct userStruct)
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
            input[10] = userStruct.SwitchFreq;
            input[11] = userStruct.AvgTimeBeforeFirstSwitch;
            input[12] = userStruct.AvgQueriesBeforeFirstSwitch;
            input[13] = userStruct.AvgClicksBeforeFirstSwitch;
            input[14] = session.AvgTimeBetweenClicksInSERP;
            input[15] = session.AvgClicksPerQuery;
            input[16] = session.AvgFirstClickResultIndexPerQuery;
            input[17] = session.AvgFirstClickTimePerQuery;
            for (int i = 0; i < 18; i++)
                input[i] = (input[i] - mean[13]) / dev[13];

            return input;
        }

        private static Dictionary<string, UserStruct> GetUsersStructHDD(string preparedUsers)
        {
            var result = new Dictionary<string, UserStruct>();
            using (var fs = new FileStream(preparedUsers, FileMode.Open))
            using (var sr = new StreamReader(fs))
            {
                string line = null;
                //while (linesReaded < MAX_LINES)
                while ((line = sr.ReadLine()) != null)
                {
                    //line = sr.ReadLine();
                    string[] tokens = line.Split(' ');

                    if (result.ContainsKey(tokens[0]))
                        continue;

                    UserStruct user = new UserStruct();
                    
                    user.SwitchFreq = double.Parse(tokens[1]);
                    user.AvgTimeBeforeFirstSwitch = double.Parse(tokens[2]);
                    user.AvgQueriesBeforeFirstSwitch = double.Parse(tokens[3]);
                    user.AvgClicksBeforeFirstSwitch = double.Parse(tokens[4]);
                    result.Add(tokens[0], user);
                }
            }
            return result;
        }        

        private static void PrepareDataHDD(string dataPath, int linesOffset, int linesToRead, string preparedDataPath, string preparedUsersPath, bool skipSwitch)
        {
            HashSet<string> oldUsers = new HashSet<string>();

            int totalUsers = 0;
            int newUsers = MAX_USERS;

            Dictionary<string, User> users = new Dictionary<string, User>();
            while (newUsers == MAX_USERS)
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
                    while ((line = sr.ReadLine()) != null && linesReaded < linesOffset + linesToRead)
                    {
                        linesReaded++;
                        if (linesReaded % 1000 == 0)
                            Console.WriteLine(linesReaded);
                        if (linesReaded < linesOffset)
                        {
                            continue;
                        }
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
                        if (!skipSwitch && tokens[2].Equals("S"))
                        {
                            if (sessions.ContainsKey(tokens[0]))
                                sessions[tokens[0]].AddSwitch(Switch.Create(tokens));
                            //else
                            //    Debug.Fail("unexpected");
                        }
                    }
                }

//<<<<<<< HEAD
//                var sess = sessions.Values.ToList(); //.FindAll(p => p.Switch != Session.SwitchType.No);            
//                var userss = users.Values.ToList();
//=======
                var sess = sessions.Values.ToList<Session>(); //.FindAll(p => p.Switch != Session.SwitchType.No);
                var userss = users.Values.ToList();

                WriteData(preparedDataPath, preparedUsersPath, sess, userss);
            }
        }

        private static List<Session> PrepareDataInMemory(string dataPath, int linesOffset, int linesToRead, bool skipSwitch)
        {
            Dictionary<string, User> users = new Dictionary<string, User>();
            Dictionary<string, Session> sessions = new Dictionary<string, Session>();

            int linesReaded = 0;
            using (var fs = new FileStream(dataPath, FileMode.Open))
            using (var sr = new StreamReader(fs))
            {
                string line = null;
                while (linesReaded < linesOffset + linesToRead)
                //while ((line = sr.ReadLine()) != null && linesReaded < linesOffset + linesToRead)
                {
                    line = sr.ReadLine(); 
                    linesReaded++;
                    if (linesReaded % 1000 == 0)
                        Console.WriteLine(linesReaded);                    
                    if (linesReaded < linesOffset)
                    {
                        continue;
                    }
                    
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
                    if (!skipSwitch && tokens[2].Equals("S"))
                    {
                        if (sessions.ContainsKey(tokens[0]))
                            sessions[tokens[0]].AddSwitch(Switch.Create(tokens));
                        //else
                        //    Debug.Fail("unexpected");
                    }
                }
            }

            return sessions.Values.ToList<Session>(); //.FindAll(p => p.Switch != Session.SwitchType.No);
        }

        private static Dictionary<string, Session> PrepareTestData(string dataPath)
        {
            Dictionary<string, User> users = new Dictionary<string, User>();            
            Dictionary<string, Session> sessions = new Dictionary<string, Session>();
            
            int linesReaded = 0;
            using (var fs = new FileStream(dataPath, FileMode.Open))
            using (var sr = new StreamReader(fs))
            {
                string line = null;
                //while (linesReaded < MAX_LINES)
                while ((line = sr.ReadLine()) != null)
                {
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
            return sessions;
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
                //OpenWindow(trainErrors, cvErrors);
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();
        }

        private static void WriteData(string dataPath, string usersPath, List<Session> sessions, List<User> users)
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

            using (StreamWriter sw = new StreamWriter(dataPath, true))
            {
                for (int m = 0; m < sessions.Count; m++)
                {
                    string s = "";
                    for (int j = 0; j < 20; j++)
                        s += data[m][j] + " ";
                    s += sessions[m].User.UserID + " ";
                    s += sessions[m].User.Sessions.Count;
                    sw.WriteLine(s.Trim());
                }
            }

            using (StreamWriter sw = new StreamWriter(usersPath, true))
            {
                for (int m = 0; m < users.Count; m++)
                {
                    string s = "";
                    s += users[m].UserID + " ";
                    s += users[m].SwitchFreq + " ";
                    s += users[m].AvgTimeBeforeFirstSwitch + " ";
                    s += users[m].AvgQueriesBeforeFirstSwitch + " ";
                    s += users[m].AvgClicksBeforeFirstSwitch;
                    sw.WriteLine(s);
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
