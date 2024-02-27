using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimulateCollision.ECS
{
    public interface IComponentArray
    {
        void EntityDestroyed(in Entity entity);
    }
}
