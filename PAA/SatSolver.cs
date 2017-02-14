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
            var input = generator.GenerateInput(500, 260, 400);
                        
            Console.WriteLine(input);
            var solver = new SatSolver(input);
            solver.Run();            
        }

        private void Run()
        {
            //var bruteForceSolver = new BruteForceSolver(weights, evaluationMethod, parametersDictionary);
            //bruteForceSolver.Run();

            var geneticAlgorithmSolver = new GeneticAlgorithmSolver(weights, evaluationMethod, parametersDictionary, argumentsDictionary);
            while (Console.ReadLine() != "exit")
            {
                ReadConfigFile();
                geneticAlgorithmSolver.Run();
            }
        }

        /// <summary>
        /// Reading configuration from cinfig file
        /// </summary>
        private void ReadConfigFile()
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
    }

}