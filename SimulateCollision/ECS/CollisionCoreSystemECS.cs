using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimulateCollision.ECS
{
    public class CollisionCoreSystemECS : ICollisionCoreSystem
    {
        private readonly float _Height, _Width;
        private readonly PriorityQueue<Event, float> _PriorityQueue = new();
        private readonly Coordinator _Coordinator;
        private readonly List<Entity> _Entities = [];
        private readonly CollisionSystem _CollisionSystem;

        private void SnapshotParticle(Entity entityA, Entity entityB)
        {
            int count = 0;
            if (entityA.Id != -1) count += 1;
            if (entityB.Id != -1) count += 1;

            if (count == 0) return;

            SnapshotData[] data = new SnapshotData[count];
            int i = 0;

            if (entityA.Id != -1)
            {
                Entity2Rec(out data[i], entityA);
                i += 1;
            }

            if (entityB.Id != -1)
            {
                Entity2Rec(out data[i], entityB);
            }

            SystemSnapshot.Add(SystemTime, data);
        }

        private void Entity2Rec(out SnapshotData rec, Entity entity)
        {
            var (posX, posY) = _Coordinator.GetComponent<Position>(entity);
            var (vecX, vecY) = _Coordinator.GetComponent<Velocity>(entity);

            rec = new(entity.Id, posX, posY, vecX, vecY);
        }


        public int QueueLength => _PriorityQueue.Count;

        public SystemSnapshot SystemSnapshot { get; } = new();

        public float SystemTime { get; private set; }

        public float NextStep()
        {
            while (_PriorityQueue.Count != 0)
            {
                Event e = _PriorityQueue.Dequeue();
                if (!IsValid(e)) continue;

                //* 处理事件 *//
                var dt = e.Time - SystemTime;
                foreach (var entity in _Entities)
                {
                    ref var p = ref _Coordinator.GetComponent<Position>(entity);
                    ref var v = ref _Coordinator.GetComponent<Velocity>(entity);
                    _CollisionSystem.UpdatePosition(ref p, v, dt);
                }

                SystemTime = e.Time;

                if (e.EntityA.Id != -1 && e.EntityB.Id != -1)
                {
                    ref var p1 = ref _Coordinator.GetComponent<Position>(e.EntityA);
                    ref var v1 = ref _Coordinator.GetComponent<Velocity>(e.EntityA);
                    ref var r1 = ref _Coordinator.GetComponent<Radius>(e.EntityA);
                    ref var mass1 = ref _Coordinator.GetComponent<Mass>(e.EntityA);
                    ref var ver1 = ref _Coordinator.GetComponent<Version>(e.EntityA);

                    ref var p2 = ref _Coordinator.GetComponent<Position>(e.EntityB);
                    ref var v2 = ref _Coordinator.GetComponent<Velocity>(e.EntityB);
                    ref var r2 = ref _Coordinator.GetComponent<Radius>(e.EntityB);
                    ref var mass2 = ref _Coordinator.GetComponent<Mass>(e.EntityB);
                    ref var ver2 = ref _Coordinator.GetComponent<Version>(e.EntityB);

                    _CollisionSystem.BounceOff(p1, ref v1, ref ver1, r1.Value, mass1.Value,
                        p2, ref v2, ref ver2, r2.Value, mass2.Value);
                }
                else if (e.EntityA.Id != -1 && e.EntityB.Id == -1)
                {
                    ref var v = ref _Coordinator.GetComponent<Velocity>(e.EntityA);
                    ref var ver = ref _Coordinator.GetComponent<Version>(e.EntityA);

                    _CollisionSystem.BounceOffVerticalWall(ref v, ref ver);
                }
                else if (e.EntityA.Id == -1 && e.EntityB.Id != -1)
                {
                    ref var v = ref _Coordinator.GetComponent<Velocity>(e.EntityB);
                    ref var ver = ref _Coordinator.GetComponent<Version>(e.EntityB);

                    _CollisionSystem.BounceOffHorizontalWall(ref v, ref ver);
                }

                if (e.EntityA.Id != -1)
                    PredictCollisions(e.EntityA);
                if (e.EntityB.Id != -1)
                    PredictCollisions(e.EntityB);

                SnapshotParticle(e.EntityA, e.EntityB);
                break;
            }

            return SystemTime;
        }

        public void SnapshotAll()
        {
            List<SnapshotData> listData = [];
            foreach (var entitye in _Entities)
            {
                //data[i] = new() { Index = i, PosX = particles[i].PosX, PosY = particles[i].PosY, VecX = particles[i].VecX, VecY = particles[i].VecY };
                Entity2Rec(out var data, entitye);
                listData.Add(data);
            }
            SystemSnapshot.Add(SystemTime, listData);
        }

        public CollisionCoreSystemECS(Particle[] particles, float width, float height)
        {
            _Width = width;
            _Height = height;

            _Coordinator = new(particles.Length);
            _Coordinator.RegisterComponent<Position>();
            _Coordinator.RegisterComponent<Velocity>();
            _Coordinator.RegisterComponent<Radius>();
            _Coordinator.RegisterComponent<Mass>();
            _Coordinator.RegisterComponent<Version>();

            _CollisionSystem = new();
            _Coordinator.RegisterSystem(_CollisionSystem);

            Signature signature = new();
            signature.Set(_Coordinator.GetComponentType<Position>().Value);
            signature.Set(_Coordinator.GetComponentType<Velocity>().Value);
            signature.Set(_Coordinator.GetComponentType<Radius>().Value);
            signature.Set(_Coordinator.GetComponentType<Mass>().Value);
            signature.Set(_Coordinator.GetComponentType<Version>().Value);
            _Coordinator.SetSystemSignature<CollisionSystem>(signature);

            foreach (var particle in particles)
            {
                var entity = _Coordinator.CreateEntity();
                _Entities.Add(entity);
                _Coordinator.AddComponent<Position>(entity, new(particle.PosX, particle.PosY));
                _Coordinator.AddComponent<Velocity>(entity, new(particle.VecX, particle.VecX));
                _Coordinator.AddComponent<Radius>(entity, new(particle.Radius));
                _Coordinator.AddComponent<Mass>(entity, new(particle.Mass));
                _Coordinator.AddComponent<Version>(entity, new(0));
            }

            Initialize();
        }

        private void Initialize()
        {
            _PriorityQueue.Clear();
            SystemSnapshot.Reset();
            SystemTime = 0;

            foreach (var entity in _Entities)
            {
                PredictCollisions(entity);
            }
        }

        private void PredictCollisions(Entity entityA)
        {
            if (entityA.Id == -1)
            {
                return;
            }

            ref var p1 = ref _Coordinator.GetComponent<Position>(entityA);
            ref var v1 = ref _Coordinator.GetComponent<Velocity>(entityA);
            ref var r1 = ref _Coordinator.GetComponent<Radius>(entityA);
            ref var ver1 = ref _Coordinator.GetComponent<Version>(entityA);

            foreach (var entityB in _Entities)
            {
                ref var p2 = ref _Coordinator.GetComponent<Position>(entityB);
                ref var v2 = ref _Coordinator.GetComponent<Velocity>(entityB);
                ref var r2 = ref _Coordinator.GetComponent<Radius>(entityB);

                var dt = _CollisionSystem.TimeToHit(p1, v1, r1.Value, p2, v2, r2.Value);
                if (dt != Particle.INFINITY)
                {
                    ref var ver2 = ref _Coordinator.GetComponent<Version>(entityB);
                    _PriorityQueue.Enqueue(
                        new() { Time = SystemTime + dt, EntityA = entityA, VersionA = ver1, EntityB = entityB, VersionB = ver2 },
                        SystemTime + dt);
                }
            }

            {
                var dtX = _CollisionSystem.TimeToHitVerticalWall(p1.X, v1.X, r1.Value, _Width);
                if (dtX != Particle.INFINITY)
                    _PriorityQueue.Enqueue(

                        new() { Time = SystemTime + dtX, EntityA = entityA, VersionA = ver1, EntityB = new(-1), },
                        SystemTime + dtX);
            }

            {
                var dtY = _CollisionSystem.TimeToHitHorizontalWall(p1.Y, v1.Y, r1.Value, 0, _Height);
                if (dtY != Particle.INFINITY)
                    _PriorityQueue.Enqueue(
                        new() { Time = SystemTime + dtY, EntityA = new(-1), EntityB = entityA, VersionB = ver1 },
                        SystemTime + dtY);
            }
        }

        private bool IsValid(Event e)
        {
            if (e.EntityA.Id != -1 &&
                e.VersionA == _Coordinator.GetComponent<Version>(e.EntityA))
            {
                return false;
            }
            if (e.EntityB.Id != -1 &&
                e.VersionB == _Coordinator.GetComponent<Version>(e.EntityB))
            {
                return false;
            }
            return true;
        }
    }
}
