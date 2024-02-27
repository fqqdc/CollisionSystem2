﻿using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Media;

namespace SimulateCollision.ECS
{
    public class ComponentManager(int MaxEntity)
    {
        public void RegisterComponent<TComponent>() where TComponent : struct
        {
            // 获取指向名字的指针
            var typeName = typeof(TComponent).Name;

            Debug.Assert(!mComponentTypes.ContainsKey(typeName), "Registering component type more than once.");

            // 把新的ComponentType加入映射
            mComponentTypes.Add(typeName, mNextComponentType);

            // 创建新的ComponentArray，并将其加入映射
            mComponentArrays.Add(typeName, new ComponentArray<TComponent>(MaxEntity));

            // 递增表示可用的ComponentType的变量
            ++mNextComponentType.Value;
        }

        public ComponentType GetComponentType<TComponent>() where TComponent : struct
        {
            var typeName = typeof(TComponent).Name;

            Debug.Assert(mComponentTypes.ContainsKey(typeName), "Component not registered before use.");

            // 根据T获得该类型的Component对应的ID
            return mComponentTypes[typeName];
        }

        public void AddComponent<TComponent>(Entity entity, TComponent component) where TComponent : struct
        {
            // 给entity添加一个新的类型为T的Component
            GetComponentArray<TComponent>().InsertData(entity, component);
        }

        public void RemoveComponent<TComponent>(Entity entity) where TComponent : struct
        {
            GetComponentArray<TComponent>().RemoveData(entity);
        }

        public ref TComponent GetComponent<TComponent>(Entity entity) where TComponent : struct
        {
            // 获取属于entity的类型为T的Component的引用
            return ref GetComponentArray<TComponent>().GetData(entity);
        }

        public void EntityDestroyed(Entity entity)
        {
            // 将每个ComponentArray中该entity对应的Component删掉
            foreach (var pair in mComponentArrays)
            {
                var component = pair.Value;
                component.EntityDestroyed(entity);
            }
        }

        // 管理所有的ComponentType
        private readonly Dictionary<string, ComponentType> mComponentTypes = [];

        // 管理所有的ComponentArray
        private readonly Dictionary<string, IComponentArray> mComponentArrays = [];

        // 指向下一个将要注册的ComponentType
        private ComponentType mNextComponentType;

        private ComponentArray<TComponent> GetComponentArray<TComponent>() where TComponent : struct
        {
            var typeName = typeof(TComponent).Name;

            Debug.Assert(mComponentTypes.ContainsKey(typeName), "Component not registered before use.");

            return (ComponentArray<TComponent>)mComponentArrays[typeName];
        }
    }


}