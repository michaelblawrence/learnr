using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Learnr.GlobalVars;

namespace Learnr
{
    public class NetworkManagerLSTM
    {
        #region private vars

        //Networks
        private NetworkPopulationLSTM networks;

        //Network management vars
        private int generationCounter = 1;
        private int currentNetwork = -1;

        //Utils
        private Random rr;
        private UiSettings uiSettings;
        private NetworkStatistics statistics;

        /// <summary>
        /// The threads for work processing network simulations.
        /// </summary>
        private List<Thread> workThreads;
        /// <summary>
        /// The current indexed list of networks occuping each thread
        /// </summary>
        private List<int> workCurrent;
        /// <summary>
        /// The queue of networks yet to be processed by the work threads.
        /// </summary>
        private List<int> workQueue;

        private int frameDelta;

        //Toggle vars
        private bool workThreading = false;
        private bool previewForward = true;
        private bool enabled = true;
        private bool nodesDisplayHeatMap = false;
        private bool nodesDisplayLinesDynamic = true;
        private bool previewHoldCurrent = true; //TODO


        //Loaded vars from GlobalConstants.R
        private int currentseed = R.Seed;
        private int threadCount = R.InitThreadCount;
        private double avedelta = 1000 / R.ComputeFrequency;
        private readonly static string[] graphDataTypes = new string[] { "Mean Fitness", "Median Fitness", "Species bests", "% bests species", "Fitness Distribution" };
        private int graphDrawIndex = graphDataTypes.Length - 1;

        #endregion

        #region public props

        /// <summary>
        /// Gets the collection of networks managed by this <see cref="T:Learnr.NetworkManagerLSTM"/> manager.
        /// </summary>
        /// <value>The networks.</value>
        public NetworkPopulationLSTM Networks { get => networks; }
        public NetworkStatistics Statistics { get => statistics; }
        public UiSettings UISettings { get => uiSettings; }

        /// <summary>
        /// Gets the current generation.
        /// </summary>
        /// <value>The generation.</value>
        public int Generation { get => generationCounter; }
        /// <summary>
        /// Gets the count of contained networks.
        /// </summary>
        /// <value>The count.</value>
        public int Count { get => Networks.Count; }
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:Learnr.NetworkManagerLSTM"/> is enabled.
        /// </summary>
        /// <value><c>true</c> if enabled; otherwise, <c>false</c>.</value>
        public bool Enabled { get => enabled; set => enabled = value; }
        /// <summary>
        /// Gets a value indicating whether this <see cref="T:Learnr.NetworkManagerLSTM"/> is running in multithreading mode.
        /// </summary>
        /// <value><c>true</c> if is threading; otherwise, <c>false</c>.</value>
        public bool IsThreading { get => workThreading; }

        // current state vars
        /// <summary>
        /// Gets or sets the index of the selected network.
        /// </summary>
        /// <value>The index of the selected network.</value>
        public int SelectedIndex { get => currentNetwork; set => currentNetwork = value; }
        /// <summary>
        /// Gets the current network.
        /// </summary>
        /// <value>The current network.</value>
        public NeuralNetworkLSTM CurrentNetwork { get { if (networks?.Count > currentNetwork) return networks[currentNetwork]; else return null; } }
        /// <summary>
        /// Gets the current game objects.
        /// </summary>
        /// <value>The current game objects.</value>
        public GameObjectCollection CurrentGameObjects { get { if (networks?.Count > currentNetwork) return networks[currentNetwork].GameObjects; else return null; } }
        /// <summary>
        /// Gets the current network nodes.
        /// </summary>
        /// <value>The current network nodes.</value>
        public NodeLayerCollection CurrentNetworkNodes { get { if (networks?.Count > currentNetwork) return networks[currentNetwork].nodes; else return null; } }


        public List<object> StatsCollection { get; private set; }
        /// <summary>
        /// Gets or sets the thread count of the multithreading operating mode.
        /// </summary>
        /// <value>The thread count.</value>
        public int ThreadCount { get => threadCount; set => threadCount = value; }

        public static string[] GraphDataTypes => graphDataTypes;
        private static Color baseColour = Color.Black;
        private static Color baseTextColour = Color.Gray;
        private double genRate = 0;
        private DateTime lastGenTime = DateTime.Now;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:Learnr.NetworkManagerLSTM"/> displays a heat map.
        /// </summary>
        /// <value><c>true</c> displays heat map; otherwise, <c>false</c>.</value>
        public bool DisplayHeatMap { get => nodesDisplayHeatMap; set => nodesDisplayHeatMap = value; }
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:Learnr.NetworkManagerLSTM"/> displays dynamic axons (lines).
        /// </summary>
        /// <value><c>true</c> displays dynamic axons; otherwise, <c>false</c>.</value>
        public bool DisplayDynamicAxons { get => nodesDisplayLinesDynamic; set => nodesDisplayLinesDynamic = value; }
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:Learnr.NetworkManagerLSTM"/> holds window preview on current network.
        /// </summary>
        /// <value><c>true</c> holds current network; otherwise, <c>false</c>.</value>
        public bool HoldCurrentPreview { get => previewHoldCurrent; set => previewHoldCurrent = value; }
        public static Color BaseColour { get => baseColour; set => baseColour = value; }
        public static Color BaseTextColour { get => baseTextColour; set => baseTextColour = value; }




        #endregion

        #region event definitions

        /// <summary>
        /// On load completed.
        /// </summary>
        public delegate void OnLoadCompleted(object sender, EventArgs e);
        public event OnLoadCompleted LoadCompleted;

        /// <summary>
        /// On frame requested.
        /// </summary>
        public delegate bool OnFrameRequested(int index, int delta, Task whenFinished, bool saveProgress);
        /// <summary>
        /// Occurs when frame requested.
        /// </summary>
        public event OnFrameRequested FrameRequested;

        /// <summary>
        /// On message received from UISettings object.
        /// </summary>
        public delegate bool OnMessageReceived(object sender, UiSettingsEventArgs e);
        /// <summary>
        /// Occurs when message received from UISettings object.
        /// </summary>
        public event OnMessageReceived MessageReceived;

        /// <summary>
        /// On network generated by manager.
        /// </summary>
        public delegate void OnNetworkGenerated(object sender, NetworkLSTMChangedEventArgs e);
        /// <summary>
        /// Occurs when network generated.
        /// </summary>
        public event OnNetworkGenerated NetworkGenerated;

        /// <summary>
        /// On new network object requested by manager. Returns new Network for manager pool.
        /// </summary>
        public delegate NeuralNetworkLSTM OnNewNetworkRequested(int inputs, int[] layers, int outputs, int dob);
        /// <summary>
        /// Occurs when new network requested. Returns new Network for manager pool.
        /// </summary>
        public event OnNewNetworkRequested NewNetworkRequested;

        /// <summary>
        /// On child network object requested by manager. Returns new Network from parent Network for manager pool.
        /// </summary>
        public delegate NeuralNetworkLSTM OnNetworkChildRequested(NeuralNetworkLSTM parent, int seed, int dob, bool mutate);
        /// <summary>
        /// Occurs when network child requested. Returns new Network from parent Network for manager pool.
        /// </summary>
        public event OnNetworkChildRequested NetworkChildRequested;
        #endregion

        #region init methods

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Learnr.NetworkManagerLSTM"/> class.
        /// </summary>
        /// <param name="uiSettings">User interface settings.</param>
        public NetworkManagerLSTM(UiSettings uiSettings)
        {
            this.uiSettings = uiSettings;
        }

        /// <summary>
        /// Load objects for this instance.
        /// </summary>
        public void Load()
        {
            InitComputationObjects();
        }

        /// <summary>
        /// Initialises the objects for computation.
        /// </summary>
        private void InitComputationObjects()
        {
            workThreads = new List<Thread>();
            workCurrent = new List<int>();
            workQueue = new List<int>();
            for (int i = 0; i < threadCount; i++)
            {
                workThreads.Add(new Thread(new ParameterizedThreadStart(ThreadWorker)));
                workCurrent.Add(-1);
            }

            NeuralNetworkLSTM.InitRandom();
            rr = new Random(currentseed);

            networks = new NetworkPopulationLSTM();
            statistics = new NetworkStatistics();

            for (int i = networks.Count; i < R.InitSize; i++)
            {
                int n_fb = rr.Next((int)(R.FeedbackNodeCount * 0.5), (int)(R.FeedbackNodeCount * 1.5));
                int n_lc = rr.Next((int)(R.NodeLayers.Length * 0.5), (int)(R.NodeLayers.Length * 1.5));
                int[] n_l = new int[n_lc];
                for (int ii = 0; ii < n_l.Length; ii++)
                {
                    n_l[ii] = rr.Next((int)(R.NodeLayers.Average() * 0.5), (int)(R.NodeLayers.Average() * 1.5));
                }
                if (R.InitPoolRandomStructures)
                    networks.Add(NewNetworkRequested?.Invoke(R.InputNodesCount + n_fb + 1, n_l, R.OutputNodesCount + n_fb, generationCounter));
                else
                    networks.Add(NewNetworkRequested?.Invoke(R.TotalLHSNodesCount, R.NodeLayers, R.TotalRHSNodesCount, generationCounter));
                int fb = networks[i].Outputs - R.OutputNodesCount;
                networks[i].InitDisplayObjects(uiSettings, i, fb);
                NetworkGenerated?.Invoke(this, new NetworkLSTMChangedEventArgs(networks.Last()));
            }
            LoadCompleted?.Invoke(this, new EventArgs());
            currentNetwork = 0;
        }

        #endregion

        #region public management methords

        /// <summary>
        /// Resets the work thread queue to set all networks status to pending simulation.
        /// </summary>
        public void ReloadQueue()
        {
            if (workQueue != null)
            {
                workQueue.Clear();
                for (int x = 0; x < networks.Count; x++) workQueue.Add(x);
            }
            else
            {
                workQueue = new List<int>();
                ReloadQueue();
            }
        }

        /// <summary>
        /// Stops the multithreaded processing of Network simulations.
        /// </summary>
        public void StopThreading()
        {
            if (workThreading)
            {
                networks.SortNetworks("NORMAL");
                SelectLast();
                previewForward = false;
            }
            workThreading = false;
        }

        /// <summary>
        /// Starts the multithreaded processing of Network simulations. (Note that any simulation preview is disabled when simulations are threaded)
        /// </summary>
        public void StartThreading()
        {
            workThreading = true;
            if (workThreading && workQueue?.Count == 0)
            {
                ReloadQueue();
            }
        }

        /// <summary>
        /// Update the manager. Checks thread pool for updates and manages network queues. Displays preview while single threaded simulation is enabled
        /// </summary>
        /// <param name="delta">Time passed, in milliseconds, since last Update call</param>
        public void Update(int delta)
        {
            delta = 1000 / R.ComputeFrequency;
            if (!workThreading && !enabled)
            {
                delta = 0;
            }
            else
            {
                frameDelta += delta;
                avedelta += (delta - avedelta) * 0.01f;
            }
            if (Count <= 0) return;
            if (workThreading)
            {
                if (workThreads.Count < threadCount)
                {
                    for (int i = workThreads.Count; i < threadCount; i++)
                    {
                        workThreads.Add(new Thread(new ParameterizedThreadStart(ThreadWorker)));
                        workCurrent.Add(-1);
                    }
                }

                if (workQueue.Count == 0 && workThreads.All(wt => wt.ThreadState != ThreadState.Running))
                {
                    if (workThreads.Count > threadCount)
                    {
                        for (int i = threadCount; i < workThreads.Count; i++)
                        {
                            workCurrent.RemoveAt(i);
                            workThreads[i].Abort();
                            workThreads.RemoveAt(i);
                        }
                    }

                    {
                        RegenNetwork(R.CullPercentage);
                        for (int x = 0; x < networks.Count; x++)
                        {
                            workQueue.Add(x);
                        }
                    }

                }

                for (int i = 0; i < workThreads.Count; i++)
                {


                    if (workThreads[i].ThreadState != ThreadState.Running && workQueue.Count > 0 && i < threadCount && workThreading)
                    {
                        //Gives thread new job
                        int index = workQueue.Last();
                        workQueue.Remove(index);
                        workCurrent[i] = index;
                        object data = new object[] {
                            index,
                            1000/R.ComputeFrequency,
                            new Task((oo) => { workCurrent[(int)oo] = -1; }, i)
                        };
                        workThreads[i] = new Thread(ThreadWorker);
                        workThreads[i].Start(data);


                    }
                }
            }
            if (!workThreading)
            {
                if (!ProcessSingleNetworkFrame(currentNetwork, delta, new Task(() => { }), false))
                {
                    currentNetwork += previewForward ? 1 : -1;
                    if (currentNetwork >= networks.Count)
                    {
                        currentNetwork = 0;
                    }
                    else if (currentNetwork < 0)
                    {
                        currentNetwork = (currentNetwork + networks.Count) % networks.Count;
                    }

                }
            }
            UpdateStats();
        }

        /// <summary>
        /// Updates the species-specific statistics for a given generation.
        /// </summary>
        /// <param name="splitSpeciesByAxionsHash">If set to <c>true</c> species will be diferentiated by an axion applied hashing function.</param>
        public void UpdateSpeciesStats(bool splitSpeciesByAxionsHash)
        {
            if (splitSpeciesByAxionsHash)
            {
                var grouped = networks.GroupBy(n => n.AxonHash.Substring(0, Math.Min(n.AxonHash.Length, R.SpeciesMatchDegree))).ToList();
                grouped.ForEach(gr =>
                {
                    int col = NeuralNetworkLSTM.GenerateRandomSpeciesColour();
                    string name = gr.ElementAt(0).AxonHash;
                    gr.ToList().ForEach(n => n.ReassignSpecies(col, GlobalConstants.PsudoSpeciesPrefix + name));
                });
                string displayMessage = grouped.Count + " species detected";
                UiSettingsEventArgs e = new UiSettingsEventArgs(UiSettingsEventArgs.EventType.ManagerEvent, displayMessage, -1);
                MessageReceived?.Invoke(this, e);
            }

            List<int> specs = networks.ConvertAll(n => n.Species);
            specs.Sort();
            IEnumerable<IGrouping<int, int>> s = specs.GroupBy(i => i);
            statistics.SpeciesCount.Clear();
            for (int i = 0; i < s.Count(); i++)
            {
                statistics.SpeciesCount.Add(s.ElementAt(i).Key, s.ElementAt(i).Count());
            }
        }

        #endregion

        #region private management workers

        /// <summary>
        /// Processes the single frame of a given network's simulation.
        /// </summary>
        /// <returns><c>true</c>, if single network frame was processed without error, <c>false</c> otherwise.</returns>
        /// <param name="index">Index of network.</param>
        /// <param name="delta">Intended time delta between frames.</param>
        /// <param name="whenFinished">Task to be run when processing of a simulation's last frame is complete.</param>
        /// <param name="saveProgress">If set to <c>true</c> fitness scores will be retained for network ranking and population regeneration.</param>
        private bool ProcessSingleNetworkFrame(int index, int delta, Task whenFinished, bool saveProgress)
        {
            if (networks[index].GameTime == 0) networks[index].GameFitness = 0;
            networks[index].GameTime += delta;

            if (networks[index].GameTime > R.TimeLimit * 1000)
            {
                if (saveProgress)
                {
                    networks[index].GameObjects.ResetAll();
                    if (saveProgress)
                    {
                        networks[index].Fitness = networks[index].GameFitness;
                    }
                    networks[index].GameFitness = 0;
                    networks[index].LSTM.Last().Clear();
                    networks[index].GameObjects.BeginGeneration();
                    whenFinished.RunSynchronously();
                    networks[index].GameTime = 0;
                    return false;
                }
            }
            if (networks[index].Index != index)
                networks[index].Index = index;

            networks[index].GameObjects.UpdateElements(delta, uiSettings);

            if (FrameRequested != null)
                return FrameRequested.Invoke(index, delta, whenFinished, saveProgress);
            else return false;
        }


        /// <summary>
        /// Truncates statistics collection to have a length clamped at maximum value with a psuedo exponential time scale
        /// </summary>
        private void UpdateStats()
        {
            StatsCollection = new List<object>() { statistics.Means, statistics.Medians, statistics.SpeciesBests, statistics.SpeciesMeanBests, statistics.Distribution };
            foreach (var datapoints in StatsCollection)
            {
                if (datapoints.GetType() == typeof(List<double>[]))
                {
                    List<double>[] dps = (List<double>[])datapoints;
                    foreach (var dp in dps)
                    {
                        if (dp?.Count > R.GraphMaxPoints)
                        {
                            for (int i = 0; i < dp.Count; i++)
                            {
                                bool del = i % 2 == 1;
                                if (del) dp.RemoveAt(dp.Count - i);
                            }
                        }
                    }
                }
                else
                {
                    List<double> dp = (List<double>)datapoints;
                    if (dp?.Count > R.GraphMaxPoints)
                    {
                        for (int i = 0; i < dp.Count; i++)
                        {
                            bool del = i % 2 == 1;
                            if (del) dp.RemoveAt(dp.Count - i);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Draws the all statistics text, graph and bar chart graphics.
        /// </summary>
        /// <param name="g">The Graphics object onto which to draw.</param>
        private void DrawStatsTextGraphics(Graphics g)
        {
            int init_h = 5;
            int pad = 18;
            int h, xx;
            bool v = CurrentNetwork != null;
            Brush b = new SolidBrush(BaseTextColour);
            Font f = SystemFonts.DefaultFont;

            h = init_h;
            xx = 5;
            g.DrawString("Gen " + (Generation > R.PreGenerations ? "" : "(P)") + Generation, f, b, xx, h);
            h += pad;
            if (v)
            {
                g.DrawString("Network " + SelectedIndex + (previewHoldCurrent ? " [HELD]" : ""), f, b, xx, h += pad);
                g.DrawString("Fitness " + Math.Round(CurrentNetwork.GameFitness, 2) + (CurrentNetwork.GameTime > R.TimeLimit * 1000 ? "*" : ""), f, Brushes.LightGray, xx, h += pad);
                g.DrawString("(Fitness) " + Math.Round(CurrentNetwork.Fitness, 2), f, b, xx, h += pad);
                g.DrawString("(Best Fitness) " + Math.Round(CurrentNetwork.BestFitness, 2), f, b, xx, h += pad);
                g.DrawString("Network Age " + (Generation - CurrentNetwork.DOB), f, b, xx, h += pad);
                g.DrawString("Species Best " + Math.Round(CurrentNetwork.InheritedBestFitness, 2), f, b, xx, h += pad);
                g.DrawString("Species  " + CurrentNetwork.SpeciesCodeName, f, b, xx, h += pad);
                g.DrawString("Child Order " + CurrentNetwork.FamilyHistoryLength, f, b, xx, h += pad);
                g.DrawString("Network #" + CurrentNetwork.ID, f, b, xx, h += pad);
            }

            h = init_h;
            xx = 145;
            g.DrawString("Threads: " + ThreadCount, f, b, xx, h);
            h += pad;
            if (IsThreading) g.DrawString("Gens / min: " + Math.Round(60.0 / genRate, 1), f, b, xx, h);
            if (v) g.DrawString("ax hash " + CurrentNetwork.AxonHash, f, b, xx, h += pad);
            if (v) g.DrawString("Remaining OGs: " + Math.Round((100f * Networks.Count(n => !n.IsChild) / Count), 2) + "%", f, b, xx, h += pad);
            g.DrawString("Cull percentage: " + Math.Round(R.CullPercentage, 2) + "%", f, b, xx, h += pad);

            h = init_h;
            xx = uiSettings.Width - 100;
            if (v)
            {
                g.DrawString("Mean: " + Math.Round(Networks.Average(n => n.Fitness), 1), f, b, xx, h += pad);
                g.DrawString("1st Q: " + Math.Round(Statistics.Quartiles[0], 1), f, b, xx, h += pad);
                g.DrawString("Median: " + Math.Round(Statistics.Quartiles[1], 1), f, b, xx, h += pad);
                g.DrawString("3rd Q: " + Math.Round(Statistics.Quartiles[2], 1), f, b, xx, h += pad);
                g.DrawString("Best: " + Math.Round(Statistics.UpperLowerLimits[0], 1), f, b, xx, h += pad);
            }
        }
        /// <summary>
        /// Draws a given statistic's graph and graphics.
        /// </summary>
        /// <param name="g">The Graphics object onto which to draw.</param>
        /// <param name="drawIndex">Index of statistic to graph; (0:Mean, 1:Median, 2:Best, 3:%age Best, 4:All Stats).</param>
        private void DrawStatsGraphGraphics(Graphics g, int drawIndex)
        {
            int g_w = 300, g_h = 100, b_h = 10;
            switch (drawIndex)
            {
                case 0:
                    DrawGraphs(g, g_w, g_h, b_h, "Mean Fitness", StatsCollection[0]);
                    break;
                case 1:
                    DrawGraphs(g, g_w, g_h, b_h, "Median Fitness", StatsCollection[1]);
                    break;
                case 2:
                    DrawGraphs(g, g_w, g_h, b_h, "Species bests", StatsCollection[2]);
                    break;
                case 3:
                    DrawGraphs(g, g_w, g_h, b_h, "% bests species", StatsCollection[3]);
                    break;
                case 4:
                    DrawGraphs(g, g_w, g_h, b_h, "Fitness Distribution", StatsCollection[4]);
                    break;
            }
        }

        /// <summary>
        /// Draws the graph and population bar display objects.
        /// </summary>
        /// <param name="g">The Graphics object onto which to draw.</param>
        /// <param name="graph_w">Graph width.</param>
        /// <param name="graph_h">Graph height.</param>
        /// <param name="bar_h">Bar height.</param>
        /// <param name="label">Label text above graph.</param>
        /// <param name="datapoints">Points of data to draw.</param>
        private void DrawGraphs(Graphics g, int graph_w, int graph_h, int bar_h, string label, object datapoints)
        {
            if (datapoints.GetType() == typeof(List<double>[]))
            {
                DrawGraphs(g, graph_w, graph_h, bar_h, label, (List<double>[])datapoints);
            }
            else
            {
                DrawGraphs(g, graph_w, graph_h, bar_h, label, (List<double>)datapoints);
            }
        }

        /// <summary>
        /// Draws the graph and population bar display objects.
        /// </summary>
        /// <param name="g">The Graphics object to which to draw.</param>
        /// <param name="graph_w">Graph width.</param>
        /// <param name="graph_h">Graph height.</param>
        /// <param name="bar_h">Bar height.</param>
        /// <param name="label">Label text above graph.</param>
		/// <param name="datapoints">Points of data to draw.</param>
        private void DrawGraphs(Graphics g, int graph_w, int graph_h, int bar_h, string label, params List<double>[] datapoints)
        {
            int top_buffer_h = 20;
            Rectangle graph = new Rectangle(uiSettings.Width / 2 - graph_w / 2,
                top_buffer_h, graph_w, graph_h);
            double means_max, means_min;

            Brush stdBrush = new SolidBrush(BaseTextColour);


            if (datapoints[0]?.Count > 1 && (means_max = datapoints.Max(dp => dp.Max())) != 0)
            {
                means_min = Math.Min(datapoints.Min(dp => dp.Min()), 0);
                foreach (List<double> dp in datapoints)
                {
                    DrawGraphLine(g, graph_w, graph_h, dp, means_min, means_max);
                }

                g.DrawString(String.Format(" {0:0.0}", Math.Round(means_max, 1)), SystemFonts.DefaultFont, stdBrush, graph.X + graph.Width, graph.Y);
                g.DrawString(String.Format(" {0:0.0}", Math.Round(means_min, 1)), SystemFonts.DefaultFont, stdBrush, graph.X + graph.Width, graph.Y + graph.Height - 13);
            }
            g.FillRectangle(new SolidBrush(BaseColour), graph.X, 0, graph.Width, top_buffer_h);

            Rectangle bar = new Rectangle(uiSettings.Width / 2 - graph_w / 2,
                top_buffer_h + graph_h, graph_w, bar_h);

            g.FillRectangle(new SolidBrush(BaseColour), bar);
            g.DrawRectangle(Pens.LightGray, graph);

            double bar_currentX = bar.X;
            for (int i = 0; i < Statistics.SpeciesCount.Count; i++)
            {
                int species = Statistics.SpeciesCount.Keys.ElementAt(i);
                int count = Statistics.SpeciesCount[species];
                double spec_w = graph_w * (count / (double)Count);
                g.FillRectangle(new SolidBrush(Color.FromArgb(species)), (float)bar_currentX, bar.Y, (float)spec_w, bar.Height);
                bar_currentX += spec_w;
            }
            g.DrawRectangle(Pens.LightGray, bar);
            float str_w = g.MeasureString(label, SystemFonts.DefaultFont).Width;
            g.DrawString(label, SystemFonts.DefaultFont, stdBrush, graph.X + graph.Width / 2 - str_w / 2, 4);

        }
        /// <summary>
        /// Draws the graph line.
        /// </summary>
        /// <param name="g">The Graphics object to which to draw.</param>
        /// <param name="graph_w">Graph width.</param>
        /// <param name="graph_h">Graph height.</param>
        /// <param name="datapoints">Points of data to draw.</param>
        /// <param name="y_min">Y-axis minimum.</param>
        /// <param name="y_max">Y-axis maximum.</param>
        private void DrawGraphLine(Graphics g, int graph_w, int graph_h, List<double> datapoints, double y_min, double y_max)
        {
            int top_buffer_h = 20;
            Rectangle graph = new Rectangle(uiSettings.Width / 2 - graph_w / 2,
                top_buffer_h, graph_w, graph_h);

            if (datapoints?.Count > 1 && y_max > 0)
            {
                float means_cc = datapoints.Count;
                int ii = 0;

                List<PointF> pts = datapoints.ConvertAll(x =>

                    new PointF(graph.X + graph_w * ii++ / (means_cc - 1),
                    graph.Y + graph_h - graph_h * (float)((datapoints[ii - 1] - y_min) / (y_max - y_min)))

                );
                g.DrawCurve(Pens.Gray, pts.ToArray());
                g.FillRectangles(Brushes.DarkGray, pts.ConvertAll(p => new RectangleF(p.X - 1, p.Y - 1, 2, 2)).ToArray());
            }
        }

        /// <summary>
        /// To be run on each thread. Computes a single network's simulation and ends when simulation in complete
        /// </summary>
        /// <param name="inp">Input network work thread data object.</param>
        private void ThreadWorker(object inp)
        {
            object[] data = (object[])inp;

            int networkindex = (int)(data[0]);
            int delta = (int)(data[1]);
            Task task = (Task)(data[2]);

            networks[networkindex].Fitness = 0;
            Array.Clear(networks[networkindex].FB, 0, networks[networkindex].FB.Length);

            bool endofsim = false;
            while (!endofsim)
            {
                endofsim = !ProcessSingleNetworkFrame(networkindex, delta, task, true);
            }
        }


        /// <summary>
        /// Ends a given generation and sorts, culls and breeds the next generation of networks.
        /// </summary>
        /// <param name="percentage">Percentage of population to be culled.</param>
        /// <param name="progressGeneration">If set to <c>true</c> the current generation will increment by one step after regeneration.</param>
        private void RegenNetwork(float percentage, bool progressGeneration)
        {
            statistics.Means.Add(networks.Average(n => n.SortingFitness));
            double tickspast = (DateTime.Now - lastGenTime).TotalMilliseconds;
            lastGenTime = DateTime.Now;
            genRate += (tickspast / 1000.0 - genRate) * 0.8;


            networks.Sort((n, n1) => (((int)(n.SortingFitness - n1.SortingFitness)) << 16) + (int)(n.InheritedBestFitness - n1.InheritedBestFitness));

            List<double> fits = networks.ConvertAll(n => n.SortingFitness);
            UpdateSpeciesStats();

            statistics.UpperLowerLimits[0] = fits.Last();
            statistics.UpperLowerLimits[1] = fits.First();
            for (int i = 0; i < statistics.Quartiles.Length; i++)
            {
                int index = (int)(fits.Count * (i + 1) / (statistics.Quartiles.Length + 1.0));
                statistics.Quartiles[i] = fits[index];
                if (i == 1) statistics.Medians.Add(statistics.Quartiles[i]);
            }

            statistics.Distribution[0].Add(statistics.UpperLowerLimits[1]);
            statistics.Distribution[1].Add(statistics.Quartiles[0]);
            statistics.Distribution[2].Add(statistics.Quartiles[1]);
            statistics.Distribution[3].Add(statistics.Quartiles[2]);
            statistics.Distribution[4].Add(statistics.UpperLowerLimits[0]);

            double max = fits.Skip((int)(fits.Count * 0.99)).Average();
            statistics.UpperLowerLimits[0] = Math.Round(max, 1);


            Random r = new Random(currentseed++);
            if (currentseed > 100000) currentseed = 0;

            List<int> toKill = new List<int>();
            if (generationCounter > R.PreGenerations)
            {
                for (int i = 0; i < networks.Count; i++)
                {
                    int ii = i;
                    double score = networks[ii].SortingFitness;
                    double probDie = 1 - 1 / (1 + Math.Exp(-score / max));
                    probDie *= 1.9;
                    double guessednumber = r.NextDouble();
                    if (guessednumber < probDie)
                    {
                        if (toKill.Count < R.InitSize * (percentage / 100))
                        {
                            toKill.Add(ii);
                        }
                    }
                }
            }
            int whileLimit = 100;
            int whileCount = 0;
            while (toKill.Count > Math.Max(0, networks.Count - 2) && whileCount++ < whileLimit)
            {
                toKill.RemoveAt(r.Next(toKill.Count - 1));
            }

            for (int i = 0; i < toKill.Count; i++)
            {
                networks.RemoveAt(toKill[toKill.Count - 1 - i]);
            }
            double totalFitness1;
            totalFitness1 = fits.Max();
            int tup = R.InitSize - networks.Count;

            double prob = 1 / max;
            double prob1 = 1 / (max * max);

            List<int> oddsIndex = new List<int>();

            for (int i = 0; i < networks.Count; i++)
            {
                for (int x = 0; x < 20; x++)
                {
                    double guessednumber = r.NextDouble();
                    if (guessednumber < prob1 * networks[i].SortingFitness * networks[i].SortingFitness)
                        oddsIndex.Add(i);

                }
            }

            int cc = networks.Count;

            if (oddsIndex.Count > 0)
            {
                for (int i = cc; i < R.InitSize; i++)
                {
                    int parentIndex = oddsIndex[r.Next(oddsIndex.Count - 1)];
                    NeuralNetworkLSTM newnetwork = NetworkChildRequested?.Invoke(networks[parentIndex], currentseed++, generationCounter, true);
                    networks.Add(newnetwork);
                    networks[networks.Count - 1].InitDisplayObjects(uiSettings, i, networks[i].Outputs - R.OutputNodesCount);
                    NetworkGenerated?.Invoke(this, new NetworkLSTMChangedEventArgs(networks.Last()));

                }
            }


            for (int i = 0; i < networks.Count; i++)
            {
                networks[i].Fitness = 0;
                networks[i].Index = i;
                networks[i].GameFitness = 0;
                networks[i].GameObjects.ResetAll();
            }

            currentNetwork = Math.Min(currentNetwork, networks.Count - 1);

            double MaxInheritedBestFitness = networks.Max(n => n.InheritedBestFitness);
            Statistics.SpeciesBests.Add(MaxInheritedBestFitness);
            Statistics.SpeciesMeanBests.Add(100 * networks.Average(n => n.InheritedBestFitness) / MaxInheritedBestFitness);
            if (progressGeneration)
                generationCounter++;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Selects the first network.
        /// </summary>
        /// <returns>The first network.</returns>
        public int SelectFirst()
        {
            return currentNetwork = 0;
        }
        /// <summary>
        /// Selects the last network.
        /// </summary>
        /// <returns>The last network.</returns>
        public int SelectLast()
        {
            return currentNetwork = Count - 1;
        }
        /// <summary>
        /// Selects the previous network.
        /// </summary>
        /// <returns>The previous network.</returns>
        public int Previous()
        {
            if (!workThreading)
            {
                return currentNetwork = (currentNetwork - 1 + networks.Count) % networks.Count;
            }
            else return -1;
        }
        /// <summary>
        /// Selects the next network.
        /// </summary>
        /// <returns>The next network.</returns>
        public int Next()
        {
            if (!workThreading)
            {
                return currentNetwork = (currentNetwork + 1 + networks.Count) % networks.Count;
            }
            else return -1;
        }
        /// <summary>
        /// Gets the network at current selected index.
        /// </summary>
        /// <returns>Selected network.</returns>
        /// <typeparam name="T">Type of nework</typeparam>
        public T At<T>()
        {
            return (T)(object)CurrentNetwork;
        }


        /// <summary>
        /// Draws the statistics text views and graphics.
        /// </summary>
        /// <param name="g">The green component.</param>
        /// <param name="drawText">If set to <c>true</c> draw text.</param>
        /// <param name="drawGraphs">If set to <c>true</c> draw graphs.</param>
        /// <param name="graphDataIndex">Graph data index.</param>
        public void DrawStatsGraphics(Graphics g, bool drawText, bool drawGraphs, int graphDataIndex)
        {
            if (drawText) DrawStatsTextGraphics(g);
            if (drawGraphs && graphDataIndex >= 0 && graphDataIndex < graphDataTypes.Length && networks.Count > 0) DrawStatsGraphGraphics(g, graphDataIndex);
        }

        /// <summary>
        /// Forces the update of statistics.
        /// </summary>
        public void ForceUpdateStats()
        {
            UpdateStats();
        }


        /// <summary>
        /// Ends a given generation and sorts, culls and breeds the next generation of networks.
        /// </summary>
        /// <param name="percentage">Percentage of population to be culled.</param>
        private void RegenNetwork(float percentage)
        {
            RegenNetwork(percentage, true);
        }


        public void UpdateSpeciesStats() { UpdateSpeciesStats(false); }

        /// <summary>
        /// Exports a collection of networks in XML format.
        /// </summary>
        /// <returns>XML String containing network collection neural data</returns>
        /// <param name="networks">Networks.</param>
        public static string ExportXML(params NeuralNetworkLSTM[] networks)
        {
            string str = String.Empty;
            foreach (NeuralNetworkLSTM net in networks)
            {
                int[] nodelayout = new int[net.Size];
                nodelayout[0] = net.Inputs;
                nodelayout[net.Size - 1] = net.Outputs;
                Array.Copy(net.Layers, 0, nodelayout, 1, net.Layers.Length);
                str += net.Axons.Serialize() + Environment.NewLine;
            }
            return str;
        }

        /// <summary>
        /// Network neural information save file.
        /// </summary>
        public struct NetworkSaveFile
        {
            public double[][][] Axons;
            public LSTMLayer[] LSTM;
            public LSTMDeepGateLayer[] LSTM_Gates;
        }

        /// <summary>
        /// Loads the network from axon XML file type.
        /// </summary>
        /// <returns><c>true</c>, if network from axon was loaded, <c>false</c> otherwise.</returns>
        /// <param name="filename">Filename.</param>
        public bool LoadNetworkFromAxonXML(string filename)
        {
            if (IsThreading) return false;

            double[][][] axons;
            NetworkSaveFile data;
            try
            {
                XmlExtension.LoadSettingsFromFile(filename, out data);
                axons = data.Axons;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }

            int ins = axons.First().Length, outs = axons.Last().First().Length;
            int[] lays = axons.ToList().Skip(1).ToList().ConvertAll(dd => dd.Length).ToArray();

            var newnet = NewNetworkRequested.Invoke(ins, lays, outs, 0);
            if (newnet.Axons.Length != axons.Length) return false;
            for (int i = 0; i < axons.Length; i++)
            {
                newnet.Axons[i] = axons[i];
            }
            int fb = newnet.Outputs - R.OutputNodesCount;
            for (int i = 0; i < data.LSTM.Length; i++)
            {
                newnet.LSTM[i].CopyFrom(data.LSTM[i]);
            }
            for (int i = 0; i < data.LSTM_Gates.Length; i++)
            {
                newnet.LSTM.Gates[i].CopyFrom(data.LSTM_Gates[i]);
            }
            newnet.InitDisplayObjects(uiSettings, networks.Count - 1, fb);
            newnet.RecalculateHash();

            networks.Add(newnet);
            NetworkGenerated?.Invoke(this, new NetworkLSTMChangedEventArgs(networks.Last()));
            SelectedIndex = networks.Count - 1;

            return true;
        }

        /// <summary>
        /// Handles the user key press.
        /// </summary>
        /// <returns><c>true</c>, if key press could be handled, <c>false</c> otherwise.</returns>
        /// <param name="keydata">Pressed Key</param>
        /// <param name="ignoreKeys">List of keys to not handle by default</param>
        public bool HandleKeyPress(Keys keydata, params Keys[] ignoreKeys)
        {
            if (ignoreKeys?.Length > 0 && ignoreKeys.Contains(keydata)) return false;
            switch (keydata)
            {
                case Keys.Left:
                    Previous();
                    break;
                case Keys.Right:
                    Next();
                    break;
                case Keys.Up:
                    ThreadCount += R.ThreadCountIncrement;
                    break;
                case Keys.Down:
                    ThreadCount -= R.ThreadCountIncrement;
                    break;
                case Keys.B:
                    Sort("BEST");
                    break;
                case Keys.N:
                    Sort("NORMAL");
                    break;
                case Keys.Space:
                    previewHoldCurrent = !previewHoldCurrent;
                    break;
                case Keys.R:
                    CurrentGameObjects.ResetAll();
                    break;
                case Keys.H:
                    nodesDisplayHeatMap = !nodesDisplayHeatMap;
                    break;
                case Keys.D0:
                    nodesDisplayLinesDynamic = !nodesDisplayLinesDynamic;
                    break;
                case Keys.Oemcomma:
                    graphDrawIndex = (graphDrawIndex - 1 + GraphDataTypes.Length) % GraphDataTypes.Length;
                    break;
                case Keys.OemPeriod:
                    graphDrawIndex = (graphDrawIndex + 1 + GraphDataTypes.Length) % GraphDataTypes.Length;
                    break;
                case Keys.Oem7:
                    UpdateSpeciesStats(true);
                    break;

                case Keys.D1: SelectedIndex = 1 * Count / 10; break;
                case Keys.D2: SelectedIndex = 2 * Count / 10; break;
                case Keys.D3: SelectedIndex = 3 * Count / 10; break;
                case Keys.D4: SelectedIndex = 4 * Count / 10; break;
                case Keys.D5: SelectedIndex = 5 * Count / 10; break;
                case Keys.D6: SelectedIndex = 6 * Count / 10; break;
                case Keys.D7: SelectedIndex = 7 * Count / 10; break;
                case Keys.D8: SelectedIndex = 8 * Count / 10; break;
                case Keys.D9: SelectedIndex = 9 * Count / 10; break;
                default:
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Handles the mouse scroll wheel.
        /// </summary>
        /// <param name="delta">Delta.</param>
        public void HandleMouseScroll(int delta)
        {
            float culldelta = R.ScrollWheelSensitity + 1;
            R.CullPercentage *= (delta > 0) ? culldelta : 1 / culldelta;
            R.CullPercentage = Math.Min(R.CullPercentage, 75);
        }

        /// <summary>
        /// Sort Network items by predetermined method
        /// </summary>
        /// <param name="method">Options: "NORMAL", "BEST"</param>
        public bool Sort(string method)
        {
            if (!workThreading)
            {
                networks.SortNetworks(method);
                SelectLast();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Abort the running thread instances.
        /// </summary>
        public void Abort()
        {
            if (workThreads?.Count > 0)
            {
                foreach (Thread t in workThreads)
                {
                    t.Abort();
                }
            }
        }

        #endregion

        /// <summary>
        /// Class representing the history of population statistics for a Network collection.
        /// </summary>
        public class NetworkStatistics
        {
            private double[] stats_quartiles = new double[3];
            private double[] stats_upperlower = new double[2];
            private List<double> stats_means;
            private List<double> stats_medians;
            private List<double>[] stats_all_quartiles_list;
            private List<double> stats_specbests;
            private List<double> stats_specbestsmean;
            private Dictionary<int, int> stats_speciescount;
            /// <summary>
            /// Gets or sets the collection of mean fitness scores.
            /// </summary>
            /// <value>The means.</value>
            public List<double> Means { get => stats_means; set => stats_means = value; }
            /// <summary>
            /// Gets or sets the collection of median fitness scores.
            /// </summary>
            /// <value>The medians.</value>
            public List<double> Medians { get => stats_medians; set => stats_medians = value; }
            /// <summary>
            /// Gets or sets the collection representing the modal distribution of fitness scores.
            /// </summary>
            /// <value>The distribution.</value>
            public List<double>[] Distribution { get => stats_all_quartiles_list; set => stats_all_quartiles_list = value; }
            /// <summary>
            /// Gets or sets the collection of maximum inherited fitness scores within the population.
            /// </summary>
            /// <value>The species bests.</value>
            public List<double> SpeciesBests { get => stats_specbests; set => stats_specbests = value; }
            /// <summary>
            /// Gets or sets the collection of the proportion of the population with the maximum value of inherited fitness score.
            /// </summary>
            /// <value>The species mean bests.</value>
            public List<double> SpeciesMeanBests { get => stats_specbestsmean; set => stats_specbestsmean = value; }
            /// <summary>
            /// Gets or sets the number of networks for each species.
            /// </summary>
            /// <value>The species count.</value>
            public Dictionary<int, int> SpeciesCount { get => stats_speciescount; set => stats_speciescount = value; }
            public double[] UpperLowerLimits { get => stats_upperlower; set => stats_upperlower = value; }
            public double[] Quartiles { get => stats_quartiles; set => stats_quartiles = value; }

            /// <summary>
            /// Initializes a new instance of the <see cref="T:Learnr.NetworkManagerLSTM.NetworkStatistics"/> class.
            /// </summary>
            public NetworkStatistics()
            {
                stats_means = new List<double>();
                stats_medians = new List<double>();
                stats_all_quartiles_list = new List<double>[5];
                for (int i = 0; i < stats_all_quartiles_list.Length; i++)
                {
                    stats_all_quartiles_list[i] = new List<double>();
                }
                stats_specbests = new List<double>();
                stats_specbestsmean = new List<double>();
                stats_speciescount = new Dictionary<int, int>();
            }
        }
    }

    /// <summary>
    /// Network collection.
    /// </summary>
    public class NetworkPopulationLSTM : List<NeuralNetworkLSTM>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Learnr.NetworkPopulationLSTM"/> class.
        /// </summary>
        public NetworkPopulationLSTM() : base()
        {

        }

        /// <summary>
        /// Sort Network items by predetermined method
        /// </summary>
        /// <param name="method">Options: "NORMAL", "BEST"</param>
        public void SortNetworks(string method)
        {
            method = method.Trim().ToUpper();
            switch (method)
            {
                case "NORMAL":
                    Sort((n, n1) => ((int)(1000 * (n.SortingFitness - n1.SortingFitness))));
                    for (int i = 0; i < Count; i++)
                    {
                        this[i].Index = i;
                        this[i].GameFitness = 0;
                        this[i].GameObjects.ResetAll();
                    }
                    break;
                case "BEST":
                    Sort((n, n1) => ((int)(1000 * (n.BestFitness - n1.BestFitness))));
                    for (int i = 0; i < Count; i++)
                    {
                        this[i].Index = i;
                        this[i].GameFitness = 0;
                        this[i].GameObjects.ResetAll();
                    }
                    break;
            }

        }

    }
    public class NetworkLSTMChangedEventArgs : EventArgs
    {
        public NeuralNetworkLSTM Network { get; set; }

        public NetworkLSTMChangedEventArgs(NeuralNetworkLSTM network) : base()
        {
            this.Network = network;
        }
    }
}
