using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yandex.Mathematics
{
    static class EnumerableExtensions
    {
        public static double Deviation(this IEnumerable<double> input, double mean)
        {
            double dev = input.Sum(p => (p - mean) * (p - mean));
            return Math.Sqrt(dev / input.Count());
        }
    }
}
