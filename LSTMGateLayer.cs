using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Learnr
{
    public class LSTMGateLayer
    {
        int n;
        int index;
        GateType type;
        const int N_ij = 3;
        public enum GateType
        {
            Input, Forget, Output
        }
        private static Random r = new Random();

        public int N { get => n; }

        public GateType Type { get => type; }

        public double[] y_i { get => y_j; set => y_j = value; }

        public double[] w_ij;
        public double[] s_j;
        internal double[] y_j;

        private static bool randomTestRun = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Learnr.LSTMGateLayer"/> class.
        /// </summary>
        /// <param name="n">Size of the gate layer.</param>
        /// <param name="type">Gate type to initialize.</param>
        public LSTMGateLayer(int n, GateType type)
        {
            Init(n, type);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Learnr.LSTMGateLayer"/> class.
        /// </summary>
        public LSTMGateLayer()
        {

        }


        /// <summary>
        /// Initializes a LSTM gate layer.
        /// </summary>
        /// <param name="n">Size of the gate layer.</param>
        /// <param name="type">Gate type to initialize.</param>
        public void Init(int n, GateType type)
        {
            this.n = n;
            this.type = type;
            this.index = (int)type;
            if (w_ij?.Length != n)
            {
                w_ij = new double[n];
            }
            s_j = new double[n];
            y_j = new double[n];
        }

        /// <summary>
        /// Generates randomly distributed weights for the LSTM gate layer.
        /// </summary>
        public void Generate()
        {
            if (!randomTestRun)
            {
                RunRandomTest();
            }

            LSTMLayer.RandomiseArray(w_ij);
        }

        /// <summary>
        /// Copies from specified LSTM gate layer.
        /// </summary>
        /// <param name="collection">Source LSTM gate layer.</param>
        public void CopyFrom(LSTMGateLayer layer)
        {
            Array.Copy(layer.w_ij, w_ij, n);
        }

        /// <summary>
        /// Mutate the gate weights of this instance.
        /// </summary>
        /// <param name="factor">Maximuim factor of axion weight mutatation.</param>
        /// <param name="prob">Probability of axion weight mutatation occuring.</param>
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
        /// Computes and updates the state of the gate.
        /// </summary>
        /// <returns>The new gate states.</returns>
        /// <param name="layer">Previos layer used as input.</param>
        public double[] ComputeState(LSTMLayer layer)
        {
            for (int j = 0; j < n; j++)
            {
                s_j[j] = w_ij[j] * layer.y_i[j]; //ungated memory cell activation
            }
            return s_j;
        }
        /// <summary>
        /// Computes the gate's output activations.
        /// </summary>
        /// <returns>Output node activations.</returns>
        /// <param name="inputs">Input node values.</param>
        public double[] ComputeOutputs()
        {
            for (int i = 0; i < n; i++)
            {
                y_j[i] = f_j(s_j[i]);
            }
            return y_j;
        }

        /// <summary>
        /// Propagates a new set on input values and compute the new gate activation outputs.
        /// </summary>
        /// <returns>Array representing the output activations of each LSTM gate.</returns>
        /// <param name="layers">Input ndoe layer instance.</param>
        public double[] Compute(LSTMLayer layer)
        {
            ComputeState(layer);
            return ComputeOutputs();
        }

        /// <summary>
        /// Compute the activation using sigmoid function
        /// </summary>
        /// <returns>Sigmoid activated double precision number.</returns>
        /// <param name="s_j">Input value.</param>
        internal static double f_j(double s_j) => 1 / (1 + Math.Exp(-s_j));

    }
}
