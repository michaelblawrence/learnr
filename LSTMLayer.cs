using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Learnr
{
    public class LSTMLayer
    {
        /// <summary>
        /// The size of the layer.
        /// </summary>
        int n;
        /// <summary>
        /// Number of LSTM gates per node
        /// </summary>
        const int N_ij = 3;
        /// <summary>
        /// Instance of Random class. For gate weights initialisation.
        /// </summary>
        private static Random r = new Random();

        /// <summary>
        /// Gets the size of the layer.
        /// </summary>
        /// <value>The size of the layer.</value>
        public int N { get => n; }

        /// <summary>
        /// Gets or sets the layer's activation outputs.
        /// </summary>
        /// <value>Array of activation outputs, y_i.</value>
        public double[] y_i { get => y_j; set => y_j = value; }

        /// <summary>
        /// Gets or sets the index of the layer.
        /// </summary>
        /// <value>The layer index.</value>
        public int Index { get => index; set => index = value; }

        public double[] w_ij;
        public double[] s_j;
        internal double[] y_j;

        private static bool randomTestRun = false;
        private int index = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Learnr.LSTMLayer"/> class.
        /// </summary>
        /// <param name="n">Size of the layer.</param>
        public LSTMLayer(int n)
        {
            Init(n);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="T:Learnr.LSTMLayer"/> class. Must be initialized with an Init call
        /// </summary>
        public LSTMLayer()
        {

        }

        /// <summary>
        /// Initializes the LSTM layer.
        /// </summary>
        /// <returns>The init.</returns>
        /// <param name="n">N.</param>
        public void Init(int n)
        {
            this.n = n;
            if (w_ij?.Length != n)
            {
                w_ij = new double[n];
            }
            s_j = new double[n];
            y_j = new double[n];
        }

        /// <summary>
        /// Generates randomly distributed weights for the LSTM gates.
        /// </summary>
        public void Generate()
        {
            if (!randomTestRun)
            {
                RunRandomTest();
            }
            RandomiseArray(w_ij);
        }

        /// <summary>
        /// Copies from specified LSTM layer.
        /// </summary>
        /// <param name="layer">Layer.</param>
        public void CopyFrom(LSTMLayer layer)
        {
            Array.Copy(layer.w_ij, w_ij, n);
        }

        /// <summary>
        /// Mutates the LSTM gate weights with a specified range and probability.
        /// </summary>
        /// <returns>The mutate.</returns>
        /// <param name="factor">Maximum factor for mutation.</param>
        /// <param name="prob">Probability of a single mutatation event occuring.</param>
        public void Mutate(double factor, double prob)
        {
            double guessednum;
            for (int x = 0; x < w_ij.Length; x++)
            {
                guessednum = r.NextDouble();
                if (guessednum < prob)
                {
                    guessednum = r.NextDouble() - 0.5;
                    double f = factor * guessednum * 2;
                    w_ij[x] += Math.Abs(w_ij[x]) * f;
                }
            }
        }

        /// <summary>
        /// Randomises a given array.
        /// </summary>
        /// <param name="array">Source array.</param>
        internal static void RandomiseArray(double[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = r.NextDouble();
            }
        }

        /// <summary>
        /// Randomises a given 2-dimensional array.
        /// </summary>
        /// <param name="array">Source array.</param>
        internal static void RandomiseArray(double[,] array)
        {
            int c0 = array.GetLength(0);
            int c1 = array.GetLength(1);
            for (int i = 0; i < c0; i++)
            {
                for (int j = 0; j < c1; j++)
                {
                    array[i, j] = r.NextDouble();
                }
            }
        }

        /// <summary>
        /// Floods an array with a given value.
        /// </summary>
        /// <param name="array">Array to be flooded.</param>
        /// <param name="value">Value to set to each array element.</param>
        internal static void FloodArray(double[,] array, double value)
        {
            int c0 = array.GetLength(0);
            int c1 = array.GetLength(1);
            for (int i = 0; i < c0; i++)
            {
                for (int j = 0; j < c1; j++)
                {
                    array[i, j] = value;
                }
            }
        }

        /// <summary>
        /// Runs the random test. 
        /// Throws NotImplementedException if random instance is unable to produce non-zero values
        /// </summary>
        private static void RunRandomTest()
        {
            bool testPassed = false;
            int COUNT = 100;
            double[] samples = new double[COUNT];
            for (int i = 0; i < COUNT; i++)
            {
                samples[i] = r.NextDouble();
            }

            double MeanSquared = samples.Sum(x => x * x);

            testPassed = MeanSquared > 0;
            randomTestRun = true;
            if (!testPassed) throw new NotImplementedException();
        }

        /// <summary>
        /// Clears and resets the cell state.
        /// </summary>
        public void Clear()
        {
            Array.Clear(s_j, 0, n);
        }

        /// <summary>
        /// Computes the cell state for each node.
        /// </summary>
        /// <returns>Array representing the computed state of each node.</returns>
        /// <param name="layers">Input ndoe layer instance.</param>
        public double[] ComputeState(LSTMLayerCollection layers)
        {
            // buffer current cell states
            double[] s_hat = (double[])s_j.Clone();

            // reset state array
            Array.Clear(s_j, 0, n);

            // input node
            int inputindex = (int)LSTMGateLayer.GateType.Input;
            layers.g_ji[inputindex].Compute(layers[index]);
            for (int j = 0; j < n; j++)
            {
                s_j[j] += layers.g_ji[inputindex].y_i[j] * layers.InputValues[j];
            }

            // memory cell node
            int forgetindex = (int)LSTMGateLayer.GateType.Forget;
            layers.g_ji[forgetindex].Compute(layers[index]);
            for (int j = 0; j < n; j++)
            {
                s_j[j] += layers.g_ji[forgetindex].y_i[j] * s_hat[j];
            }

            // return state node
            return s_j;
        }

        /// <summary>
        /// Computes the outputs value for each node after a single recurrent iteration.
        /// </summary>
        /// <returns>Array representing the weighted output of each LSTM node in the layer.</returns>
        /// <param name="layers">Input ndoe layer instance.</param>
        public double[] ComputeOutputs(LSTMLayerCollection layers)
        {
            // activation function
            for (int i = 0; i < n; i++)
            {
                y_j[i] = f_j(s_j[i]);
            }

            // output node
            Array.Clear(layers.OutputValues, 0, n);
            int outputindex = (int)LSTMGateLayer.GateType.Output;
            layers.g_ji[outputindex].Compute(layers[index]);
            for (int j = 0; j < n; j++)
            {
                layers.OutputValues[j] = layers.g_ji[outputindex].y_i[j] * w_ij[j] * s_j[j];
            }

            // return weighted outputs
            return layers.OutputValues;
        }

        /// <summary>
        /// Propagates a new set on input values and compute the new cell state and outputs.
        /// </summary>
        /// <returns>Array representing the weighted output of each LSTM node in the layer.</returns>
        /// <param name="layers">Input ndoe layer instance.</param>
        public double[] Compute(LSTMLayerCollection layers)
        {
            ComputeState(layers);
            return ComputeOutputs(layers);
        }

        /// <summary>
        /// Compute the activation using sigmoid function
        /// </summary>
        /// <returns>Sigmoid activated double precision number.</returns>
        /// <param name="s_j">Input value.</param>
        internal static double f_j(double s_j) => 1 / (1 + Math.Exp(-s_j));

    }

    public class LSTMLayerCollection : List<LSTMLayer>
    {
        private double[] x_j;
        private double[] h_j;

        /// <summary>
        /// The LSTM gate sets for LSTM node processing.
        /// </summary>
        internal LSTMDeepGateLayer[] g_ji = new LSTMDeepGateLayer[3];

        readonly int n;
        /// <summary>
        /// Gets the size of each layer.
        /// </summary>
        /// <value>The n.</value>
        public int N => n;

        /// <summary>
        /// Gets or sets the input node values for processing in the next LSTMLayer.Compute call.
        /// </summary>
        /// <value>The input values.</value>
        public double[] InputValues { get => x_j; set { x_j = value; Invalidated = true; } }
        /// <summary>
        /// Gets or sets the activated output node values from the last LSTMLayer.Compute call.
        /// </summary>
        /// <value>The activated output values.</value>
        public double[] OutputValues { get => h_j; internal set { h_j = value; Invalidated = false; } }
        /// <summary>
        /// Gets or sets the gate sets for LSTM node processing.
        /// </summary>
        /// <value>The LSTM gates.</value>
        public LSTMDeepGateLayer[] Gates { get => g_ji; set => g_ji = value; }

        /// <summary>
        /// Gets a value indicating whether this <see cref="T:Learnr.LSTMLayerCollection"/> is invalidated.
        /// </summary>
        /// <value><c>true</c> if invalidated; otherwise, <c>false</c>.</value>
        public bool Invalidated { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Learnr.LSTMLayerCollection"/> class.
        /// </summary>
        /// <param name="n">Size of the layer.</param>
        public LSTMLayerCollection(int n) : base()
        {
            this.n = n;
            for (int i = 0; i < g_ji.Length; i++)
            {
                g_ji[i] = new LSTMDeepGateLayer(n, (LSTMDeepGateLayer.GateType)i);
            }
            x_j = new double[n];
            h_j = new double[n];
        }

        /// <summary>
        /// Add another LSTM layer to this instance.
        /// </summary>
        public void Add()
        {
            var lstm = new LSTMLayer(n);
            lstm.Index = this.Count;
            this.Add(lstm);
        }

        /// <summary>
        /// Generates randomly distributed weights for the LSTM axons and gates.
        /// </summary>
        public void Generate()
        {
            for (int i = 0; i < this.Count; i++)
                this[i].Generate();
            for (int i = 0; i < g_ji.Length; i++)
                g_ji[i].Generate();
        }

        /// <summary>
        /// Copies from specified collection of LSTM layers.
        /// </summary>
        /// <param name="collection">Collection of LSTM layers.</param>
        public void CopyFrom(LSTMLayerCollection collection)
        {
            for (int i = 0; i < g_ji.Length; i++)
            {
                g_ji[i].CopyFrom(collection.g_ji[i]);
            }
            for (int i = 0; i < Count; i++)
            {
                this[i].CopyFrom(collection[i]);
            }
        }

        /// <summary>
        /// Mutate the axion weights of this instance
        /// </summary>
        /// <param name="factor">Maximuim factor of axion weight mutatation.</param>
        /// <param name="prob">Probability of axion weight mutatation occuring.</param>
        public void Mutate(double factor, double prob)
        {
            foreach (var g in g_ji)
            {
                g.Mutate(factor, prob);
            }
            foreach (var l in this)
            {
                l.Mutate(factor, prob);
            }
        }
    }
}
