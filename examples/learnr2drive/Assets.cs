using Learnr;
using System.Drawing;
using static learnr2drive.Global;
using System.Collections.Generic;
using System;
using System.Windows.Forms;
using System.Linq;

namespace learnr2drive
{
    public class Car : RenderObject
    {

        private const double DEG2RAD = Math.PI / 180.0;
        private KeyStateManager keyManager;

        private Map map;

        internal KeyStateManager KeyManager { get => keyManager; set => keyManager = value; }
        public float ForwardThrottle { get => forwardThrottle; set => forwardThrottle = value; }
        public float ForwardDirection { get => forwardDirection; set => forwardDirection = value; }
        public float ForwardSteerRate { get => forwardSteerRate; set => forwardSteerRate = value; }
        public double DistanceTravelled { get => distanceTravelled; set => distanceTravelled = value; }
        public List<Tuple<int, int>[]> SensorData { get => sensorData; set => sensorData = value; }
        public int SensorDataLength { get => sensorData.First().Length; }
        public double BestDistanceTravelled { get => bestDistanceTravelled; set => bestDistanceTravelled = value; }
        public static bool DisplayVisionLines { get => displayVisionLines; set => displayVisionLines = value; }
        public PointF MovingAveragePosition { get => movingAveragePosition; set => movingAveragePosition = value; }

        private static bool displayVisionLines = R.CAR_RENDER_VISIONLINESDISPLAYED;

        private float initial_mx, initial_my;
        double distanceTravelled = 0;
        double bestDistanceTravelled = 0;
        PointF movingAveragePosition = PointF.Empty;

        float costheta;
        float sintheta;
        float sinthetaL;
        float costhetaL;
        float costhetaR;
        float sinthetaR;
        float sinthetaL1;
        float costhetaL1;
        float costhetaR1;
        float sinthetaR1;
        
        List<Tuple<int, int>[]> sensorData;

        private float forwardSpeed = Global.R.CAR_INIT_VELOCITY;
        private float forwardThrottle = 0;
        private float forwardDirection = Global.R.CAR_INIT_ANGLE;
        private float forwardSteerRate = 0;// (deg) where +ve x-axis is 0 degress


        public Car(UiSettings uiSettings) : base(uiSettings)
        {
            Init();
        }
        public Car(UiSettings uiSettings, RectangleF bounds, Color color, Map map) : base(uiSettings, bounds, color)
        {
            this.map = map;
            Init();
        }

        private void Init()
        {
            keyManager = new KeyStateManager(Keys.Left, Keys.Right, Keys.Up, Keys.Down);
            int maxpts = (int)Math.Ceiling(Global.R.CAR_RENDER_VISIONLINESRANGE / Global.R.CAR_RENDER_VISIONLINESRESOLUTION);
            
            sensorData = new List<Tuple<int, int>[]>();
            sensorData.AddRange(new Tuple<int, int>[][] { new Tuple<int, int>[maxpts], new Tuple<int, int>[maxpts] });

            for (int sensor = 0; sensor < sensorData.Count; sensor++)
            {
                for (int i = 0; i < maxpts; i++)
                {
                    sensorData[sensor][i] = new Tuple<int, int>(0, 0);
                }
            }
            initial_mx = mx;
            initial_my = my;
            Reset();
        }
        
        public override void UpdateElement(int delta)
        {
            if (playState != 0)
            {
                float timescale = delta / 10f;

                forwardDirection += forwardSteerRate * forwardSpeed;
                costheta = (float)Math.Cos(forwardDirection * DEG2RAD);
                sintheta = (float)Math.Sin(forwardDirection * DEG2RAD);
                float lookdeg = Global.R.CAR_RENDER_VISIONLINESANGLE / 2;
                float lookdeg1 = Global.R.CAR_RENDER_VISIONLINESANGLE1 / 2;
                float dir = forwardDirection;

                costhetaL = (float)Math.Cos((dir + lookdeg) * DEG2RAD);
                sinthetaL = (float)Math.Sin((dir + lookdeg) * DEG2RAD);
                costhetaR = (float)Math.Cos((dir - lookdeg) * DEG2RAD);
                sinthetaR = (float)Math.Sin((dir - lookdeg) * DEG2RAD);
                costhetaL1 = (float)Math.Cos((dir + lookdeg1) * DEG2RAD);
                sinthetaL1 = (float)Math.Sin((dir + lookdeg1) * DEG2RAD);
                costhetaR1 = (float)Math.Cos((dir - lookdeg1) * DEG2RAD);
                sinthetaR1 = (float)Math.Sin((dir - lookdeg1) * DEG2RAD);
                ComputeSensors();

                if (Global.R.CAR_DRIVE_EXPERIMENTAL)
                {
                    dx += forwardThrottle * costheta * timescale;
                    dy += forwardThrottle * -sintheta * timescale;

                    float F1_x = Global.R.CAR_SPEED_DRAG, F1_y = Global.R.CAR_SPEED_TYREFRICTION;
                    float clampcos = Math.Abs(costheta);
                    float clampsin = Math.Abs(sintheta);

                    float F_x = F1_x * clampcos - F1_y * clampsin, F_y = F1_x * clampsin + F1_y * clampcos;

                    float ax = Math.Abs(F_x * clampcos - F_y * clampsin), ay = Math.Abs(F_x * clampsin + F_y * clampcos);

                    dx *= Math.Max(Math.Min(1 - ax, 1), 0);
                    dy *= Math.Max(Math.Min(1 - ay, 1), 0);
                    var v = Math.Sqrt(dx * dx + dy * dy);
                    forwardSpeed = (float)v;
                    distanceTravelled += v * timescale / 10;
                }
                else
                {

                    forwardSpeed += forwardThrottle / 3f;

                    dx = forwardSpeed * (float)Math.Cos(forwardDirection * DEG2RAD) * timescale;
                    dy = forwardSpeed * -(float)Math.Sin(forwardDirection * DEG2RAD) * timescale;

                    forwardSpeed *= 1 - Global.R.CAR_SPEED_DRAG - Global.R.CAR_SPEED_STEERDRAG * (Math.Abs(forwardSteerRate) / Global.R.CAR_STEER_MAXRATE);

                    distanceTravelled += Math.Abs(forwardSpeed * timescale / 10);
                }

                if (map.CellStateFromCoodinates(mx, my) > 0)
                {
                    bestDistanceTravelled = Math.Max(distanceTravelled, bestDistanceTravelled);
                    
                    distanceTravelled = 0;
                }

                gameTime += playState * delta / 1000.0f;

                if (mx + dx < 0 && mx >= 0) dx *= -1;
                if (mx + dx >= uiSettings.Width && mx < uiSettings.Width) dx *= -1;
                if (my + dy < 0 && my >= 0) dy *= -1;
                if (my + dy >= uiSettings.Height && my < uiSettings.Height) dy *= -1;

                mx += dx;
                my += dy;

                float movAveAlpha = 1 / (1 + Global.R.CAR_SCORE_GOINGNOWHEREPENELTY);

                movingAveragePosition.X += movAveAlpha * (mx - movingAveragePosition.X);
                movingAveragePosition.Y += movAveAlpha * (my - movingAveragePosition.Y);
            }
        }

        private void ComputeSensors()
        {
            for (int i = 0; i < SensorDataLength; i++)
            {
                float h = Global.R.CAR_RENDER_VISIONLINESRANGE * i / (float)SensorDataLength;
                sensorData[0][i] = new Tuple<int, int>(
                    map.CellStateFromCoodinates(mx + h * costhetaL, my - h * sinthetaL),
                    map.CellStateFromCoodinates(mx + h * costhetaR, my - h * sinthetaR)
                    );
                float h1 = Global.R.CAR_RENDER_VISIONLINESRANGE1 * i / (float)SensorDataLength;
                sensorData[1][i] = new Tuple<int, int>(
                    map.CellStateFromCoodinates(mx + h1 * costhetaL1, my - h1 * sinthetaL1),
                    map.CellStateFromCoodinates(mx + h1 * costhetaR1, my - h1 * sinthetaR1)
                    );
            }
        }

        public override void Reset()
        {
            dx = 0;
            dy = 0;
            if (R.CAR_SPAWN_RANDOMPOS_ENABLED)
            {
                int tries = 0;
                int cellX = 1, cellY = 1;
                while(tries++ < R.CAR_SPAWN_RANDOMPOS_MAXATTEMPTS)
                {
                    if (map.CellStateFromRandomCell(out cellX, out cellY) < 1) break;
                }
                var pos = map.GetCellCenterPoint(cellX, cellY);
                mx = pos.X; my = pos.Y;
            }
            else
            {
                mx = initial_mx;
                my = initial_my;
            }
            forwardThrottle = 0;
            forwardSpeed = 0;
            distanceTravelled = 0;
            bestDistanceTravelled = 0;
            forwardDirection = Global.R.CAR_INIT_ANGLE;
        }

        private void HandleKeys()
        {
            if (keyManager.GetState(Keys.Up))
            {
                forwardSpeed += Global.R.CAR_SPEED_MAXACCELERATION;
                forwardThrottle = Global.R.CAR_SPEED_MAXACCELERATION;
            }
            else
            if (keyManager.GetState(Keys.Down))
            {
                forwardSpeed = forwardSpeed > 0.1 ? forwardSpeed * (1 - Global.R.CAR_SPEED_BRAKESDRAG) : forwardSpeed - Global.R.CAR_REV_MAXACCELERATION;
                forwardThrottle = forwardSpeed > 0.1 ? forwardSpeed * (-Global.R.CAR_SPEED_BRAKESDRAG) : -Global.R.CAR_REV_MAXACCELERATION;
            }
            else forwardThrottle = 0;
            if (keyManager.GetState(Keys.Left))
            {
                forwardSteerRate = Global.R.CAR_STEER_MAXRATE;
            }
            else
            if (keyManager.GetState(Keys.Right))
            {
                forwardSteerRate = -Global.R.CAR_STEER_MAXRATE;
            }
            else
                forwardSteerRate = 0;
        }

        public override void DrawElement(Graphics g)
        {
            var rect = new RectangleF(mx - w / 2, my - h / 2, w, h);
            if (DisplayVisionLines)
            {
                PointF[] sensorsL = new PointF[] { new PointF(mx, my), new PointF(mx, my) };
                PointF[] sensorsR = new PointF[] { new PointF(mx, my), new PointF(mx, my) };
                for (int i = 0; i < SensorDataLength; i++)
                {
                    
                    float h = Global.R.CAR_RENDER_VISIONLINESRANGE * i / (float)SensorDataLength;
                    float r = 2;
                    if (i > 0 && sensorData[0][i - 1].Item1 < 1) g.FillEllipse(Brushes.Red, (sensorsL[0].X = mx + h * costhetaL) - r, (sensorsL[0].Y = my - h * sinthetaL) - r, r * 2, r * 2);
                    if (i > 0 && sensorData[0][i - 1].Item2 < 1) g.FillEllipse(Brushes.Red, (sensorsR[0].X = mx + h * costhetaR) - r, (sensorsR[0].Y = my - h * sinthetaR) - r, r * 2, r * 2);
                }
                for (int i = 0; i < SensorDataLength; i++)
                {
                    float h = Global.R.CAR_RENDER_VISIONLINESRANGE1 * i / (float)SensorDataLength;
                    float r = 2;
                    if (i > 0 && sensorData[1][i - 1].Item1 < 1) g.FillEllipse(Brushes.Red, (sensorsL[1].X = mx + h * costhetaL1) - r, (sensorsL[1].Y = my - h * sinthetaL1) - r, r * 2, r * 2);
                    if (i > 0 && sensorData[1][i - 1].Item2 < 1) g.FillEllipse(Brushes.Red, (sensorsR[1].X = mx + h * costhetaR1) - r, (sensorsR[1].Y = my - h * sinthetaR1) - r, r * 2, r * 2);
                    else if (i > 0 && sensorData[1][i - 1].Item1 > 0) break;
                }

                g.DrawLine(Pens.DarkRed, new PointF(mx, my), sensorsL[0]);
                g.DrawLine(Pens.DarkRed, new PointF(mx, my), sensorsR[0]);
                g.DrawLine(Pens.DarkRed, new PointF(mx, my), sensorsL[1]);
                g.DrawLine(Pens.DarkRed, new PointF(mx, my), sensorsR[1]);
            }
            g.FillEllipse(brush, rect);
            g.FillPie(GetWhiteBrushFromAlpha(80), Rectangle.Truncate(rect), -forwardDirection - Global.R.CAR_RENDER_INDICATORANGLE / 2, Global.R.CAR_RENDER_INDICATORANGLE);
        }
    }


    public class KeyStateManager
    {
        private List<bool> keysState = new List<bool>();
        private List<Keys> keyList = new List<Keys>();
        internal bool[] keys { get => keysState.ToArray(); }

        public KeyStateManager(params Keys[] handledKeys)
        {
            keyList.AddRange(handledKeys);
            for (int i = 0; i < handledKeys.Length; i++)
            {
                keysState.Add(false);
            }
        }

        public void Update(KeyEventArgs e, bool keyDown)
        {
            int index = keyList.IndexOf(e.KeyCode);
            if (index >= 0)
            {
                keysState[index] = keyDown;
            }
        }
        public bool GetState(Keys key)
        {
            int index = keyList.IndexOf(key);
            return keysState[index];
        }
    }

    public class Map : RenderObject
    {
        private const int UTILS_RANDOM_SEED = 4;
        private const float MAP_INIT_FILLPERCENT = 45;

        private const double DEG2RAD = Math.PI / 180.0;

        int cells_w, cells_h;
        int[,] grid;
        int[,] savedgrid;

        public static int[,] initgrid = null;
        private static int seedindex = 0;
        public Random rand;

        int p_count = 0, p_parity = 0, p_parity1 = 0;

        public int[,] Grid { get => grid; set => grid = value; }

        public Map(UiSettings uiSettings) : base(uiSettings)
        {
            cells_w = 10;
            cells_h = 10;
            Init();
        }
        public Map(UiSettings uiSettings, RectangleF bounds, Color color, int cell_w, int cell_h) : base(uiSettings, bounds, color)
        {
            cells_w = cell_w;
            cells_h = cell_h;
            Init();
        }

        private void Init()
        {
            grid = new int[cells_w, cells_h];
            savedgrid = new int[cells_w, cells_h];
            rand = new Random(UTILS_RANDOM_SEED + seedindex++);
            if (initgrid == null)
            {
                GernerateRandom();
                initgrid = new int[cells_w, cells_h];
                CopyGrid(grid, ref initgrid);
                CopyGrid(savedgrid, ref initgrid);
            }
            else
                CopyFrom(initgrid);
        }

        public void CopyFrom(int[,] sourceGrid)
        {
            CopyGrid(sourceGrid, ref grid);
        }

        private void CopyGrid(int[,] sourceGrid, ref int[,] destinationGrid)
        {
            for (int x = 0; x < cells_w; x++)
            {
                for (int y = 0; y < cells_h; y++)
                {
                    destinationGrid[x, y] = sourceGrid[x, y];
                }
            }
        }

        public void GernerateRandom()
        {
            for (int x = 0; x < cells_w; x++)
            {
                for (int y = 0; y < cells_h; y++)
                {
                    if (x <= 0 || x >= cells_w - 1 || y <= 0 || y >= cells_h - 1) grid[x, y] = savedgrid[x, y] = 1;
                    else
                    {
                        grid[x, y] = savedgrid[x, y] = rand.NextDouble() < MAP_INIT_FILLPERCENT / 100f ? 1 : 0;
                    }
                }
            }
        }

        public int CellStateFromCoodinates(float screenX, float screenY)
        {
            int cellX = (int)(cells_w * screenX / (float)uiSettings.Width);
            int cellY = (int)(cells_h * screenY / (float)uiSettings.Height);
            if (cellX < 0 || cellX > cells_w - 1) return 1;
            if (cellY < 0 || cellY > cells_h - 1) return 1;
            return grid[cellX, cellY];
        }
        public int[] CellPositionFromCoodinates(float screenX, float screenY)
        {
            int cellX = (int)(cells_w * screenX / (float)uiSettings.Width);
            cellX = (cellX < 0) ? 0 : (cellX >= cells_w ? cells_w - 1 : cellX); // Clamp to grip
            int cellY = (int)(cells_h * screenY / (float)uiSettings.Height);
            cellY = (cellY < 0) ? 0 : (cellY >= cells_h ? cells_h - 1 : cellY); // Clamp to grip
            return new int[] { cellX, cellY };
        }

        public int CellStateFromRandomCell(out int cellX, out int cellY)
        {
            cellX = rand.Next(cells_w);
            cellY = rand.Next(cells_h);
            return grid[cellX, cellY];
        }
        public void SetCellStateFromCell(int cellX, int cellY, int value)
        {
            grid[cellX, cellY] = value;
        }

        public PointF GetCellCenterPoint(int cellX, int cellY) => new PointF(uiSettings.Width / (float)cells_w * (cellX + 0.5f), uiSettings.Height / (float)cells_h * (cellY + 0.5f));

        public override void UpdateElement(int delta)
        {
            if (playState != 0)
            {
                float timescale = delta / 10f;


                gameTime += playState * delta / 1000.0f;
            }
        }

        public override void Reset()
        {
            CopyGrid(savedgrid, ref grid);
        }

        public void Smooth()
        {
            int maxtries = 100;
            int itterations = 0;
            while (!SmoothOnce())
            {
                if (itterations++ > maxtries) break;
            }
            CopyGrid(grid, ref savedgrid);
        }

        public bool SmoothOnce()
        {
            int count = 0, parity = 0, parity1 = 0;
            const int PARITY_LEVEL = 6;

            int[,] n_grid = new int[cells_w, cells_h];
            for (int x = 0; x < cells_w; x++)
            {
                for (int y = 0; y < cells_h; y++)
                {
                    int neighbourCount = 0;
                    for (int cellx = x - 1; cellx <= x + 1; cellx++)
                    {
                        for (int celly = y - 1; celly <= y + 1; celly++)
                        {
                            if (cellx == x && celly == y) continue;
                            if (cellx <= 0 || cellx >= cells_w - 1 || celly <= 0 || celly >= cells_h - 1) neighbourCount++;
                            else neighbourCount += grid[cellx, celly] > 0 ? 1 : 0;
                        }
                    }
                    n_grid[x, y] = neighbourCount;

                }
            }
            for (int x = 0; x < cells_w; x++)
            {
                long ppar = 0;
                for (int y = 0; y < cells_h; y++)
                {
                    var neighbourCount = n_grid[x, y];
                    if (neighbourCount > 4) grid[x, y] = 1;
                    else if (neighbourCount < 4) grid[x, y] = 0;

                    parity += (1 << (x % PARITY_LEVEL));
                    parity += (1 << ((y % PARITY_LEVEL) + PARITY_LEVEL));
                    ppar += grid[x, y] > 0 ? 1 << (x % 50) : 0;
                    count += grid[x, y] > 0 ? 1 : 0;
                }
                parity1 = (int)(parity1 ^ ppar);
            }
            bool smoothingFinished = p_parity1 == parity1;
            p_count = count; p_parity = parity; p_parity1 = parity1;
            return smoothingFinished;
        }

        public void SmoothLegacy()
        {
            for (int x = 0; x < cells_w; x++)
            {
                for (int y = 0; y < cells_h; y++)
                {
                    int neighbourCount = 0;
                    for (int cellx = x - 1; cellx <= x + 1; cellx++)
                    {
                        for (int celly = y - 1; celly <= y + 1; celly++)
                        {
                            if (cellx == x && celly == y) continue;
                            if (cellx <= 0 || cellx >= cells_w - 1 || celly <= 0 || celly >= cells_h - 1) neighbourCount++;
                            else neighbourCount += grid[cellx, celly] > 0 ? 1 : 0;
                        }
                    }

                    if (neighbourCount > 4) grid[x, y] = 1;
                    else if (neighbourCount < 4) grid[x, y] = 0;
                }
            }
        }

        public override void DrawElement(Graphics g)
        {
            var cw = uiSettings.Width / (float)cells_w;
            var ch = uiSettings.Height / (float)cells_h;
            for (int x = 0; x < cells_w; x++)
            {
                for (int y = 0; y < cells_h; y++)
                {
                    if (grid[x, y] > 0)
                    {
                        g.FillRectangle(brush, new RectangleF(cw * x, ch * y, cw, ch));
                    }
                }
            }
        }
    }



    public class NetworkGameObjects : GameObjectCollection
    {
        private Car car;
        private Map map;

        public Car Car { get => car; set => car = value; }
        public Map Map { get => map; set => map = value; }

        public NetworkGameObjects(int index, UiSettings uiSettings, int species, int fbnodecount) : base(index, uiSettings, species, fbnodecount)
        {
            const int gridsize = 80;
            map = new Map(uiSettings, new RectangleF(0, 0, 10, 10), Color.Gray, gridsize, (int)(gridsize * (uiSettings.Height / (double)uiSettings.Width)));
            map.Smooth();
            car = new Car(uiSettings, new RectangleF(0, 0, 10, 10), Color.BlueViolet, map);
            map.SetIndex(index);
            car.SetIndex(index);
        }

        public override void ResetAll()
        {
            Car.Reset();
            Map.Reset();
            base.ResetAll();
        }

        public override void UpdateElements(int delta, UiSettings settings)
        {
            Car.UpdateElement(delta);
        }

        public override void SetIndex(int index)
        {
            map.SetIndex(index);
            car.SetIndex(index);
        }

        public override void SetChild()
        {

        }

        public override void SetSpecies(int species)
        {
            base.SetSpecies(species);
        }

        public override List<RenderObject> GetItems()
        {
            return new List<RenderObject>() { map, car };
        }

        public override void BeginGeneration()
        {

        }
    }
}
