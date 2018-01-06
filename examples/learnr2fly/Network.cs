using Learnr;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace learnr2fly
{
    public class NetworkLSTM : NeuralNetworkLSTM
    {
        public Spaceship Spaceship { get => ((NetworkGameObjects)gameObjects).Spaceship; }
        public Asteroid[] Asteroids { get => ((NetworkGameObjects)gameObjects).Asteroids; }
        public new double SortingFitness { get => (BestFitness + Fitness * 3) / 4; }

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
