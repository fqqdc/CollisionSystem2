using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
    public partial class CollisionBoxWindow : Window
    {
        private class ParticleData
        {
            public double Update { get; set; }
            public double PosX { get; set; }
            public double PosY { get; set; }
            public double VecX { get; set; }
            public double VecY { get; set; }
        }

        /// <summary>
        /// 粒子对象
        /// </summary>
        private List<Particle> lstParticle;
        /// <summary>
        /// 粒子对应的UI对象
        /// </summary>
        private List<UIElement> lstParticleUIElement;
        /// <summary>
        /// 快照对象
        /// </summary>
        private SystemSnapshot snapshot;
        /// <summary>
        /// 粒子位置、位移信息
        /// </summary>
        private List<ParticleData> lstParticleData;

        static (double, double) GaussianRandom68(double min, double max)
        {
            double mu = (min + max) / 2;
            double sigma = (max - mu);

            return GenerateGaussianNoise(mu, sigma);
        }

        static (double, double) GenerateGaussianNoise(double mean, double stdDev)
        {
            double u, v, s;
            Random r = new Random();
            do
            {
                u = r.NextDouble() * 2.0 - 1.0;
                v = r.NextDouble() * 2.0 - 1.0;
                s = u * u + v * v;
            } while (s >= 1.0 || s == 0.0);

            s = Math.Sqrt(-2.0 * Math.Log(s) / s);

            var z0 = mean + stdDev * u * s;
            var z1 = mean + stdDev * v * s;
            return (z0, z1);
        }

        static void SaveParticles(Window win, List<Particle> particles)
        {
            var fi = new FileInfo("particles.data");
            if (fi.Exists) fi.Delete();
            using var fs = fi.Open(FileMode.CreateNew, FileAccess.Write);
            using var bs = new BinaryWriter(fs);
            bs.Write(win.ActualWidth);
            bs.Write(win.ActualHeight);
            foreach (var p in particles)
            {
                bs.Write(p.PosX);
                bs.Write(p.PosY);
                bs.Write(p.VecX);
                bs.Write(p.VecY);
                bs.Write(p.Radius);
                bs.Write(p.Mass);
            }

            bs.Flush();
        }

        static List<Particle> LoadParticles(Window win)
        {
            List<Particle> particles = new List<Particle>();
            var fi = new FileInfo("particles.data");
            if (!fi.Exists) return particles;

            using var fs = fi.Open(FileMode.Open, FileAccess.Read);
            using var bs = new BinaryReader(fs);

            try
            {
                if (bs.BaseStream.Position < bs.BaseStream.Length)
                {
                    var width = bs.ReadDouble();
                    var height = bs.ReadDouble();
                    win.WindowState = WindowState.Normal;
                    win.Width = width;
                    win.Height = height;
                }

                while (bs.BaseStream.Position < bs.BaseStream.Length)
                {
                    var rx = bs.ReadDouble();
                    var ry = bs.ReadDouble();
                    var vx = bs.ReadDouble();
                    var vy = bs.ReadDouble();
                    var radius = bs.ReadDouble();
                    var mass = bs.ReadDouble();

                    particles.Add(new(rx, ry, vx, vy, radius, mass));
                }
            }
            catch (EndOfStreamException) { }

            return particles;
        }

        static List<Particle> CreateParticles(double size, double sizeDev,
            double panelWidth, double panelHeight,
            double leftMargin, double rightMargin, double topMargin, double bottomMargin,
            double velocity, int particlesNumber)
        {
            List<Particle> lstParticle = new();
            Random r = new();
            var dtStart = DateTime.Now;
            var max_px = rightMargin - leftMargin; Debug.Assert(max_px > 0);
            var max_py = bottomMargin - topMargin; Debug.Assert(max_py > 0);
            var max_vx = panelWidth * velocity; Debug.Assert(max_vx > 0);
            var max_vy = panelHeight * velocity; Debug.Assert(max_vy > 0);

            //尝试生成粒子的次数
            var countTry = 0;
            while (countTry < particlesNumber * 10 && lstParticle.Count < particlesNumber)
            {
                countTry += 1;

                var (rndSize, _) = GaussianRandom68(size - sizeDev, size + sizeDev);
                if (rndSize < 0.5) continue;

                var rad = rndSize * 5;
                var mass = rndSize * rndSize;

                var px = r.NextDouble() * max_px + leftMargin;
                var py = r.NextDouble() * max_py + topMargin;
                var vx = (r.NextDouble() - 0.5) * max_vx;
                var vy = (r.NextDouble() - 0.5) * max_vy;

                Particle newObj = new(px, py, vx, vy, rad, mass);

                if (lstParticle.All(p => newObj.Intersect(p) == false)
                    && newObj.PosX - newObj.Radius > leftMargin && newObj.PosX + newObj.Radius < rightMargin
                    && newObj.PosY - newObj.Radius > topMargin && newObj.PosY + newObj.Radius < bottomMargin)
                {
                    lstParticle.Add(newObj);
                }
            }

            return lstParticle;
        }

        public CollisionBoxWindow()
        {
            InitializeComponent();
        }

        private void btnGenerate_Click(object sender, RoutedEventArgs e)
        {
            ClearCalculateResult();
            SetUIItem(true);

            GenerateWindow generateWin = new();
            var result = generateWin.ShowDialog();
            if (result != true) return;

            double size = generateWin.Size;
            double sizeDev = generateWin.SizeDev;
            double margin = generateWin.BoxMargin;

            double panelWidth = mainPanel.ActualWidth;
            double panelHeight = mainPanel.ActualHeight;
            double leftMargin = panelWidth * margin;
            double rightMargin = panelWidth - leftMargin;
            double topMargin = panelHeight * margin;
            double bottomMargin = panelHeight - topMargin;
            int n = generateWin.Number;
            double velocity = generateWin.Velocity;

            lstParticle = CreateParticles(size, sizeDev, panelWidth, panelHeight, leftMargin, rightMargin, topMargin, bottomMargin, velocity, n);
            lstParticleUIElement = CreateEllipses(lstParticle);
            mainPanel.Children.Clear();
            lstParticleUIElement.ForEach(e => mainPanel.Children.Add(e));

            Redraw();
            if (lstParticle.Count > 0)
                Title = $"{lstParticle.Count} 个粒子已生成";
            else
            {
                lstParticle = null;
                Title = $"未能生成粒子";
                MessageBox.Show(this, "未能生成粒子", "失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private List<UIElement> CreateEllipses(List<Particle> lstParticle)
        {
            List<UIElement> lstEllipse = new();

            foreach (var particle in lstParticle)
            {
                var ell = new Ellipse() { Width = particle.Radius * 2, Height = particle.Radius * 2, Fill = Brushes.Black };
                lstEllipse.Add(ell);
            }

            return lstEllipse;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (lstParticle == null)
            {
                MessageBox.Show(this, "未存在可保存的粒子", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            SaveParticles(this, lstParticle);
        }

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            ClearCalculateResult();
            SetUIItem(true);

            lstParticle = LoadParticles(this);
            lstParticleUIElement = CreateEllipses(lstParticle);
            mainPanel.Children.Clear();
            lstParticleUIElement.ForEach(e => mainPanel.Children.Add(e));

            Redraw();
            Title = $"{lstParticle.Count} 个粒子已生成";
        }

        private void SetUIItem(bool isEnabled)
        {
            ResizeMode = isEnabled ? ResizeMode.CanResize : ResizeMode.NoResize;
            miGenerate.IsEnabled = isEnabled;
            miSave.IsEnabled = isEnabled;
            miLoad.IsEnabled = isEnabled;
            miCalculate.IsEnabled = isEnabled;
            miPlay.IsEnabled = isEnabled;
            miReset.IsEnabled = isEnabled;
            miResetEnd.IsEnabled = isEnabled;
            miStop.IsEnabled = isEnabled;
        }

        private async void btnCalculate_Click(object sender, RoutedEventArgs e)
        {
            SetUIItem(false);
            if (lstParticle == null)
            {
                MessageBox.Show(this, "还未生成粒子", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                SetUIItem(true);
                return;
            }

            var winCalculate = new CalculateWindow();
            winCalculate.ShowDialog();
            if (winCalculate.DialogResult != true)
            {
                SetUIItem(true);
                return;
            }

            double simTime = winCalculate.SimTime;
            double panelWidth = mainPanel.ActualWidth;
            double panelHeight = mainPanel.ActualHeight;

            CollisionCoreSystemIndexUnlimit ccs = new(lstParticle.ToArray(), panelWidth, panelHeight);
            int n,max = 0;
            await Task.Run(() =>
            {
                int count = 0;
                n = ccs.QueueLength;
                double ccsTime = ccs.NextStep();
                
                while (ccsTime < simTime)
                {
                    count += 1;
                    if (count % 100 == 0)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            Title = $"演算进度：{ccs.SystemTime,6:F4} / {simTime} | 队列长度：{n,7} / {max,7}";
                        });
                    }

                    max = Math.Max(max, n);
                    n = ccs.QueueLength;
                    ccsTime = ccs.NextStep();
                }

                ccs.SnapshotAll();
                Dispatcher.Invoke(() =>
                {
                    Title = $"演算进度：{ccs.SystemTime,6:F4} / {simTime} | 队列长度：{n,7} / {max,7}";
                });
            });

            this.snapshot = ccs.SystemSnapshot;

            Redraw();
            SetUIItem(true);

            Debug.WriteLine($"max:{max}");
            MessageBox.Show(this, "演算结束", "完成", MessageBoxButton.OK);
            Title = $"模拟 {lstParticle.Count} 个粒子的碰撞演算已完成";
        }

        private bool isPlaying = false;

        private async void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            SetUIItem(false);
            if (snapshot == null)
            {
                MessageBox.Show(this, "还未进行演算", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                SetUIItem(true);
                return;
            }

            const double intervalSec = 1.0 / 120;
            int pos = 0;
            int maxPos = snapshot.SnapshotTime.Count;
            TimeSpan dur = TimeSpan.Zero; // 计时器

            InitializeParticle();
            isPlaying = true;
            miStop.IsEnabled = true;

            while (isPlaying)
            {
                await Task.Delay((int)(intervalSec * 1000)); // 等待1帧间隔

                dur = dur.Add(TimeSpan.FromSeconds(intervalSec)); // 更新计时器
                var durSec = dur.TotalSeconds; // 计时器更新后的秒数

                while (pos + 1 < maxPos && durSec > snapshot.SnapshotTime[pos + 1])
                {
                    pos += 1; // 更新快照位置

                    // 根据快照信息更新粒子的信息
                    for (int i = 0; i < snapshot.SnapshotData[pos].Length; i++)
                    {
                        var index = snapshot.SnapshotData[pos][i].Index;
                        lstParticleData[index].Update = snapshot.SnapshotTime[pos];
                        lstParticleData[index].PosX = snapshot.SnapshotData[pos][i].PosX;
                        lstParticleData[index].PosY = snapshot.SnapshotData[pos][i].PosY;
                        lstParticleData[index].VecX = snapshot.SnapshotData[pos][i].VecX;
                        lstParticleData[index].VecY = snapshot.SnapshotData[pos][i].VecY;
                    }
                }
                if (pos + 1 == maxPos) break;

                UpdateParticleAndRedrawAt(durSec); // 根据当前时间，更新粒子位置，并重绘UI
            }

            SetUIItem(true);
            if (!isPlaying)
                miSave.IsEnabled = false;
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            isPlaying = false;
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            if (snapshot == null)
            {
                MessageBox.Show(this, "还未进行演算", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            InitializeParticle();

            miSave.IsEnabled = false;
        }

        private void btnResetEnd_Click(object sender, RoutedEventArgs e)
        {
            if (lstParticle == null)
            {
                MessageBox.Show(this, "还未生成粒子", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Redraw();
            SetUIItem(true);
        }

        private void InitializeParticle()
        {
            // 根据快照初始化粒子信息
            this.lstParticleData = new();
            for (int i = 0; i < snapshot.SnapshotData[0].Length; i++)
            {
                this.lstParticleData.Add(new()
                {
                    Update = 0,
                    PosX = snapshot.SnapshotData[0][i].PosX,
                    PosY = snapshot.SnapshotData[0][i].PosY,
                    VecX = snapshot.SnapshotData[0][i].VecX,
                    VecY = snapshot.SnapshotData[0][i].VecY,
                });
            }

            UpdateParticleAndRedrawAt(0); // 重绘UI
        }

        private void UpdateParticleAndRedrawAt(double time)
        {
            for (int i = 0; i < lstParticleData.Count; i++)
            {
                var rad = lstParticle[i].Radius;
                var dt = time - lstParticleData[i].Update;

                lstParticleData[i].Update = time;

                var x = lstParticleData[i].PosX + lstParticleData[i].VecX * dt;
                lstParticleData[i].PosX = x;

                var y = lstParticleData[i].PosY + lstParticleData[i].VecY * dt;
                lstParticleData[i].PosY = y;

                Canvas.SetLeft(lstParticleUIElement[i], x - rad);
                Canvas.SetTop(lstParticleUIElement[i], y - rad);
            }
        }

        private void Redraw()
        {
            for (int i = 0; i < lstParticle.Count; i++)
            {
                var particle = lstParticle[i];
                var ell = lstParticleUIElement[i];

                Canvas.SetLeft(ell, particle.PosX - particle.Radius);
                Canvas.SetTop(ell, particle.PosY - particle.Radius);
            }
        }

        private void ClearCalculateResult()
        {
            this.snapshot = null;
            this.lstParticleData = null;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ClearCalculateResult();

            lstParticle = null;
            lstParticleUIElement = null;
            mainPanel.Children.Clear();
        }


    }
}
