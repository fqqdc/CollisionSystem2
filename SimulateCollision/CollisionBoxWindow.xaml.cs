﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SimulateCollision
{
    public partial class CollisionBoxWindow : Window
    {
        /// <summary>
        /// 粒子对象
        /// </summary>
        private List<Particle> lstParticle = new();
        /// <summary>
        /// 快照对象
        /// </summary>
        private SystemSnapshot snapshot;

        private ParticleUI particleUI;
        private ParticleAnimation animation;



        private void SaveParticles(List<Particle> particles)
        {
            var fi = new FileInfo("particles.data");
            if (fi.Exists) fi.Delete();
            using var fs = fi.Open(FileMode.CreateNew, FileAccess.Write);
            using var bw = new BinaryWriter(fs);
            bw.Write(ActualWidth);
            bw.Write(ActualHeight);
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

        private List<Particle> LoadParticles()
        {
            List<Particle> particles = new List<Particle>();
            var fi = new FileInfo("particles.data");
            if (fi.Exists)
            {
                using var fs = fi.Open(FileMode.Open, FileAccess.Read);
                using var br = new BinaryReader(fs);

                var width = br.ReadDouble();
                var height = br.ReadDouble();
                WindowState = WindowState.Normal;
                Width = width;
                Height = height;
                var minMass = br.ReadDouble();
                var maxMass = br.ReadDouble();
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
            }
            return particles;
        }

        public CollisionBoxWindow()
        {
            InitializeComponent();

            particleUI = ParticleUI.Create(mainPanel, null);
            snapshot = new();
            animation = new(snapshot);
        }

        private void btnGenerate_Click(object sender, RoutedEventArgs e)
        {
            GenerateWindow generateWin = new();
            var result = generateWin.ShowDialog();
            if (result != true)
                return;

            ClearCalculateResult();
            SetUIItem(false);

            ParticlesBuilder builder = new();

            builder.Size = generateWin.Size;
            builder.SizeDev = generateWin.SizeDev;
            builder.PanelWidth = mainPanel.ActualWidth;
            builder.PanelHeight = mainPanel.ActualHeight;
            builder.Velocity = generateWin.Velocity;
            builder.MaxNumber = generateWin.Number;

            var margin = generateWin.BoxMargin;
            builder.LeftMargin = builder.PanelWidth * margin;
            builder.TopMargin = builder.PanelHeight * margin;
            builder.RightMargin = builder.PanelWidth - builder.LeftMargin;
            builder.BottomMargin = builder.PanelHeight - builder.TopMargin;            

            lstParticle = builder.Build();
            particleUI = ParticleUI.Create(mainPanel, lstParticle);

            if (lstParticle.Count > 0)
            {
                Title = $"{lstParticle.Count} 个粒子已生成";
            }
            else
            {
                lstParticle = new();
                Title = $"未能生成粒子";
                MessageBox.Show(this, "未能生成粒子", "失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            SetUIItem(true);
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
            particleUI = ParticleUI.Create(mainPanel, lstParticle);
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
            //miStop.IsEnabled = isEnabled;
        }

        bool isCalcing = false;
        private async void btnCalculate_Click(object sender, RoutedEventArgs e)
        {
            if (isCalcing) return;
            isCalcing = true;

            try
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

                this.snapshot = await Task<SystemSnapshot>.Run(() => Calculate(winCalculate.SimTime));

                particleUI.Redraw(lstParticle);
                SetUIItem(true);

                MessageBox.Show(this, "演算结束", "完成", MessageBoxButton.OK);
            }
            finally
            {
                isCalcing = false;
            }
        }

        private SystemSnapshot Calculate(double simTime)
        {
            double panelWidth = mainPanel.ActualWidth;
            double panelHeight = mainPanel.ActualHeight;

            CollisionCoreSystemIndex coreSystem = new(lstParticle.ToArray(), (float)panelWidth, (float)panelHeight);

            int n, max = 0, count = 0;
            Stopwatch sw = Stopwatch.StartNew();

            n = coreSystem.QueueLength;
            double ccsTime = coreSystem.NextStep();

            while (ccsTime < simTime)
            {
                count += 1;
                if (count % 1000 == 0)
                {
                    Dispatcher.Invoke(() =>
                    {
                        Title = $"进度：{coreSystem.SystemTime,6:F4} / {simTime} | 队列：{n,7} / {max,7} | 事件：{(int)(count / sw.Elapsed.TotalSeconds),7} / {count,7}";
                    });
                }

                max = Math.Max(max, n);
                n = coreSystem.QueueLength;
                ccsTime = coreSystem.NextStep();
            }

            coreSystem.SnapshotAll();

            Dispatcher.Invoke(() =>
            {
                Title = $"演算已完成，模拟 {lstParticle.Count} 个粒子 {simTime} 秒碰撞，平均每秒计算 {(int)(count / sw.Elapsed.TotalSeconds)} 次碰撞，总计发生 {count} 次。";
            });

            return coreSystem.SystemSnapshot;
        }

        private bool isPlaying = false;
        private CancellationTokenSource csPlay = new();

        private async void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            if (isPlaying) return;
            isPlaying = true;

            SetUIItem(false);
            miStop.IsEnabled = true;

            try
            {
                if (snapshot.IsEmpty)
                {
                    MessageBox.Show(this, "还未进行演算", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                isPlaying = true;
                miStop.IsEnabled = true;

                csPlay = new();
                animation = new(snapshot);
                await animation.PlayAnimationAsync(csPlay.Token, particleUI.ParticleEllipses);
            }
            finally
            {
                miStop.IsEnabled = false;
                SetUIItem(true);
                isPlaying = false;
            }
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            isPlaying = false;
            csPlay.Cancel();
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            if (snapshot == null)
            {
                MessageBox.Show(this, "还未进行演算", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            InitializeAnimation();

            miSave.IsEnabled = false;
        }

        private void InitializeAnimation()
        {
            animation = new(snapshot); // 根据快照初始化动画信息
            animation.InitializeAnimation(particleUI.ParticleEllipses);
        }

        private void ClearCalculateResult()
        {
            snapshot = new();
            animation = new(snapshot);
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ClearCalculateResult();

            lstParticle = new();
            particleUI = ParticleUI.Create(mainPanel, null);
            mainPanel.Children.Clear();
        }


    }

    class ParticleAnimation
    {
        public struct ParticleInfo
        {
            public float Update { get; set; }
            public float PosX { get; set; }
            public float PosY { get; set; }
            public float VecX { get; set; }
            public float VecY { get; set; }
        }

        private ParticleInfo[] arrInfos = new ParticleInfo[0];

        SystemSnapshot snapshot;

        public ParticleAnimation(SystemSnapshot snapshot)
        {
            this.snapshot = snapshot;
            InitializeAnimation();
        }

        /// <summary>
        /// 根据快照信息更新粒子的信息
        /// </summary>
        /// <param name="snapshotTime">快照时间</param>
        /// <param name="snapshotData">相应时间的粒子信息</param>
        public void UpdateBy(float snapshotTime, SnapshotData[] snapshotData)
        {
            for (int i = 0; i < snapshotData.Length; i++)
            {
                var index = snapshotData[i].Index;
                arrInfos[index].Update = snapshotTime;
                arrInfos[index].PosX = snapshotData[i].PosX;
                arrInfos[index].PosY = snapshotData[i].PosY;
                arrInfos[index].VecX = snapshotData[i].VecX;
                arrInfos[index].VecY = snapshotData[i].VecY;
            }
        }

        /// <summary>
        /// 根据粒子当前信息与当前时间的差值，更新粒子位置
        /// </summary>
        /// <param name="time">当前时间</param>
        public void UpdateBy(double time, int index)
        {
            var dt = (float)time - arrInfos[index].Update;
            if (dt == 0) return;

            arrInfos[index].Update = (float)time;

            var x = arrInfos[index].PosX + arrInfos[index].VecX * dt;
            arrInfos[index].PosX = x;

            var y = arrInfos[index].PosY + arrInfos[index].VecY * dt;
            arrInfos[index].PosY = y;
        }

        /// <summary>
        /// 根据粒子当前信息与当前时间的差值，更新粒子UI的位置
        /// </summary>
        /// <param name="time">当前时间</param>
        private void UpdateAnimationAndRedrawAt(double time, IList<Ellipse> elements)
        {
            for (int i = 0; i < elements.Count; i++)
            {
                UpdateBy(time, i);

                Canvas.SetLeft(elements[i], arrInfos[i].PosX - elements[i].Width * 0.5);
                Canvas.SetTop(elements[i], arrInfos[i].PosY - elements[i].Height * 0.5);
            }
        }

        private void InitializeAnimation()
        {
            // 根据快照初始化动画信息
            if (snapshot.SnapshotData.Count > 0)
            {
                arrInfos = new ParticleInfo[snapshot.SnapshotData[0].Length];
                UpdateBy(snapshot.SnapshotTime[0], snapshot.SnapshotData[0]);
            }
        }

        public void InitializeAnimation(IList<Ellipse> elements)
        {
            InitializeAnimation();
            UpdateAnimationAndRedrawAt(0, elements); // 重绘UI
        }

        public async Task PlayAnimationAsync(CancellationToken ct, IList<Ellipse> elements)
        {
            double intervalSec = 1.0 / 120;
            int delayMilliseconds = (int)Math.Max((500 * intervalSec), 1);
            int pos = 0;
            int maxPos = snapshot.SnapshotTime.Count;
            Stopwatch swPlay = new(); // 计时器

            InitializeAnimation();
            UpdateAnimationAndRedrawAt(0, elements); // 重绘UI

            var lastTime = swPlay.Elapsed;
            swPlay.Start();
            while (!ct.IsCancellationRequested)
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
                    this.UpdateBy(snapshot.SnapshotTime[pos], snapshot.SnapshotData[pos]); // 根据快照信息更新动画信息
                }
                if (pos + 1 == maxPos) break;


                UpdateAnimationAndRedrawAt(durSec, elements); // 根据当前时间，更新粒子位置，并重绘UI
                lastTime = swPlay.Elapsed;
            }
        }
    }
    class ParticleUI
    {
        private List<Ellipse> particleEllipses;
        private Canvas mainCanvas;
        public ReadOnlyCollection<Ellipse> ParticleEllipses { get => new(particleEllipses); }

        /// <summary>
        /// 根据粒子的初始信息，更新粒子UI的位置
        /// </summary>
        public void Redraw(IList<Particle> particles)
        {
            if (particleEllipses.Count != particles.Count)
                throw new NotSupportedException("粒子数不一致");

            for (int i = 0; i < particles.Count; i++)
            {
                var particle = particles[i];
                var ell = particleEllipses[i];

                Canvas.SetLeft(ell, particle.PosX - particle.Radius);
                Canvas.SetTop(ell, particle.PosY - particle.Radius);
            }
        }

        private ParticleUI(Canvas mainCanvas, IEnumerable<Particle> particles)
        {
            this.mainCanvas = mainCanvas;
            particleEllipses = new();

            var minMass = particles.Min(p => p.Mass);
            var maxMass = particles.Max(p => p.Mass);

            foreach (var particle in particles)
            {
                var ell = new Ellipse()
                {
                    Width = particle.Radius * 2,
                    Height = particle.Radius * 2,
                    Fill = new SolidColorBrush(CreateColorByMass(particle.Mass, minMass, maxMass))
                };

                Canvas.SetLeft(ell, particle.PosX - particle.Radius);
                Canvas.SetTop(ell, particle.PosY - particle.Radius);

                particleEllipses.Add(ell);
            }

            mainCanvas.Children.Clear();
            foreach (var element in particleEllipses)
            {
                mainCanvas.Children.Add(element);
            }
        }

        private ParticleUI(Canvas mainCanvas)
        {
            particleEllipses = new();
            this.mainCanvas = mainCanvas;
            mainCanvas.Children.Clear();
        }

        private static readonly byte[][] ColorTable = new byte[][] {
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

        private static Color CreateColorByMass(double mass, double minMass, double maxMass)
        {
            var maxLevel = ColorTable.Length - 1;
            var interval = 1.0 / maxLevel;
            var value = (mass - minMass) / (maxMass - minMass);
            if (double.IsNaN(value))
                value = 1;
            value = Math.Min(value, 1);
            value = Math.Max(value, 0);
            var level = (int)(value / interval);

            if (value == 0 || value == 1)
                return Color.FromRgb(ColorTable[level][0], ColorTable[level][1], ColorTable[level][2]);

            var colorStart = ColorTable[level];
            var colorEnd = ColorTable[level + 1];

            value = value - (interval * level);
            var factor = value / interval;

            var r = (byte)(colorStart[0] + (colorEnd[0] - colorStart[0]) * factor);
            var g = (byte)(colorStart[1] + (colorEnd[1] - colorStart[1]) * factor);
            var b = (byte)(colorStart[2] + (colorEnd[2] - colorStart[2]) * factor);
            return Color.FromRgb(r, g, b);
        }

        public static ParticleUI Create(Canvas mainCanvas, IEnumerable<Particle>? particles)
        {
            if (particles == null || !particles.Any())
            {
                return new(mainCanvas);
            }
            else
            {
                return new(mainCanvas, particles);
            }
        }
    }

    class ParticlesBuilder
    {
        public double Size { get; set; }
        public double SizeDev { get; set; }
        public double PanelWidth { get; set; }
        public double PanelHeight { get; set; }
        public double LeftMargin { get; set; }
        public double TopMargin { get; set; }
        public double RightMargin { get; set; }
        public double BottomMargin { get; set; }
        public double Velocity { get; set; }
        public int MaxNumber { get; set; }

        public List<Particle> Build()
        {
            List<Particle> lstParticle = new();
            Random r = new();
            var dtStart = DateTime.Now;
            var max_px = RightMargin; Debug.Assert(max_px > 0);
            var max_py = BottomMargin; Debug.Assert(max_py > 0);
            var max_vx = PanelWidth * Velocity; Debug.Assert(max_vx > 0);
            var max_vy = PanelHeight * Velocity; Debug.Assert(max_vy > 0);

            //尝试生成粒子的次数
            var countTry = 0;
            while (countTry < MaxNumber * 10 && lstParticle.Count < MaxNumber)
            {
                countTry += 1;

                var (rndSize, _) = GaussianRandom68(Size - SizeDev, Size + SizeDev);
                if (rndSize < 0.1) continue; // 直径小于一个像素

                var rad = rndSize * 5;
                var mass = rndSize * rndSize;

                var px = r.NextDouble() * max_px + LeftMargin;
                var py = r.NextDouble() * max_py + TopMargin;
                var vx = (r.NextDouble() - 0.5) * max_vx * 2;
                var vy = (r.NextDouble() - 0.5) * max_vy * 2;

                Particle newObj = new((float)px, (float)py, (float)vx, (float)vy, (float)rad, (float)mass);

                if (lstParticle.All(p => newObj.Intersect(p) == false)
                    && newObj.PosX - newObj.Radius > LeftMargin && newObj.PosX + newObj.Radius < RightMargin
                    && newObj.PosY - newObj.Radius > TopMargin && newObj.PosY + newObj.Radius < BottomMargin)
                {
                    lstParticle.Add(newObj);
                }
            }

            return lstParticle;
        }

        private static (double, double) GaussianRandom68(double min, double max)
        {
            double mu = (min + max) / 2;
            double sigma = (max - mu);

            return GenerateGaussianNoise(mu, sigma);
        }

        private static (double, double) GenerateGaussianNoise(double mean, double stdDev)
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
    }
}
