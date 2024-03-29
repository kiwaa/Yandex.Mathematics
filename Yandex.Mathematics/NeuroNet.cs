﻿using System;
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

        double[] mean;
        double[] dev;

        public void Train(List<Session> train, List<Session> cv, out List<double> trainErrors, out List<double> cvErrors)
        {
            Train(train, cv, out trainErrors, out cvErrors, new SigmoidFunction(1.5));
        }

        public void Train(List<Session> train, List<Session> cv, out List<double> trainErrors, out List<double> cvErrors, IActivationFunction function)
        {
            trainErrors = new List<double>();
            cvErrors = new List<double>();

            var count = train.Count;

            // prepare learning data
            Console.WriteLine("prepare learning data");
            double[][] input = new double[count][];
            double[][] output = new double[count][];

            // preparing the data
            for (int i = 0; i < count; i++)
            {
                input[i] = CreateInput(train[i]);
                output[i] = CreateOutput(train[i]);
            }

            Console.WriteLine("feature scaling");
            mean = new double[inputSize];
            dev = new double[inputSize];

            for (int i = 0; i < inputSize; i++)
            {
                var query = input.Select(p => p[i]);
                mean[i] = query.Average();
                dev[i] = query.Deviation(mean[i]);
            }

            for (int i = 0; i < count; i++)
                for (int j = 0; j < inputSize; j++)
                {
                    input[i][j] = (input[i][j] - mean[j]) / dev[j];
                }

            Console.WriteLine("prepare cv data");
            // prepare cv data
            double[][] cvIn = new double[cv.Count][];
            double[][] cvOut = new double[cv.Count][];
            // preparing the data
            for (int i = 0; i < cv.Count; i++)
            {
                cvIn[i] = CreateInput(cv[i]);
                cvOut[i] = CreateOutput(cv[i]);
            }

            Console.WriteLine("cv feature scaling");
            for (int i = 0; i < cv.Count; i++)
                cvIn[i] = ScaleInput(cvIn[i]);

            Console.WriteLine("create network");

            // create perceptron
            _network = new ActivationNetwork(function, inputSize, inputSize, classesCount);
            _network.Randomize();
            // create teacher
            //PerceptronLearning teacher = new PerceptronLearning(_network);
            BackPropagationLearning teacher = new BackPropagationLearning(_network);

            // set learning rate
            teacher.LearningRate = 0.01;

            // loop
            int iter = 0;
            double error = 999;
            double delta = 999;
            Console.WriteLine("Train Network");
            //while (iter < 1000)
            while (delta > 1 && iter < 5000)
            //while (iter < 2000)
            {
                // run epoch of learning procedure
                double trainError = teacher.RunEpoch(input, output);

                double trainError2 = ComputeCVError(_network, input, output);
                double cvError = ComputeCVError(_network, cvIn, cvOut);

                delta = Math.Abs(error - trainError);
                error = trainError;
                trainErrors.Add(trainError2);
                cvErrors.Add(cvError);
                iter++;
                if (iter % 100 == 0)
                    Console.WriteLine(iter);
            }
            Console.WriteLine(iter);
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
            input = ScaleInput(input);
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

        private double[] ScaleInput(double[] input)
        {
            for (int j = 0; j < inputSize; j++)
            {
                input[j] = (input[j] - mean[j]) / dev[j];
            }
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
