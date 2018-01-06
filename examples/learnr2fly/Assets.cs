using Learnr;
using System.Drawing;
using System;
using static learnr2fly.Global;
using System.Collections.Generic;
using learnr2fly.Extensions;
using System.Linq;

namespace learnr2fly
{
    namespace Extensions
    {
        public static class RenderObjectEx
        {
            public static Random random = new Random();
            public static Random rand(this RenderObject ro)
            {
                return random;
            }
            public static void PeriodicBC(this RenderObject ro)
            {
                if (ro.x < 0)
                {
                    ro.x = ro.BaseSettings.Width + ro.x;
                }
                else if (ro.x >= ro.BaseSettings.Width)
                {
                    ro.x = ro.x - ro.BaseSettings.Width;
                }
                if (ro.y < 0)
                {
                    ro.y = ro.BaseSettings.Height + ro.y;
                }
                else if (ro.y >= ro.BaseSettings.Height)
                {
                    ro.y = ro.y - ro.BaseSettings.Height;
                }
            }

        }
    }

    public class Ball : RenderObject
    {
        public bool CollisionLastFrame;

        public Ball(UiSettings uiSettings) :
            base(uiSettings)
        {
        }
        public Ball(UiSettings uiSettings, RectangleF bounds, Color color) :
            base(uiSettings, bounds, color)
        {
            this.x = uiSettings.Width / 2;
            this.y = uiSettings.Height / 4;
            uiSettings.SendMessage(UiSettingsEventArgs.EventType.SettingChangeEvent, "hello new ball");
            this.uiSettings = uiSettings;
        }

        public override void UpdateElement(int delta)
        {
            if (playState != 0)
            {
                gameTime += playState * delta / 1000.0f;
                var sp = 1.1f;
                if (!CollisionLastFrame) dy += sp * delta / 16f;
                dy *= 0.95f;

                mx += dx;
                my += dy;
            }
        }

        public override void DrawElement(Graphics g)
        {
            if (this.my > uiSettings.Height)
            {
                g.FillEllipse(brush, mx - w / 2, uiSettings.Height - h / 2, w, h);
            }
            else
            {
                g.FillEllipse(brush, mx - w / 2, my - h / 2, w, h);
            }
        }

        public void BounceX() { dx = -dx * R.BOUNCINESS; }
        public void BounceY() { dy = -dy * R.BOUNCINESS; }

        public void BounceOffFloor(int delta)
        {
            if (this.my + this.h * 0.5 > uiSettings.Height && !CollisionLastFrame)
            { this.BounceY(); CollisionLastFrame = true; }
            else if (this.my + this.h > uiSettings.Height && CollisionLastFrame)
                CollisionLastFrame = true;
            else
                CollisionLastFrame = false;
        }

        public override void Reset()
        {
            this.y = uiSettings.Height / 4;
            uiSettings.SendMessage(UiSettingsEventArgs.EventType.SettingChangeEvent, "hello new ball");
        }
    }

    public enum Turning
    {
        Right,
        Left,
        Still
    }
    public enum Boosters
    {
        Accelerate,
        NoChange,
        Decelerate
    }

    public class Spaceship : RenderObject
    {
        public new Color Colour { get { return color; } set { color = value; brush = new SolidBrush(color); linesBrush = new SolidBrush(Color.FromArgb(80, (int)(255-color.R * 0.2), (int)(255 - color.G * 0.2), (int)(255 - color.B * 0.2))); pen = new Pen(brush); } }
        public float Direction { get { return facing; } set { facing = value; } }

        public float NetSpeed { get => netSpeed; set => netSpeed = value; }
        public float NetRotRate { get => netRotRate; set => netRotRate = value; }
        public Spaceship EnemySpaceship { get => enemySpaceship; set => enemySpaceship = value; }
        public float LookRot { get => lookRot; set => lookRot = Math.Min(Math.Max(value, -R.SPACESHIP_HEAD_ROT_RANGE / 2), R.SPACESHIP_HEAD_ROT_RANGE / 2); }

        private float rotSpeed => R.TURNING_SPEED * netRotRate;
        public float speed;
        private float lookRot = 0;
        public Turning turning;
        public Boosters boosters;
        public Bullet[] bulletList = new Bullet[R.BULLET_LIMIT];
        public static int ammo = R.BULLET_LIMIT;
        public BoosterSmoke[] boosterList = new BoosterSmoke[R.BOOSTER_ANIM_LIMIT];
        public PointF[] shooter = new PointF[3];
        public double Score = 0;
        public double LastHitScore = 0;

        protected float facing;
        private Polygon[] polys;
        public static bool PreviewVisionLines;
        public static bool CheckPBCForVisionLines = true;
        private float lastshoot;
        private Brush linesBrush = Brushes.White;
        private float netSpeed = 1;
        private float netRotRate = 1;
        private Spaceship enemySpaceship;

        public Spaceship(UiSettings uiSettings, RectangleF rect, Color color) :
            base(uiSettings, rect, color)
        {
            this.x = uiSettings.Width / 2;
            this.y = uiSettings.Height / 2;
            speed = 0;
            Direction = 0;
            mw = 15;
            mh = 15;
            turning = Turning.Still;
            boosters = Boosters.NoChange;
            this.uiSettings = uiSettings;
        }

        public override void Reset() => Reset(uiSettings.Width / 2, uiSettings.Height / 2);
        public void Reset(float mx, float my)
        {
            this.x = mx;
            this.y = my;
            speed = 0;
            lookRot = 0;
            Direction = 0;
            turning = Turning.Still;
            boosters = Boosters.NoChange;
        }

        public double[] GenerateVisionLines(Asteroid[] asteroids, int number, bool LRSplit, float first, float stride, float width, float widthstride)
        {
            int n = number;
            double[] lines = new double[n];
            Polygon[] polys = new Polygon[n];
            bool oddn = n % 2 == 1;
            float cone = 2.7f;
            float dir = Direction + LookRot;

            for (int i = 0; i < n; i++)
            {
                if (LRSplit)
                {
                    int ii = i / 2;
                    if (oddn && i == n-1)
                        polys[i] = GetVisionPoly((ii+1) * stride + first, (width + (ii+1) * widthstride) * cone);
                    else
                    {
                        polys[i] = GetVisionPolyLR(ii * stride + first, width + ii * widthstride, cone, dir, i%2==0);
                    }
                }
                else
                    polys[i] = GetVisionPoly(i * stride + first, width + i * widthstride);

            }
            float max_x = polys.Max(p => p.Points.Max(pt => pt.X)), max_y = polys.Max(p => p.Points.Max(pt => pt.Y));
            float min_x = polys.Min(p => p.Points.Min(pt => pt.X)), min_y = polys.Min(p => p.Points.Min(pt => pt.Y));

            for (int ii = 0; ii < n; ii++)
            {
                if (lines[ii] != 0.0) continue;
                int iii = LRSplit ? ii / 2 : ii;
                double stregnth = 1 / (iii + 1.0);
                List<RenderObject> validAsts = new List<RenderObject>();
                validAsts.AddRange(asteroids.Where(a => a != null));
                if (EnemySpaceship != null)
                    validAsts.Add(EnemySpaceship);
                for (int i = 0; i < validAsts.Count; i++)
                {
                    if (validAsts[i] == null) continue;
                    Polygon p = validAsts[i].rect;
                    if (p.IntersectsWith(polys[ii]))
                    {
                        lines[ii] = stregnth;
                        int index = ii;
                        while (index < (oddn ? n-1 : n))
                        {
                            iii=LRSplit? index / 2 : index;
                            lines[index] = 1 / (iii + 1.0);
                            index += LRSplit ? 2 : 1;
                        }
                        break;
                    }
                    else lines[ii] = -stregnth;
                }
                if (CheckPBCForVisionLines)
                {
                    List<Tuple<string, RenderObject>> overflowAsts = null;
                    if (max_x > uiSettings.Width)
                    {
                        float overflow = max_x - uiSettings.Width;
                        overflowAsts = validAsts.Where(a => a.x < overflow).ToList().ConvertAll(ast => new Tuple<string, RenderObject>("R", ast));
                    }
                    else if (min_x < 0)
                    {
                        overflowAsts = validAsts.Where(a => a.x > uiSettings.Width + min_x).ToList().ConvertAll(ast => new Tuple<string, RenderObject>("L", ast));
                    }
                    if (max_y > uiSettings.Height)
                    {
                        float overflow = max_y - uiSettings.Height;
                        overflowAsts = validAsts.Where(a => a.y < overflow).ToList().ConvertAll(ast => new Tuple<string, RenderObject>("B", ast));
                    }
                    else if (min_y < 0)
                    {
                        overflowAsts = validAsts.Where(a => a.y > uiSettings.Height + min_y).ToList().ConvertAll(ast => new Tuple<string, RenderObject>("T", ast));
                    }
                    if (overflowAsts != null)
                    {
                        for (int i = 0; i < overflowAsts.Count; i++)
                        {
                            Polygon p = overflowAsts[i].Item2.rect;
                            string side = overflowAsts[i].Item1;
                            switch (side)
                            {
                                case "L":
                                    p.Points = p.Points.ConvertAll(pt => new PointF(pt.X - uiSettings.Width, pt.Y));
                                    break;
                                case "R":
                                    p.Points = p.Points.ConvertAll(pt => new PointF(pt.X + uiSettings.Width, pt.Y));
                                    break;
                                case "T":
                                    p.Points = p.Points.ConvertAll(pt => new PointF(pt.X, pt.Y - uiSettings.Height));
                                    break;
                                case "B":
                                    p.Points = p.Points.ConvertAll(pt => new PointF(pt.X, pt.Y + uiSettings.Height));
                                    break;
                            }
                            if (p.IntersectsWith(polys[ii]))
                            {
                                lines[ii] = stregnth;
                                int index = ii;
                                while (index < n)
                                {
                                    lines[index] = stregnth;
                                    index += LRSplit ? 2 : 1;
                                }
                                break;
                            }
                            else lines[ii] = -stregnth;
                        }
                    }
                }
            }

            if (this.polys == null) this.polys = new Polygon[n];
            Array.Copy(polys, this.polys, n);
            return lines;
        }

        public Polygon GetVisionPoly(float r, float h)
        {
            double theta = Direction * R.RADIANCONVERSION;
            double theta1 = (90-Direction) * R.RADIANCONVERSION;
            float origin_x = (float)(mx + (h /2) * Math.Sin(theta));
            float origin_y = (float)(my - (h / 2) * Math.Cos(theta));
            float p1_x = (float)(origin_x + r * Math.Cos(theta));
            float p1_y = (float)(origin_y + r * Math.Sin(theta));
            float p2_x = (float)(origin_x - h * Math.Cos(theta1));
            float p2_y = (float)(origin_y + h * Math.Sin(theta1));
            float p3_x = p1_x + p2_x - origin_x;
            float p3_y = p1_y + p2_y - origin_y;
            return new Polygon(new PointF(origin_x, origin_y), new PointF(p1_x, p1_y), new PointF(p3_x, p3_y), new PointF(p2_x, p2_y));
        }
        public Polygon GetVisionPolyLR(float r, float h, float conegradient, float direction, bool sideleft)
        {
            h /= 2;
            double theta = direction * R.RADIANCONVERSION;
            double theta1 = (90 - direction) * R.RADIANCONVERSION;
            float origin_x = (float)(mx + (h / 2) * Math.Sin(theta));
            float origin_y = (float)(my - (h / 2) * Math.Cos(theta));
            origin_x = mx;
            origin_y = my;
            float p1_x = (float)(origin_x + r * Math.Cos(theta) * (sideleft ? +1 : +1));
            float p1_y = (float)(origin_y + r * Math.Sin(theta) * (sideleft ? +1 : +1));
            float p2_x = (float)(origin_x - h * Math.Cos(theta1) * (sideleft ? -1 : +1));
            float p2_y = (float)(origin_y + h * Math.Sin(theta1) * (sideleft ? -1 : +1));
            float p3_x = (p1_x - origin_x) + (p2_x - origin_x) * conegradient + origin_x;
            float p3_y = (p1_y - origin_y) + (p2_y - origin_y) * conegradient + origin_y;

            return new Polygon(new PointF(origin_x, origin_y), new PointF(p1_x, p1_y), new PointF(p3_x, p3_y), new PointF(p2_x, p2_y));
        }

        
        

        public override void UpdateElement(int delta)
        {
            if (playState != 0)
            {

                BoostingShip(this.boosters);
                TurningShip(this.turning);

                if (boosters == Boosters.Accelerate)
                {
                    dy += R.ACCELERATE * netSpeed * (float)Math.Sin(Direction * R.RADIANCONVERSION);
                    dx += R.ACCELERATE * netSpeed * (float)Math.Cos(Direction * R.RADIANCONVERSION);
                    BoostersAnimation(uiSettings);
                }

                speed = (float)Math.Sqrt(dy * dy + dx * dx);

                BoosterDustCheck();

                if (gameTime % 5 > 1) { Reload(); }

                dy = (speed > 20) ? dy *= 0.85f : dy *= 0.97f;
                dx = (speed > 20) ? dx *= 0.85f : dx *= 0.97f;

                mx += dx;
                my += dy;

                this.PeriodicBC();

                if (gameTime % 5 == 0)
                {

                }

                gameTime += playState * delta / 1000.0f;
            }
        }

        public override void DrawElement(Graphics g)
        {
            if (PreviewVisionLines)
                foreach (Polygon p in polys.Where(p=>p!=null)) g.DrawPolygon(new Pen(linesBrush, 0.2f), p.Points.ToArray());
            g.FillEllipse(brush, (mx - mw / 7) + (def_radius * (float)Math.Cos(Direction * R.RADIANCONVERSION + 3.14159265359f)),
                (my - mh / 7) + (def_radius * (float)Math.Sin(Direction * R.RADIANCONVERSION + 3.14159265359f)),
                mw / 3,
                mh / 3);
            g.FillPolygon(brush, shooter);
        }

        public void TurningShip(Turning t)
        {
            switch (t)
            {
                case Turning.Right:
                    Direction += rotSpeed;
                    break;
                case Turning.Left:
                    Direction -= rotSpeed;
                    break;
                case Turning.Still:
                    break;
                default:
                    break;
            }
            LookRot -= rotSpeed * 0.25f * (lookRot * lookRot / (R.SPACESHIP_HEAD_ROT_RANGE  * R.SPACESHIP_HEAD_ROT_RANGE));
            shooter[0].X = mx + (float)Math.Cos(Direction * R.RADIANCONVERSION) * 1.3f * mw;
            shooter[0].Y = my + (float)Math.Sin(Direction * R.RADIANCONVERSION) * 1.3f * mh;
            shooter[1].X = mx + (float)Math.Cos(Direction * R.RADIANCONVERSION + 1.57079632679) * 0.6f * mw;
            shooter[1].Y = my + (float)Math.Sin(Direction * R.RADIANCONVERSION + 1.57079632679) * 0.6f * mh;
            shooter[2].X = mx + (float)Math.Cos(Direction * R.RADIANCONVERSION - 1.57079632679) * 0.6f * mh;
            shooter[2].Y = my + (float)Math.Sin(Direction * R.RADIANCONVERSION - 1.57079632679) * 0.6f * mh;
        }

        public void BoostingShip(Boosters b)
        {
            switch (b)
            {
                case Boosters.Accelerate:
                    break;
                case Boosters.NoChange:

                    break;
                case Boosters.Decelerate:
                    if (speed > 0)
                        speed -= R.ACCELERATE;
                    else
                        speed = 0;
                    break;
                default:
                    break;
            }
        }

        public void Reload()
        {
            for (int i = 0; i < R.BULLET_LIMIT; i++)
            {
                if (bulletList[i] != null)
                {
                    if (bulletList[i].GameTime > R.RELOAD_SPEED)
                    {
                        bulletList[i] = null;
                        LastHitScore *= 1-R.PTS_MISSED_SHOT_PEN;
                        Score -= LastHitScore;
                        ammo++;
                    }
                }
            }
        }

        public void Shoot()
        {
            if (gameTime - lastshoot >= R.RELOAD_RATE)
            {
                Bullet b = new Bullet(uiSettings, Direction, shooter[0].X, shooter[0].Y);
                b.dx += dx/4;
                b.dy += dy/4;
                for (int i = 0; i < R.BULLET_LIMIT; i++)
                {
                    if (bulletList[i] == null)
                    {
                        bulletList[i] = b;
                        ammo--;
                        break;
                    }
                }
                lastshoot = GameTime;
            }
        }

        public void BoostersAnimation(UiSettings uiSettings)
        {
            BoosterSmoke b = new BoosterSmoke(uiSettings, Direction, mx, my);
            for (int i = 0; i < R.BOOSTER_ANIM_LIMIT; i++)
            {
                if (boosterList[i] == null) { boosterList[i] = b; }
            }
        }

        public void BoosterDustCheck()
        {
            for (int i = 0; i < R.BOOSTER_ANIM_LIMIT; i++)
            {
                if (boosterList[i] != null && boosterList[i].GameTime > R.DELETE_DUST_SPEED) { boosterList[i] = null; }
            }
        }
    }

    public class Polygon
    {
        public List<PointF> Points { get; internal set; }

        public Polygon()
        {
            this.Points = new List<PointF>();
        }
        
        public Polygon(params PointF[] pts)
        {
            this.Points = new List<PointF>();
            Points.AddRange(pts);
        }

        public static implicit operator Polygon(RectangleF d)
        {
            Polygon p = new Polygon();
            float w = d.Width;
            float h = d.Height;
            p.Points = new List<PointF>() {
                new PointF(d.X, d.Y),
                new PointF(d.X + w, d.Y),
                new PointF(d.X + w, d.Y + h),
                new PointF(d.X, d.Y + h),
            };
            return p;
        }

        public bool IntersectsWith(Polygon poly)
        {
            if (R.AST_COLLISION_USE_TRIS)
            {
                if(poly.Points.Count != 4) return IsPolygonsIntersecting(this, poly);

                Triangle[] tri1 = new Triangle[] {
                    new Triangle(Points[0], Points[1], Points[2]),
                    new Triangle(Points[0], Points[2], Points[3])
                };
                Triangle[] tri2 = new Triangle[] {
                    new Triangle(poly.Points[0], poly.Points[1], poly.Points[2]),
                    new Triangle(poly.Points[0], poly.Points[2], poly.Points[3])
                };
                
                for (int i = 0; i < 2; i++)
                {
                    if (trianglesIntersect3(tri1[i], tri2[0]) || trianglesIntersect3(tri1[i], tri2[1])) return true;
                }
                return false;
            }
            else
            {
                return IsPolygonsIntersecting(this, poly);
            }
        }

        public static bool IsPolygonsIntersecting(Polygon a, Polygon b)
        {
            int loopcount = 0;
            while (a == null || b == null) { loopcount++; if (loopcount > 25) return false; }
            foreach (var polygon in new[] { a, b })
            {
                for (int i1 = 0; i1 < polygon.Points.Count; i1++)
                {
                    int i2 = (i1 + 1) % polygon.Points.Count;
                    var p1 = polygon.Points[i1];
                    var p2 = polygon.Points[i2];

                    var normal = new PointF(p2.Y - p1.Y, p1.X - p2.X);

                    double? minA = null, maxA = null;
                    foreach (var p in a.Points)
                    {
                        var projected = normal.X * p.X + normal.Y * p.Y;
                        if (minA == null || projected < minA)
                            minA = projected;
                        if (maxA == null || projected > maxA)
                            maxA = projected;
                    }

                    double? minB = null, maxB = null;
                    while (b == null) { }
                    foreach (var p in b.Points)
                    {
                        var projected = normal.X * p.X + normal.Y * p.Y;
                        if (minB == null || projected < minB)
                            minB = projected;
                        if (maxB == null || projected > maxB)
                            maxB = projected;
                    }

                    if (maxA < minB || maxB < minA)
                        return false;
                }
            }
            return true;
        }

        public static bool trianglesIntersect3(Triangle t0, Triangle t1)
        {
            var normal0 = (t0.b.X - t0.a.X) * (t0.c.Y - t0.a.Y) -
                          (t0.b.Y - t0.a.Y) * (t0.c.X - t0.a.X);
            var normal1 = (t1.b.X - t1.a.X) * (t1.c.Y - t1.a.Y) -
                      (t1.b.Y - t1.a.Y) * (t1.c.X - t1.a.X);

            return !(cross(t1, t0.a, t0.b, normal0) ||
              cross(t1, t0.b, t0.c, normal0) ||
              cross(t1, t0.c, t0.a, normal0) ||
              cross(t0, t1.a, t1.b, normal1) ||
              cross(t0, t1.b, t1.c, normal1) ||
              cross(t0, t1.c, t1.a, normal1));
        }

        public static bool cross (Triangle points, PointF b, PointF c, float normal)
        {
            var bx = b.X;
            var by = b.Y;
            var cyby = c.Y - by;
            var cxbx = c.X - bx;
            var pa = points.a;
            var pb = points.b;
            var pc = points.c;
            return !(
              (((pa.X - bx) * cyby - (pa.Y - by) * cxbx) * normal < 0) ||
              (((pb.X - bx) * cyby - (pb.Y - by) * cxbx) * normal < 0) ||
              (((pc.X - bx) * cyby - (pc.Y - by) * cxbx) * normal < 0));
        }
    }

    public class Triangle
    {
        public PointF a { get; set; }
        public PointF b { get; set; }
        public PointF c { get; set; }

        public Triangle(PointF a, PointF b, PointF c)
        {
            this.a = a;
            this.b = b;
            this.c = c;
        }
    }

    public class BoosterSmoke : RenderObject
    {
        public float DirectionR;
        public float Speed;

        private float def_x, def_y, def_angle;

        public BoosterSmoke(UiSettings uiSettings, float angle, float x, float y) :
            base(uiSettings)
        {
            def_angle = angle;
            DirectionR = angle * R.RADIANCONVERSION + 3.14159265359f + (((float)this.rand().NextDouble() - 0.5f) * (3.14159265359f / 3f));
            this.x = def_x = x; this.y = def_y = y;
            Speed = R.BOOSTER_ANIMATION_SPEED;
            dy = R.BOOSTER_ANIMATION_SPEED * (float)Math.Sin(DirectionR);
            dx = R.BOOSTER_ANIMATION_SPEED * (float)Math.Cos(DirectionR);
            mw = R.BOOSTER_SMOKE_SIZE; mh = R.BOOSTER_SMOKE_SIZE;
            brush = Brushes.Yellow;
        }

        public override void Reset()
        {
            this.x = def_x;
            this.y = def_y;
            DirectionR = def_angle * R.RADIANCONVERSION + 3.14159265359f + (((float)this.rand().NextDouble() - 0.5f) * (3.14159265359f / 3f));
            dy = R.BOOSTER_ANIMATION_SPEED * (float)Math.Sin(DirectionR);
            dx = R.BOOSTER_ANIMATION_SPEED * (float)Math.Cos(DirectionR);
            mw = R.BOOSTER_SMOKE_SIZE; mh = R.BOOSTER_SMOKE_SIZE;
        }

        public override void UpdateElement(int delta)
        {
            if (playState != 0)
            {
                gameTime += playState * delta / 1000.0f;
                mx += dx;
                my += dy;

                this.PeriodicBC();
            }
        }

        public override void DrawElement(Graphics g)
        {
            g.FillEllipse(brush, mx - w / 2, my - h / 2, w, h);
        }
    }

    public class Bullet : RenderObject
    {
        public float DirectionR;
        public float Speed;

        private float def_x, def_y, def_angle;

        public Bullet(UiSettings uiSettings, RectangleF rect, Color color) :
            base(uiSettings, rect, color)
        {

        }

        public Bullet(UiSettings uiSettings, float angle, float x, float y) :
            base(uiSettings)
        {
            def_angle = angle;
            DirectionR = angle * R.RADIANCONVERSION;
            this.x = def_x = x; this.y = def_y = y;
            Speed = R.BULLET_SPEED;
            dy = R.BULLET_SPEED * (float)Math.Sin(DirectionR);
            dx = R.BULLET_SPEED * (float)Math.Cos(DirectionR);
            mw = R.BULLET_CALIBRE;
            mh = R.BULLET_CALIBRE;
            brush = Brushes.Cyan;
        }

        public override void Reset()
        {
            this.x = def_x;
            this.y = def_y;
            DirectionR = def_angle * R.RADIANCONVERSION;
            dy = R.BULLET_SPEED * (float)Math.Sin(DirectionR);
            dx = R.BULLET_SPEED * (float)Math.Cos(DirectionR);
            mw = R.BULLET_CALIBRE;
            mh = R.BULLET_CALIBRE;
        }

        public override void UpdateElement(int delta)
        {
            if (playState != 0)
            {
                gameTime += playState * delta / 1000.0f;
                mx += dx;
                my += dy;

                this.PeriodicBC();
            }
        }

        public override void DrawElement(Graphics g)
        {
            g.FillEllipse(brush, mx - mw / 2, my - mh / 2, mw, mh);
        }
    }

    public enum BoardSide
    {
        Top,
        Bottom,
        Left,
        Right
    }

    public class Asteroid : RenderObject
    {
        public float DirectionR;
        public float Speed;
        public int Health;

        private float def_speed, def_angle;
        private BoardSide def_boardSide;
        private int def_health;


        protected static new int playState = 1;
        public static int PlayState { get => playState; set => playState = value; }

        public static int NoAst = 20;

        public Asteroid(UiSettings uiSettings, RectangleF rect, Color color) :
            base(uiSettings, rect, color)
        {

        }

        public Asteroid(UiSettings uiSettings, float angle, float speed, BoardSide side, int h) :
            base(uiSettings)
        {
            def_boardSide = side;
            def_speed = speed;
            def_angle = angle;
            def_health = h;
            switch (side)
            {
                case BoardSide.Top:
                    angle += 30;
                    y = 1;
                    x = this.rand().Next(uiSettings.Width);
                    break;
                case BoardSide.Bottom:
                    angle += 210;
                    y = uiSettings.Height - 1;
                    x = this.rand().Next(uiSettings.Width);
                    break;
                case BoardSide.Left:
                    angle += 300;
                    y = this.rand().Next(uiSettings.Height);
                    x = 0;
                    break;
                case BoardSide.Right:
                    angle += 120;
                    y = this.rand().Next(uiSettings.Height);
                    x = uiSettings.Width;
                    break;
                default:
                    break;
            }

            DirectionR = angle * R.RADIANCONVERSION;
            Speed = speed;
            dy = speed * (float)Math.Sin(DirectionR);
            dx = speed * (float)Math.Cos(DirectionR);
            mw = 35;
            mh = 35;
            brush = new SolidBrush(Color.FromArgb(180, Color.DarkOrange));

            Health = h;
        }

        public override void Reset()
        {
            DirectionR = def_angle * R.RADIANCONVERSION;
            Speed = def_speed;
            dy = def_speed * (float)Math.Sin(DirectionR);
            dx = def_speed * (float)Math.Cos(DirectionR);
            mw = 35;
            mh = 35;
            Health = def_health;
            gameTime = 0;
        }

        public override void UpdateElement(int delta)
        {
            if (PlayState == 0 && gameTime < R.AST_HALTAFTER)
            {
            }
            else
            {
                this.PeriodicBC();

                mx += dx;
                my += dy;

                w = Health * 40;
                h = Health * 40;
            }
            gameTime += delta / 1000.0f;
        }

        public override void DrawElement(Graphics g)
        {
            if (Health > 0)
                g.FillRectangle(brush, mx - mw / 2, my - mh / 2, mw, mh);
        }

        public void TakeDamage(int dmg)
        {
            this.Health = Health - dmg;
            this.Speed = Speed + dmg * 3;
        }
    }

    public class NetworkGameObjects : GameObjectCollection
    {
        private Spaceship spaceship;
        private Asteroid[] asteroidList = new Asteroid[R.MAX_ASTEROIDS];
        private volatile List<RenderObject> objs;

        public Spaceship Spaceship { get => spaceship; set => spaceship = value; }
        public Asteroid[] Asteroids { get => asteroidList; set => asteroidList = value; }

        private Random random = new Random();

        public NetworkGameObjects(int index, UiSettings uiSettings, int species, int fbnodecount) : base(index, uiSettings, species, fbnodecount)
        {
            objs = new List<RenderObject>();
            spaceship = new Spaceship(uiSettings, new RectangleF(30, 30, 30, 30), Color.White);
            spaceship.SetIndex(index);
            spaceship.Colour = Color.FromArgb(species);
        }


        public override void UpdateElements(int delta, UiSettings settings)
        {
            spaceship.UpdateElement(delta);
            objs.Clear();
            for (int i = 0; i < R.BULLET_LIMIT; i++)
            {
                if (spaceship.bulletList[i] != null)
                {
                    spaceship.bulletList[i].UpdateElement(delta);
                    objs.Add(spaceship.bulletList[i]);
                }
            }

            for (int i = 0; i < R.BOOSTER_ANIM_LIMIT; i++)
            {
                if (spaceship.boosterList[i] != null)
                {
                    spaceship.boosterList[i].UpdateElement(delta);
                    objs.Add(spaceship.boosterList[i]);
                }
            }

            for (int i = 0; i < R.MAX_ASTEROIDS; i++)
            {
                if (asteroidList[i] == null)
                {
                    double d = random.NextDouble();
                    if (d > 0.99)
                    {
                        BoardSide b;
                        Array boardSides = Enum.GetValues(typeof(BoardSide));
                        b = (BoardSide)boardSides.GetValue(random.Next(boardSides.Length));

                        Asteroid a = new Asteroid(settings,
                            random.Next(120),
                            (float)random.NextDouble() * (R.AST_MAX_SPEED - 2) + 2,
                            b,
                            random.Next(1, 4));
                        asteroidList[i] = a;
                    }
                }

                if (asteroidList[i] != null)
                {
                    asteroidList[i].UpdateElement(delta);
                }
            }

            Collisions.BltAstLstC(spaceship.bulletList, asteroidList, spaceship);

            for (int i = 0; i < asteroidList.Length; i++)
            {
                if (Collisions.RectangleCollisionBool(spaceship, asteroidList[i]))
                {
                    NewGame();
                    spaceship.Score = (int)(spaceship.Score * 100 * (1 - R.PTS_DEATH_PEN)) / 100;
                    spaceship.LastHitScore = (int)(spaceship.LastHitScore * 100 * (1 - R.PTS_DEATH_PEN)) / 100;
                }

                if (asteroidList[i] != null)
                {
                    objs.Add(asteroidList[i]);
                }
            }
        }
        public void NewGame()
        {
            ResetAll();

        }
        public override void ResetAll()
        {
            spaceship.Reset();
            asteroidList = new Asteroid[R.MAX_ASTEROIDS];
            base.ResetAll();
        }

        public override void SetIndex(int index)
        {
            spaceship.SetIndex(index);
        }

        public override void SetChild()
        {
        }

        public override void SetSpecies(int species)
        {
            spaceship.Colour = Color.FromArgb(species);
            base.SetSpecies(species);
        }

        public override List<RenderObject> GetItems()
        {
            List<RenderObject> list = new List<RenderObject>() { Spaceship };
            var add = objs.Where(o => o != null).ToList();
            list.AddRange(add);
            return list;
        }

        public override void BeginGeneration()
        {
            spaceship.Score = 0;
        }
    }

    public class CompetitiveNetworkGameObjects : GameObjectCollection
    {
        private Spaceship spaceship;
        private Spaceship spaceship1;
        private Asteroid[] asteroidList = new Asteroid[R.MAX_ASTEROIDS];
        private volatile List<RenderObject> objs;
        
        public Spaceship Spaceship { get => spaceship; set => spaceship = value; }
        public Spaceship Spaceship1 { get => spaceship1; set => spaceship1 = value; }
        public Asteroid[] Asteroids { get => asteroidList; set => asteroidList = value; }

        private Random random = new Random();

        public CompetitiveNetworkGameObjects(int index, UiSettings uiSettings, int species, int fbnodecount) : base(index, uiSettings, species, fbnodecount)
        {
            objs = new List<RenderObject>();
            spaceship = new Spaceship(uiSettings, new RectangleF(30, 30, 30, 30), Color.White);
            spaceship.SetIndex(index);
            spaceship.Colour = Color.FromArgb(species);
            spaceship1 = new Spaceship(uiSettings, new RectangleF(30, 30, 30, 30), Color.White);
            spaceship1.SetIndex(index);
            spaceship1.Colour = Color.FromArgb(species);
            spaceship.EnemySpaceship = spaceship1;
            spaceship1.EnemySpaceship = spaceship;
        }


        public override void UpdateElements(int delta, UiSettings settings)
        {
            spaceship.UpdateElement(delta);
            spaceship1.UpdateElement(delta);
            objs.Clear();
            for (int i = 0; i < R.BULLET_LIMIT; i++)
            {
                if (spaceship.bulletList[i] != null)
                {
                    spaceship.bulletList[i].UpdateElement(delta);
                    objs.Add(spaceship.bulletList[i]);
                }
                if (spaceship1.bulletList[i] != null)
                {
                    spaceship1.bulletList[i].UpdateElement(delta);
                    objs.Add(spaceship1.bulletList[i]);
                }
            }

            for (int i = 0; i < R.BOOSTER_ANIM_LIMIT; i++)
            {
                if (spaceship.boosterList[i] != null)
                {
                    spaceship.boosterList[i].UpdateElement(delta);
                    objs.Add(spaceship.boosterList[i]);
                }
                if (spaceship1.boosterList[i] != null)
                {
                    spaceship1.boosterList[i].UpdateElement(delta);
                    objs.Add(spaceship1.boosterList[i]);
                }
            }

            for (int i = 0; i < R.MAX_ASTEROIDS; i++)
            {
                if (asteroidList[i] == null)
                {
                    double d = random.NextDouble();
                    if (d > 0.99)
                    {
                        BoardSide b;
                        Array boardSides = Enum.GetValues(typeof(BoardSide));
                        b = (BoardSide)boardSides.GetValue(random.Next(boardSides.Length));

                        Asteroid a = new Asteroid(settings,
                            random.Next(120),
                            (float)random.NextDouble()*(R.AST_MAX_SPEED - 2) + 2,
                            b,
                            random.Next(1, 4));
                        asteroidList[i] = a;
                    }
                }

                if (asteroidList[i] != null)
                {
                    asteroidList[i].UpdateElement(delta);
                }
            }

            Collisions.BltAstLstC(spaceship.bulletList, asteroidList, spaceship);
            Collisions.BltAstLstC(spaceship1.bulletList, asteroidList, spaceship1);
            Collisions.BltSpcLstC(spaceship.bulletList, spaceship, spaceship1);
            Collisions.BltSpcLstC(spaceship1.bulletList, spaceship1, spaceship);

            for (int i = 0; i < asteroidList.Length; i++)
            {
                if (Collisions.RectangleCollisionBool(spaceship, asteroidList[i]))
                {
                    NewGame();
                    spaceship.Score = (int)(spaceship.Score * 100 * (1 - R.PTS_DEATH_PEN)) / 100;
                    spaceship.LastHitScore = (int)(spaceship.LastHitScore * 100 * (1 - R.PTS_DEATH_PEN)) / 100;
                }
                if (Collisions.RectangleCollisionBool(spaceship1, asteroidList[i]))
                {
                    NewGame();
                    spaceship1.Score = (int)(spaceship1.Score * 100 * (1 - R.PTS_DEATH_PEN)) / 100;
                    spaceship1.LastHitScore = (int)(spaceship1.LastHitScore * 100 * (1 - R.PTS_DEATH_PEN)) / 100;
                }

                if (asteroidList[i] != null)
                {
                    objs.Add(asteroidList[i]);
                }
            }
        }
        public void NewGame()
        {
            ResetAll();

        }
        public override void ResetAll()
        {
            spaceship.Reset();
            spaceship1.Reset(30, 30);
            asteroidList = new Asteroid[R.MAX_ASTEROIDS];
            base.ResetAll();
        }

        public override void SetIndex(int index)
        {
            spaceship.SetIndex(index);
        }

        public override void SetChild()
        {
        }

        public override void SetSpecies(int species)
        {
            spaceship.Colour = Color.FromArgb(species);
            spaceship1.Colour = Color.FromArgb(-species);
            base.SetSpecies(species);
        }

        public override List<RenderObject> GetItems()
        {
            List<RenderObject> list = new List<RenderObject>() { Spaceship, spaceship1 };
            var add = objs.Where(o => o != null).ToList();
            list.AddRange(add);
            return list;
        }

        public override void BeginGeneration()
        {
            spaceship.Score = 0;
            spaceship1.Score = 0;
        }
    }

    static class Collisions
    {
        public static void BulletAsteroidC(Bullet b, Asteroid a)
        {
            float xDiff = Math.Abs(a.x - b.x);
            float yDiff = Math.Abs(a.y - b.y);
            if (xDiff < (a.w + b.w) / 2 && yDiff < (a.h + b.h) / 2)
            {
                a.Health--;
            }
        }



        public static void BltAstLstC(Bullet[] bList, Asteroid[] aList, Spaceship ship)
        {
            int bulletMax = bList.Length;
            int asteroidMax = aList.Length;

            for (int i = 0; i < bulletMax; i++)
            {
                for (int j = 0; j < asteroidMax; j++)
                {
                    if (RectangleCollisionBool(aList[j], bList[i]))
                    {
                        ship.LastHitScore = aList[j].Health;
                        ship.Score += aList[j].Health;
                        aList[j].TakeDamage(1);
                        bList[i] = null;
                        Spaceship.ammo++;
                        if (aList[j].Health < 1) aList[j] = null;
                    }
                }
            }
        }
        public static void BltSpcLstC(Bullet[] bList, Spaceship ship, Spaceship ship1)
        {
            int bulletMax = bList.Length;

            for (int i = 0; i < bulletMax; i++)
            {
                if (RectangleCollisionBool(ship1, bList[i]))
                {
                    ship.Score += 5;
                    ship1.Score -= 5;
                    bList[i] = null;
                    Spaceship.ammo++;
                }
            }
        }

        public static bool RectangleCollisionBool(RenderObject a, RenderObject b)
        {
            if (a != null && b != null)
            {
                float xDiff = Math.Abs(a.x - b.x);
                float yDiff = Math.Abs(a.y - b.y);
                if (xDiff < (a.w + b.w) / 2 && yDiff < (a.h + b.h) / 2)
                {
                    return true;
                }
            }
            return false;
        }


    }
}
