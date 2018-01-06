using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Learnr;
using static learnr2fly.Global;
using System.Linq;

namespace learnr2fly
{
    public class Renderer
    {
        #region private vars
        private NetworkManagerLSTM manager;
        private UiSettings uiSettings;

        private int graphDrawIndex;
        private float aveFrameDelta;
        private int killInputs = 0;
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
            manager.LoadCompleted += Manager_LoadCompleted;
            manager.FrameRequested += ProcessSingleNetworkFrame;
            manager.NetworkGenerated += Manager_NetworkGenerated;
            manager.NewNetworkRequested += Manager_NewNetworkRequested;
            manager.NetworkChildRequested += Manager_NetworkChildRequested;
            manager.Load();
        }

        private void initDisplayObjects()
        {
            aveFrameDelta = 1000 / GlobalVars.R.ComputeFrequency;
            graphDrawIndex = NetworkManagerLSTM.GraphDataTypes.Length - 1;

            manager.DisplayHeatMap = false;
            manager.DisplayDynamicAxons = true;
            Spaceship.PreviewVisionLines = false;
        }

        #endregion

        #region network generation

        private NeuralNetworkLSTM Manager_NewNetworkRequested(int inputs, int[] layers, int outputs, int dob)
        {
            return new NetworkLSTM(inputs, layers, outputs, dob).Generate(); ;
        }

        private void Manager_LoadCompleted(object sender, EventArgs e)
        {

        }

        private NeuralNetworkLSTM Manager_NetworkChildRequested(NeuralNetworkLSTM parent, int seed, int dob, bool mutate)
        {
            return new NetworkLSTM((NetworkLSTM)parent, seed, dob, mutate);
        }

        private void Manager_NetworkGenerated(object sender, NetworkLSTMChangedEventArgs e)
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
                    case UiSettingsEventArgs.EventType.KeyDownEvent:
                        eventArgsK = (KeyEventArgs)(e.EventInfo);
                        switch (eventArgsK.KeyData)
                        {
                        }
                        break;
                    case UiSettingsEventArgs.EventType.KeyUpEvent:
                        eventArgsK = (KeyEventArgs)(e.EventInfo);
                        if (manager.HandleKeyPress(eventArgsK.KeyData, new Keys[]{ Keys.Space })) break;
                        switch (eventArgsK.KeyData)
                        {
                            case Keys.T:
                                PreviewGeneration(false);
                                break;
                            case Keys.U:
                                PreviewGeneration(true);
                                break;
                            case Keys.L:
                                Spaceship.PreviewVisionLines = !Spaceship.PreviewVisionLines;
                                break;
                            case Keys.F3:
                                if (!manager.IsThreading)
                                {
                                    GameWindow.SaveNetwork(manager.CurrentNetwork);
                                }

                                break;
                            case Keys.F4:
                                if (!manager.IsThreading)
                                {
                                    string path = GameWindow.LoadNetworkPath();
                                    if (path != null)
                                        path.Split('\n').ToList().ForEach(s=>manager.LoadNetworkFromAxonXML(s));
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
                            case Keys.Z:
                                killInputs = Math.Max(0, killInputs - 1);
                                break;
                            case Keys.X:
                                killInputs = Math.Min(3, killInputs + 1);
                                break;
                            case Keys.C:
                                Asteroid.PlayState = Asteroid.PlayState != 0 ? 0 : 1;
                                break;
                        }
                        break;
                    case UiSettingsEventArgs.EventType.MouseEvent:
                        if (e.EventInfo.GetType() == typeof(HandledMouseEventArgs))
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
                            else if ((e.Flags & UiSettingsEventArgs.FLAGS_UI_MOUSEWHEEL) != 0)
                            {
                                //Insert on MouseWheel Event here
                                manager.HandleMouseScroll(eventArgsM.Delta);
                            }
                            else
                            {
                                if (eventArgsM.Button == MouseButtons.Left)
                                {
                                    //Insert on MouseDrag Event here
                                }
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
                        else if  (e.EventInfo.GetType() == typeof(object[]))
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
                manager.Networks.ForEach(n => ((NetworkLSTM)n).Spaceship.Score = 0);
                manager.StartThreading();
            }
        }


        private bool ProcessSingleNetworkFrame(int index, int delta, Task whenFinished, bool saveProgress)
        {
            NetworkLSTM n = (NetworkLSTM)manager.Networks[index];

            double wh_dim = Math.Max(uiSettings.Width, uiSettings.Height);

            double deg = n.Spaceship.Direction > 0 ? n.Spaceship.Direction % 360 : (n.Spaceship.Direction % 360) + 360;
            double look = n.Spaceship.LookRot * 2 / R.SPACESHIP_HEAD_ROT_RANGE;

            // Defines network node inputs structure
            double[] networkInputs = new double[] {
                2 * Math.PI * (n.Spaceship.x / uiSettings.Width-0.5),
                2 * Math.PI * (n.Spaceship.y / uiSettings.Height-0.5),
                n.Spaceship.dx / (8.0*R.ACCELERATE),
                n.Spaceship.dy / (8.0*R.ACCELERATE),
                (deg - 180) / 180.0,
                look,
            };

            int hitlinescount = R.Learnr.InputNodesCount - networkInputs.Length;
            double[] hitlines = n.Spaceship.GenerateVisionLines(n.Asteroids, hitlinescount, true, 95, 120, 25, 30);

            if (killInputs > 0) Array.Clear(networkInputs, 0, networkInputs.Length);

            int oldLength = networkInputs.Length;
            Array.Resize(ref networkInputs, R.Learnr.InputNodesCount);

            if (killInputs < 2)
                Array.Copy(hitlines, 0, networkInputs, oldLength, hitlines.Length);


            // Check to ensure input param range fits in network node size
            if (networkInputs.Length > n.Inputs) throw new ArgumentOutOfRangeException("networkInputs");

            // Includes feedback data and const value node to input nodes array
            oldLength = networkInputs.Length;
            Array.Resize(ref networkInputs, R.Learnr.InputNodesCount + n.FB.Length + 1);
            Array.Copy(n.FB, 0, networkInputs, oldLength, n.FB.Length);
            if (killInputs < 3)
                networkInputs[networkInputs.Length - 1] = 1; //constant node

            // Clamp input nodes values between -1 and +1
            for (int i = 0; i < networkInputs.Length; i++) networkInputs[i] = Math.Max(-1, Math.Min(1, networkInputs[i]));

            // Init node index itterator
            int nodeindex = 0;

            // Output node values are computed
            double[] networkresponse = n.ComputeOutputs(networkInputs);

            if (networkresponse[nodeindex++] > 0.5) n.Spaceship.Shoot();

            int dc = 3;
            double[] direction = new double[dc];
            Array.Copy(networkresponse, nodeindex, direction, 0, dc);
            n.Spaceship.turning = Turning.Left;
            if (R.SPACESHIP_BIDIRECTION_ENABLED)
            {
                n.Spaceship.NetRotRate = (float)(direction[0] - 0.5) * 2.0f;
            }
            else
            {
                n.Spaceship.NetRotRate = (float)direction[0];
            }

            nodeindex++;
            n.Spaceship.boosters = Boosters.Accelerate;
            if (R.SPACESHIP_BRAKING_ENABLED)
            {
                n.Spaceship.NetSpeed = (float)(direction[1] - 0.3) * 1.1428f;
            }
            else
                n.Spaceship.NetSpeed = (float)direction[1];
            nodeindex++;
            if (R.SPACESHIP_DIRECTIONAL_SIGHT_ENABLED)
            {
                n.Spaceship.LookRot += (float)(direction[0] - 0.5) * 2 * R.SPACESHIP_HEAD_ROT_MAXRATE;
            }
            else
                n.Spaceship.LookRot = 0;
            nodeindex++;



            // Feedback input nodes are updated to feedback output node values
            nodeindex = Math.Max(nodeindex, networkresponse.Length - n.FB.Length);
            int fbnodes = Math.Min(n.FB.Length, networkresponse.Length - nodeindex);
            for (int i = 0; i < fbnodes; i++) n.FB[i] = networkresponse[nodeindex++];

            n.GameFitness = Math.Pow(n.Spaceship.Score, 2);

            return true;
        }

        public void RenderFrame(Graphics g, int delta)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            aveFrameDelta += (delta - aveFrameDelta) * 0.01f;
            manager.Update(delta);

            if (!manager.IsThreading) DrawObjects(g);

            manager.DrawStatsGraphics(g, true, true, graphDrawIndex);
        }

        public void DrawObjects(Graphics g)
        {
            if (manager.CurrentNetwork == null) return;
            foreach (RenderObject obj in manager.CurrentNetwork.GameObjects.Items)
                obj?.DrawElement(g);
            if (manager.CurrentNetworkNodes != null)
            {
                foreach (NodeLayer nodelayer in manager.CurrentNetworkNodes)
                {
                    nodelayer.DrawElement(g, manager.CurrentNetwork, true, manager.DisplayDynamicAxons, manager.DisplayHeatMap);
                }
            }

        }
    }

}
