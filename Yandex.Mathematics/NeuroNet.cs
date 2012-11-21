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

        public IEnumerable<double> Train(List<Session> sessions)
        {
            List<double> errors = new List<double>();
            var count = sessions.Count;
            var inputSize = 8;
            var classesCount = 2;
            // prepare learning data
            double[][] input = new double[count][];
            double[][] output = new double[count][];
            // preparing the data
            for (int i = 0; i < count; i++)
            {
                input[i] = CreateInput(sessions[i]);
                
                output[i] = new double[classesCount];
                output[i][0] = sessions[i].Switch == Session.SwitchType.No ? 1 : 0;
                output[i][1] = sessions[i].Switch != Session.SwitchType.No ? 1 : 0;
            }

            // create perceptron
            _network = new ActivationNetwork(new ThresholdFunction(), 8, classesCount);
            // create teacher
            PerceptronLearning teacher = new PerceptronLearning(_network);
            // set learning rate
            teacher.LearningRate = 0.1;
            // loop
            int iter = 0;
            while (iter < 10000)
            {
                // run epoch of learning procedure
                double error = teacher.RunEpoch( input, output );
                errors.Add(error);
                iter++;
            }
            return errors;
        }

        public bool DetectSwitch(Session session)
        {
            double[] input = CreateInput(session);
            double[] output = _network.Compute(input);
            if (output[0] > output[1])
                return false;
            return true;
        }

        private double[] CreateInput(Session session)
        {
            double[] input = new double[8];
            input[0] = session.FirstClickTime;
            input[1] = session.TotalTimes;
            input[2] = session.MinTimeBetweenQueries;
            input[3] = session.MaxTimeBetweenQueries;
            input[4] = session.MinTimeBetweenClicks;
            input[5] = session.MaxTimeBetweenClicks;
            input[6] = session.TotalClicks;
            input[7] = session.TotalQueries;
            return input;
        }
    }
}
