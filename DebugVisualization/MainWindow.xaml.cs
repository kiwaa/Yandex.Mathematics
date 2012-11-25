using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using OxyPlot;

namespace DebugVisualization
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private PlotModel _model;

        private List<double> trainErrors;
        private List<double> cvErrors;

        public MainWindow()
        {
            InitializeComponent();
        }

        public MainWindow(List<double> trainErrors, List<double> cvErrors) : this()
        {
            _model = new PlotModel("ANN errors") { LegendSymbolLength = 24 };
            var s1 = new LineSeries("train error")
            {
                Color = OxyColors.SkyBlue,
                MarkerType = MarkerType.Circle,
                MarkerSize = 6,
                MarkerStroke = OxyColors.White,
                MarkerFill = OxyColors.SkyBlue,
                MarkerStrokeThickness = 1.5
            };
            for (int i = 0; i < trainErrors.Count; i++)
                s1.Points.Add(new DataPoint(i * 10000, trainErrors[i]));
            _model.Series.Add(s1);

            var s2 = new LineSeries("cv error")
            {
                Color = OxyColors.Teal,
                MarkerType = MarkerType.Diamond,
                MarkerSize = 6,
                MarkerStroke = OxyColors.White,
                MarkerFill = OxyColors.Teal,
                MarkerStrokeThickness = 1.5
            };
            for (int i = 0; i < cvErrors.Count; i++)
                s2.Points.Add(new DataPoint(i * 10000, cvErrors[i]));
            _model.Series.Add(s2);
            this.DataContext = _model;
        }
    }
}
