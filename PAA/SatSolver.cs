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
            input = inputP;
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

        private const string inputP = "30 339 307 121 8 176 106 215 351 18 214 302 10 101 215 15 303 347 21 33 184 389 118 312 379 369 337 330 221 274 234 (!x010 | !x017 | !x024) & (!x007 | x023 | !x004) & (!x017 | !x008 | !x009) & (x009 | x011 | !x008) & (!x023 | x007 | x030) & (!x018 | !x003 | !x016) & (!x002 | x020 | !x005) & (x029 | !x003 | x019) & (x029 | !x004 | !x005) & (!x028 | !x026 | !x029) & (!x011 | x017 | !x008) & (!x020 | !x001 | x013) & (x004 | !x024 | !x003) & (x010 | x020 | x029) & (x011 | !x004 | !x024) & (x021 | !x020 | x002) & (!x015 | !x007 | !x018) & (!x005 | x011 | !x017) & (x003 | x014 | x010) & (!x002 | !x007 | !x012) & (x027 | !x003 | !x015) & (x029 | x009 | x008) & (!x016 | x009 | x029) & (x016 | !x020 | x010) & (x001 | !x015 | x030) & (!x027 | !x014 | x020) & (x030 | x015 | !x024) & (x005 | !x029 | !x007) & (x008 | !x018 | x007) & (x023 | x026 | !x001) & (x009 | x011 | !x028) & (x003 | !x008 | !x025) & (!x007 | x001 | x003) & (x022 | !x005 | x030) & (x030 | x015 | !x009) & (x019 | x030 | !x014) & (!x024 | x027 | !x016) & (!x023 | x011 | !x013) & (!x010 | !x011 | !x024) & (x001 | !x014 | !x010) & (!x029 | x027 | x016) & (x011 | x012 | !x002) & (!x004 | x022 | !x017) & (x004 | !x022 | x026) & (x006 | !x027 | x022) & (!x008 | !x005 | !x007) & (!x010 | !x023 | x002) & (!x029 | !x016 | !x004) & (x025 | !x026 | x006) & (!x007 | x011 | x001) & (!x022 | !x003 | !x008) & (x001 | x010 | x024) & (!x003 | x015 | !x006) & (x018 | x025 | !x013) & (!x010 | !x020 | x030) & (!x022 | !x010 | !x025) & (!x008 | x003 | x014) & (x001 | !x003 | x022) & (!x025 | !x023 | !x014) & (!x014 | !x003 | !x030) & (x025 | x008 | !x020) & (!x010 | x011 | !x007) & (!x029 | !x010 | x011) & (x007 | x019 | !x003) & (x023 | x012 | !x004) & (!x010 | !x012 | !x030) & (!x023 | x015 | !x018) & (!x030 | x018 | !x009) & (!x021 | !x002 | x014) & (!x017 | !x029 | !x007) & (x028 | x017 | !x010) & (!x019 | x023 | x026) & (!x016 | !x028 | !x004) & (!x017 | !x004 | x010) & (!x016 | !x005 | x028) & (!x025 | !x029 | x018) & (x026 | !x016 | !x020) & (x008 | !x010 | !x018) & (x003 | x011 | x012) & (x002 | x025 | x014) & (!x025 | x001 | !x018) & (x009 | x017 | x004) & (!x021 | !x003 | x018) & (!x016 | x015 | x014) & (x016 | !x007 | x013) & (x006 | x003 | !x004) & (!x010 | x024 | x017) & (!x007 | x017 | x002) & (!x006 | !x028 | !x009) & (x018 | x002 | x024) & (x014 | !x018 | !x002) & (!x019 | x025 | x015) & (x023 | x017 | !x004) & (x026 | !x004 | !x007) & (!x009 | x024 | !x017) & (x007 | !x016 | x020) & (x022 | !x026 | x017) & (!x015 | !x010 | !x030) & (x009 | x027 | x018) & (!x028 | x027 | x019) & (x024 | !x012 | !x005) & (!x001 | x003 | !x025) & (!x026 | x001 | x009) & (!x019 | x027 | !x003) & (!x023 | !x002 | !x017) & (x021 | x006 | !x022) & (x027 | x028 | x006) & (!x002 | !x022 | !x027) & (!x017 | !x030 | x028) & (x008 | !x013 | !x030) & (x022 | !x009 | x013) & (x016 | !x002 | !x023) & (x027 | x030 | !x012) & (!x028 | !x029 | x021) & (x006 | !x015 | x023) & (x020 | x019 | x029) & (x002 | x017 | !x004) & (!x011 | x007 | x006) & (x009 | x005 | !x002) & (x024 | x018 | !x006) & (!x001 | x009 | x003) & (!x007 | x029 | x028) & (!x010 | x025 | x022) & (x030 | x011 | x010) & (x008 | !x001 | !x016) & (x023 | x020 | x010) & (!x005 | !x025 | x013) & (!x024 | !x014 | x016) & (x027 | !x007 | x020) & (!x030 | x025 | x006) & (!x012 | x005 | x021) & (!x015 | !x004 | !x007) & (x024 | !x016 | x006) & (x021 | x014 | !x013) & (!x029 | x026 | x006) & (!x016 | !x005 | !x010) & (x010 | !x007 | !x028) & (x001 | x028 | x030) & (!x023 | !x018 | !x007) & (!x018 | !x010 | !x021) & (!x015 | !x026 | !x006) & (x012 | x014 | !x028) & (x001 | x016 | !x012) & (x001 | x027 | x014) & (!x029 | !x026 | !x019) & (x019 | x018 | x003) & (x009 | !x019 | !x013) & (!x002 | x026 | x018) & (!x023 | !x017 | x008) & (!x013 | !x009 | !x016)";

    }

}