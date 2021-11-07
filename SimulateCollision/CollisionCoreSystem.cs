using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimulateCollision
{
    public class CollisionCoreSystem
    {
        private readonly Particle[] particles;
        private double height, width;
        private PriorityQueue<Event> priorityQueue;
        private SystemSnapshot snapshot;
        private double simTime;

        /**
         * 模拟时钟
         */
        private double systemTime;

        public double SystemTime
        {
            get { return systemTime; }
            private set { systemTime = value; }
        }


        public CollisionCoreSystem(Particle[] particles, double width, double height, double simTime)
        {
            this.particles = particles;
            this.width = width;
            this.height = height;
            this.simTime = simTime;

            Initialize();
        }

        private void Initialize()
        {
            priorityQueue = new(new EventComparer());
            snapshot = new SystemSnapshot();
            systemTime = 0;

            for (int i = 0; i < particles.Length; i++)
            {
                PredictCollisions(particles[i]);
            }

            SnapshotAll();
        }

        public bool NextStep()
        {
            while (priorityQueue.Count != 0)
            {
                //处理一个事件
                Event e = priorityQueue.Dequeue();

                if (!e.IsValid()) continue;

                var dt = e.Time - systemTime;
                for (int i = 0; i < particles.Length; i++)
                    particles[i].Move(dt);

                systemTime = e.Time;
                Particle a = e.A, b = e.B;
                if (a != null && b != null)
                {
                    a.BounceOff(b);
                }
                else if (a != null && b == null)
                {
                    a.BounceOffVerticalWall();
                }
                else if (a == null && b != null)
                {
                    b.BounceOffHorizontalWall();
                }

                PredictCollisions(a);
                PredictCollisions(b);

                SnapshotParticle(a, b);
                break;
            }

            Console.WriteLine($"{systemTime}:{priorityQueue.Count }");

            return priorityQueue.Count != 0;
        }

        public SystemSnapshot SystemSnapshot
        {
            get
            {
                return this.snapshot;
            }
        }

        /**
         * 预测其他粒子的碰撞事件
         */
        private void PredictCollisions(Particle a)
        {
            if (a == null)
            {
                return;
            }

            for (int i = 0; i < particles.Length; i++)
            {
                double dt = a.TimeToHit(particles[i]);
                if (systemTime + dt <= simTime)
                    priorityQueue.Enqueue(new Event(systemTime + dt, a, particles[i]));
            }

            double dtX = a.TimeToHitVerticalWall(0, this.width);
            if (systemTime + dtX <= simTime)
                priorityQueue.Enqueue(new Event(systemTime + dtX, a, null));

            double dtY = a.TimeToHitHorizontalWall(0, this.height);
            if (systemTime + dtY <= simTime)
                priorityQueue.Enqueue(new Event(systemTime + dtY, null, a));
        }

        private void SnapshotAll()
        {
            snapshot.SnapshotTime.Add(systemTime);
            SnapshotData[] data = new SnapshotData[particles.Length];
            for (int i = 0; i < particles.Length; i++)
            {
                data[i] = new() { Index = i, PosX = particles[i].PosX, PosY = particles[i].PosY, VecX = particles[i].VecX, VecY = particles[i].VecY };
            }
            snapshot.SnapshotData.Add(data);
        }

        private void SnapshotParticle(Particle a, Particle b)
        {
            int count = 0;
            if (a != null) count += 1;
            if (b != null) count += 1;

            if (count == 0) return;

            snapshot.SnapshotTime.Add(systemTime);

            SnapshotData[] data = new SnapshotData[count];
            int i = 0;

            if (a != null)
            {
                data[i] = new() { Index = Array.IndexOf(particles, a), PosX = a.PosX, PosY = a.PosY, VecX = a.VecX, VecY = a.VecY };
                i += 1;
            }

            if (b != null)
            {
                data[i] = new() { Index = Array.IndexOf(particles, b), PosX = b.PosX, PosY = b.PosY, VecX = b.VecX, VecY = b.VecY };
            }

            snapshot.SnapshotData.Add(data);
        }
    }
}
