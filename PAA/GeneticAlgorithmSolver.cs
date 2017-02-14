using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace PAA
{
    public class GeneticAlgorithmSolver
    {
        // Static properties for global usage
        public static Dictionary<string, int> ArgumentsDictionary { get; set; }
        public static Entity BestEntity { get; set; }
        public static int[] Weights { get; set; }
        public static Delegate EvaluationMethod;

        private int SameSteps = 0;
        private int generationCount = 0;
        
        private Dictionary<string, ParameterExpression> parametersDictionary;
        private int genomSize;
        private Generation currentGeneration;

        public GeneticAlgorithmSolver(int[] weights, Delegate evaluationMethod, Dictionary<string, ParameterExpression> parametersDictionary, Dictionary<string, int> argumentsDictionary)
        {
            Weights = weights;
            EvaluationMethod = evaluationMethod;
            this.parametersDictionary = parametersDictionary;
            genomSize = parametersDictionary.Count;
            ArgumentsDictionary = argumentsDictionary;

            Parameters.Weights = weights;

            InitAlghoritm();
        }

        /// <summary>
        /// Reinicialization of alghorithm for repetitive running
        /// </summary>
        private void InitAlghoritm()
        {
            generationCount = 0;
            currentGeneration = new Generation(genomSize);
            var bestEntity = currentGeneration.GenerateRandomEntity();
            currentGeneration.BestEntity = bestEntity;
            BestEntity = bestEntity;
        }

        /// <summary>
        /// Evolution of given generation to outgoing generation
        /// </summary>
        public Generation Evolution(Generation generation)
        {
            var newGeneration = new Generation(genomSize);
            
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

        /// <summary>
        /// Adaptable mechanism for increasing mutation factor when stuck in local maximum
        /// </summary>
        private void UpdateMutationFactor(Generation newGeneration)
        {
            // If fitness of the best entity of previous generation is the sema like in present one
            // Increase Mutation factor
            if (currentGeneration.BestEntity.Fitness == newGeneration.BestEntity.Fitness)
            {
                SameSteps++;
                if (SameSteps % Parameters.MUTATION_STEP == 0)
                {
                    // For generation without satisfable entity set maximum mutation factor immediately for quicker find some solution
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
            }
            else
            {
                SameSteps = 0;
                Parameters.MUTATION_FACTOR = Parameters.MINIMUM_MUTATION_FACTOR;
            }            
            currentGeneration = newGeneration;
        }

        /// <summary>
        /// Main method which starts the evolution
        /// </summary>
        /// <returns></returns>
        public void Evolve()
        {
            InitAlghoritm();
            var generation = new Generation(genomSize);
            generation.InitializePopulation();
            Parameters.CERTAINITY = false;
            for (int i = 0; i < Parameters.GENERATIONS_COUNT; i++)
            {
                generation = Evolution(generation);
                if (Parameters.CERTAINITY)
                    break;
            }
        }

        public void Run()
        {
            Evolve();
            PrintBestSolution();
        }

        /// <summary>
        /// Output of best global known solution
        /// </summary>
        private void PrintBestSolution()
        {
            Console.WriteLine("\nGenetic Alghoritm:\n");
            if (!BestEntity.Satisfability)
            {
                Console.WriteLine("There doesn't exist solution\nBest solution has {0} clausules TRUE, Fitness {1}", BestEntity.CorrectClausules, BestEntity.Fitness);
                return;
            }
            for (int i = 0; i < BestEntity.Genom.Length; i++)
            {
                // print of configuration variables
                Console.Write(string.Format("{0,3} ", BestEntity.Genom[i] ? "1" : "0"));
            }
            Console.WriteLine("\n");

            Console.WriteLine("Impact:");

            for (int i = 0; i < BestEntity.Genom.Length; i++)
            {
                Console.Write(string.Format("{0,3} ", BestEntity.GenomImpactClausules[i]));
            }

            Console.WriteLine("\nWeight: {0}\n", BestEntity.Weight);

        }

    }
}
