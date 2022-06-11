using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimulateCollision
{
    /**
     * 基于事件驱动模拟的粒子碰撞
     * 粒子对象
     */
    public struct Particle
    {
        public const float INFINITY = float.PositiveInfinity;

        /**
         * position(位置)
         */
        public float PosX { get { return px; } }
        public float PosY { get { return py; } }
        public float px, py;

        /**
         * velocity(速度)
         */
        public float VecX { get { return vx; } }
        public float VecY { get { return vy; } }
        private float vx, vy;

        /**
         * 半径
         */
        public float Radius { get; private set; }

        /**
         * 质量
         */
        public float Mass { get; private set; }

        public int Count { get; private set; }

        public Particle(float rx, float ry, float vx, float vy, float radius, float mass)
        {
            this.px = rx;
            this.py = ry;
            this.vx = vx;
            this.vy = vy;

            this.Radius = radius;
            this.Mass = mass;

            this.Count = 0;
        }

        public static Particle CreateParticle()
        {
            Random r = new();
            return new(r.NextSingle(), r.NextSingle(), r.NextSingle() * 0.01f - 0.005f, r.NextSingle() * 0.01f - 0.005f, 0.02f, 0.5f);
        }

        public static Particle CreateParticleByInt()
        {
            Random r = new();
            return new(r.Next(1, 101), r.Next(1, 101), r.Next(1, 11) - 5, r.Next(1, 11) - 5, 2, 1);
        }

        public void Move(float dt)
        {
            px += vx * dt;
            py += vy * dt;
        }

        /**
         * 距离该粒子和粒子b碰撞所需的时间
         */
        public float TimeToHit(ref Particle that)
        {
            //if (this == that)
            //{
            //    return INFINITY;
            //}

            var (rx1, ry1) = (this.px, this.py);
            var (rx2, ry2) = (that.px, that.py);
            var (vx1, vy1) = (this.vx, this.vy);
            var (vx2, vy2) = (that.vx, that.vy);

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
            float sigma = this.Radius + that.Radius;
            float d = (dvdr * dvdr) - dvdv * (drdr - sigma * sigma);
            if (d < 0)
            {
                return INFINITY;
            }
            return -(dvdr + MathF.Sqrt(d)) / dvdv;
        }

        public float TimeToHit2(ref Particle that)
        {
            var (t1, t2) = TimeToHitByCurve(ref that);
            return MathF.Min(t1, t2);
        }

        public bool Intersect(ref Particle that)
        {
            var r = this.Radius + that.Radius;
            var (x1, y1) = (this.px, this.py);
            var (x2, y2) = (that.px, that.py);

            return (x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2) < r * r; ;
        }

        public (float, float) TimeToHitByCurve(ref Particle that)
        {
            var (x1o, y1o) = (this.px, this.py);
            var (x2o, y2o) = (that.px, that.py);
            var (v1x, v1y) = (this.vx, this.vy);
            var (v2x, v2y) = (that.vx, that.vy);
            var r1 = this.Radius;
            var r2 = that.Radius;

            //(        - v1x   ^2   - v1    y^2 + 2   v1x   v2x - v2x   ^2  + 2   v1y   v2y - v2    y^2)
            var exp5 = -(v1x * v1x) - v1y * v1y + 2 * v1x * v2x - v2x * v2x + 2 * v1y * v2y - v2y * v2y;
            if (exp5 == 0) return (INFINITY, INFINITY);

            //         2   v1x   x1o - 2   v2x   x1o - 2   v1x   x2o + 2   v2x   x2o + 2   v1y   y1o - 2   v2y   y1o - 2   v1y   y2o + 2   v2y   y2o
            var exp1 = 2 * v1x * x1o - 2 * v2x * x1o - 2 * v1x * x2o + 2 * v2x * x2o + 2 * v1y * y1o - 2 * v2y * y1o - 2 * v1y * y2o + 2 * v2y * y2o;

            //(        -2   v1x   x1o + 2   v2x   x1o + 2   v1x   x2o - 2   v2x   x2o - 2   v1y   y1o + 2   v2y   y1o + 2   v1y   y2o - 2   v2y   y2o)^2
            var exp2 = -2 * v1x * x1o + 2 * v2x * x1o + 2 * v1x * x2o - 2 * v2x * x2o - 2 * v1y * y1o + 2 * v2y * y1o + 2 * v1y * y2o - 2 * v2y * y2o;
            exp2 *= exp2;

            // (       -v1x^2       - v1y   ^2  + 2   v1x   v2x - v2x   ^2  + 2   v1y   v2y - v2y   ^2)
            var exp3 = -(v1x * v1x) - v1y * v1y + 2 * v1x * v2x - v2x * v2x + 2 * v1y * v2y - v2y * v2y;

            //(        r1   ^2 + 2   r1   r2 + r2   ^2 - x1o   ^2  + 2   x1o   x2o - x2o   ^2  - y1o   ^2  + 2   y1o   y2o - y2o   ^2)
            var exp4 = r1 * r1 + 2 * r1 * r2 + r2 * r2 - x1o * x1o + 2 * x1o * x2o - x2o * x2o - y1o * y1o + 2 * y1o * y2o - y2o * y2o;

            var expSqrt = exp2 - 4 * exp3 * exp4;
            if (expSqrt < 0) return (INFINITY, INFINITY);

            var t1 = (exp1 - MathF.Sqrt(exp2 - 4 * exp3 * exp4)) / (2 * exp5);
            var t2 = (exp1 + MathF.Sqrt(exp2 - 4 * exp3 * exp4)) / (2 * exp5);

            return (t1, t2);
        }

        /**
         * 距离该粒子和垂直墙体碰撞所需的时间
         */
        public float TimeToHitVerticalWall(float left = 0, float right = 1)
        {
            if (vx > 0)
            {
                return (right - px - Radius) / vx;
            }
            else if (vx < 0)
            {
                return (px - left - Radius) / -vx;
            }
            else
            {
                return INFINITY;
            }
        }

        /**
         * 距离该粒子和水平墙体碰撞所需的时间
         */
        public float TimeToHitHorizontalWall(float top = 0, float bottom = 1)
        {
            if (vy > 0)
            {
                return (bottom - py - Radius) / vy;
            }
            else if (vy < 0)
            {
                return (py - top - Radius) / -vy;
            }
            else
            {
                return INFINITY;
            }
        }

        /**
         * 碰撞后该粒子的速度
         */
        public void BounceOff(ref Particle that)
        {
            var (r1x, r1y) = (this.px, this.py);
            var (r2x, r2y) = (that.px, that.py);
            var (v1x, v1y) = (this.vx, this.vy);
            var (v2x, v2y) = (that.vx, that.vy);

            float dx = r2x - r1x;
            float dy = r2y - r1y;
            float dvx = v2x - v1x;
            float dvy = v2y - v1y;
            float dvdr = dx * dvx + dy * dvy;
            float dist = this.Radius + that.Radius;

            float magnitude = 2 * this.Mass * that.Mass * dvdr / ((this.Mass + that.Mass) * dist);

            float fx = magnitude * dx / dist;
            float fy = magnitude * dy / dist;

            v1x += fx / this.Mass;
            v1y += fy / this.Mass;
            //this.Velocity = new(v1x, v1y);
            this.vx = v1x;
            this.vy = v1y;
            v2x -= fx / that.Mass;
            v2y -= fy / that.Mass;
            //that.Velocity = new(v2x, v2y);
            that.vx = v2x;
            that.vy = v2y;

            this.Count++;
            that.Count++;
        }

        /**
         * 碰撞垂直墙体后该粒子的速度
         */
        public void BounceOffVerticalWall()
        {
            vx = -vx;
            Count++;
        }

        /**
         * 碰撞水平墙体后该粒子的速度
         */
        public void BounceOffHorizontalWall()
        {
            vy = -vy;
            Count++;
        }

        /**
        * 动能
        */
        public float KineticEnergy()
        {
            return 0.5f * Mass * (vx * vx + vy * vy);
        }
    }
}
