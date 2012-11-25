using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AForge.Neuro;
using AForge.Neuro.Learning;

namespace Yandex.Mathematics
{
    internal class NeuroNet
    {
        private ActivationNetwork _network;
        private const int inputSize = 18;
        private const int classesCount = 2;

        public void Train(List<Session> train, List<Session> cv, out List<double> trainErrors, out List<double> cvErrors)
        {
            trainErrors = new List<double>();
            cvErrors = new List<double>();

            var count = train.Count;
            
            // prepare learning data
            double[][] input = new double[count][];
            double[][] output = new double[count][];            
            // preparing the data
            for (int i = 0; i < count; i++)
            {
                input[i] = CreateInput(train[i]);
                output[i] = CreateOutput(train[i]);
            }

            // prepare cv data
            double[][] cvIn = new double[cv.Count][];
            double[][] cvOut = new double[cv.Count][];
            // preparing the data
            for (int i = 0; i < cv.Count; i++)
            {
                cvIn[i] = CreateInput(cv[i]);
                cvOut[i] = CreateOutput(cv[i]);
            }

            // create perceptron
            _network = new ActivationNetwork(new SigmoidFunction(), inputSize, classesCount);
            // create teacher
            PerceptronLearning teacher = new PerceptronLearning(_network);
            // set learning rate
            teacher.LearningRate = 0.01;
            // loop
            int iter = 0;
            double error = 999;
            double delta = 999;
            Console.WriteLine("Train Network");
            //while (iter < 1000)
            while (delta > 0.000001 && iter < 10000)
            {                
                // run epoch of learning procedure
                double trainError = teacher.RunEpoch(input, output);

                double trainError2 = ComputeCVError(_network, input, output);
                double cvError = ComputeCVError(_network, cvIn, cvOut);

                delta = Math.Abs(error - cvError);
                error = cvError;
                trainErrors.Add(trainError2);
                cvErrors.Add(cvError);
                iter++;
                Console.WriteLine(iter);
            }
        }

        private double ComputeCVError(ActivationNetwork network, double[][] dataIn, double[][] dataOut)
        {
            double error = 0;
            for (int i = 0; i < dataIn.Length; i++)
            {
                double[] output = network.Compute(dataIn[i]);                
                for (int j = 0; j < output.Length; j++)
                    error += (output[j] - dataOut[i][j]) * (output[j] - dataOut[i][j]);                
            }
            return error / 2 / dataIn.Length;
        }

        private double[] Run(Session session)
        {
            double[] input = CreateInput(session);            
            return _network.Compute(input);
        }

        public bool DetectSwitch(Session session)
        {
            double[] output = Run(session);
            if (output[0] > output[1])
                return false;
            return true;
        }

        private static double[] CreateInput(Session session)
        {
            double[] input = new double[inputSize];
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
            double[] output = new double[classesCount];
            output[0] = session.Switch == Session.SwitchType.No ? 1 : 0;
            output[1] = session.Switch != Session.SwitchType.No ? 1 : 0;
            return output;
        }
    }
}
