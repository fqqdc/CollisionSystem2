using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SimulateCollision.ECS
{
    public class EntityManager
    {
        // 所有未使用的ID
        private readonly Queue<Entity> mAvailableEntities = [];

        // 用于管理Entity和对应的Signature
        private readonly List<Signature> mSignatures = [];

        // 当前可用的EntityId的数量
        private int mLivingEntityCount = 0;

        public int MaxEntity { get; init; }

        public EntityManager(int maxEntity)
        {
            MaxEntity = maxEntity;

            // 用所有有效的Entity进行初始化
            for (Entity entity = new(0); entity.Id < MaxEntity; ++entity.Id)
            {
                mAvailableEntities.Enqueue(entity);
            }
            CollectionsMarshal.SetCount(mSignatures, MaxEntity);
        }

        public Entity CreateEntity()
        {
            Debug.Assert(mLivingEntityCount < MaxEntity, "Too many entities in existence.");

            // 从队列首部拿出一个ID
            Entity id = mAvailableEntities.Dequeue();
            ++mLivingEntityCount;
            return id;
        }

        public void DestroyEntity(Entity entity)
        {
            Debug.Assert(entity.Id < MaxEntity, "Entity out of range.");

            // 重置signature
            mSignatures[entity.Id].Reset();

            // 把销毁的Entity的Id从新放回队列的尾部
            mAvailableEntities.Enqueue(entity);
            --mLivingEntityCount;
        }

        public void SetSignature(Entity entity, Signature signature)
        {
            Debug.Assert(entity.Id < MaxEntity, "Entity out of range.");

            mSignatures[entity.Id] = signature;
        }

        public Signature GetSignature(Entity entity)
        {
            Debug.Assert(entity.Id < MaxEntity, "Entity out of range.");

            return mSignatures[entity.Id];
        }
    }
}
