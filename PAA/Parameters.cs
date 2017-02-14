using System;

namespace PAA
{
    public class Parameters
    {
        #region Static parameters

        // Size of population in generation
        public static int POPULATION_SIZE = 150;

        // Count of generation throught evolution
        public static int GENERATIONS_COUNT = 2000;

        // Minimum mutation factor
        public static double MINIMUM_MUTATION_FACTOR = 0.05;        

        // Basic mutation factor
        public static double MUTATION_FACTOR = 0.05;

        // Mutation increase step size
        public static double MUTATION_STEP_SIZE = 0.05;

        // Mutation increase step
        public static int MUTATION_STEP = 2;

        // Maximum mutation with variable mutation factor
        public static double MAXIMUM_MUTATION_FACTOR = 0.30;

        // Crossover probability factor
        public static double CROSSOVER_FACTOR = 0.6;

        // Percentage of same entities to stop evolution earlier
        public static int DIFFERENCE_LEVEL = 90;

        // Number of entities pairs go straight forward to next generation
        public static int ELITES_COUNT = 2;

        // Malus for no satisfyable formula
        public static int FITNESS_SATISFYABLE = 20;

        // Bonus for one satisfyable clausule
        public static int FITNESS_CORRECTNESS_VALUE = 1;

        // Bonus for 1 unit of weight
        public static int FITNESS_COST_VALUE = 1;

        // Clausuless count
        public static int CLAUSULES_COUNT = 100;

        // Used crossover type
        public static CrossoverEnum CROSSOVER_TYPE = CrossoverEnum.Simple;

        // Used selection type
        public static SelectionEnum SELECTION_TYPE = SelectionEnum.Simple;

        #endregion

        #region General properties

        // General Random property
        public static Random RANDOM = new Random();

        // General Certainity property
        public static bool CERTAINITY = false;

        // Global weights for manipulation
        public static int[] Weights;

        #endregion

    }
}
