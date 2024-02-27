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
                Debug.Assert(dt > 0);
                if (dt != Particle.INFINITY)
                    priorityQueue.Enqueue(EventIndex.CreateEvent(systemTime + dt, a.Count, indexA, particles[i].Count, i), systemTime + dt);
            };

            {
                var dtX = a.TimeToHitVerticalWall(0, this.width);
                Debug.Assert(dtX > 0);
                if (dtX != Particle.INFINITY)
                    priorityQueue.Enqueue(EventIndex.CreateEventHitVertical(systemTime + dtX, a.Count, indexA), systemTime + dtX);
            }

            {
                var dtY = a.TimeToHitHorizontalWall(0, this.height);
                Debug.Assert(dtY > 0);
                if (dtY != Particle.INFINITY)
                    priorityQueue.Enqueue(EventIndex.CreateEventHitHorizontal(systemTime + dtY, a.Count, indexA), systemTime + dtY);
            }
        }

        public void SnapshotAll()
        {
            for (int i = 0; i < particles.Length; i++)
            {
                //data[i] = new() { Index = i, PosX = particles[i].PosX, PosY = particles[i].PosY, VecX = particles[i].VecX, VecY = particles[i].VecY };
                Pos2Rec(i, out var data, in particles[i]);
                snapshot.Add(systemTime, data);
            }
            
        }

        private void Pos2Rec(int i, out SnapshotData rec, in Particle particle)
        {
            rec = new(i, particle.PosX, particle.PosY, particle.VecX, particle.VecY);
        }

        private void SnapshotParticle(int indexA, int indexB)
        {
            if (indexA != -1)
            {
                //data[i] = new() { Index = indexA, PosX = a.PosX, PosY = a.PosY, VecX = a.VecX, VecY = a.VecY };
                Pos2Rec(indexA, out var data, in particles[indexA]);
                snapshot.Add(systemTime, data);
            }

            if (indexB != -1)
            {
                //var b = particles[indexB];
                //data[i] = new() { Index = indexB, PosX = b.PosX, PosY = b.PosY, VecX = b.VecX, VecY = b.VecY };
                Pos2Rec(indexB, out var data, in particles[indexB]);
                snapshot.Add(systemTime, data);
            }
        }
    }

}
