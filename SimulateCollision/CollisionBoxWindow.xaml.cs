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
        private struct ParticleData
        {
            public float Update { get; set; }
            public float PosX { get; set; }
            public float PosY { get; set; }
            public float VecX { get; set; }
            public float VecY { get; set; }
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
        private ParticleData[] arrParticleData;

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

        void SaveParticles(List<Particle> particles)
        {
            var fi = new FileInfo("particles.data");
            if (fi.Exists) fi.Delete();
            using var fs = fi.Open(FileMode.CreateNew, FileAccess.Write);
            using var bw = new BinaryWriter(fs);
            bw.Write(ActualWidth);
            bw.Write(ActualHeight);
            bw.Write(minMass);
            bw.Write(maxMass);
            bw.Write(particles.Count);
            foreach (var p in particles)
            {
                bw.Write(p.PosX);
                bw.Write(p.PosY);
                bw.Write(p.VecX);
                bw.Write(p.VecY);
                bw.Write(p.Radius);
                Debug.Assert(p.Radius > 0);
                bw.Write(p.Mass);
                Debug.Assert(p.Mass > 0);
            }

            bw.Flush();
        }

        List<Particle> LoadParticles()
        {
            List<Particle> particles = new List<Particle>();
            var fi = new FileInfo("particles.data");
            if (!fi.Exists) return particles;

            using var fs = fi.Open(FileMode.Open, FileAccess.Read);
            using var br = new BinaryReader(fs);

            var width = br.ReadDouble();
            var height = br.ReadDouble();
            WindowState = WindowState.Normal;
            Width = width;
            Height = height;
            minMass = br.ReadDouble();
            maxMass = br.ReadDouble();
            int nParticles = br.ReadInt32();
            for (int i = 0; i < nParticles; i++)
            {
                var rx = br.ReadSingle();
                var ry = br.ReadSingle();
                var vx = br.ReadSingle();
                var vy = br.ReadSingle();
                var radius = br.ReadSingle();
                Debug.Assert(radius > 0);
                var mass = br.ReadSingle();
                Debug.Assert(mass > 0);

                particles.Add(new(rx, ry, vx, vy, radius, mass));
            }

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
                if (rndSize < 0.1) continue;

                var rad = rndSize * 5;
                var mass = rndSize * rndSize;

                var px = r.NextDouble() * max_px + leftMargin;
                var py = r.NextDouble() * max_py + topMargin;
                var vx = (r.NextDouble() - 0.5) * max_vx;
                var vy = (r.NextDouble() - 0.5) * max_vy;

                Particle newObj = new((float)px, (float)py, (float)vx, (float)vy, (float)rad, (float)mass);

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

            maxMass = (size + sizeDev * 2) * (size + sizeDev * 2);
            minMass = (size - sizeDev * 2) * (size - sizeDev * 2);
            minMass = Math.Max(minMass, 0);

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

        double maxMass = 0;
        double minMass = 0;
        byte[][] colors = new byte[][] {
            new byte[] { 127, 127, 127 },
            new byte[] { 163, 73, 164 },
            new byte[] { 63, 72, 204 },
            new byte[] { 0, 162, 232 },
            new byte[] { 34, 177, 76 },
            new byte[] { 255, 242, 0 },
            new byte[] { 255, 127, 39 },
            new byte[] { 237, 28, 36 },
            new byte[] { 136, 0, 21 },
            new byte[] { 0, 0, 0 }};

        private Color CreateColorByMass(double mass)
        {


            var maxLevel = colors.Length - 1;
            var interval = 1.0 / maxLevel;
            var value = (mass - minMass) / (maxMass - minMass);
            if (double.IsNaN(value))
                value = 1;
            value = Math.Min(value, 1);
            value = Math.Max(value, 0);
            var level = (int)(value / interval);

            if (value == 0 || value == 1)
                return Color.FromRgb(colors[level][0], colors[level][1], colors[level][2]);

            var colorStart = colors[level];
            var colorEnd = colors[level + 1];

            value = value - (interval * level);
            var factor = value / interval;

            var r = (byte)(colorStart[0] + (colorEnd[0] - colorStart[0]) * factor);
            var g = (byte)(colorStart[1] + (colorEnd[1] - colorStart[1]) * factor);
            var b = (byte)(colorStart[2] + (colorEnd[2] - colorStart[2]) * factor);
            return Color.FromRgb(r, g, b);
        }

        private List<UIElement> CreateEllipses(List<Particle> lstParticle)
        {
            List<UIElement> lstEllipse = new();

            foreach (var particle in lstParticle)
            {
                var ell = new Ellipse() { Width = particle.Radius * 2, Height = particle.Radius * 2, Fill = new SolidColorBrush(CreateColorByMass(particle.Mass)) };
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
            SaveParticles(lstParticle);
        }

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            ClearCalculateResult();
            SetUIItem(true);

            lstParticle = LoadParticles();
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

            CollisionCoreSystemIndex ccs = new(lstParticle.ToArray(), (float)panelWidth, (float)panelHeight);
            int n, max = 0, count = 0;
            Stopwatch sw = Stopwatch.StartNew();
            await Task.Run(() =>
            {
                n = ccs.QueueLength;
                double ccsTime = ccs.NextStep();

                while (ccsTime < simTime)
                {
                    count += 1;
                    if (count % 1000 == 0)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            Title = $"进度：{ccs.SystemTime,6:F4} / {simTime} | 队列：{n,7} / {max,7} | 事件：{(int)(count / sw.Elapsed.TotalSeconds),7} / {count,7}";
                        });
                    }

                    max = Math.Max(max, n);
                    n = ccs.QueueLength;
                    ccsTime = ccs.NextStep();
                }

                ccs.SnapshotAll();
            });

            this.snapshot = ccs.SystemSnapshot;

            Redraw();
            SetUIItem(true);

            Title = $"演算已完成，模拟 {lstParticle.Count} 个粒子 {simTime} 秒碰撞，平均每秒计算 {(int)(count / sw.Elapsed.TotalSeconds)} 次碰撞，总计发生 {count} 次。";
            MessageBox.Show(this, "演算结束", "完成", MessageBoxButton.OK);
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

            double intervalSec = 1.0 / 120;
            int delayMilliseconds = (int)Math.Max((500 * intervalSec), 1);
            int pos = 0;
            int maxPos = snapshot.SnapshotTime.Count;
            Stopwatch swPlay = new(); // 计时器

            InitializeParticle();
            isPlaying = true;
            miStop.IsEnabled = true;

            var lastTime = swPlay.Elapsed;
            swPlay.Start();
            while (isPlaying)
            {
                if (swPlay.Elapsed.Subtract(lastTime).TotalSeconds < intervalSec)
                {
                    await Task.Delay(delayMilliseconds); // 等待直到超过1帧时间
                    continue;
                }
                var durSec = swPlay.Elapsed.TotalSeconds; // 计时器更新后的秒数

                while (pos + 1 < maxPos && durSec > snapshot.SnapshotTime[pos + 1])
                {
                    pos += 1; // 更新快照位置

                    // 根据快照信息更新粒子的信息
                    for (int i = 0; i < snapshot.SnapshotData[pos].Length; i++)
                    {
                        var index = snapshot.SnapshotData[pos][i].Index;
                        arrParticleData[index].Update = snapshot.SnapshotTime[pos];
                        arrParticleData[index].PosX = snapshot.SnapshotData[pos][i].PosX;
                        arrParticleData[index].PosY = snapshot.SnapshotData[pos][i].PosY;
                        arrParticleData[index].VecX = snapshot.SnapshotData[pos][i].VecX;
                        arrParticleData[index].VecY = snapshot.SnapshotData[pos][i].VecY;
                    }
                }
                if (pos + 1 == maxPos) break;

                UpdateParticleAndRedrawAt(durSec); // 根据当前时间，更新粒子位置，并重绘UI
                lastTime = swPlay.Elapsed;
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
            this.arrParticleData = new ParticleData[snapshot.SnapshotData[0].Length];
            for (int i = 0; i < snapshot.SnapshotData[0].Length; i++)
            {
                this.arrParticleData[i]= new()
                {
                    Update = 0,
                    PosX = snapshot.SnapshotData[0][i].PosX,
                    PosY = snapshot.SnapshotData[0][i].PosY,
                    VecX = snapshot.SnapshotData[0][i].VecX,
                    VecY = snapshot.SnapshotData[0][i].VecY,
                };
            }

            UpdateParticleAndRedrawAt(0); // 重绘UI
        }

        private void UpdateParticleAndRedrawAt(double time)
        {
            for (int i = 0; i < arrParticleData.Length; i++)
            {
                var rad = lstParticle[i].Radius;
                var dt = (float)time - arrParticleData[i].Update;
                var newItem = arrParticleData[i];

                newItem.Update = (float)time;

                var x = arrParticleData[i].PosX + arrParticleData[i].VecX * dt;
                newItem.PosX = x;

                var y = arrParticleData[i].PosY + arrParticleData[i].VecY * dt;
                newItem.PosY = y;

                arrParticleData[i] = newItem;
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
            this.arrParticleData = null;
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
