using Learnr;
using System;

namespace learnr2drive
{
    public class NetworkLSTM : NeuralNetworkLSTM
    {
        public Car Car { get => ((NetworkGameObjects)gameObjects).Car; }
        public Map Map { get => ((NetworkGameObjects)gameObjects).Map; }
        public new double SortingFitness { get => Math.Pow(Math.Sqrt(BestFitness) + Math.Sqrt(Fitness) , 2) / 4; }

        public NetworkLSTM(NetworkLSTM parent, int seed, int dob, bool mutate) : base(parent, seed, dob, mutate)
        {

        }
        public NetworkLSTM(NetworkLSTM parent, int seed, int dob) : base(parent, seed, dob)
        {

        }
        public NetworkLSTM(int inputs, int[] layers, int outputs, int dob) : base(inputs, layers, outputs, dob)
        {

        }
        public static NetworkLSTM GenerateFromTemplate(NetworkLSTM template, int dob)
        {
            return new NetworkLSTM(template.Inputs, template.Layers, template.Outputs, dob).Generate();
        }

        public new NetworkLSTM Generate()
        {
            return (NetworkLSTM)base.Generate();
        }

        public override void InitGameObjects(UiSettings uiSettings, int index, int fbnodecount)
        {
            gameObjects = new NetworkGameObjects(index, uiSettings, Species, fbnodecount);
            if (isChild) gameObjects.SetChild();
        }
    }
}
