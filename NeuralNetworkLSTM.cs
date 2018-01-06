using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Learnr
{
    public abstract class NeuralNetworkLSTM
    {
        /// <summary>
        /// Neural Network node value 2-D matrix
        /// </summary>
        protected double[][] nn = null;
        /// <summary>
        /// Neural Network axion value 3-D matrix
        /// </summary>
        protected double[][][] ax = null;

        /// <summary>
        /// Collection of all node layers
        /// </summary>
        public NodeLayerCollection nodes;

        /// <summary>
        /// Collection of LSTM-type node layers
        /// </summary>
        protected LSTMLayerCollection lstm = null;

        /// <summary>
        /// The number of inputs.
        /// </summary>
        protected int countInputs = -1;
        /// <summary>
        /// The number of outputs.
        /// </summary>
        protected int countOutputs = -1;
        /// <summary>
        /// Array containing the number of nodes for each node layer.
        /// </summary>
        protected int[] countsLayers;

        /// <summary>
        /// Gets the number of inputs.
        /// </summary>
        /// <value>The number of inputs.</value>
        public int Inputs { get { return countInputs; } }
        /// <summary>
        /// Gets the number of outputs.
        /// </summary>
        /// <value>The number of outputs.</value>
        public int Outputs { get { return countOutputs; } }
        /// <summary>
        /// Gets the layers.
        /// </summary>
        /// <value>Array containing the number of nodes for each node layer.</value>
        public int[] Layers { get { return countsLayers; } }

        /// <summary>
        /// The game objects for simulation.
        /// </summary>
        protected GameObjectCollection gameObjects;
        /// <summary>
        /// Gets or sets the game objects for simulation.
        /// </summary>
        /// <value>The game objects for simulation.</value>
        public GameObjectCollection GameObjects { get => gameObjects; set => gameObjects = value; }
        /// <summary>
        /// Gets or sets the game objects for simulation.
        /// </summary>
        /// <value>The game objects for simulation.</value>
        public GameObjectCollection GO { get => gameObjects; set => gameObjects = value; }

        /// <summary>
        /// Gets or sets the value of feedback nodes.
        /// </summary>
        /// <value>The fb.</value>
        public double[] FB { get => gameObjects.Feedback; set => gameObjects.Feedback = value; }
        /// <summary>
        /// Gets or sets the time, in seconds, since the simulation began.
        /// </summary>
        /// <value>The game time in seconds.</value>
        public int GameTime { get => gameObjects.Time; set => gameObjects.Time = value; }
        /// <summary>
        /// Gets or sets the fitness score of the current simulation for network ranking.
        /// </summary>
        /// <value>The fitness score.</value>
        public double GameFitness { get => gameObjects.Fitness; set => gameObjects.Fitness = value; }

        /// <summary>
        /// Gets or sets the generation # on which this network was created.
        /// </summary>
        /// <value>The generation of network creation.</value>
        public int DOB { get; set; }

        protected double fitness = 0;

        /// <summary>
        /// Gets or sets the current fitness score of the running simulation.
        /// </summary>
        /// <value>The fitness score.</value>
        public double Fitness { get => fitness; set { fitness = value; BestFitness = value > BestFitness ? value : BestFitness; InheritedBestFitness = value > InheritedBestFitness ? value : InheritedBestFitness; } }
        /// <summary>
        /// Gets or sets this network's best fitness score of the all running simulation.
        /// </summary>
        /// <value>The best fitness score.</value>
        public double BestFitness { get; set; }
        /// <summary>
        /// Gets a weighted fitness score for ranking.
        /// </summary>
        /// <value>The weighted fitness.</value>
        public double SortingFitness { get => (BestFitness + Fitness) / 2; }

        /// <summary>
        /// Gets or sets the best fitness score from the present and any parent networks.
        /// </summary>
        /// <value>The inherited best fitness.</value>
        public double InheritedBestFitness { get; set; }

        /// <summary>
        /// Gets the depth of the node layers.
        /// </summary>
        /// <value>The node depth.</value>
        public int Size { get { return nn.Length; } }
        /// <summary>
        /// Gets the axons 3 dimentional array.
        /// </summary>
        /// <value>3D Array representing the axon values.</value>
        public double[][][] Axons { get => ax; }
        /// <summary>
        /// Gets the total number of axon.
        /// </summary>
        /// <value>The count of axon.</value>
        public int AxonCount { get { return ax.Length * ax[0].Length * ax[0][0].Length; } }
        /// <summary>
        /// Gets a hash representing the axons' values.
        /// </summary>
        /// <value>The axon hash.</value>
        public string AxonHash { get => axonHash; }

        /// <summary>
        /// Gets or sets the species ID.
        /// </summary>
        /// <value>The species ID in species colour Int32 format.</value>
        public int Species { get => species; set { species = value; speciesCodeName = GetCodeNameFromSpecies(value); gameObjects.SetSpecies(value); } }

        public string SpeciesCodeName { get => DOB > 1 ? speciesCodeName : speciesCodeName + " (OG)"; }
        public int Index { get => index; set { index = value; gameObjects?.SetIndex(index); } }
        public int ID { get => id; }

        /// <summary>
        /// Gets a value indicating whether this <see cref="T:Learnr.NeuralNetworkLSTM"/> is a child.
        /// </summary>
        /// <value><c>true</c> if was created from a parent network; otherwise, <c>false</c>.</value>
        public bool IsChild { get => isChild; }
        /// <summary>
        /// Gets the generational length of the parent network history.
        /// </summary>
        /// <value>The length of the family history tree.</value>
        public int FamilyHistoryLength { get => familyHistory; }
        public LSTMLayerCollection LSTM { get => lstm; set => lstm = value; }

        protected bool isChild = false;

        protected int species = 0;
        protected string speciesCodeName = "";
        protected string axonHash = "";
        protected int familyHistory = 0;

        protected int index = -1;
        protected int id = -1;

        protected static Random rand;
        protected static int initseed = GlobalVars.R.Seed;
        protected static int spawnedNetworks = 0;

        protected double mMutateProb = GlobalVars.R.DefaultMutateProb;
        protected double mMutateFact = GlobalVars.R.DefaultMutateFact;
        protected bool mMutateIndiv = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Learnr.NeuralNetworkLSTM"/> class.
        /// </summary>
        /// <param name="parent">Parent network.</param>
        /// <param name="seed">Random generation seed.</param>
        /// <param name="dob">Network DOB, number of generations since start.</param>
        /// <param name="mutate">If set to <c>true</c> parent's cloned axons will mutate.</param>
        public NeuralNetworkLSTM(NeuralNetworkLSTM parent, int seed, int dob, bool mutate) : this(parent, seed, dob)
        {
            if (mutate) Mutate();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Learnr.NeuralNetworkLSTM"/> class.
        /// </summary>
        /// <param name="parent">Parent network.</param>
        /// <param name="seed">Random generation seed.</param>
        /// <param name="dob">Network DOB, number of generations since start.</param>
        public NeuralNetworkLSTM(NeuralNetworkLSTM parent, int seed, int dob) : this(parent.Inputs, parent.Layers, parent.Outputs, dob)
        {
            if (rand == null) rand = new Random(seed);
            if (parent != null)
            {
                CopyFrom(parent);
                lstm.CopyFrom(parent.lstm);
                species = parent.Species;
                speciesCodeName = parent.speciesCodeName;
                axonHash = parent.AxonHash;
                InheritedBestFitness = Math.Max(parent.InheritedBestFitness, parent.Fitness);
                isChild = true;
                familyHistory = parent.familyHistory + 1;
            }
        }

        /// <summary>
        /// Copies axon weights from another network.
        /// </summary>
        /// <param name="parent">Network from which to copy.</param>
        private void CopyFrom(NeuralNetworkLSTM parent)
        {
            ax = new double[parent.ax.Length][][];
            for (int x = 0; x < ax.Length; x++)
            {
                ax[x] = new double[parent.ax[x].Length][];
                for (int y = 0; y < ax[x].Length; y++)
                {
                    ax[x][y] = new double[parent.ax[x][y].Length];
                    Array.Copy(parent.ax[x][y], ax[x][y], parent.ax[x][y].Length);
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Learnr.NeuralNetworkLSTM"/> class.
        /// </summary>
        /// <param name="inputs">Number of input nodes.</param>
        /// <param name="layers">Number of nodes for each node layer.</param>
        /// <param name="outputs">Number of output nodes.</param>
        /// <param name="dob">Network DOB, number of generations since start.</param>
        public NeuralNetworkLSTM(int inputs, int[] layers, int outputs, int dob)
        {
            countInputs = inputs;
            countOutputs = outputs;
            countsLayers = (int[])layers.Clone();
            DOB = dob;
            id = spawnedNetworks++;
            InitNetwork(inputs, layers, outputs);

            int n = Math.Max(0, nn[nn.Length - 2].Length);
            lstm = new LSTMLayerCollection(n);
            lstm.Add();

            if (rand == null) rand = new Random(0);

            species = GenerateRandomSpeciesColour();
            speciesCodeName = GetCodeNameFromSpecies(species);
        }

        /// <summary>
        /// Initialises the network object.
        /// </summary>
        /// <param name="inputs">Number of input nodes.</param>
        /// <param name="layers">Number of nodes for each node layer.</param>
        /// <param name="outputs">Number of output nodes.</param>
        private void InitNetwork(int inputs, int[] layers, int outputs)
        {
            nn = new double[layers.Length + 2][];
            nn[0] = new double[inputs];
            nn[nn.Length - 1] = new double[outputs];
            for (int i = 0; i < layers.Length; i++)
            {
                nn[i + 1] = new double[layers[i]];
            }
            ax = new double[layers.Length + 1][][];
            ax[0] = new double[inputs][];
            for (int x = 0; x < inputs; x++)
            {
                ax[0][x] = new double[layers[0]];
            }
            ax[ax.Length - 1] = new double[layers.Last()][];
            for (int x = 0; x < layers.Last(); x++)
            {
                ax[ax.Length - 1][x] = new double[outputs];
            }
            for (int i = 0; i < layers.Length - 1; i++)
            {
                ax[i + 1] = new double[layers[i]][];
                for (int x = 0; x < layers[i]; x++)
                {
                    ax[i + 1][x] = new double[layers[i + 1]];
                }
            }
        }

        /// <summary>
        /// Generates a random species colour.
        /// </summary>
        /// <returns>Random colour in Int32 format</returns>
        public static int GenerateRandomSpeciesColour()
        {
            double cx = Math.Sin(2 * Math.PI * rand.NextDouble()) * 0.4 + 0.5;
            double cy = Math.Sin(2 * Math.PI * (rand.NextDouble() + 1 / 3)) * 0.4 + 0.5;
            double cz = Math.Sin(2 * Math.PI * (rand.NextDouble() + 2 / 3)) * 0.4 + 0.5;
            return Color.FromArgb(255, (int)(cx * 255), (int)(cy * 255), (int)(cz * 255)).ToArgb();
        }

        /// <summary>
        /// Reassigns the species of this network.
        /// </summary>
        /// <param name="colour">Species ID in form of species colour in Int32 format.</param>
        /// <param name="codename">Codename for species.</param>
        public void ReassignSpecies(int colour, string codename)
        {
            species = colour;
            speciesCodeName = codename;
            gameObjects.SetSpecies(species);
        }

        /// <summary>
        /// Gets the code name from species ID.
        /// </summary>
        /// <returns>The code name from species.</returns>
        /// <param name="species">Species ID in form of species colour in Int32 format.</param>
        public static string GetCodeNameFromSpecies(Int32 species)
        {
            return GetCodeNameFromSpecies(species, true);
        }

        /// <summary>
        /// Gets the code name from species ID.
        /// </summary>
        /// <returns>The code name from species.</returns>
        /// <param name="species">Species ID in form of species colour in Int32 format.</param>
        /// <param name="littleendian">Set to <c>true</c> for little endian encoding, false otherwise.</param>
        public static string GetCodeNameFromSpecies(Int32 species, bool littleendian)
        {
            string s = "";
            string mask = GlobalVars.R.SpeciesMask;
            uint val = (UInt32)species;
            uint cc = (uint)mask.Length;
            while (val != 0)
            {
                uint mod = val % cc;
                string newchar = mask.ElementAt((int)mod).ToString();
                if (littleendian)
                    s += newchar;
                else
                    s = s.Insert(0, newchar);
                val -= mod;
                val /= cc;
            }
            return s;
        }

        /// <summary>
        /// Sets the properties of child axion weight mutatation.
        /// </summary>
        /// <returns><c>true</c>, if mutate properties was set, <c>false</c> otherwise.</returns>
        /// <param name="factor">Maximuim factor of axion weight mutatation.</param>
        /// <param name="prob">Probability of axion weight mutatation occuring.</param>
        public bool SetMutateProperties(double factor, double prob)
        {
            if (factor >= 0 && prob >= 0)
            {
                mMutateProb = Math.Min(1, prob);
                mMutateFact = Math.Min(1, factor);
                mMutateIndiv = true;
            }
            else
            {
                throw new ArgumentOutOfRangeException();
            }
            return mMutateIndiv;
        }

        /// <summary>
        /// Sets the properties of child axion weight mutatation.
        /// </summary>
        /// <param name="factor">Maximuim factor of axion weight mutatation.</param>
        /// <param name="prob">Probability of axion weight mutatation occuring.</param>
        public static void SetDefaultMutateProperties(double factor, double prob)
        {
            if (factor >= 0 && prob >= 0)
            {
                GlobalVars.R.DefaultMutateProb = Math.Min(1, prob);
                GlobalVars.R.DefaultMutateFact = Math.Min(1, factor);
            }
            else
            {
                throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Mutate the axion weights of this instance.
        /// </summary>
        public void Mutate()
        {
            if (mMutateIndiv)
                Mutate(mMutateFact, mMutateProb);
            else
                Mutate(GlobalVars.R.DefaultMutateFact, GlobalVars.R.DefaultMutateProb);
        }

        /// <summary>
        /// Mutate the axion weights of this instance.
        /// </summary>
        /// <param name="factor">Maximuim factor of axion weight mutatation.</param>
        /// <param name="prob">Probability of axion weight mutatation occuring.</param>
        public void Mutate(double factor, double prob)
        {
            MutateAxons(factor, prob);
            lstm.Mutate(factor, prob);
            RecalculateHash();
        }

        /// <summary>
        /// Mutates the axons weights.
        /// </summary>
        /// <param name="factor">Maximuim factor of axion weight mutatation.</param>
        /// <param name="prob">Probability of axion weight mutatation occuring.</param>
        private void MutateAxons(double factor, double prob)
        {
            double guessednum;
            for (int x = 0; x < ax.Length; x++)
            {
                for (int y = 0; y < ax[x].Length; y++)
                {
                    for (int z = 0; z < ax[x][y].Length; z++)
                    {
                        guessednum = rand.NextDouble();
                        if (guessednum < prob)
                        {
                            guessednum = rand.NextDouble() - 0.5;
                            double f = factor * guessednum * 2;
                            ax[x][y][z] += Math.Abs(ax[x][y][z]) * f;
                            if (rand.NextDouble() < (prob * factor))
                            {
                                ax[x][y][z] = -ax[x][y][z];
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Recalculates the hash from axon weights.
        /// </summary>
        public void RecalculateHash()
        {
            if (ax?.Length > 1 && ax[0]?.Length > 1 && ax[0][0]?.Length > 1)
            {
                double factor = ax.Sum(dd => dd.Sum(d => d.Sum(i => i * i))) / AxonCount;
                factor = Math.Log10(factor * 9 + 1);
                int hash = (int)(int.MaxValue * factor);
                axonHash = GetCodeNameFromSpecies(hash, false);
            }
            else axonHash = "";
        }

        /// <summary>
        /// Gets the axon weights of a given node.
        /// </summary>
        /// <returns>The axons weights.</returns>
        /// <param name="layer">Index of node layer.</param>
        /// <param name="nodeIndex">Index of node.</param>
        public double[] GetAxons(int layer, int nodeIndex)
        {
            return ax[layer][nodeIndex];
        }

        /// <summary>
        /// Gets the axon weights of each node in a given node layer.
        /// </summary>
        /// <returns>The axons weights.</returns>
        /// <param name="layer">Index of node layer.</param>
        public double[][] GetAxons(int layer)
        {
            return ax[layer];
        }

        /// <summary>
        /// Gets the axon weights of each node for each node layer.
        /// </summary>
        /// <returns>The axons weights.</returns>
        public double[][][] GetAxons()
        {
            return ax;
        }

        /// <summary>
        /// Gets the node states of each node in a given node layer.
        /// </summary>
        /// <returns>The value of the node's state.</returns>
        /// <param name="layer">Index of node layer.</param>
        public double[] GetNodes(int layer)
        {
            return nn[layer];
        }

        /// <summary>
        /// Inits the Random object. Must be called before Generate.
        /// </summary>
        public static void InitRandom()
        {
            rand = new Random(GlobalVars.R.Seed);
        }

        /// <summary>
        /// Generate the axons and LSTM nodes for instance.
        /// </summary>
        /// <returns>The current network instance.</returns>
        public NeuralNetworkLSTM Generate()
        {
            GenerateAxons();
            lstm.Generate();
            return this;
        }

        /// <summary>
        /// Generates the axon weights using PRNG.
        /// </summary>
        private void GenerateAxons()
        {
            for (int x = 0; x < ax.Length; x++)
            {
                for (int y = 0; y < ax[x].Length; y++)
                {
                    for (int z = 0; z < ax[x][y].Length; z++)
                    {
                        ax[x][y][z] = rand.NextDouble() * 2 - 1;
                    }
                }
            }
        }

        /// <summary>
        /// Computes the network's output node activations.
        /// </summary>
        /// <returns>Output node activations.</returns>
        /// <param name="inputs">Input node values.</param>
        public double[] ComputeOutputs(double[] inputs)
        {
            for (int x = 0; x < nn.Length; x++) if (x > 0) Array.Clear(nn[x], 0, nn[x].Length);
            Array.Copy(inputs, nn[0], Math.Min(inputs.Length, nn.First().Length));

            int last_x = 0;
            for (int x = 1; x < nn.Length; x++)
            {
                if (GlobalVars.R.lstm_enabledlayers.Contains(x))
                {
                    ComputeLayerActivation(x, nn[last_x]);
                    Array.Copy(nn[x], lstm.InputValues, lstm.N);
                    double[] res = lstm.Last().Compute(lstm);
                    Array.Copy(res, nn[x], lstm.N);
                    continue;
                }

                ComputeLayerActivation(x, nn[last_x]);
                last_x = x;
            }

            return nn.Last();
        }

        /// <summary>
        /// Computes the layer activation.
        /// </summary>
        /// <param name="x">Index of node layer.</param>
        /// <param name="input">Input array containing the output of the previous layer.</param>
        private void ComputeLSTMLayerActivation(int x, double[] input)
        {
            for (int y = 0; y < nn[x].Length; y++)
            {
                double rawnode = 0;
                for (int z = 0; z < ax[x - 1].Length; z++)
                {
                    rawnode += ax[x - 1][z][y] * input[z];
                }
                nn[x][y] = 1 / (1 + Math.Exp(-rawnode)); // sigmoid sctivation
            }
        }

        /// <summary>
        /// Computes the layer activation.
        /// </summary>
        /// <param name="x">Index of node layer.</param>
        /// <param name="input">Input array containing the output of the previous layer.</param>
        private void ComputeLayerActivation(int x, double[] input)
        {
            for (int y = 0; y < nn[x].Length; y++)
            {
                double rawnode = 0;
                for (int z = 0; z < ax[x - 1].Length; z++)
                {
                    rawnode += ax[x - 1][z][y] * input[z];
                }
                nn[x][y] = 1 / (1 + Math.Exp(-rawnode)); // sigmoid activation
            }
        }

        /// <summary>
        /// Initialises the neural network preview and game asset display objects.
        /// </summary>
        /// <param name="uiSettings">UI settings context.</param>
        /// <param name="index">Network index.</param>
        /// <param name="fbnodecount">Number of feedback nodes to initialise.</param>
        public void InitDisplayObjects(UiSettings uiSettings, int index, int fbnodecount)
        {
            NodeLayer.SetDefaultProps(ref uiSettings, 20, 5, Color.LightCoral);
            nodes = new NodeLayerCollection();
            for (int i = 0; i < nn.Length; i++)
            {
                nodes.Add(new NodeLayer(nn[i].Length, fbnodecount));
                nodes[i].y = uiSettings.Height / 2;
                nodes[i].Index = i;
                nodes[i].Count = nn.Length;
            }
            InitGameObjects(uiSettings, index, fbnodecount);

        }


        /// <summary>
        /// Initialises the neural network preview and game asset display objects.
        /// </summary>
        /// <param name="uiSettings">UI settings context.</param>
        /// <param name="index">Network index.</param>
        /// <param name="fbnodecount">Number of feedback nodes to initialise.</param>
        public abstract void InitGameObjects(UiSettings uiSettings, int index, int fbnodecount);

        /// <summary>
        /// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:Learnr.NeuralNetworkLSTM"/>.
        /// </summary>
        /// <returns>A <see cref="T:System.String"/> that represents the current <see cref="T:Learnr.NeuralNetworkLSTM"/>.</returns>
        public override string ToString()
        {
            return String.Format("{0}. DOB: {001} [{2}]", speciesCodeName, DOB, index);
        }
    }
}
