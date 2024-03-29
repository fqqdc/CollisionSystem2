﻿using System;

namespace SimulateCollision.ECS
{
    public class CollisionSystem : System
    {
        public const float INFINITY = float.PositiveInfinity;

        // 距离该粒子1和粒子2碰撞所需的时间
        public float TimeToHit(in Particle p1, in Particle p2)
        {
            var (rx1, ry1) = (p1.PositionX, p1.PositionY);
            var (rx2, ry2) = (p2.PositionX, p2.PositionY);
            var (vx1, vy1) = (p1.VelocityX, p1.VelocityY);
            var (vx2, vy2) = (p2.VelocityX, p2.VelocityY);

            float dx = rx2 - rx1;
            float dy = ry2 - ry1;
            float dvx = vx2 - vx1;
            float dvy = vy2 - vy1;
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
            float sigma = p1.Radius + p2.Radius;
            float d = (dvdr * dvdr) - dvdv * (drdr - sigma * sigma);
            if (d < 0)
            {
                return INFINITY;
            }
            return -(dvdr + MathF.Sqrt(d)) / dvdv;
        }


        // 距离该粒子和垂直墙体碰撞所需的时间
        public float TimeToHitVerticalWall(in float px, in float vx, in float r, in float left, in float right)
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
        public float TimeToHitHorizontalWall(in float py, in float vy, in float r, float top, float bottom)
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

        public void UpdatePosition(ref Particle p, float dt)
        {
            p.PositionX += p.VelocityX * dt;
            p.PositionY += p.VelocityY * dt;
        }


        // 更新粒子碰撞后的速度
        public void BounceOff(ref Particle p1, ref Particle p2)
        {
            var (p1x, p1y) = (p1.PositionX, p1.PositionY);
            var (v1x, v1y) = (p1.VelocityX, p1.VelocityY);
            var (p2x, p2y) = (p2.PositionX, p2.PositionY);
            var (v2x, v2y) = (p2.VelocityX, p2.VelocityY);

            float dx = p2x - p1x;
            float dy = p2y - p1y;
            float dvx = v2x - v1x;
            float dvy = v2y - v1y;
            float dvdr = dx * dvx + dy * dvy;
            float dist = p1.Radius + p2.Radius;

            float magnitude = 2 * p1.Mass * p2.Mass * dvdr / ((p1.Mass + p2.Mass) * dist);

            float fx = magnitude * dx / dist;
            float fy = magnitude * dy / dist;

            v1x += fx / p1.Mass;
            v1y += fy / p1.Mass;
            p1.VelocityX = v1x;
            p1.VelocityY = v1y;
            ++p1.Version;

            v2x -= fx / p2.Mass;
            v2y -= fy / p2.Mass;
            p2.VelocityX = v2x;
            p2.VelocityY = v2y;
            ++p2.Version;
        }

        // 碰撞垂直墙体后该粒子的速度
        public void BounceOffVerticalWall(ref Particle p)
        {
            p.VelocityX *= -1;
            ++p.Version;
        }

        // 碰撞水平墙体后该粒子的速度
        public void BounceOffHorizontalWall(ref Particle p)
        {
            p.VelocityY *= -1;
            ++p.Version;
        }
    }
}
