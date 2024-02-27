using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimulateCollision.ECS
{
    public class System
    {
        protected HashSet<Entity> _Entities = [];

        public void Add(Entity entity) => _Entities.Add(entity);
        public void Remove(Entity entity) => _Entities.Remove(entity);
    }
}
