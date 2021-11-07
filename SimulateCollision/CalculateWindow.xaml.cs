using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SimulateCollision
{
    /// <summary>
    /// CalculateWindow..xaml 的交互逻辑
    /// </summary>
    public partial class CalculateWindow : Window
    {

        private double simTime;

        public double SimTime
        {
            get { return simTime; }
            set { simTime = value; }
        }

        public CalculateWindow()
        {
            InitializeComponent();
        }

        private void btnSubmit_Click(object sender, RoutedEventArgs e)
        {
            if (!double.TryParse(txtSimTime.Text, out simTime))
                simTime = 5;
            simTime = Math.Max(simTime, 0.1);
            simTime = Math.Min(simTime, 999);

            DialogResult = true;
        }
    }
}
