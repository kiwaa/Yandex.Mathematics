using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;

namespace Yandex.Mathematics.Wpf
{
    public class MainViewModel
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(MainViewModel));

        public MainViewModel()
        {
            log.Info("test");
        }
    }
}
