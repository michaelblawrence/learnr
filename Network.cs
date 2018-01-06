using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Learnr
{
    public abstract class NeuralNetwork
    {
        protected double[][] nn = null;
        //protected!
        protected double[][][] ax = null;
        public NodeLayerCollection nodes;

        protected int countInputs = -1;
        protected int countOutputs = -1;
        protected int[] countsLayers;

        public int Inputs { get { return countInputs; } }
        public int Outputs { get { return countOutputs; } }
        public int[] Layers { get { return countsLayers; } }

        protected GameObjectCollection gameObjects;
        public GameObjectCollection GameObjects { get => gameObjects; set => gameObjects = value; }
        public GameObjectCollection GO { get => gameObjects; set => gameObjects = value; }
        //public Ball Ball { get => gameObjects.Ball; }
        //public Paddle Paddle { get => gameObjects.Paddle; }

        public double[] FB { get => gameObjects.Feedback; set => gameObjects.Feedback = value; }
        public int GameTime { get => gameObjects.Time; set => gameObjects.Time = value; }
        public double GameFitness { get => gameObjects.Fitness; set => gameObjects.Fitness = value; }

        public int DOB { get; set; }

        protected double fitness = 0;
        public double Fitness { get => fitness; set { fitness = value; BestFitness = value > BestFitness ? value : BestFitness; InheritedBestFitness = value > InheritedBestFitness ? value : InheritedBestFitness; } }
        public double BestFitness { get; set; }
        public double SortingFitness { get => (BestFitness + Fitness) / 2; }

        public double InheritedBestFitness { get; set; }

        public int Size { get { return nn.Length; } }
        public double[][][] Axons { get => ax; }
        public int AxonCount { get { return ax.Length * ax[0].Length * ax[0][0].Length; } }
        public string AxonHash { get => axonHash; }

        public int Species { get => species; set { species = value; speciesCodeName = GetCodeNameFromSpecies(value); gameObjects.SetSpecies(value); } }

        public string SpeciesCodeName { get => DOB > 1 ? speciesCodeName : speciesCodeName + " (OG)"; }
        public int Index { get => index; set { index = value; gameObjects?.SetIndex(index); } }
        public int ID { get => id; }

        public bool IsChild { get => isChild; }
        public int FamilyHistoryLength { get => familyHistory; }

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

        public NeuralNetwork(NeuralNetwork parent, int seed, int dob, bool mutate) : this(parent, seed, dob)
        {
            if (mutate) Mutate();
        }

        public NeuralNetwork(NeuralNetwork parent, int seed, int dob) : this(parent.Inputs, parent.Layers, parent.Outputs, dob)
        {
            if (parent != null)
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
                species = parent.Species;
                speciesCodeName = parent.speciesCodeName;
                axonHash = parent.AxonHash;
                InheritedBestFitness = Math.Max(parent.InheritedBestFitness, parent.Fitness);
                isChild = true;
                familyHistory = parent.familyHistory + 1;
            }
        }

        public NeuralNetwork(int inputs, int[] layers, int outputs, int dob)
        {
            countInputs = inputs;
            countOutputs = outputs;
            countsLayers = (int[])layers.Clone();
            DOB = dob;
            id = spawnedNetworks++;

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

            species = GenerateRandomSpeciesColour();
            speciesCodeName = GetCodeNameFromSpecies(species);
        }

        public static int GenerateRandomSpeciesColour()
        {
            double cx = Math.Sin(2 * Math.PI * rand.NextDouble()) * 0.4 + 0.5;
            double cy = Math.Sin(2 * Math.PI * (rand.NextDouble() + 1 / 3)) * 0.4 + 0.5;
            double cz = Math.Sin(2 * Math.PI * (rand.NextDouble() + 2 / 3)) * 0.4 + 0.5;
            return Color.FromArgb(255, (int)(cx * 255), (int)(cy * 255), (int)(cz * 255)).ToArgb();
        }

        public void ReassignSpecies(int colour, string codename)
        {
            species = colour;
            speciesCodeName = codename;
            gameObjects.SetSpecies(species);
        }

        public static string GetCodeNameFromSpecies(Int32 species)
        {
            return GetCodeNameFromSpecies(species, true);
        }
        public static string GetCodeNameFromSpecies(Int32 species, bool littleendian)
        {
            string s = "";
            string mask = GlobalVars.R.SpeciesMask;
            uint val = (UInt32)species;
            uint cc = (uint)mask.Length;
            //List<int> inds = new List<int>();
            while (val != 0)
            {
                uint mod = val % cc;
                string newchar = mask.ElementAt((int)mod).ToString();
                if (littleendian)
                    s += newchar;
                else
                    s = s.Insert(0, newchar);
                //inds.A
                val -= mod;
                val /= cc;
            }
            return s;
        }


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


        public void Mutate()
        {
            if (mMutateIndiv)
                Mutate(mMutateFact, mMutateProb);
            else
                Mutate(GlobalVars.R.DefaultMutateFact, GlobalVars.R.DefaultMutateProb);
        }

        public void Mutate(double factor, double prob)
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
                            //ax[x][y][z] *= (rand.NextDouble() < (prob * factor) ? -1 : 1);
                        }
                    }
                }
            }
            RecalculateHash();
        }

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

        public double[] GetAxons(int layer, int nodeIndex)
        {
            return ax[layer][nodeIndex];
        }

        public double[][] GetAxons(int layer)
        {
            return ax[layer];
        }

        public double[][][] GetAxons()
        {
            return ax;
        }

        public double[] GetNodes(int layer)
        {
            return nn[layer];
        }

        public static void InitRandom()
        {
            rand = new Random(GlobalVars.R.Seed);
        }

        public NeuralNetwork Generate()
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
            RecalculateHash();
            return this;
        }

        public double[] ComputeOutputs(double[] inputs)
        {
            for (int x = 0; x < nn.Length; x++) if (x > 0) Array.Clear(nn[x], 0, nn[x].Length);
            Array.Copy(inputs, nn[0], Math.Min(inputs.Length, nn.First().Length));


            for (int x = 1; x < nn.Length; x++)
            {
                for (int y = 0; y < nn[x].Length; y++)
                {
                    double rawnode = 0;
                    for (int z = 0; z < ax[x - 1].Length; z++)
                    {
                        rawnode += ax[x - 1][z][y] * nn[x - 1][z];
                    }
                    //if (y < nn[x].Length - 1)
                    nn[x][y] = 1 / (1 + Math.Exp(-rawnode));
                    //else
                    //    nn[x][y] = rawnode;
                }
            }

            return nn.Last();
        }

        public void InitDisplayObjects(UiSettings uiSettings, int index, int fbnodecount)
        {
            NodeLayer.SetDefaultProps(ref uiSettings, 20, 5, Color.LightCoral);
            nodes = new NodeLayerCollection();
            for (int i = 0; i < nn.Length; i++)
            {
                //if (i < nn.Length - 1)
                nodes.Add(new NodeLayer(nn[i].Length, fbnodecount));
                nodes[i].y = uiSettings.Height / 2;
                nodes[i].Index = i;
                nodes[i].Count = nn.Length;
            }
            InitGameObjects(uiSettings, index, fbnodecount);

            //nodes.Add(new NodeLayer(2));
        }


        public abstract void InitGameObjects(UiSettings uiSettings, int index, int fbnodecount);

        public override string ToString()
        {
            return String.Format("{0}. DOB: {001} [{2}]", speciesCodeName, DOB, index);
        }
    }

    

}
