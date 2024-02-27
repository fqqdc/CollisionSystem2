using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SimulateCollision.ECS
{
    public class SystemManager
    {
        // 包含所有的签名
        private readonly Dictionary<Type, Signature> mSignatures = [];

        // 包含所有的System
        private readonly Dictionary<Type, System> mSystems = [];

        public void RegisterSystem<TSystem>(TSystem system) where TSystem : System
        {
            var type = typeof(TSystem);

            Debug.Assert(!mSystems.ContainsKey(type), "Registering system more than once.");

            mSystems.Add(type, system);
        }

        public void SetSignature<TSystem>(Signature signature) where TSystem : System
        {
            var type = typeof(TSystem);

            Debug.Assert(mSystems.ContainsKey(type), "System used before registered.");

            // 设置System的签名，该签名描述了该System包含了哪些Component
            mSignatures.Add(type, signature);
        }

        public void EntityDestroyed(Entity entity)
        {
            foreach (var pair in mSystems)
            {
                var system = pair.Value;

                system.Remove(entity);
            }
        }

        public void EntitySignatureChanged(Entity entity, Signature entitySignature)
        {
            foreach (var pair in mSystems)
            {
                var type = pair.Key;
                var system = pair.Value;
                var systemSignature = mSignatures[type];

                // 如果Entity包含了System所需的所有Component
                if ((entitySignature.Value & systemSignature.Value) == systemSignature.Value)
                {
                    system.Add(entity);
                }
                // 否则删除该Entity（说明此时System需要的某些Component没有被该Entity包含）
                // 此时System无法处理该Entity
                else
                {
                    system.Remove(entity);
                }
            }
        }
    }
}
