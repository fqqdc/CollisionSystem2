using System.Collections.Generic;
using System.Diagnostics;

namespace SimulateCollision.ECS
{
    public class EntityManager
    {
        // 用于管理Entity和对应的Signature
        private readonly Dictionary<int, Signature> mEntityToSignatureMap = [];

        // 当前可用的EntityId的数量
        private int mIncEntityIndex = 0;

        public Entity CreateEntity()
        {
            var entity = new Entity(mIncEntityIndex++);
            mEntityToSignatureMap.Add(entity.Id, new());
            return entity;
        }

        public void DestroyEntity(Entity entity)
        {
            Debug.Assert(mEntityToSignatureMap.ContainsKey(entity.Id), "Entity out of range.");

            mEntityToSignatureMap.Remove(entity.Id);
        }

        public void SetSignature(Entity entity, Signature signature)
        {
            Debug.Assert(mEntityToSignatureMap.ContainsKey(entity.Id), "Entity out of range.");

            mEntityToSignatureMap[entity.Id] = signature;
        }

        public Signature GetSignature(Entity entity)
        {
            Debug.Assert(mEntityToSignatureMap.ContainsKey(entity.Id), "Entity out of range.");

            return mEntityToSignatureMap[entity.Id];
        }
    }
}
