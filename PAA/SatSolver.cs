using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace PAA
{
    public class SatSolver
    {
        Dictionary<string, ParameterExpression> parametersDictionary;
        Dictionary<string, int> argumentsDictionary;
        Delegate evaluationMethod;

        int[] weights;
        private int size;
        private string formula;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:PAA.SatSolver"/> class.
        /// </summary>
        public SatSolver(string input)
        {
            ParseInput(input);
            parametersDictionary = new Dictionary<string, ParameterExpression>();
            argumentsDictionary = new Dictionary<string, int>();
            evaluationMethod = GetMethod();

            var order = 0;
            foreach (var item in parametersDictionary.OrderBy(x => x.Key))
            {
                argumentsDictionary.Add(item.Key, order++);
            }
        }

        /// <summary>
        /// Parses the input to local variables
        /// </summary>
        private void ParseInput(string configuration)
        {
            var inputRegex = new Regex(@"([0-9][0-9]*)\s((([0-9][0-9]*)\s)*)(.*)");
            var match = inputRegex.Match(configuration);

            size = int.Parse(match.Groups[1].Value);
            weights = new int[size];

            var w = match.Groups[4].Captures.Cast<Capture>();

            for (int i = 0; i < size; i++)
            {
                weights[i] = int.Parse(w.ElementAt(i).Value);
            }

            formula = match.Groups[5].Value;
        }

        /// <summary>
        /// Gets the parameter expression by givven key (helper method for DRY)
        /// </summary>
        private ParameterExpression GetParameterExpression(string literal)
        {
            ParameterExpression par;
            parametersDictionary.TryGetValue(literal, out par);
            if (par == null)
            {
                par = Expression.Parameter(typeof(bool), literal);
                parametersDictionary.Add(literal, par);
            }
            return par;
        }

        /// <summary>
        /// Get invokable query for determine satisfability of X-SAT formula
        /// </summary>
        private Delegate GetMethod()
        {
            var entityType = typeof(Entity);
            var entityParameter = Expression.Parameter(entityType);
            var regex = new Regex(@"(?<open>\()((\!?[a-z]*[0-9]*)\s?([|])?)*(?<-open>\))\s?([&])?\s?");

            var expressions = new List<Expression>();
            var clausules = regex.Matches(formula);

            Parameters.CLAUSULES_COUNT = clausules.Count;

            foreach (Match clausule in clausules)
            {
                // constant expression for easier appending of another terms
                Expression clausuleExpression = Expression.Constant(false);
                var literals = clausule.Groups[2].Captures.Cast<Capture>().Select(x => x.Value).Where(x => !string.IsNullOrEmpty(x)).ToList();
                var arguments = new Dictionary<string, bool>();
                for (int i = 0; i < literals.Count; i++)
                {
                    var literal = literals.ElementAt(i);
                    // distinguish negation of term
                    if (literal[0] == '!')
                    {
                        literal = literal.Substring(1);
                        var par = GetParameterExpression(literal);
                        clausuleExpression = Expression.Or(clausuleExpression, Expression.Not(par));
                        arguments.Add(par.Name, false);
                    }
                    else
                    {
                        var par = GetParameterExpression(literal);
                        clausuleExpression = Expression.Or(clausuleExpression, par);
                        arguments.Add(par.Name, true);
                    }
                }

                // after clausule evaluation invoke true/false method on instance for partial counting of solved clausules and genom impact
                expressions.Add(Expression.IfThenElse(clausuleExpression,
                                                      Expression.Call(entityParameter, entityType.GetMethod(nameof(Entity.IsTrue)), Expression.Constant(arguments)),
                                                      Expression.Call(entityParameter, entityType.GetMethod(nameof(Entity.IsFalse)), Expression.Constant(arguments)
                                                     )));
            }

            expressions.Add(entityParameter);

            var parameters = parametersDictionary.OrderBy(x => x.Key).Select(x => x.Value).ToList();
            parameters.Add(entityParameter);

            return Expression.Lambda(Expression.Block(expressions), parameters).Compile();
        }

        /// <summary>
        /// The entry point of the program, where the program control starts and ends.
        /// </summary>
        public static void Main(string[] args)
        {
            var generator = new InputGenerator();
            percentages = new double[tries];
            for (int i = 0; i < tries; i++)
            {
                var input = generator.GenerateInput(100, 100, 400);
                var solver = new SatSolver(input);
                Console.WriteLine(input);
                solver.RunTests(i);
                Console.WriteLine();
            }
            Console.WriteLine(percentages.Average().ToString("0.00"));
            Console.ReadLine();
        }

        private static int tries = 10;
        private static double[] percentages;

        public void RunTests(int index)
        {
            var geneticAlgorithmSolver = new GeneticAlgorithmSolver(weights, evaluationMethod, parametersDictionary, argumentsDictionary);

            var tmpBestWeight = 0;
            bool[] solution = null;
            var bestWeights = new int[tries];
            var satisfaction = new bool[tries];
            var solutionPercentage = new double[tries];
            for (int i = 0; i < tries; i++)
            {
                ReadConfigFile();
                geneticAlgorithmSolver.Run();
                satisfaction[i] = GeneticAlgorithmSolver.BestEntity.Satisfability;
                bestWeights[i] = satisfaction[i] ? GeneticAlgorithmSolver.BestEntity.Weight : 0;
                if(GeneticAlgorithmSolver.BestEntity.Satisfability & tmpBestWeight < bestWeights[i])
                {
                    solution = GeneticAlgorithmSolver.BestEntity.Genom;
                }
            }

            var max = bestWeights.Max();
            var satisfied = satisfaction.Any(x => x);
            for (int i = 0; i < tries; i++)
            {
                if(satisfied && satisfaction[i])
                {
                    solutionPercentage[i] = (double)bestWeights[i] / max;
                }
                else if(satisfied)
                {
                    solutionPercentage[i] = 0;
                }
                else
                {
                    solutionPercentage[i] = 1;
                }
            }
            Console.WriteLine(string.Join(" ", solutionPercentage.Select(x => x.ToString("0.00"))));
            if ( solution == null)
            {
                
                Console.WriteLine("There doesn't exist solution");
            }
            else
            {
                Console.WriteLine(string.Join(" ", solution.Select(x => x ? "1" : "0")));
            }
            Console.WriteLine(solutionPercentage.Average().ToString("0.00"));
            percentages[index] = solutionPercentage.Average();


        }


        /// <summary>
        /// Reading configuration from cinfig file
        /// </summary>
        public static void ReadConfigFile()
        {
            var lines = System.IO.File.ReadLines(@"configuration.config");
            var values = lines.Select(line => Regex.Replace(line, @".*\s=\s", "")).ToArray();

            Parameters.POPULATION_SIZE = Int32.Parse(values[0]);
            Parameters.GENERATIONS_COUNT = Int32.Parse(values[1]);
            Parameters.MINIMUM_MUTATION_FACTOR = Double.Parse(values[2]);
            Parameters.MUTATION_FACTOR = Double.Parse(values[3]);
            Parameters.MUTATION_STEP_SIZE = Double.Parse(values[4]);
            Parameters.MUTATION_STEP = Int32.Parse(values[5]);
            Parameters.MAXIMUM_MUTATION_FACTOR = Double.Parse(values[6]);
            Parameters.CROSSOVER_FACTOR = Double.Parse(values[7]);
            Parameters.DIFFERENCE_LEVEL = Int32.Parse(values[8]);
            Parameters.ELITES_COUNT = Int32.Parse(values[9]);
            Parameters.FITNESS_SATISFYABLE = Int32.Parse(values[10]);
            Parameters.FITNESS_CORRECTNESS_VALUE = Int32.Parse(values[11]);
            Parameters.FITNESS_COST_VALUE = Int32.Parse(values[12]);
            Parameters.CROSSOVER_TYPE = (CrossoverEnum)Enum.Parse(typeof(CrossoverEnum), values[13]);
            Parameters.SELECTION_TYPE = (SelectionEnum)Enum.Parse(typeof(SelectionEnum), values[14]);
        }

        private const string inputP = "40 43 98 367 96 24 20 180 116 177 55 260 185 138 365 362 98 98 246 251 61 164 353 247 253 207 242 113 394 355 119 49 42 228 145 281 332 110 128 245 378 (!x011 | !x029 | !x014) & (x010 | x001 | !x016) & (x014 | x037 | !x013) & (!x007 | x040 | x008) & (x028 | x023 | x024) & (x036 | !x034 | !x018) & (x004 | !x030 | !x031) & (!x023 | !x022 | !x030) & (!x030 | !x014 | !x028) & (!x012 | x014 | x032) & (!x010 | !x036 | !x016) & (!x001 | x031 | x009) & (x001 | x040 | !x029) & (x036 | !x019 | x021) & (x030 | x032 | x016) & (!x021 | !x011 | !x025) & (!x027 | !x004 | x021) & (!x004 | x014 | x025) & (!x027 | !x011 | !x016) & (!x027 | !x017 | x007) & (!x004 | x010 | x021) & (x038 | x028 | x031) & (x023 | x025 | !x022) & (!x026 | x024 | !x021) & (!x006 | !x018 | x039) & (!x014 | !x018 | !x021) & (!x034 | !x006 | x007) & (x028 | x004 | x003) & (x011 | x024 | !x023) & (x029 | x030 | !x027) & (x012 | !x026 | !x040) & (!x025 | !x039 | !x031) & (x027 | !x015 | x001) & (!x039 | !x012 | x027) & (!x030 | x036 | x033) & (!x004 | x020 | !x035) & (!x001 | !x005 | x018) & (!x002 | !x010 | x023) & (!x007 | x011 | !x008) & (!x025 | !x032 | x004) & (x031 | !x035 | x017) & (!x007 | !x023 | x027) & (x011 | !x013 | !x022) & (!x030 | !x039 | !x008) & (!x001 | !x017 | x018) & (!x015 | !x028 | !x008) & (x017 | !x005 | !x040) & (x024 | x035 | !x032) & (x031 | !x035 | x011) & (x040 | !x002 | !x026) & (x023 | !x016 | !x011) & (!x024 | x004 | !x005) & (x025 | x040 | x020) & (!x031 | x028 | x005) & (x009 | x032 | !x021) & (x028 | !x007 | !x005) & (!x001 | x017 | x018) & (x011 | x009 | x031) & (x022 | x036 | x018) & (!x019 | x027 | !x033) & (x030 | x036 | x035) & (!x003 | x019 | x013) & (x022 | x034 | x031) & (!x029 | !x017 | !x011) & (!x016 | x021 | x012) & (!x015 | !x030 | x016) & (x014 | !x028 | x003) & (!x022 | x040 | !x014) & (!x024 | !x023 | !x009) & (x034 | x038 | x030)";

    }

}