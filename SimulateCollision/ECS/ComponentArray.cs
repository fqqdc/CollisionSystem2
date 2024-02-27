using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace SimulateCollision.ECS
{
    public class ComponentArray<TComponent> : IComponentArray
        where TComponent : struct
    {
        private readonly List<TComponent> mComponentArray = [];

        // Entity到index的映射
        private readonly Dictionary<Entity, int> mEntityToIndexMap = [];

        // index到Entity的映射
        private readonly Dictionary<int, Entity> mIndexToEntityMap = [];

        // 有效的Entity的数量
        int mSize;

        public ComponentArray(int maxEntity)
        {
            CollectionsMarshal.SetCount(mComponentArray, maxEntity);
        }

        public void InsertData(Entity entity, TComponent component)
        {
            Debug.Assert(!mEntityToIndexMap.ContainsKey(entity), "Component added to same entity more than once.");

            // 把新的Entity加在末尾，并更新映射
            int newIndex = mSize;
            mEntityToIndexMap[entity] = newIndex;
            mIndexToEntityMap[newIndex] = entity;
            mComponentArray[newIndex] = component;
            ++mSize;
        }

        public void RemoveData(Entity entity)
        {
            Debug.Assert(mEntityToIndexMap.ContainsKey(entity), "Removing non-existent component.");

            // 把最后一个元素放到被删除的位置，以保持数组数据的紧凑性
            int indexOfRemovedEntity = mEntityToIndexMap[entity];
            int indexOfLastElement = mSize - 1;
            mComponentArray[indexOfRemovedEntity] = mComponentArray[indexOfLastElement];

            // 更新映射
            Entity entityOfLastElement = mIndexToEntityMap[indexOfLastElement];
            mEntityToIndexMap[entityOfLastElement] = indexOfRemovedEntity;
            mIndexToEntityMap[indexOfRemovedEntity] = entityOfLastElement;

            mEntityToIndexMap.Remove(entity);
            mIndexToEntityMap.Remove(indexOfLastElement);
            --mSize;
        }

        public ref TComponent GetData(Entity entity)
        {
            Debug.Assert(mEntityToIndexMap.ContainsKey(entity), "Retrieving non-existent component.");

            // 返回Component的引用
            return ref CollectionsMarshal.AsSpan(mComponentArray)[mEntityToIndexMap[entity]];
        }

        public void EntityDestroyed(Entity entity)
        {
            if (mEntityToIndexMap.ContainsKey(entity))
            {
                // Remove the entity's component if it existed
                RemoveData(entity);
            }
        }
    }
}
