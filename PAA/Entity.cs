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

        /// <summary>
        /// Contructor
        /// </summary>
        public Entity(int size)
        {
            Satisfability = true;
            CorrectClausules = 0;
            Genom = new bool[size];
            GenomImpactClausules = new int[size];
        }

        /// <summary>
        /// Calculate the fitness value of entity
        /// </summary>
        public void CountFitness()
        {
            var parameters = Genom.Cast<object>().ToList();
            parameters.Add(this);
            GeneticAlgorithmSolver.EvaluationMethod.DynamicInvoke(parameters.ToArray());

            CountWeight();

            if (Satisfability)
            {
                Fitness = Parameters.CLAUSULES_COUNT * Parameters.FITNESS_CORRECTNESS_VALUE + Weight * Parameters.FITNESS_COST_VALUE;
            }
            else
            {
                Fitness = CorrectClausules * Parameters.FITNESS_CORRECTNESS_VALUE + GenomImpactClausules.Sum();
            }
        }

        /// <summary>
        /// Calculate weight of TRUE alelas
        /// </summary>
        private void CountWeight()
        {
            for (int i = 0; i < Genom.Count(); i++)
            {
                if (Genom[i])
                {
                    Weight += GeneticAlgorithmSolver.Weights[i];
                } 
            }
        }

        /// <summary>
        /// Mutation of genom
        /// </summary>
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

        /// <summary>
        /// Partial evaluation of 1 clausule from formula where result is FALSE
        /// </summary>
        public void IsFalse(Dictionary<string, bool> values)
        {
            AddImpactForLiterals(values);
            Satisfability = false;
        }

        /// <summary>
        /// Partial evaluation of 1 clausule from formula where result is TRUE
        /// </summary>
        public void IsTrue(Dictionary<string, bool> values)
        {
            AddImpactForLiterals(values);
            CorrectClausules++;
        }

        /// <summary>
        /// Add each genom impact value
        /// </summary>
        private void AddImpactForLiterals(Dictionary<string, bool> values)
        {
            var array = values.Select(x => new Tuple<int, bool>(GeneticAlgorithmSolver.ArgumentsDictionary[x.Key], x.Value == Genom[GeneticAlgorithmSolver.ArgumentsDictionary[x.Key]])).ToArray();

            for (int i = 0; i < array.Length; i++)
            {
                // If Genom in present value is TRUE and others literals of clausule are FALSE, then increase Impact of this Genom
                // eg.: (a !b !c) current values are 1 1 1, then a has major(2) impact for satisfability of clausule
                //                                   1 0 1, then a has minor(1) impact and b has minor(1) impact for satisfability of clausule
                var others = array.Where(x => x != array[i] && x.Item2).Count();
                if (array[i].Item2)
                {
                    if(others == 0)
                    {
                        GenomImpactClausules[array[i].Item1] += 2;
                    }
                    if (others == 1)
                    {
                        GenomImpactClausules[array[i].Item1] += 1;
                    }
                }
            }
        }
    }
}
