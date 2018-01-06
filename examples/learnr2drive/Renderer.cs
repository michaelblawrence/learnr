using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Learnr;
using static learnr2drive.Global;
using System.Linq;

namespace learnr2drive
{
    public class Renderer
    {
        #region private vars

        private NetworkManagerLSTM manager;
        private UiSettings uiSettings;

        private int graphDrawIndex;
        private float aveFrameDelta;
        private bool DisplayNetworkNodes = true;
        private bool DisplayNetworkInfo = true;
        private bool DepositBoxes = true;
        #endregion

        #region init

        public Renderer(UiSettings uiSettings)
        {
            this.uiSettings = uiSettings;
            this.uiSettings.uiSettingsChanged += MessageRecieved;
            initComputationObjects();
            initDisplayObjects();
        }

        private void initComputationObjects()
        {

            manager = new NetworkManagerLSTM(uiSettings);
            manager.NetworkGenerated += Manager_NetworkGeneratedLSTM;
            manager.NewNetworkRequested += ManagerLSTM_NewNetworkRequested;
            manager.NetworkChildRequested += ManagerLSTM_NetworkChildRequested;
            manager.LoadCompleted += Manager_LoadCompleted;
            manager.FrameRequested += ProcessSingleNetworkFrame;
            manager.Load();
        }

        private void initDisplayObjects()
        {
            aveFrameDelta = 1000 / GlobalVars.R.ComputeFrequency;
            graphDrawIndex = NetworkManagerLSTM.GraphDataTypes.Length - 1;

            manager.DisplayHeatMap = false;
            manager.DisplayDynamicAxons = true;
        }

        #endregion

        #region network generation
        
        private void Manager_LoadCompleted(object sender, EventArgs e)
        {

        }


        private NeuralNetworkLSTM ManagerLSTM_NewNetworkRequested(int inputs, int[] layers, int outputs, int dob)
        {
            return new NetworkLSTM(inputs, layers, outputs, dob).Generate(); ;
        }
        private NeuralNetworkLSTM ManagerLSTM_NetworkChildRequested(NeuralNetworkLSTM parent, int seed, int dob, bool mutate)
        {
            return new NetworkLSTM((NetworkLSTM)parent, seed, dob, mutate);
        }
        private void Manager_NetworkGeneratedLSTM(object sender, NetworkLSTMChangedEventArgs e)
        {

        }


        #endregion


        private void MessageRecieved(object sender, UiSettingsEventArgs e)
        {
            KeyEventArgs eventArgsK;
            MouseEventArgs eventArgsM;
            if ((e.Flags & UiSettingsEventArgs.FLAGS_UI) != 0)
                switch (e.Type)
                {
                    case UiSettingsEventArgs.EventType.KeyUpEvent:
                        eventArgsK = (KeyEventArgs)(e.EventInfo);
                        if (manager.HandleKeyPress(eventArgsK.KeyData)) break;
                        switch (eventArgsK.KeyData)
                        {
                            case Keys.T:
                                PreviewGeneration(false);
                                break;
                            case Keys.U:
                                PreviewGeneration(true);
                                break;
                            case Keys.L:
                                Car.DisplayVisionLines = !Car.DisplayVisionLines;
                                break;
                            case Keys.Q:
                                DisplayNetworkNodes = !DisplayNetworkNodes;
                                break;
                            case Keys.I:
                                DisplayNetworkInfo = !DisplayNetworkInfo;
                                break;
                            case Keys.A:
                                DepositBoxes = !DepositBoxes;
                                break;
                            case Keys.M:
                                CreateNewMap();
                                break;
                            case Keys.F3:
                                if (!manager.IsThreading)
                                    GameWindow.SaveNetwork(manager.CurrentNetwork);
                                break;
                            case Keys.F4:
                                if (!manager.IsThreading)
                                {
                                    string path = GameWindow.LoadNetworkPath();
                                    if (path != null)
                                        path.Split('\n').ToList().ForEach(s => manager.LoadNetworkFromAxonXML(s));
                                }
                                break;
                            case Keys.F5:
                                GameWindow.LoadGlobals(true);
                                manager.Networks.ForEach(n => { n.BestFitness *= 0.33; n.InheritedBestFitness *= 0.33; });
                                break;
                            case Keys.F11:
                                GameWindow.LoadGlobals();
                                manager.Networks.ForEach(n => { n.BestFitness *= 0.33; n.InheritedBestFitness *= 0.33; });
                                break;
                            case Keys.F12:
                                GameWindow.SaveGlobals();
                                break;
                        }
                        break;
                    case UiSettingsEventArgs.EventType.MouseEvent:
                        if (e.EventInfo.GetType() == typeof(MouseEventArgs))
                        {
                            eventArgsM = (MouseEventArgs)(e.EventInfo);
                            PointF mousept = new PointF(eventArgsM.X, eventArgsM.Y);

                            if ((e.Flags & UiSettingsEventArgs.FLAGS_UI_MOUSEDOWN) != 0)
                            {
                                //Insert on MouseDown Event here
                            }
                            else if ((e.Flags & UiSettingsEventArgs.FLAGS_UI_MOUSEUP) != 0)
                            {
                                //Insert on MouseUp Event here
                            }
                            else
                            {
                                if (eventArgsM.Button == MouseButtons.Left)
                                {
                                    //Insert on MouseDrag Event here
                                }
                            }
                        }
                        if (e.EventInfo.GetType() == typeof(HandledMouseEventArgs))
                        { 
                            eventArgsM = (MouseEventArgs)(e.EventInfo);
                            if ((e.Flags & UiSettingsEventArgs.FLAGS_UI_MOUSEWHEEL) != 0)
                            {
                                //Insert on MouseWheel Event here
                                manager.HandleMouseScroll(eventArgsM.Delta);
                            }
                        }
                        else
                        {
                            //Insert on MouseClick Event here
                        }
                        break;
                    case UiSettingsEventArgs.EventType.WindowEvent:
                        if (e.EventInfo.GetType() == typeof(DragEventArgs))
                        {
                            var dragEvent = (DragEventArgs)e.EventInfo;
                            string[] files = (string[])dragEvent.Data.GetData(DataFormats.FileDrop);
                            LoadDataFromPaths(files);
                        }
                        else if (e.EventInfo.GetType() == typeof(object[]))
                        {
                            var objs = ((object[])e.EventInfo);
                            if ((string)objs[0] == Global.LoadPathsEvent)
                            {
                                LoadDataFromPaths((string[])objs[1]);
                            }
                        }
                        break;
                }

        }

        private void CreateNewMap()
        {
            if (!manager.IsThreading)
            {
                var res = MessageBox.Show("Regenerate map from random", "New Map?", MessageBoxButtons.YesNo);
                if (res == DialogResult.Yes)
                {
                    Map map = ((NetworkLSTM)manager.CurrentNetwork).Map;
                    map.GernerateRandom();
                    map.Smooth();
                    var grid = map.Grid;
                    Map.initgrid = (int[,])grid.Clone();
                    manager.Networks.ConvertAll(n => (NetworkLSTM)n).ForEach(n => n.Map.CopyFrom(grid));
                }
            }
        }

        private void LoadDataFromPaths(params string[] files)
        {
            foreach (string file in files)
            {
                if (file.EndsWith(ConfigNetworkFileFilter.Split('.').Last()))
                {
                    manager.LoadNetworkFromAxonXML(file);
                }
                else if (file.EndsWith(ConfigFileFilter.Split('.').Last()))
                {
                    Global.LoadSettingsFromFile(file, out Global.R);
                }
            }
        }

        private void PreviewGeneration(bool preview)
        {
            if (preview)
            {
                manager.StopThreading();
                manager.HoldCurrentPreview = true;
            }
            else
            {
                manager.StartThreading();
            }
        }


        private bool ProcessSingleNetworkFrame(int index, int delta, Task whenFinished, bool saveProgress)
        {
            NetworkLSTM n = (NetworkLSTM)manager.Networks[index];

            double wh_dim = Math.Max(uiSettings.Width, uiSettings.Height);

            double sensorL = Array.FindIndex(n.Car.SensorData[0], tp => tp.Item1 > 0);
            double sensorR = Array.FindIndex(n.Car.SensorData[0], tp => tp.Item2 > 0);
            sensorL = sensorL >= 0 ? 1 - (sensorL + 1) / (double)n.Car.SensorDataLength : -1;
            sensorR = sensorR >= 0 ? 1 - (sensorR + 1) / (double)n.Car.SensorDataLength : -1;
            double sensor1L = Array.FindIndex(n.Car.SensorData[1], tp => tp.Item1 > 0);
            double sensor1R = Array.FindIndex(n.Car.SensorData[1], tp => tp.Item2 > 0);

            sensor1L = sensor1L >= 0 ? 1 - (sensor1L + 1) / (double)n.Car.SensorDataLength : -1;
            sensor1R = sensor1R >= 0 ? 1 - (sensor1R + 1) / (double)n.Car.SensorDataLength : -1;

            // Defines network node inputs structure
            double[] networkInputs = new double[] {
                n.Car.x / wh_dim,
                ((int) n.Car.ForwardDirection % 360) / 180f,
                n.Car.y / wh_dim,
                n.Car.DistanceTravelled / (n.Car.BestDistanceTravelled + 1),
                n.Car.dx / 3,
                n.Car.dy / 3,
                sensorL,
                sensorR,
                sensor1L,
                sensor1R,
            };

            // Check to ensure input param range fits in network node size
            if (networkInputs.Length > R.Learnr.InputNodesCount) throw new ArgumentOutOfRangeException("networkInputs");

            // Includes feedback data and const value node to input nodes array
            int oldLength = networkInputs.Length;
            Array.Resize(ref networkInputs, R.Learnr.InputNodesCount + n.FB.Length + 1);
            Array.Copy(n.FB, 0, networkInputs, oldLength, n.FB.Length);
            networkInputs[networkInputs.Length - 1] = 1; //constant node

            // Clamp input nodes values between -1 and +1
            for (int i = 0; i < networkInputs.Length; i++) networkInputs[i] = Math.Max(-1, Math.Min(1, networkInputs[i]));

            // Init node index itterator
            int nodeindex = 0;

            // Output node values are computed
            double[] networkresponse = n.ComputeOutputs(networkInputs);
            n.Car.ForwardThrottle = ((float)networkresponse[nodeindex++] - 0.3f) / 0.7f * R.CAR_SPEED_MAXACCELERATION;
            n.Car.ForwardSteerRate = ((float)networkresponse[nodeindex++] - 0.5f) * 2 * R.CAR_STEER_MAXRATE;
            while (nodeindex < n.Outputs)
            {
                double nodeoutput = networkresponse[nodeindex++];

            }

            // Feedback input nodes are updated to feedback output node values
            nodeindex = Math.Max(nodeindex, networkresponse.Length - n.FB.Length);
            int fbnodes = Math.Min(n.FB.Length, networkresponse.Length - nodeindex);
            for (int i = 0; i < fbnodes; i++) n.FB[i] = networkresponse[nodeindex++];

            n.GameFitness = Math.Round(Math.Pow(n.Car.DistanceTravelled + 0.29* n.Car.BestDistanceTravelled / 4, 2), 1);
            if (n.Map.rand.NextDouble() < 0.1f && DepositBoxes)
            {
                int[] cellP = n.Map.CellPositionFromCoodinates(n.Car.x, n.Car.y);
                n.Map.SetCellStateFromCell(cellP[0], cellP[1], 1);
            }


            return true;
        }

        public void RenderFrame(Graphics g, int delta)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            aveFrameDelta += (delta - aveFrameDelta) * 0.01f;
            manager.Update(delta);

            if (!manager.IsThreading) DrawObjects(g);

            if (DisplayNetworkInfo)
                manager.DrawStatsGraphics(g, true, true, graphDrawIndex);
        }

        public void DrawObjects(Graphics g)
        {

            foreach (RenderObject obj in manager.CurrentNetwork.GameObjects.Items)
                obj.DrawElement(g);
            if (manager.CurrentNetworkNodes != null && DisplayNetworkNodes)
            {
                foreach (NodeLayer nl in manager.CurrentNetworkNodes)
                    nl.DrawElement(g, manager.CurrentNetwork, true, manager.DisplayDynamicAxons, manager.DisplayHeatMap);
            }
        }
    }

}
