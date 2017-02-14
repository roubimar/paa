using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace PAA
{
    public class GeneticAlgorithmSolver
    {
        private int SameSteps = 0;

        private int generationCount = 0;

        private int[] weights;
        private int[] heatMap;
        private Delegate evaluationMethod;
        private Dictionary<string, ParameterExpression> parametersDictionary;
        public static Dictionary<string, int> ArgumentsDictionary;
        public static Entity BestEntity;
        private Generation currentGeneration;

        public GeneticAlgorithmSolver(int[] weights, Delegate evaluationMethod, Dictionary<string, ParameterExpression> parametersDictionary, Dictionary<string, int> argumentsDictionary)
        {
            this.weights = weights;
            this.evaluationMethod = evaluationMethod;
            this.parametersDictionary = parametersDictionary;
            ArgumentsDictionary = argumentsDictionary;

            Parameters.Weights = weights;

            InitAlghoritm();
        }

        private void InitAlghoritm()
        {
            heatMap = new int[weights.Length];

            generationCount = 0;
            currentGeneration = new Generation(weights.Length, weights, evaluationMethod);
            var bestEntity = currentGeneration.GenerateRandomEntity();
            currentGeneration.BestEntity = bestEntity;
            BestEntity = bestEntity;
        }

        public Generation Evolution(Generation generation)
        {
            var newGeneration = new Generation(generation.GenomSize, generation.Weights, evaluationMethod);

            generation.EntitiesByImpact = generation.Entities.OrderBy(y => y.GenomImpactClausules.Sum()).ToList();
            //generation.EntitiesByHamilton = generation.Entities.OrderBy(x => CalculateHamiltonLength(x, BestEntity)).ToList();


            for (int i = 0; i < (Parameters.POPULATION_SIZE + 1)/ 2 - Parameters.ELITES_COUNT; i++)
            {
                CreateNewEntities(newGeneration, generation, i);
            }

            //Elitism
            Elitism(newGeneration, generation);

            newGeneration.Entities.Sort(EntityComparator.Instance);
            newGeneration.CheckDifference();
            newGeneration.BestEntity = newGeneration.Entities.First();

            bool best = false;
            if (BestEntity == null || newGeneration.BestEntity.Fitness > BestEntity.Fitness)
            {
                BestEntity = newGeneration.BestEntity;
                best = true;
            }

            generationCount++;
            if (best)
                PrintBest();
            UpdateMutationFactor(newGeneration);
            return newGeneration;
        }

        private static int CalculateHamiltonLength(Entity counted, Entity referenced)
        {
            int hamiltonLength = 0;
            for (int i = 0; i < counted.Genom.Length; i++)
            {
                if (counted.Genom[i] != referenced.Genom[i])
                    hamiltonLength++;
            }

            if (referenced.Fitness * 0.7 > counted.Fitness)
            {
                return -1;
            }
            return hamiltonLength;
        }

        private void CreateNewEntities(Generation newGeneration, Generation generation, int i)
        {
            var firstParent = generation.SelectionMethod(generation);
            Entity secondParent = null;
            do
            {
                secondParent = generation.SelectionMethod(generation);
            } while (firstParent == secondParent || firstParent.Fitness == secondParent.Fitness);
            generation.CrossoverMethod(newGeneration, firstParent, secondParent);
        }

        private void Elitism(Generation newGeneration, Generation generation)
        {
            var addedEntitiesFitness = new List<int>();
            for (int i = 0; addedEntitiesFitness.Count < (Parameters.ELITES_COUNT * 2) - 1 && i < Parameters.POPULATION_SIZE; i++)
            {
                if (!addedEntitiesFitness.Contains(generation.Entities[i].Fitness))
                {
                    addedEntitiesFitness.Add(generation.Entities[i].Fitness);
                    newGeneration.Entities.Add(generation.Entities[i]);
                }
            }
            newGeneration.Entities.Add(BestEntity);

            for (int i = 0; i < (Parameters.POPULATION_SIZE - newGeneration.Entities.Count) + 2 / 2; i++)
            {
                CreateNewEntities(newGeneration, generation, i);
            }
            if (newGeneration.Entities.Count < Parameters.POPULATION_SIZE)
            {
                newGeneration.Entities.Add(BestEntity);
            }
        }

        private void PrintBest()
        {
            Console.WriteLine("SATISFY: {6}, BEST: {0}, GENERATION_STEP: {3}, MUTATION_FACTOR: {4}, IMPACT: {5} ",
                BestEntity.Fitness, currentGeneration.Entities.Where(x => x.Satisfability).Count(),
                currentGeneration.Entities.Count(), generationCount, Parameters.MUTATION_FACTOR,
                BestEntity.GenomImpactClausules.Sum(), BestEntity.Satisfability);
            for (int i = 0; i < BestEntity.Genom.Length; i++)
            {
                // print of configuration variables
                Console.Write(string.Format("{0,3} ", BestEntity.Genom[i] ? "1" : "0"));
            }
            Console.WriteLine();
        }

        private void UpdateMutationFactor(Generation newGeneration)
        {
            var tmpFitness = newGeneration.BestEntity.Fitness;


            if (currentGeneration.BestEntity.Fitness == tmpFitness)
            {
                SameSteps++;
                if (SameSteps % Parameters.MUTATION_STEP == 0)
                {
                    if (currentGeneration.BestEntity.Satisfability)
                    {
                        Parameters.MUTATION_FACTOR += Parameters.MUTATION_STEP_SIZE;
                        Parameters.MUTATION_FACTOR = Math.Min(Parameters.MUTATION_FACTOR, Parameters.MAXIMUM_MUTATION_FACTOR);
                    }
                    else
                    {
                        Parameters.MUTATION_FACTOR = Parameters.MAXIMUM_MUTATION_FACTOR;
                    }
                }
                //if (SameSteps % 25 == 0)
                //{
                //    newGeneration.Entities.Sort(CustomEntityComparator.Instance);
                //}
                //if (SameSteps % 17 == 0)
                //{
                //    Parameters.CROSSOVER_TYPE = Parameters.CROSSOVER_TYPE == CrossoverEnum.Custom ?
                //        CrossoverEnum.Simple : Parameters.CROSSOVER_TYPE == CrossoverEnum.Simple ?
                //        CrossoverEnum.Random : CrossoverEnum.Custom;
                //}
                //if (SameSteps % 13 == 0)
                //{
                //    Parameters.SELECTION_TYPE = Parameters.SELECTION_TYPE == SelectionEnum.Rank ?
                //        SelectionEnum.Groupping : Parameters.SELECTION_TYPE == SelectionEnum.Groupping ?
                //        SelectionEnum.Simple : SelectionEnum.Rank;
                //}
            }
            else
            {
                SameSteps = 0;
                Parameters.MUTATION_FACTOR = Parameters.MINIMUM_MUTATION_FACTOR;
                for (int i = 0; i < newGeneration.BestEntity.Genom.Length; i++)
                {
                    if (newGeneration.BestEntity.Genom[i] != currentGeneration.BestEntity.Genom[i])
                        heatMap[i]++;
                }
            }

            Parameters.CERTAINITY = Parameters.CERTAINITY || SameSteps > 500;
            currentGeneration = newGeneration;
        }

        public Generation Evolve()
        {
            InitAlghoritm();
            var generation = new Generation(parametersDictionary.Count(), weights, evaluationMethod);
            generation.InitializePopulation();
            Parameters.CERTAINITY = false;
            for (int i = 0; i < Parameters.GENERATIONS_COUNT; i++)
            {
                generation = Evolution(generation);
                if (Parameters.CERTAINITY)
                    break;
            }
            return generation;
        }

        public void Run()
        {
            var time = new Stopwatch();
            var milliseconds = 0L;
            time.Restart();

            Generation generation = Evolve();
            milliseconds = time.ElapsedMilliseconds;
            Console.WriteLine("\nGenetic Alghoritm:\t{0} ms\n", milliseconds);
            PrintBestSolution();
        }

        private void PrintBestSolution()
        {
            if (!BestEntity.Satisfability)
            {
                Console.WriteLine("There doesn't exist solution\nBest solution has {0} clausules TRUE, Fitness {1}, Certainity {2:0.00}", BestEntity.CorrectClausules, BestEntity.Fitness, currentGeneration.Certainity);
                return;
            }
            for (int i = 0; i < BestEntity.Genom.Length; i++)
            {
                // print of configuration variables
                Console.Write(string.Format("{0,3} ", BestEntity.Genom[i] ? "1" : "0"));
            }
            Console.WriteLine("\n");
            Console.WriteLine("HeatMap:");

            for (int i = 0; i < heatMap.Length; i++)
            {
                Console.Write(string.Format("{0,3} ", heatMap[i]));
            }
            Console.WriteLine("\n");


            Console.WriteLine("Impact:");

            for (int i = 0; i < BestEntity.Genom.Length; i++)
            {
                Console.Write(string.Format("{0,3} ", BestEntity.GenomImpactClausules[i]));
            }

            Console.WriteLine("\nWeight: {0}\nCertainity: {1:0.00}\n", BestEntity.Weight, currentGeneration.Certainity);

        }

    }
}
