using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimulateCollision.ECS
{
    public class CollisionSystem() : System
    {
        public const float INFINITY = float.PositiveInfinity;

        // 距离该粒子1和粒子2碰撞所需的时间
        public float TimeToHit(in Position p1, in Velocity v1, float r1, in Position p2, in Velocity v2, float r2)
        {
            var (p1x, p1y) = p1;
            var (v1x, v1y) = v1;
            var (p2x, p2y) = p2;
            var (v2x, v2y) = v2;

            float dx = p2x - p1x;
            float dy = p2y - p1y;
            float dvx = v2x - v1x;
            float dvy = v2y - v1y;
            float dvdr = dx * dvx + dy * dvy;
            if (dvdr > 0)
            {
                return INFINITY;
            }
            float dvdv = dvx * dvx + dvy * dvy;
            if (dvdv == 0)
            {
                return INFINITY;
            }
            float drdr = dx * dx + dy * dy;
            float sigma = r1 + r2;
            float d = (dvdr * dvdr) - dvdv * (drdr - sigma * sigma);
            if (d < 0)
            {
                return INFINITY;
            }
            return -(dvdr + MathF.Sqrt(d)) / dvdv;
        }


        // 距离该粒子和垂直墙体碰撞所需的时间
        public float TimeToHitVerticalWall(in float px, in float vx, in float r, in float left = 0, in float right = 1)
        {
            if (vx > 0)
            {
                return (right - px - r) / vx;
            }
            else if (vx < 0)
            {
                return (px - left - r) / -vx;
            }
            else
            {
                return INFINITY;
            }
        }

        
        // 距离该粒子和水平墙体碰撞所需的时间
        public float TimeToHitHorizontalWall(in float py, in float vy, in float r, float top = 0, float bottom = 1)
        {
            if (vy > 0)
            {
                return (bottom - py - r) / vy;
            }
            else if (vy < 0)
            {
                return (py - top - r) / -vy;
            }
            else
            {
                return INFINITY;
            }
        }

        public void UpdatePosition(ref Position p, in Velocity v, float dt)
        {
            p.X += v.X * dt;
            p.Y += v.Y * dt;
        }


        // 更新粒子碰撞后的速度
        public void BounceOff(in Position p1, ref Velocity v1, ref Version ver1, float r1, float mass1,
            in Position p2, ref Velocity v2, ref Version ver2, float r2, float mass2)
        {
            var (p1x, p1y) = p1;
            var (v1x, v1y) = v1;
            var (p2x, p2y) = p2;
            var (v2x, v2y) = v2;

            float dx = p2x - p1x;
            float dy = p2y - p1y;
            float dvx = v2x - v1x;
            float dvy = v2y - v1y;
            float dvdr = dx * dvx + dy * dvy;
            float dist = r1 + r2;

            float magnitude = 2 * mass1 * mass2 * dvdr / ((mass1 + mass2) * dist);

            float fx = magnitude * dx / dist;
            float fy = magnitude * dy / dist;

            v1x += fx / mass1;
            v1y += fy / mass1;
            v1.X = v1x;
            v1.Y = v1y;
            ++ver1.Value;

            v2x -= fx / mass2;
            v2y -= fy / mass2;
            v2.X = v2x;
            v2.Y = v2y;
            ++ver2.Value;
        }

        /**
         * 碰撞垂直墙体后该粒子的速度
         */
        public void BounceOffVerticalWall(ref Velocity v, ref Version ver)
        {
            v.X = -v.X;
            ++ver.Value;
        }

        /**
         * 碰撞水平墙体后该粒子的速度
         */
        public void BounceOffHorizontalWall(ref Velocity v, ref Version ver)
        {
            v.Y = -v.Y;
            ++ver.Value;
        }
    }
}
