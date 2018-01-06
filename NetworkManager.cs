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
    public class NetworkManager
    {
        #region private vars

        //Networks
        private NetworkPopulation networks;


        //Network management vars
        private int generationCounter = 1;
        private int currentNetwork = -1;

        private Random rr;
        private UiSettings uiSettings;
        private List<Thread> workThreads;
        private List<int> workCurrent;
        private List<int> workQueue;
        private NetworkStatistics statistics;

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


        public NetworkPopulation Networks { get => networks; }
        public NetworkStatistics Statistics { get => statistics; }
        public UiSettings UISettings { get => uiSettings; }

        public int Generation { get => generationCounter; }
        public int Count { get => Networks.Count; }
        public bool Enabled { get => enabled; set => enabled = value; }
        public bool IsThreading { get => workThreading; }

        // current state vars
        public int SelectedIndex { get => currentNetwork; set => currentNetwork = value; }
        public NeuralNetwork CurrentNetwork { get { if (networks?.Count > currentNetwork) return networks[currentNetwork]; else return null; } }
        public GameObjectCollection CurrentGameObjects { get { if (networks?.Count > currentNetwork) return networks[currentNetwork].GameObjects; else return null; } }
        public NodeLayerCollection CurrentNetworkNodes { get { if (networks?.Count > currentNetwork) return networks[currentNetwork].nodes; else return null; } }

        public List<object> StatsCollection { get; private set; }
        public int ThreadCount { get => threadCount; set => threadCount = value; }

        public static string[] GraphDataTypes => graphDataTypes;
        private static Color baseColour = Color.Black;
        private static Color baseTextColour = Color.Gray;
        private double genRate = 0;
        private DateTime lastGenTime = DateTime.Now;

        public bool DisplayHeatMap { get => nodesDisplayHeatMap; set => nodesDisplayHeatMap = value; }
        public bool DisplayDynamicAxons { get => nodesDisplayLinesDynamic; set => nodesDisplayLinesDynamic = value; }
        public bool HoldCurrentPreview { get => previewHoldCurrent; set => previewHoldCurrent = value; }
        public static Color BaseColour { get => baseColour; set => baseColour = value; }
        public static Color BaseTextColour { get => baseTextColour; set => baseTextColour = value; }




        #endregion

        #region event definitions


        public delegate void OnLoadCompleted(object sender, EventArgs e);
        public event OnLoadCompleted LoadCompleted;

        public delegate bool OnFrameRequested(int index, int delta, Task whenFinished, bool saveProgress);
        public event OnFrameRequested FrameRequested;

        public delegate bool OnMessageReceived(object sender, UiSettingsEventArgs e);
        public event OnMessageReceived MessageReceived;

        public delegate void OnNetworkGenerated(object sender, NetworkChangedEventArgs e);
        public event OnNetworkGenerated NetworkGenerated;

        public delegate NeuralNetwork OnNewNetworkRequested(int inputs, int[] layers, int outputs, int dob);
        public event OnNewNetworkRequested NewNetworkRequested;

        public delegate NeuralNetwork OnNetworkChildRequested(NeuralNetwork parent, int seed, int dob, bool mutate);
        public event OnNetworkChildRequested NetworkChildRequested;
        #endregion

        #region init methods

        public NetworkManager(UiSettings uiSettings)
        {
            this.uiSettings = uiSettings;
        }

        public void Load()
        {
            InitComputationObjects();
        }

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

            NeuralNetwork.InitRandom();
            rr = new Random(currentseed);


            //neural = NewNetworkRequested?.Invoke(GlobalVars.R.TotalLHSNodesCount, GlobalVars.R.NodeLayers, GlobalVars.R.TotalRHSNodesCount, generationCounter);
                //new Network(GlobalVars.R.TotalLHSNodesCount, GlobalVars.R.NodeLayers, GlobalVars.R.TotalRHSNodesCount, generationCounter);
            //neural.Generate();

            networks = new NetworkPopulation();
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
                    networks.Add(
                        NewNetworkRequested?.Invoke(R.InputNodesCount + n_fb + 1, n_l, R.OutputNodesCount + n_fb, generationCounter)
                        //new Network(GlobalVars.R.InputNodesCount + n_fb + 1, n_l, GlobalVars.R.OutputNodesCount + n_fb, generationCounter).Generate()
                        );
                else
                    networks.Add(NewNetworkRequested?.Invoke(R.TotalLHSNodesCount, R.NodeLayers, R.TotalRHSNodesCount, generationCounter));
                int fb = networks[i].Outputs - R.OutputNodesCount;
                networks[i].InitDisplayObjects(uiSettings, i, fb);
                NetworkGenerated?.Invoke(this, new NetworkChangedEventArgs(networks.Last()));
            }
            LoadCompleted?.Invoke(this, new EventArgs());
            currentNetwork = 0;
        }

        #endregion

        #region public management methords

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

        public void StartThreading()
        {
            workThreading = true;
            if (workThreading && workQueue?.Count == 0)
            {
                ReloadQueue();
            }
        }

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
                            new Task((oo) => { /*if (workQueue.Count == 0)*/ workCurrent[(int)oo] = -1; }, i)
                        };
                        workThreads[i] = new Thread(ThreadWorker);
                        workThreads[i].Start(data);


                    }
                    //workCurrent.Add(-1);
                }
            }
            if (!workThreading)
            {
                if (!ProcessSingleNetworkFrame(currentNetwork, delta, new Task(() => { }), false))
                {
                    currentNetwork += previewForward ? 1 : -1;
                    if (currentNetwork >= networks.Count)
                    {
                        //RegenNetwork(cullpercentage, false);
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

        public void UpdateSpeciesStats(bool splitSpeciesByAxionsHash)
        {
            if (splitSpeciesByAxionsHash)
            {
                var grouped = networks.GroupBy(n => n.AxonHash.Substring(0, Math.Min(n.AxonHash.Length, R.SpeciesMatchDegree))).ToList();
                grouped.ForEach(gr =>
                {
                    int col = NeuralNetwork.GenerateRandomSpeciesColour();
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
                        if (dp?.Count > R.GraphMaxPoints)//#RED
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
                    if (dp?.Count > R.GraphMaxPoints)//#RED
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
            //g.DrawString(displayMessage, f, b, xx, h += pad);
            if (IsThreading) g.DrawString("Gens / min: " + Math.Round(60.0/genRate, 1), f, b, xx, h);
            if (v) g.DrawString("ax hash " + CurrentNetwork.AxonHash, f, b, xx, h += pad);
            if (v) g.DrawString("Remaining OGs: " + Math.Round((100f * Networks.Count(n => !n.IsChild) / Count), 2) + "%", f, b, xx, h += pad);
            g.DrawString("Cull percentage: " + Math.Round(R.CullPercentage, 2) + "%", f, b, xx, h += pad);

            h = init_h;
            xx = uiSettings.Width - 100;
            if (v)
            {
                //g.DrawString(Math.Round(1000 / avedelta, 1) + "fps", f, b, xx, h);
                g.DrawString("Mean: " + Math.Round(Networks.Average(n => n.Fitness), 1), f, b, xx, h += pad);
                g.DrawString("1st Q: " + Math.Round(Statistics.Quartiles[0], 1), f, b, xx, h += pad);
                g.DrawString("Median: " + Math.Round(Statistics.Quartiles[1], 1), f, b, xx, h += pad);
                g.DrawString("3rd Q: " + Math.Round(Statistics.Quartiles[2], 1), f, b, xx, h += pad);
                g.DrawString("Best: " + Math.Round(Statistics.UpperLowerLimits[0], 1), f, b, xx, h += pad);
            }
        }
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
        private void DrawGraphs(Graphics g, int graph_w, int graph_h, int bar_h, string label, params List<double>[] datapoints)
        {
            int top_buffer_h = 20;
            Rectangle graph = new Rectangle(uiSettings.Width / 2 - graph_w / 2,
                top_buffer_h, graph_w, graph_h);
            double means_max, means_min;

            Brush stdBrush = new SolidBrush(BaseTextColour);

            //List<double> datapoints = (List<object>)points;

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
        private void DrawGraphLine(Graphics g, int graph_w, int graph_h, List<double> datapoints, double y_min, double y_max)
        {
            int top_buffer_h = 20;
            Rectangle graph = new Rectangle(uiSettings.Width / 2 - graph_w / 2,
                top_buffer_h, graph_w, graph_h);
            //double means_max, means_min;

            if (datapoints?.Count > 1 && y_max > 0)
            {
                float means_cc = datapoints.Count;
                //means_min = Math.Min(datapoints.Min(), 0);
                int ii = 0;

                List<PointF> pts = datapoints.ConvertAll(x =>

                    new PointF(graph.X + graph_w * ii++ / (means_cc - 1),
                    graph.Y + graph_h - graph_h * (float)((datapoints[ii - 1] - y_min) / (y_max - y_min)))

                );
                g.DrawCurve(Pens.Gray, pts.ToArray());
                g.FillRectangles(Brushes.DarkGray, pts.ConvertAll(p => new RectangleF(p.X - 1, p.Y - 1, 2, 2)).ToArray());
            }
        }


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
                    NeuralNetwork newnetwork = NetworkChildRequested?.Invoke(networks[parentIndex], currentseed++, generationCounter, true);
                    networks.Add(newnetwork);
                    networks[networks.Count - 1].InitDisplayObjects(uiSettings, i, networks[i].Outputs - R.OutputNodesCount);
                    NetworkGenerated?.Invoke(this, new NetworkChangedEventArgs(networks.Last()));

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


        public int SelectFirst()
        {
            return currentNetwork = 0;
        }
        public int SelectLast()
        {
            return currentNetwork = Count - 1;
        }
        public int Previous()
        {
            if (!workThreading)
            {
                return currentNetwork = (currentNetwork - 1 + networks.Count) % networks.Count;
            }
            else return -1;
        }
        public int Next()
        {
            if (!workThreading)
            {
                return currentNetwork = (currentNetwork + 1 + networks.Count) % networks.Count;
            }
            else return -1;
        }
        public T At<T>()
        {
            return (T)(object)CurrentNetwork;
        }

        
        public void DrawStatsGraphics(Graphics g, bool drawText, bool drawGraphs, int graphDataIndex)
        {
            if (drawText) DrawStatsTextGraphics(g);
            if (drawGraphs && graphDataIndex >= 0 && graphDataIndex < graphDataTypes.Length && networks.Count > 0) DrawStatsGraphGraphics(g, graphDataIndex);
        }

        public void ForceUpdateStats()
        {
            UpdateStats();
        }


        private void RegenNetwork(float percentage)
        {
            RegenNetwork(percentage, true);
        }


        public void UpdateSpeciesStats() { UpdateSpeciesStats(false); }

        public static string ExportXML(params NeuralNetwork[] networks)
        {
            string str = String.Empty;
            foreach (NeuralNetwork net in networks)
            {
                int[] nodelayout = new int[net.Size];
                nodelayout[0] = net.Inputs;
                nodelayout[net.Size - 1] = net.Outputs;
                Array.Copy(net.Layers, 0, nodelayout, 1, net.Layers.Length);
                str += net.Axons.Serialize() + Environment.NewLine;
            }
            return str;
        }

        public bool LoadNetworkFromAxonXML(string filename)
        {
            if (IsThreading) return false;

            double[][][] axons;
            try
            {
                XmlExtension.LoadSettingsFromFile<double[][][]>(filename, out axons);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }

            int ins = axons.First().Length, outs = axons.Last().First().Length;
            int[] lays = axons.ToList().Skip(1).ToList().ConvertAll(dd => dd.Length).ToArray();

            var newnet =  NewNetworkRequested.Invoke(ins, lays, outs, 0);
            if (newnet.Axons.Length != axons.Length) return false;
            for (int i = 0; i < axons.Length; i++)
            {
                newnet.Axons[i] = axons[i];
            }
            int fb = newnet.Outputs - R.OutputNodesCount;
            newnet.InitDisplayObjects(uiSettings, networks.Count - 1, fb);
            newnet.RecalculateHash();

            networks.Add(newnet);
            NetworkGenerated?.Invoke(this, new NetworkChangedEventArgs(networks.Last()));
            SelectedIndex = networks.Count - 1;

            return true;
        }

        public bool HandleKeyPress(Keys keydata)
        {
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

            public List<double> Means { get => stats_means; set => stats_means = value; }
            public List<double> Medians { get => stats_medians; set => stats_medians = value; }
            public List<double>[] Distribution { get => stats_all_quartiles_list; set => stats_all_quartiles_list = value; }
            public List<double> SpeciesBests { get => stats_specbests; set => stats_specbests = value; }
            public List<double> SpeciesMeanBests { get => stats_specbestsmean; set => stats_specbestsmean = value; }
            public Dictionary<int, int> SpeciesCount { get => stats_speciescount; set => stats_speciescount = value; }
            public double[] UpperLowerLimits { get => stats_upperlower; set => stats_upperlower = value; }
            public double[] Quartiles { get => stats_quartiles; set => stats_quartiles = value; }

            public NetworkStatistics()
            {
                stats_means = new List<double>();
                stats_medians = new List<double>();
                //stats_all_quartiles = new List<Tuple<double, double, double, double, double>>();
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

    public class NetworkPopulation : List<NeuralNetwork>
    {
        public NetworkPopulation() : base()
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
                        //network[i].Fitness = 0;
                        this[i].Index = i;
                        this[i].GameFitness = 0;
                        this[i].GameObjects.ResetAll();
                    }
                    break;
                case "BEST":
                    Sort((n, n1) => ((int)(1000 * (n.BestFitness - n1.BestFitness))));
                    for (int i = 0; i < Count; i++)
                    {
                        //network[i].Fitness = 0;
                        this[i].Index = i;
                        this[i].GameFitness = 0;
                        this[i].GameObjects.ResetAll();
                    }
                    break;
            }

        }

    }

    public class NetworkChangedEventArgs : EventArgs
    {
        public NeuralNetwork Network { get; set; }

        public NetworkChangedEventArgs(NeuralNetwork network) : base()
        {
            this.Network = network;
        }
    }
}
