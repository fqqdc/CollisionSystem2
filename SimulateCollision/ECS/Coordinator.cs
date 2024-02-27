using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SimulateCollision.ECS
{
    public class Coordinator
    {
        private readonly ComponentManager mComponentManager;
        private readonly EntityManager mEntityManager;
        private readonly SystemManager mSystemManager;

        public Coordinator(int maxEntity, int maxComponent = 32)
        {
            // 创建指向每个Manager的指针
            mComponentManager = new(maxEntity);
            mEntityManager = new(maxEntity);
            mSystemManager = new();
        }

        // Entity 相关方法
        public Entity CreateEntity()
        {
            return mEntityManager.CreateEntity();
        }

        public void DestroyEntity(Entity entity)
        {
            mEntityManager.DestroyEntity(entity);
            mComponentManager.EntityDestroyed(entity);
            mSystemManager.EntityDestroyed(entity);
        }

        // Component 相关方法
        public void RegisterComponent<TComponent>() where TComponent : struct
        {
            mComponentManager.RegisterComponent<TComponent>();
        }

        public void AddComponent<TComponent>(Entity entity, TComponent component) where TComponent : struct
        {
            mComponentManager.AddComponent(entity, component);

            var signature = mEntityManager.GetSignature(entity);
            signature.Set(mComponentManager.GetComponentType<TComponent>().Value, true);
            mEntityManager.SetSignature(entity, signature);

            mSystemManager.EntitySignatureChanged(entity, signature);
        }

        public void RemoveComponent<TComponent>(Entity entity) where TComponent : struct
        {
            mComponentManager.RemoveComponent<TComponent>(entity);

            var signature = mEntityManager.GetSignature(entity);
            signature.Set(mComponentManager.GetComponentType<TComponent>().Value, false);
            mEntityManager.SetSignature(entity, signature);

            mSystemManager.EntitySignatureChanged(entity, signature);
        }

        public ref TComponent GetComponent<TComponent>(Entity entity) where TComponent : struct
        {
            return ref mComponentManager.GetComponent<TComponent>(entity);
        }

        public ComponentType GetComponentType<TComponent>() where TComponent : struct
        {
            return mComponentManager.GetComponentType<TComponent>();
        }

        // System 相关方法
        public void RegisterSystem<TSystem>(TSystem system) where TSystem : System
        {
            mSystemManager.RegisterSystem(system);
        }

        public void SetSystemSignature<TSystem>(Signature signature) where TSystem : System
        {
            mSystemManager.SetSignature<TSystem>(signature);
        }
    }
}
