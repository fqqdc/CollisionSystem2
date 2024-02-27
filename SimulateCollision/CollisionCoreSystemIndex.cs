using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using System.Windows.Controls;

namespace SimulateCollision
{
    public class CollisionCoreSystemIndex : ICollisionCoreSystem
    {
        private readonly Particle[] particles;
        private readonly float height, width;

        private PriorityQueue<EventIndex, float> priorityQueue;

        private SystemSnapshot snapshot;

        /**
         * 模拟时钟
         */
        private float systemTime;

        public float SystemTime
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

        public CollisionCoreSystemIndex(Particle[] particles, float width, float height)
        {

            this.particles = particles;
            this.width = width;
            this.height = height;

            priorityQueue = new();
            snapshot = new();

            Initialize();
        }

        private void Initialize()
        {
            priorityQueue = new();
            snapshot.Reset();
            systemTime = 0;

            for (int i = 0; i < particles.Length; i++)
            {
                PredictCollisions(i);
            }

            SnapshotAll();
        }

        public float NextStep()
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

                if (e.IndexA != -1 && e.IndexB != -1)
                {
                    particles[e.IndexA].BounceOff(ref particles[e.IndexB]);
                }
                else if (e.IndexA != -1 && e.IndexB == -1)
                {
                    particles[e.IndexA].BounceOffVerticalWall();
                }
                else if (e.IndexA == -1 && e.IndexB != -1)
                {
                    particles[e.IndexB].BounceOffHorizontalWall();
                }

                if (e.IndexA != -1)
                    PredictCollisions(e.IndexA);
                if (e.IndexB != -1)
                    PredictCollisions(e.IndexB);

                SnapshotParticle(e.IndexA, e.IndexB);
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
        private void PredictCollisions(int indexA)
        {
            if (indexA == -1)
            {
                return;
            }

            ref Particle a = ref particles[indexA];

            for (int i = 0; i < particles.Length; i++)
            {
                var dt = a.TimeToHit(ref particles[i]);
                if (dt != Particle.INFINITY)
                    priorityQueue.Enqueue(EventIndex.CreateEvent(systemTime + dt, a.Count, indexA, particles[i].Count, i), systemTime + dt);
            }

            //SpinLock spinLock = new();
            //Parallel.For(0, particles.Length,
            //    new() { MaxDegreeOfParallelism = Environment.ProcessorCount },
            //    i =>
            //    {
            //        ref Particle a = ref particles[indexA];
            //        var dt = a.TimeToHit(ref particles[i]);
            //        if (dt != Particle.INFINITY)
            //        {
            //            var e = EventIndex.CreateEvent(systemTime + dt, a.Count, indexA, particles[i].Count, i);

            //            bool lockTaken = false;
            //            spinLock.Enter(ref lockTaken);
            //            try
            //            {
            //                priorityQueue.Enqueue(e, systemTime + dt);
            //            }
            //            finally
            //            {
            //                if (lockTaken)
            //                    spinLock.Exit();
            //            }
            //        }
            //    });

            {
                var dtX = a.TimeToHitVerticalWall(0, this.width);
                if (dtX != Particle.INFINITY)
                    priorityQueue.Enqueue(EventIndex.CreateEventHitVertical(systemTime + dtX, a.Count, indexA), systemTime + dtX);
            }

            {
                var dtY = a.TimeToHitHorizontalWall(0, this.height);
                if (dtY != Particle.INFINITY)
                    priorityQueue.Enqueue(EventIndex.CreateEventHitHorizontal(systemTime + dtY, a.Count, indexA), systemTime + dtY);
            }
        }

        public void SnapshotAll()
        {
            SnapshotData[] data = new SnapshotData[particles.Length];
            for (int i = 0; i < particles.Length; i++)
            {
                //data[i] = new() { Index = i, PosX = particles[i].PosX, PosY = particles[i].PosY, VecX = particles[i].VecX, VecY = particles[i].VecY };
                Pos2Rec(i, out data[i], in particles[i]);
            }
            snapshot.Add(systemTime, data);
        }

        private void Pos2Rec(int i, out SnapshotData rec, in Particle particle)
        {
            rec = new(i, particle.PosX, particle.PosY, particle.VecX, particle.VecY);
        }

        private void SnapshotParticle(int indexA, int indexB)
        {
            int count = 0;
            if (indexA != -1) count += 1;
            if (indexB != -1) count += 1;

            if (count == 0) return;

            SnapshotData[] data = new SnapshotData[count];
            int i = 0;

            if (indexA != -1)
            {
                //data[i] = new() { Index = indexA, PosX = a.PosX, PosY = a.PosY, VecX = a.VecX, VecY = a.VecY };
                Pos2Rec(indexA, out data[i], in particles[indexA]);
                i += 1;
            }

            if (indexB != -1)
            {
                //var b = particles[indexB];
                //data[i] = new() { Index = indexB, PosX = b.PosX, PosY = b.PosY, VecX = b.VecX, VecY = b.VecY };
                Pos2Rec(indexB, out data[i], in particles[indexB]);
            }

            snapshot.Add(systemTime, data);
        }
    }

}
