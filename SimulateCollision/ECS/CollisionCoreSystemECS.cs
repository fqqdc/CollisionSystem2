using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;

namespace SimulateCollision.ECS
{
    public class CollisionCoreSystemECS : ICollisionCoreSystem
    {
        private readonly Float _Height, _Width;
        private readonly PriorityQueue<Event, Float> _PriorityQueue = new();
        private readonly Coordinator _Coordinator;
        private readonly List<Entity> _Entities = [];
        private readonly CollisionSystem _CollisionSystem;

        private void SnapshotParticle(Entity entityA, Entity entityB)
        {
            if (entityA.Id != -1)
            {
                Entity2Rec(out var data, entityA);
                SystemSnapshot.Add(SystemTime, data);
            }

            if (entityB.Id != -1)
            {
                Entity2Rec(out var data, entityB);
                SystemSnapshot.Add(SystemTime, data);
            }

        }

        private void Entity2Rec(out SnapshotData rec, Entity entity)
        {
            //var (posX, posY) = _Coordinator.GetComponent<Position>(entity);
            //var (vecX, vecY) = _Coordinator.GetComponent<Velocity>(entity);
            ref var p = ref _Coordinator.GetComponent<Particle>(entity);

            rec = new(entity.Id, p.PositionX, p.PositionY, p.VelocityX, p.VelocityY);
        }


        public int QueueLength => _PriorityQueue.Count;

        public SystemSnapshot SystemSnapshot { get; } = new();

        public Float SystemTime { get; private set; }

        public Float NextStep()
        {
            while (_PriorityQueue.Count != 0)
            {
                Event e = _PriorityQueue.Dequeue();
                if (!IsValid(e)) continue;

                //* 处理事件 *//
                var dt = e.Time - SystemTime;
                Debug.Assert(dt >= 0);
                foreach (var entity in _Entities)
                {
                    //ref var p = ref _Coordinator.GetComponent<Position>(entity);
                    //ref var v = ref _Coordinator.GetComponent<Velocity>(entity);
                    ref var p = ref _Coordinator.GetComponent<Particle>(entity);
                    _CollisionSystem.UpdatePosition(ref p, dt);
                }

                SystemTime = e.Time;

                if (e.EntityA.Id != -1 && e.EntityB.Id != -1)
                {
                    //ref var p1 = ref _Coordinator.GetComponent<Position>(e.EntityA);
                    //ref var v1 = ref _Coordinator.GetComponent<Velocity>(e.EntityA);
                    //ref var r1 = ref _Coordinator.GetComponent<Radius>(e.EntityA);
                    //ref var mass1 = ref _Coordinator.GetComponent<Mass>(e.EntityA);
                    //ref var ver1 = ref _Coordinator.GetComponent<Version>(e.EntityA);

                    //ref var p2 = ref _Coordinator.GetComponent<Position>(e.EntityB);
                    //ref var v2 = ref _Coordinator.GetComponent<Velocity>(e.EntityB);
                    //ref var r2 = ref _Coordinator.GetComponent<Radius>(e.EntityB);
                    //ref var mass2 = ref _Coordinator.GetComponent<Mass>(e.EntityB);
                    //ref var ver2 = ref _Coordinator.GetComponent<Version>(e.EntityB);

                    //_CollisionSystem.BounceOff(p1, ref v1, ref ver1, r1.Value, mass1.Value,
                    //    p2, ref v2, ref ver2, r2.Value, mass2.Value);

                    ref var p1 = ref _Coordinator.GetComponent<Particle>(e.EntityA);
                    ref var p2 = ref _Coordinator.GetComponent<Particle>(e.EntityB);
                    _CollisionSystem.BounceOff(ref p1, ref p2);
                }
                else if (e.EntityA.Id != -1 && e.EntityB.Id == -1)
                {
                    //ref var v = ref _Coordinator.GetComponent<Velocity>(e.EntityA);
                    //ref var ver = ref _Coordinator.GetComponent<Version>(e.EntityA);

                    //_CollisionSystem.BounceOffVerticalWall(ref v, ref ver);

                    ref var p = ref _Coordinator.GetComponent<Particle>(e.EntityA);
                    _CollisionSystem.BounceOffVerticalWall(ref p);
                }
                else if (e.EntityA.Id == -1 && e.EntityB.Id != -1)
                {
                    //ref var v = ref _Coordinator.GetComponent<Velocity>(e.EntityB);
                    //ref var ver = ref _Coordinator.GetComponent<Version>(e.EntityB);

                    //_CollisionSystem.BounceOffHorizontalWall(ref v, ref ver);
                    ref var p = ref _Coordinator.GetComponent<Particle>(e.EntityB);
                    _CollisionSystem.BounceOffHorizontalWall(ref p);
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
            foreach (var entitye in _Entities)
            {
                //data[i] = new() { Index = i, PosX = particles[i].PosX, PosY = particles[i].PosY, VecX = particles[i].VecX, VecY = particles[i].VecY };
                Entity2Rec(out var data, entitye);
                SystemSnapshot.Add(SystemTime, data);
            }
        }

        public CollisionCoreSystemECS(SimulateCollision.Particle[] particles, float width, float height)
        {
            _Width = width;
            _Height = height;

            _Coordinator = new(particles.Length);
            //_Coordinator.RegisterComponent<Position>();
            //_Coordinator.RegisterComponent<Velocity>();
            //_Coordinator.RegisterComponent<Radius>();
            //_Coordinator.RegisterComponent<Mass>();
            //_Coordinator.RegisterComponent<Version>();
            _Coordinator.RegisterComponent<Particle>();

            _CollisionSystem = new();
            _Coordinator.RegisterSystem(_CollisionSystem);

            Signature signature = new();
            //signature.Set(_Coordinator.GetComponentType<Position>().Value);
            //signature.Set(_Coordinator.GetComponentType<Velocity>().Value);
            //signature.Set(_Coordinator.GetComponentType<Radius>().Value);
            //signature.Set(_Coordinator.GetComponentType<Mass>().Value);
            //signature.Set(_Coordinator.GetComponentType<Version>().Value);
            signature.Set(_Coordinator.GetComponentType<Particle>().Value);
            _Coordinator.SetSystemSignature<CollisionSystem>(signature);

            foreach (var particle in particles)
            {
                var entity = _Coordinator.CreateEntity();
                _Entities.Add(entity);
                //_Coordinator.AddComponent<Position>(entity, new(particle.PosX, particle.PosY));
                //_Coordinator.AddComponent<Velocity>(entity, new(particle.VecX, particle.VecX));
                //_Coordinator.AddComponent<Radius>(entity, new(particle.Radius));
                //_Coordinator.AddComponent<Mass>(entity, new(particle.Mass));
                //_Coordinator.AddComponent<Version>(entity, new(0));
                _Coordinator.AddComponent<Particle>(entity, 
                    new(particle.PosX, particle.PosY, 
                    particle.VecX, particle.VecY, 
                    particle.Radius, particle.Mass, 0));
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

            SnapshotAll();
        }

        private void PredictCollisions(Entity entityA)
        {
            if (entityA.Id == -1)
            {
                return;
            }

            //ref var p1 = ref _Coordinator.GetComponent<Position>(entityA);
            //ref var v1 = ref _Coordinator.GetComponent<Velocity>(entityA);
            //ref var r1 = ref _Coordinator.GetComponent<Radius>(entityA);
            //ref var ver1 = ref _Coordinator.GetComponent<Version>(entityA);
            ref var p1 = ref _Coordinator.GetComponent<Particle>(entityA);

            foreach (var entityB in _Entities)
            {
                if (entityA.Id == entityB.Id)
                    continue;

                //ref var p2 = ref _Coordinator.GetComponent<Position>(entityB);
                //ref var v2 = ref _Coordinator.GetComponent<Velocity>(entityB);
                //ref var r2 = ref _Coordinator.GetComponent<Radius>(entityB);
                ref var p2 = ref _Coordinator.GetComponent<Particle>(entityB);

                //var dt = _CollisionSystem.TimeToHit(p1, v1, r1.Value, p2, v2, r2.Value);
                var dt = _CollisionSystem.TimeToHit(p1, p2);
                if (dt != CollisionSystem.INFINITY)
                {
                    Debug.Assert(dt > 0);
                    //ref var ver2 = ref _Coordinator.GetComponent<Version>(entityB);
                    _PriorityQueue.Enqueue(
                        //new() { Time = SystemTime + dt, EntityA = entityA, VersionA = ver1, EntityB = entityB, VersionB = ver2 },
                        new() { Time = SystemTime + dt, EntityA = entityA, VersionA = p1.Version, EntityB = entityB, VersionB = p2.Version },
                        SystemTime + dt);
                }
            }

            {
                //var dtX = _CollisionSystem.TimeToHitVerticalWall(p1.X, v1.X, r1.Value, 0, _Width);
                var dtX = _CollisionSystem.TimeToHitVerticalWall(p1.PositionX, p1.VelocityX, p1.Radius, 0, _Width);
                if (dtX != CollisionSystem.INFINITY)
                {
                    Debug.Assert(dtX > 0);
                    _PriorityQueue.Enqueue(
                        new() { Time = SystemTime + dtX, EntityA = entityA, VersionA = p1.Version, EntityB = new(-1), },
                        SystemTime + dtX);
                }
            }

            {
                //var dtY = _CollisionSystem.TimeToHitHorizontalWall(p1.Y, v1.Y, r1.Value, 0, _Height);
                var dtY = _CollisionSystem.TimeToHitHorizontalWall(p1.PositionY, p1.VelocityY, p1.Radius, 0, _Height);
                if (dtY != CollisionSystem.INFINITY)
                {
                    Debug.Assert(dtY > 0);
                    _PriorityQueue.Enqueue(
                            new() { Time = SystemTime + dtY, EntityA = new(-1), EntityB = entityA, VersionB = p1.Version },
                            SystemTime + dtY);
                }
            }
        }

        private bool IsValid(Event e)
        {
            if (e.EntityA.Id != -1 &&
                //e.VersionA != _Coordinator.GetComponent<Version>(e.EntityA))
                e.VersionA != _Coordinator.GetComponent<Particle>(e.EntityA).Version)
            {
                return false;
            }
            if (e.EntityB.Id != -1 &&
                //e.VersionB != _Coordinator.GetComponent<Version>(e.EntityB))
                e.VersionB != _Coordinator.GetComponent<Particle>(e.EntityB).Version)
            {
                return false;
            }
            return true;
        }
    }
}
