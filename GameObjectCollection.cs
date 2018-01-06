using System;
using System.Collections.Generic;

namespace Learnr
{
    public abstract class GameObjectCollection
    {
        protected int index;
        protected int time;
        protected double fitness;
        protected double[] feedback;
        protected int species;

        public int Index { get => index; set => SetIndex(value); }
        public int Time { get => time; set => time = value; }
        public double Fitness { get => fitness; set => fitness = value; }
        public double[] Feedback { get => feedback; set => feedback = value; }
        public int Species { get => species; set => SetSpecies(value); }
        public List<RenderObject> Items { get => GetItems(); }

        public GameObjectCollection(int index, UiSettings uiSettings, int species, int fbnodecount)
        {
            this.index = index;
            this.species = species;
            Feedback = new double[fbnodecount];
        }

        public virtual void ResetAll()
        {
            Array.Clear(Feedback, 0, Feedback.Length);
        }

        public abstract void UpdateElements(int delta, UiSettings settings);

        public virtual void SetSpecies(int species)
        {
            this.species = species;
        }

        public abstract void BeginGeneration();

        public abstract void SetIndex(int index);

        public abstract void SetChild();

        public abstract List<RenderObject> GetItems();
    }
}
