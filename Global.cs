using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Learnr
{
    public class GlobalVars : GlobalConstants
    {
        public static GlobalVars R;

        public GlobalVars(int nodeinputcount, int nodeoutputcount) : base(nodeinputcount, nodeoutputcount)
        {
        }

        public GlobalVars() : base()
        {

        }
    }
    
    public abstract class GlobalConstants
    {
        // ~~Evolution parameters~~
        private int initPoolSize = 200;
        private int pregenerations = 2;
        private float cullpercentage = 15f;
        private double defaultMutateProb = 0.75;
        private double defaultMutateFact = 0.08;
        private double timelimit = 30;
        // ~~Misc~~
        private int seed = 129;
        private int initThreadCount = 180;
        private bool initPoolRandomStructures = false;
        private int computefreq = 60;
        private string speciesnamecharmask = "abcefghijlmnopqrstuvw";
        private int speciesmatchdegree = 3;
        private int graphmaxpoints = 200;
        private const string psudoSpeciesPrefix = "$";
        public float nodelayer_colourdim = 0.75f;
        private int threadcountincrement = 10;
        private float scrollWheelSensitity = 0.2f;
        // ~~Neural Network Config~~
        private readonly int nodeinputcount = s_nodeinputcount;//
        private int[] nodelayers = new int[] { 14, 10 };
        private readonly int nodeoutputcount = s_nodeoutputcount;//
        private int nodefeedbackcount = 2;
        public int[] lstm_enabledlayers = new int[] { 2 };

        protected static int s_nodeinputcount = 8;
        protected static int s_nodeoutputcount = 3;

        public GlobalConstants(int nodeinputcount, int nodeoutputcount)
        {
            this.nodeinputcount = s_nodeinputcount = nodeinputcount;
            this.nodeoutputcount = s_nodeoutputcount = nodeoutputcount;
        }

        public GlobalConstants()
        {

        }
        
        public int InitSize { get => initPoolSize; set => initPoolSize = value; }
        public int PreGenerations { get => pregenerations; set => pregenerations = value; }
        public float CullPercentage { get => cullpercentage; set => cullpercentage = value; }
        public double DefaultMutateProb { get => defaultMutateProb; set => defaultMutateProb = value; }
        public double DefaultMutateFact { get => defaultMutateFact; set => defaultMutateFact = value; }
        public double TimeLimit { get => timelimit; set => timelimit = value; }
        public int Seed { get => seed; set => seed = value; }
        public int InitThreadCount { get => initThreadCount; set => initThreadCount = value; }
        public bool InitPoolRandomStructures { get => initPoolRandomStructures; set => initPoolRandomStructures = value; }
        public int ComputeFrequency { get => computefreq; set => computefreq = value; }
        public int ThreadCountIncrement { get => threadcountincrement; set => threadcountincrement = value; }
        public float ScrollWheelSensitity { get => scrollWheelSensitity; set => scrollWheelSensitity = value; }
        public string SpeciesMask { get => speciesnamecharmask; set => speciesnamecharmask = value; }
        public int SpeciesMatchDegree { get => speciesmatchdegree; set => speciesmatchdegree = value; }
        public int InputNodesCount { get => nodeinputcount; }
        public int TotalLHSNodesCount { get => nodeinputcount + nodefeedbackcount + 1; }
        public int TotalRHSNodesCount { get => nodeoutputcount + nodefeedbackcount; }
        public int[] NodeLayers { get => nodelayers; set => nodelayers = value; }
        public int OutputNodesCount { get => nodeoutputcount; }
        public int FeedbackNodeCount { get => nodefeedbackcount; set => nodefeedbackcount = value; }
        public int GraphMaxPoints { get => graphmaxpoints; set => graphmaxpoints = value; }

        public static string PsudoSpeciesPrefix => psudoSpeciesPrefix;



    }
    public static class XmlExtension
    {
        public static string Serialize<T>(this T value)
        {
            if (value == null) return string.Empty;

            var xmlSerializer = new XmlSerializer(typeof(T));

            using (var stringWriter = new StringWriter())
            {
                using (var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings { Indent = true }))
                {
                    xmlSerializer.Serialize(xmlWriter, value);
                    return stringWriter.ToString();
                }
            }
        }
        public static object LoadSettingsFromFile<T>(string filename, out T R)
        {
            XmlSerializer deserializer = new XmlSerializer(typeof(T));
            using (TextReader reader = new StreamReader(filename))
            {
                object obj = deserializer.Deserialize(reader);
                R = (T)obj;
                return obj;
            }
        }

    }
}
