using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimulateCollision
{
    public class CollisionCoreSystemIndexUnlimit
    {
        private readonly Particle[] particles;
        private double height, width;
        private PriorityQueue<EventIndex> priorityQueue;
        private SystemSnapshot snapshot;

        /**
         * 模拟时钟
         */
        private double systemTime;

        public double SystemTime
        {
            get { return systemTime; }
        }

        public int QueueLength
        {
            get
            {
                return priorityQueue.Count;
            }
        }


        public CollisionCoreSystemIndexUnlimit(Particle[] particles, double width, double height)
        {
            this.particles = particles;
            this.width = width;
            this.height = height;

            Initialize();
        }

        private void Initialize()
        {
            priorityQueue = new(new EventIndexComparer());
            snapshot = new SystemSnapshot();
            systemTime = 0;

            for (int i = 0; i < particles.Length; i++)
            {
                PredictCollisions(particles[i], i);
            }

            SnapshotAll();
        }

        public double NextStep()
        {
            while (priorityQueue.Count != 0)
            {
                EventIndex e = priorityQueue.Dequeue();
                if (!e.IsValid(particles)) continue;

                //* 处理事件 *//
                var dt = e.Time - systemTime;
                for (int i = 0; i < particles.Length; i++)
                    particles[i].Move(dt);

                systemTime = e.Time;

                Particle a = null, b = null;

                if (e.IndexA != -1)
                {
                    a = particles[e.IndexA];
                }

                if (e.IndexB != -1)
                {
                    b = particles[e.IndexB];
                }


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

                PredictCollisions(a, e.IndexA);
                PredictCollisions(b, e.IndexB);

                SnapshotParticle(a, e.IndexA, b, e.IndexB);
                break;
            }

            return systemTime;
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
        private void PredictCollisions(Particle a, int indexA)
        {
            if (a == null)
            {
                return;
            }

            for (int i = 0; i < particles.Length; i++)
            {
                double dt = a.TimeToHit(particles[i]);
                if (dt != Particle.INFINITY)
                    priorityQueue.Enqueue(new EventIndex(systemTime + dt, a, indexA, particles[i], i));
            }

            {
                double dtX = a.TimeToHitVerticalWall(0, this.width);
                if (dtX != Particle.INFINITY)
                    priorityQueue.Enqueue(new EventIndex(systemTime + dtX, a, indexA, null, -1));
            }

            {
                double dtY = a.TimeToHitHorizontalWall(0, this.height);
                if (dtY != Particle.INFINITY)
                    priorityQueue.Enqueue(new EventIndex(systemTime + dtY, null, -1, a, indexA));
            }
        }

        public void SnapshotAll()
        {
            snapshot.SnapshotTime.Add(systemTime);
            SnapshotData[] data = new SnapshotData[particles.Length];
            for (int i = 0; i < particles.Length; i++)
            {
                data[i] = new() { Index = i, PosX = particles[i].PosX, PosY = particles[i].PosY, VecX = particles[i].VecX, VecY = particles[i].VecY };
            }
            snapshot.SnapshotData.Add(data);
        }

        private void SnapshotParticle(Particle a, int indexA, Particle b, int indexB)
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
                data[i] = new() { Index = indexA, PosX = a.PosX, PosY = a.PosY, VecX = a.VecX, VecY = a.VecY };
                i += 1;
            }

            if (b != null)
            {
                data[i] = new() { Index = indexB, PosX = b.PosX, PosY = b.PosY, VecX = b.VecX, VecY = b.VecY };
            }

            snapshot.SnapshotData.Add(data);
        }
    }
}
