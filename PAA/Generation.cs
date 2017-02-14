using PAA;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PAA
{
    public class Generation
    {
        public Entity BestEntity;

        public int GenomSize { get; set; }
        public Delegate EvaluationMethod { get; set; }
        public int[] Weights { get; set; }
        public List<Entity> Entities { get; set; }
        public List<Entity> EntitiesByImpact { get; set; }
        public double Certainity { get; set; }

        public delegate void Crossover(Generation g, Entity a, Entity b);
        public Crossover CrossoverMethod;

        public delegate Entity Selection(Generation g);
        public Selection SelectionMethod;

        public Generation(int size, int[] weights, Delegate evaluationMethod)
        {
            Weights = weights;
            GenomSize = size;
            EvaluationMethod = evaluationMethod;
            Entities = new List<Entity>();

            InitiateOperators();
        }

        private void InitiateOperators()
        {
            switch (Parameters.CROSSOVER_TYPE)
            {
                case CrossoverEnum.Random:
                    CrossoverMethod = RandomCrossover;
                    break;
                case CrossoverEnum.Simple:
                    CrossoverMethod = SimpleCrossover;
                    break;
                case CrossoverEnum.Custom:
                    CrossoverMethod = CustomCrossover;
                    break;
            }

            switch (Parameters.SELECTION_TYPE)
            {
                case SelectionEnum.Custom:
                    SelectionMethod = CustomSelection;
                    break;
                case SelectionEnum.Simple:
                    SelectionMethod = SimpleSelection;
                    break;
                case SelectionEnum.Groupping:
                    SelectionMethod = GrouppingSelection;
                    break;
                case SelectionEnum.Rank:
                    SelectionMethod = RankSelection;
                    break;
            }

        }

        public void InitializePopulation()
        {
            for (int i = 0; i < Parameters.POPULATION_SIZE; i++)
            {
                Entities.Add(GenerateRandomEntity());
            }
            Entities.Sort(EntityComparator.Instance);
            BestEntity = Entities.First();
        }

        public Entity GenerateRandomEntity()
        {
            var entity = new Entity(GenomSize, EvaluationMethod);
            for (int i = 0; i < GenomSize; i++)
            {
                entity.Genom[i] = Parameters.RANDOM.NextDouble() > 0.5;
            }
            entity.CountFitness(Weights);
            return entity;
        }

        //Random crossover
        public static void RandomCrossover(Generation generation, Entity firstParent, Entity secondParent)
        {
            if (!CrossoverBefore(generation, firstParent, secondParent))
            {
                var firstChild = new Entity(generation.GenomSize, generation.EvaluationMethod);
                var secondChild = new Entity(generation.GenomSize, generation.EvaluationMethod);

                for (int i = 0; i < generation.GenomSize; i++)
                {
                    firstChild.Genom[i] = Parameters.RANDOM.NextDouble() > 0.5 ? firstParent.Genom[i] : secondParent.Genom[i];
                    secondChild.Genom[i] = Parameters.RANDOM.NextDouble() > 0.5 ? firstParent.Genom[i] : secondParent.Genom[i];
                }

                CrossoverAfter(generation, firstChild, secondChild);
            }
        }

        private static void GenerateGenom(Entity child, Generation generation, Entity firstParent, Entity secondParent, int sum, int index)
        {
            var random = Parameters.RANDOM.Next(0, sum);
            if (random < firstParent.GenomImpactClausules[index])
            {
                child.Genom[index] = firstParent.Genom[index];
            }
            else
            {
                child.Genom[index] = secondParent.Genom[index];
            }
        }

        //Custom crossover
        public static void CustomCrossover(Generation generation, Entity firstParent, Entity secondParent)
        {
            if (!CrossoverBefore(generation, firstParent, secondParent))
            {
                var firstChild = new Entity(generation.GenomSize, generation.EvaluationMethod);
                var secondChild = new Entity(generation.GenomSize, generation.EvaluationMethod);


                for (int i = 0; i < generation.GenomSize; i++)
                {
                    var firstImpact = firstParent.GenomImpactClausules[i];
                    var secondImpact = secondParent.GenomImpactClausules[i];
                    var sum = firstImpact + secondImpact;
                    if (firstImpact < secondImpact)
                    {
                        GenerateGenom(firstChild, generation, firstParent, secondParent, sum, i);
                        GenerateGenom(secondChild, generation, firstParent, secondParent, sum, i);
                    }
                    else if (firstImpact > secondImpact)
                    {
                        GenerateGenom(firstChild, generation, secondParent, firstParent, sum, i);
                        GenerateGenom(secondChild, generation, secondParent, firstParent, sum, i);
                    }
                    else
                    {
                        firstChild.Genom[i] = Parameters.RANDOM.NextDouble() > 0.5 ? firstParent.Genom[i] : secondParent.Genom[i];
                        secondChild.Genom[i] = Parameters.RANDOM.NextDouble() > 0.5 ? firstParent.Genom[i] : secondParent.Genom[i];
                    }
                }

                CrossoverAfter(generation, firstChild, secondChild);
            }
        }

        //Simple crossover
        public static void SimpleCrossover(Generation generation, Entity firstParent, Entity secondParent)
        {
            if (!CrossoverBefore(generation, firstParent, secondParent))
            {
                var firstChild = new Entity(generation.GenomSize, generation.EvaluationMethod);
                var secondChild = new Entity(generation.GenomSize, generation.EvaluationMethod);
                var firstCrossPoint = Parameters.RANDOM.Next(0, generation.GenomSize);
                var secondCrossPoint = Parameters.RANDOM.Next(0, generation.GenomSize);

                var tmpPoint = firstCrossPoint;
                if (secondCrossPoint < firstCrossPoint)
                {
                    firstCrossPoint = secondCrossPoint;
                    secondCrossPoint = tmpPoint;
                }

                for (int i = 0; i < generation.GenomSize; i++)
                {
                    if (i < firstCrossPoint || i > secondCrossPoint)
                    {
                        firstChild.Genom[i] = firstParent.Genom[i];
                        secondChild.Genom[i] = secondParent.Genom[i];
                    }
                    else
                    {
                        firstChild.Genom[i] = secondParent.Genom[i];
                        secondChild.Genom[i] = firstParent.Genom[i];
                    }
                }

                CrossoverAfter(generation, firstChild, secondChild);
            }
        }

        private static void CrossoverAfter(Generation generation, Entity firstChild, Entity secondChild)
        {
            // Mutate new entities
            firstChild.Mutation();
            secondChild.Mutation();

            firstChild.CountFitness(generation.Weights);
            secondChild.CountFitness(generation.Weights);

            generation.Entities.Add(firstChild);
            generation.Entities.Add(secondChild);
        }

        private static bool CrossoverBefore(Generation generation, Entity firstParent, Entity secondParent)
        {
            if (Parameters.RANDOM.NextDouble() > Parameters.CROSSOVER_FACTOR)
            {
                generation.Entities.Add(firstParent);
                generation.Entities.Add(secondParent);
                return true;
            }
            return false;
        }

        // RankSelection
        public static Entity RankSelection(Generation generation)
        {
            var tmp = 0;
            var sum = (generation.Entities.Count * (1 + generation.Entities.Count)) / 2;
            int i = generation.Entities.Count;
            var result = Parameters.RANDOM.Next(1, sum);
            for (; i > 0; i--)
            {
                tmp += i;
                if (result < tmp)
                    break;
            }
            return generation.Entities[i - 1];
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

        // CustomSelection
        public static Entity CustomSelection(Generation generation)
        {            
            var tmp = 0;

            var groups = new List<int>() { 35, 25, 20, 10, 5, 3, 2 };
            var result = Parameters.RANDOM.Next(0, 101);
            int i = 0;
            foreach (var item in groups)
            {
                tmp += item;
                if (result <= tmp)
                {
                    break;
                }
                i++;
            }

            var coef = Parameters.POPULATION_SIZE / groups.Count;
            var random = Parameters.RANDOM.Next(0, coef);

            return 
             Parameters.RANDOM.NextDouble() > 0.2 ?
             generation.Entities[i * coef + random] :
             generation.EntitiesByImpact[Parameters.POPULATION_SIZE -(i * coef + random) - 1];
        }

        // GrouppingSelection
        public static Entity GrouppingSelection(Generation generation)
        {
            var tmp = 0;

            var groups = new List<int>() { 40, 30, 15, 10, 5 };
            var result = Parameters.RANDOM.Next(0, 101);
            int i = 0;
            foreach (var item in groups)
            {
                tmp += item;
                if (result <= tmp)
                {
                    break;
                }
                i++;
            }

            var coef = Parameters.POPULATION_SIZE / groups.Count;
            var random = Parameters.RANDOM.Next(0, coef);

            return generation.Entities[i * coef + random];
        }

        public static Entity SimpleSelection(Generation generation)
        {
            var tmp = 0;
            var sum = generation.Entities.Sum((arg) => Math.Max(arg.Fitness, 0));
            int i = generation.GenomSize;
            var result = Parameters.RANDOM.Next(0, sum);
            for (; i > 0; i--)
            {
                tmp += generation.Entities[i - 1].Fitness;
                if (result < tmp)
                    break;
            }
            return generation.Entities[i == 0 ? 0 : i - 1];
        }

        public void CheckDifference()
        {
            var count = 0;
            var bestFitness = Entities[0].Fitness;
            foreach (var entity in Entities)
            {
                if (entity.Fitness == bestFitness)
                    count++;
            }
            var sameness = (Entities.Count * Parameters.DIFFERENCE_LEVEL) / 100;
            Certainity = (double)count / Entities.Count * 100;
            if (count >= sameness)
                Parameters.CERTAINITY = true;
        }
    }
}