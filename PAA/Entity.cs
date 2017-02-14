using System;
using System.Collections.Generic;
using System.Linq;

namespace PAA
{
    public class Entity
    {
        public int CorrectClausules { get; private set; }
        public int[] GenomImpactClausules { get; private set; }
        public bool[] Genom { get; private set; }
        public bool Satisfability { get; private set; }
        public int Fitness { get; set; }
        public int Weight { get; private set; }
        public Delegate EvalulationMethod { get; set; }    

        public Entity(int size, Delegate method)
        {
            Satisfability = true;
            CorrectClausules = 0;
            Genom = new bool[size];
            GenomImpactClausules = new int[size];
            EvalulationMethod = method;
        }

        public void CountFitness(int [] weights)
        {
            var parameters = Genom.Cast<object>().ToList();
            parameters.Add(this);
            EvalulationMethod.DynamicInvoke(parameters.ToArray());

            CountWeight(weights);

            if (Satisfability)
            {
                Fitness = Parameters.CLAUSULES_COUNT * Parameters.FITNESS_CORRECTNESS_VALUE + Weight * Parameters.FITNESS_COST_VALUE + GenomImpactClausules.Sum();
            }
            else
            {
                Fitness = CorrectClausules * Parameters.FITNESS_CORRECTNESS_VALUE + GenomImpactClausules.Sum();
            }
        }

        private void CountWeight(int[] weights)
        {
            for (int i = 0; i < Genom.Count(); i++)
            {
                if (Genom[i]) Weight += weights[i];
            }
        }

        public void Mutation()
        {
            for (int i = 0; i < Genom.Length; i++)
            {
                if (Parameters.RANDOM.NextDouble() < Parameters.MUTATION_FACTOR)
                {
                    if(Parameters.RANDOM.NextDouble() < 0.2)
                    {
                        Genom[i] = Parameters.RANDOM.NextDouble() > 0.5 ? false : true;
                    }
                    else
                    {
                        Genom[i] = !Genom[i];
                    }
                }
            }
        }

        public void IsFalse(Dictionary<string, bool> values)
        {
            AddImpactForLiterals(values);
            Satisfability = false;
        }

        public void IsTrue(Dictionary<string, bool> values)
        {
            AddImpactForLiterals(values);
            CorrectClausules++;
        }

        private void AddImpactForLiterals(Dictionary<string, bool> values)
        {
            var array = values.Select(x => new Tuple<int, bool>(GeneticAlgorithmSolver.ArgumentsDictionary[x.Key], x.Value == Genom[GeneticAlgorithmSolver.ArgumentsDictionary[x.Key]])).ToArray();

            for (int i = 0; i < array.Length; i++)
            {
                if (array[i].Item2 && !array.Any(x => x != array[i] && x.Item2))
                {
                    GenomImpactClausules[array[i].Item1]++;
                }
            }



            //foreach (var literalInClausule in values)
            //{
            //    var index = GeneticAlgorithmSolver.ArgumentsDictionary[literalInClausule.Key];
            //    var valueInGenom = Genom[index];
            //    if (literalInClausule.Value)
            //    {
            //        if (valueInGenom)
            //            GenomImpactClausules[index]++;
            //    }
            //    else
            //    {
            //        if (!valueInGenom)
            //            GenomImpactClausules[index]++;
            //    }
            //}
        }
    }
}
