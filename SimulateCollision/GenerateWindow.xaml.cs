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
    /// GenerateWindow.xaml 的交互逻辑
    /// </summary>
    public partial class GenerateWindow : Window
    {
        private int number;

        public int Number
        {
            get { return number; }
            set { number = value; }
        }


        private double size;

        public double Size
        {
            get { return size; }
            set { size = value; }
        }

        private double sizeDev;

        public double SizeDev
        {
            get { return sizeDev; }
            set { sizeDev = value; }
        }

        private double velocity;

        public double Velocity
        {
            get { return velocity; }
            set { velocity = value; }
        }

        private double boxMargin;

        public double BoxMargin
        {
            get { return boxMargin; }
            set { boxMargin = value; }
        }






        public GenerateWindow()
        {
            InitializeComponent();
        }

        private void btnSubmit_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtNumber.Text, out number))
                number = 5;
            number = Math.Max(1, number);
            number = Math.Min(number, 9999);

            if (!double.TryParse(txtSize.Text, out size) || size < 0)
                size = 2.0;
            size = Math.Max(0.5, size);

            if (!double.TryParse(txtSizeDev.Text, out sizeDev))
                sizeDev = 1;
            sizeDev = Math.Max(0, sizeDev);

            if (!double.TryParse(txtVelocity.Text, out velocity))
                velocity = 0.5;
            velocity = Math.Max(0.01, velocity);

            if (!double.TryParse(txtMargin.Text, out boxMargin))
                boxMargin = 0.25;
            boxMargin = Math.Max(0, boxMargin);
            boxMargin = Math.Min(boxMargin, 0.49);

            DialogResult = true;
        }
    }
}
