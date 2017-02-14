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
        /// Parses the input
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

                // after clausule evaluation invoke true/false method on instance
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
            //var input = generator.GenerateInput(80, 240, 400);
            var input = inputF;

            //Just for init
            var p = new Parameters();

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
            Console.ReadKey();
        }

        private void ReadConfigFile()
        {
            var lines = System.IO.File.ReadLines(@"C:\Users\mroubicek\Documents\Visual Studio 2017\Projects\PAA\PAA\configuration.config");
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
       
        private static void ResetParameters(Parameters parameters)
        {
            Parameters.SetNewParameters(parameters);
        }

        private const string inputT = "30 175 33 53 195 5 279 241 14 307 149 73 357 376 376 197 341 355 180 236 197 131 377 269 218 342 59 111 208 92 16 (!x011 | !x010 | !x006) & (x016 | x025 | !x014) & (!x021 | x011 | x006) & (x019 | x028 | !x014) & (!x003 | !x018 | x013) & (x017 | !x014 | x004) & (x009 | x013 | !x027) & (x021 | x002 | !x010) & (!x001 | !x030 | !x006) & (x021 | x012 | !x022) & (!x017 | !x013 | !x007) & (!x007 | x028 | !x018) & (x021 | x029 | !x002) & (!x012 | x003 | x016) & (x001 | x024 | x025) & (!x020 | x021 | x009) & (x008 | x026 | !x024) & (!x026 | x005 | !x008) & (!x028 | !x005 | !x019) & (x025 | !x006 | x023) & (!x029 | !x027 | x019) & (!x012 | !x029 | !x020) & (!x001 | !x010 | x024) & (!x005 | x010 | !x027) & (!x010 | x014 | x030) & (x026 | !x009 | !x001) & (!x002 | !x023 | !x029) & (x018 | !x027 | x030) & (x015 | !x005 | x017) & (x019 | !x030 | !x009) & (!x029 | !x026 | !x023) & (x005 | !x019 | x015) & (!x009 | !x011 | x012) & (!x023 | !x030 | !x024) & (!x009 | x003 | !x018) & (x012 | !x008 | x005) & (!x023 | !x003 | x024) & (x026 | x021 | x007) & (!x007 | !x018 | !x017) & (x030 | !x004 | x016) & (!x019 | !x020 | x010) & (x030 | x023 | !x028) & (!x029 | !x030 | x026) & (!x006 | !x008 | !x029) & (!x012 | !x021 | x029) & (!x027 | !x004 | !x006) & (x016 | x004 | !x010) & (!x015 | !x026 | !x020) & (x023 | !x006 | !x016) & (x024 | x025 | !x017) & (x014 | x020 | !x003) & (!x002 | x005 | x018) & (!x023 | x013 | x016) & (!x001 | !x022 | !x018) & (!x013 | x021 | !x018) & (x026 | !x025 | x022) & (!x018 | !x030 | !x021) & (!x027 | x012 | !x025) & (x013 | !x021 | x024) & (x001 | x004 | !x012) & (!x018 | !x026 | !x001) & (x015 | !x008 | x002) & (x022 | x010 | x012) & (!x022 | x009 | x013) & (x019 | !x004 | x007) & (x022 | x014 | x004) & (x002 | !x027 | x016) & (x028 | x016 | x029) & (x023 | !x021 | x008) & (!x013 | x012 | !x010) & (x002 | x008 | !x005) & (!x020 | x004 | !x010) & (x007 | !x019 | x015) & (!x022 | x021 | !x014) & (!x023 | x021 | x002) & (!x006 | !x012 | !x016) & (!x008 | !x030 | x026) & (x017 | x011 | !x012) & (!x024 | !x007 | x001) & (!x027 | x014 | x012) & (!x006 | !x023 | x012) & (x011 | !x003 | x006) & (x022 | x026 | x028) & (!x011 | x009 | x008) & (x030 | x010 | !x004) & (!x007 | !x013 | x001) & (!x019 | !x027 | !x011) & (!x027 | x030 | !x022) & (!x015 | x030 | !x025) & (!x024 | !x011 | x009) & (!x027 | !x004 | !x012) & (!x027 | x002 | x018) & (!x024 | x017 | !x019) & (!x029 | x020 | !x008) & (!x018 | !x020 | !x029) & (!x018 | !x028 | x023) & (x010 | !x003 | x022) & (x010 | x018 | x001) & (!x004 | x030 | !x016) & (!x006 | !x012 | x008) & (x008 | !x019 | !x023) & (!x012 | x004 | !x026) & (x023 | x007 | x026) & (!x018 | x013 | !x007) & (x007 | x030 | !x026) & (!x028 | x018 | !x009) & (!x012 | !x003 | x013) & (!x015 | !x021 | !x022) & (!x019 | !x018 | !x028) & (!x029 | !x018 | !x002) & (!x009 | x029 | !x004) & (x029 | x013 | x014) & (!x010 | !x017 | x012) & (x011 | !x009 | !x006) & (x016 | !x022 | x007) & (!x017 | !x022 | x005) & (!x019 | x011 | x012) & (!x016 | x018 | x012) & (!x006 | !x021 | !x030) & (!x020 | x010 | !x016) & (!x023 | x007 | !x012) & (x002 | !x008 | x012) & (x026 | x018 | !x020) & (x029 | !x010 | !x001) & (x026 | x022 | !x016) & (!x008 | !x014 | x010) & (x028 | !x012 | x030) & (x022 | x005 | !x021) & (!x003 | x004 | x009) & (x009 | !x029 | x018) & (x027 | !x026 | !x014) & (x027 | x005 | !x018) & (x030 | !x009 | x018) & (x016 | !x029 | !x015) & (x010 | x013 | x014) & (x014 | !x001 | x004) & (x014 | !x018 | x028) & (!x016 | !x010 | x030) & (x010 | x002 | x029) & (x026 | x016 | !x019) & (!x019 | x028 | !x018) & (x011 | !x026 | !x003) & (x002 | !x023 | x004) & (x014 | x003 | x005) & (x028 | !x009 | x014) & (!x025 | x007 | x011) & (x005 | !x013 | !x009) & (!x004 | !x011 | x002) & (x011 | x004 | !x016) & (!x022 | !x020 | !x013) & (x026 | x029 | !x024) & (x027 | !x016 | x019) & (!x020 | x010 | !x013) & (!x007 | !x006 | !x002) & (!x020 | !x021 | !x015) & (x028 | x020 | !x021) & (!x004 | x019 | x024) & (!x016 | !x008 | x009) & (!x018 | x020 | !x009) & (x002 | !x009 | x026) & (x001 | x022 | !x021) & (!x022 | !x006 | x015) & (!x021 | x016 | x030) & (x030 | !x012 | x015) & (!x011 | x027 | !x015) & (!x025 | !x023 | x022) & (x024 | !x018 | x017) & (!x011 | !x005 | x009) & (!x002 | x004 | x022) & (x002 | x015 | !x001) & (x020 | !x010 | x015) & (x024 | x012 | !x019) & (!x005 | !x008 | x023) & (x014 | !x019 | x023) & (!x009 | !x020 | !x010) & (!x025 | x019 | !x022) & (!x014 | !x009 | !x015) & (!x011 | !x019 | !x014) & (!x012 | !x022 | !x020) & (x018 | x007 | x029) & (x023 | x008 | !x011) & (!x020 | x001 | !x030) & (x004 | x003 | !x011) & (x020 | !x005 | !x027) & (x025 | !x007 | x012) & (x013 | !x017 | x028) & (x008 | !x005 | !x003) & (x021 | !x016 | x011) & (x023 | !x026 | !x013) & (!x009 | x005 | !x003) & (x028 | !x016 | x019) & (x010 | !x021 | !x003) & (!x030 | x009 | x026) & (!x027 | !x030 | !x019) & (!x028 | x015 | !x007) & (x029 | x030 | x021) & (!x002 | x021 | x029) & (!x015 | x014 | x021) & (x004 | !x006 | x028) & (x028 | !x021 | x026)";

        // 3774 Best
        private const string inputR = "30 339 307 121 8 176 106 215 351 18 214 302 10 101 215 15 303 347 21 33 184 389 118 312 379 369 337 330 221 274 234 (!x010 | !x017 | !x024) & (!x007 | x023 | !x004) & (!x017 | !x008 | !x009) & (x009 | x011 | !x008) & (!x023 | x007 | x030) & (!x018 | !x003 | !x016) & (!x002 | x020 | !x005) & (x029 | !x003 | x019) & (x029 | !x004 | !x005) & (!x028 | !x026 | !x029) & (!x011 | x017 | !x008) & (!x020 | !x001 | x013) & (x004 | !x024 | !x003) & (x010 | x020 | x029) & (x011 | !x004 | !x024) & (x021 | !x020 | x002) & (!x015 | !x007 | !x018) & (!x005 | x011 | !x017) & (x003 | x014 | x010) & (!x002 | !x007 | !x012) & (x027 | !x003 | !x015) & (x029 | x009 | x008) & (!x016 | x009 | x029) & (x016 | !x020 | x010) & (x001 | !x015 | x030) & (!x027 | !x014 | x020) & (x030 | x015 | !x024) & (x005 | !x029 | !x007) & (x008 | !x018 | x007) & (x023 | x026 | !x001) & (x009 | x011 | !x028) & (x003 | !x008 | !x025) & (!x007 | x001 | x003) & (x022 | !x005 | x030) & (x030 | x015 | !x009) & (x019 | x030 | !x014) & (!x024 | x027 | !x016) & (!x023 | x011 | !x013) & (!x010 | !x011 | !x024) & (x001 | !x014 | !x010) & (!x029 | x027 | x016) & (x011 | x012 | !x002) & (!x004 | x022 | !x017) & (x004 | !x022 | x026) & (x006 | !x027 | x022) & (!x008 | !x005 | !x007) & (!x010 | !x023 | x002) & (!x029 | !x016 | !x004) & (x025 | !x026 | x006) & (!x007 | x011 | x001) & (!x022 | !x003 | !x008) & (x001 | x010 | x024) & (!x003 | x015 | !x006) & (x018 | x025 | !x013) & (!x010 | !x020 | x030) & (!x022 | !x010 | !x025) & (!x008 | x003 | x014) & (x001 | !x003 | x022) & (!x025 | !x023 | !x014) & (!x014 | !x003 | !x030) & (x025 | x008 | !x020) & (!x010 | x011 | !x007) & (!x029 | !x010 | x011) & (x007 | x019 | !x003) & (x023 | x012 | !x004) & (!x010 | !x012 | !x030) & (!x023 | x015 | !x018) & (!x030 | x018 | !x009) & (!x021 | !x002 | x014) & (!x017 | !x029 | !x007) & (x028 | x017 | !x010) & (!x019 | x023 | x026) & (!x016 | !x028 | !x004) & (!x017 | !x004 | x010) & (!x016 | !x005 | x028) & (!x025 | !x029 | x018) & (x026 | !x016 | !x020) & (x008 | !x010 | !x018) & (x003 | x011 | x012) & (x002 | x025 | x014) & (!x025 | x001 | !x018) & (x009 | x017 | x004) & (!x021 | !x003 | x018) & (!x016 | x015 | x014) & (x016 | !x007 | x013) & (x006 | x003 | !x004) & (!x010 | x024 | x017) & (!x007 | x017 | x002) & (!x006 | !x028 | !x009) & (x018 | x002 | x024) & (x014 | !x018 | !x002) & (!x019 | x025 | x015) & (x023 | x017 | !x004) & (x026 | !x004 | !x007) & (!x009 | x024 | !x017) & (x007 | !x016 | x020) & (x022 | !x026 | x017) & (!x015 | !x010 | !x030) & (x009 | x027 | x018) & (!x028 | x027 | x019) & (x024 | !x012 | !x005) & (!x001 | x003 | !x025) & (!x026 | x001 | x009) & (!x019 | x027 | !x003) & (!x023 | !x002 | !x017) & (x021 | x006 | !x022) & (x027 | x028 | x006) & (!x002 | !x022 | !x027) & (!x017 | !x030 | x028) & (x008 | !x013 | !x030) & (x022 | !x009 | x013) & (x016 | !x002 | !x023) & (x027 | x030 | !x012) & (!x028 | !x029 | x021) & (x006 | !x015 | x023) & (x020 | x019 | x029) & (x002 | x017 | !x004) & (!x011 | x007 | x006) & (x009 | x005 | !x002) & (x024 | x018 | !x006) & (!x001 | x009 | x003) & (!x007 | x029 | x028) & (!x010 | x025 | x022) & (x030 | x011 | x010) & (x008 | !x001 | !x016) & (x023 | x020 | x010) & (!x005 | !x025 | x013) & (!x024 | !x014 | x016) & (x027 | !x007 | x020) & (!x030 | x025 | x006) & (!x012 | x005 | x021) & (!x015 | !x004 | !x007) & (x024 | !x016 | x006) & (x021 | x014 | !x013) & (!x029 | x026 | x006) & (!x016 | !x005 | !x010) & (x010 | !x007 | !x028) & (x001 | x028 | x030) & (!x023 | !x018 | !x007) & (!x018 | !x010 | !x021) & (!x015 | !x026 | !x006) & (x012 | x014 | !x028) & (x001 | x016 | !x012) & (x001 | x027 | x014) & (!x029 | !x026 | !x019) & (x019 | x018 | x003) & (x009 | !x019 | !x013) & (!x002 | x026 | x018) & (!x023 | !x017 | x008) & (!x013 | !x009 | !x016)";

        // 4281 Best
        // 0 1 0 0 0 0 1 1 1 0 1 1 1 0 1 1 1 1 1 0 1 1 1 0 1 1 1 1 1 1
        private const string inputP = "30 60 399 392 65 323 82 59 81 362 359 39 340 223 128 36 399 266 230 92 267 315 203 278 230 249 309 97 68 149 87 (x016 | !x012 | x010) & (x028 | !x019 | !x020) & (x029 | x002 | x023) & (!x028 | !x003 | !x027) & (!x001 | !x009 | !x019) & (x011 | x002 | x019) & (!x011 | !x010 | x007) & (x030 | !x028 | !x009) & (!x029 | !x028 | !x005) & (x028 | x014 | x022) & (!x004 | x026 | !x007) & (!x024 | !x028 | !x004) & (x029 | !x028 | x019) & (x008 | !x024 | !x025) & (x024 | x028 | !x021) & (x027 | !x018 | x003) & (!x004 | x020 | !x015) & (x001 | !x024 | !x030) & (x030 | x023 | x006) & (x009 | x028 | !x021) & (!x020 | x003 | x027) & (x009 | x004 | !x015) & (x010 | x024 | x023) & (x004 | x029 | !x013) & (!x013 | !x005 | !x024) & (x003 | x026 | !x029) & (x027 | x024 | !x026) & (!x015 | x027 | !x002) & (!x006 | !x004 | x017) & (x021 | x020 | x013) & (!x003 | !x010 | !x011) & (!x015 | x025 | !x027) & (!x009 | x018 | !x023) & (x003 | !x025 | x009) & (x012 | !x015 | !x024) & (!x029 | !x022 | x008) & (!x014 | x012 | x019) & (!x024 | !x019 | !x023) & (x015 | x002 | !x007) & (x010 | x016 | !x030) & (!x004 | !x006 | x018) & (!x011 | x006 | !x001) & (!x010 | !x017 | !x025) & (!x021 | x007 | !x028) & (x017 | !x022 | x012) & (!x006 | !x020 | x019) & (!x013 | x009 | x026) & (x024 | !x014 | !x022) & (!x012 | !x019 | !x006) & (!x029 | x017 | !x005) & (x014 | x005 | x009) & (x021 | x010 | !x003) & (x011 | !x012 | !x003) & (!x002 | x030 | !x009) & (x002 | !x021 | x026) & (!x026 | !x028 | !x003) & (x022 | !x006 | x013) & (x017 | !x008 | x005) & (x015 | !x005 | x023) & (!x009 | x013 | !x016) & (!x014 | !x010 | !x021) & (!x014 | x024 | !x026) & (x009 | !x022 | !x014) & (!x020 | x019 | x026) & (!x005 | x018 | x007) & (!x015 | x018 | !x020) & (x027 | x013 | x010) & (x005 | x017 | !x010) & (x016 | x029 | !x027) & (!x026 | x015 | !x018) & (!x011 | x022 | !x001) & (x026 | x011 | x017) & (!x027 | !x020 | x014) & (x022 | x011 | !x001) & (!x008 | x011 | x028) & (!x015 | x023 | !x024) & (x019 | !x017 | !x008) & (!x001 | x005 | !x025) & (!x025 | !x017 | x026) & (x019 | x027 | !x002) & (x014 | !x029 | !x024) & (!x022 | x023 | x013) & (!x004 | x015 | !x014) & (x023 | !x017 | !x007) & (!x019 | !x011 | x016) & (!x022 | x014 | !x024) & (!x002 | !x011 | x027) & (x013 | !x012 | x016) & (x007 | x006 | !x028) & (!x027 | x003 | x007) & (x012 | !x015 | x030) & (!x001 | !x029 | !x008) & (!x001 | !x015 | x022) & (!x006 | x013 | !x003) & (!x020 | !x002 | !x024) & (x017 | !x020 | !x015) & (!x028 | !x012 | x015) & (x008 | x028 | x026) & (x012 | !x029 | !x013) & (x003 | !x030 | !x014)";

        // 6325
        // 1   1   1   0   0   1   1   0   0   1   1   1   1   0   1   0   0   1   1   1   1   1   1   1   0   1   1   1   1   0   1   1   1   1   1   1   1   1   1   1

        // 6179 KnownBest
        // 1   1   1   1   1   0   1   0   1   0   0   1   0   0   1   1   1   1   1   1   1   1   0   1   1   1   1   1   1   1   1   1   1   1   1   1   1   1   0   1
        private const string inputF = "40 43 98 367 96 24 20 180 116 177 55 260 185 138 365 362 98 98 246 251 61 164 353 247 253 207 242 113 394 355 119 49 42 228 145 281 332 110 128 245 378 (!x011 | !x029 | !x014) & (x010 | x001 | !x016) & (x014 | x037 | !x013) & (!x007 | x040 | x008) & (x028 | x023 | x024) & (x036 | !x034 | !x018) & (x004 | !x030 | !x031) & (!x023 | !x022 | !x030) & (!x030 | !x014 | !x028) & (!x012 | x014 | x032) & (!x010 | !x036 | !x016) & (!x001 | x031 | x009) & (x001 | x040 | !x029) & (x036 | !x019 | x021) & (x030 | x032 | x016) & (!x021 | !x011 | !x025) & (!x027 | !x004 | x021) & (!x004 | x014 | x025) & (!x027 | !x011 | !x016) & (!x027 | !x017 | x007) & (!x004 | x010 | x021) & (x038 | x028 | x031) & (x023 | x025 | !x022) & (!x026 | x024 | !x021) & (!x006 | !x018 | x039) & (!x014 | !x018 | !x021) & (!x034 | !x006 | x007) & (x028 | x004 | x003) & (x011 | x024 | !x023) & (x029 | x030 | !x027) & (x012 | !x026 | !x040) & (!x025 | !x039 | !x031) & (x027 | !x015 | x001) & (!x039 | !x012 | x027) & (!x030 | x036 | x033) & (!x004 | x020 | !x035) & (!x001 | !x005 | x018) & (!x002 | !x010 | x023) & (!x007 | x011 | !x008) & (!x025 | !x032 | x004) & (x031 | !x035 | x017) & (!x007 | !x023 | x027) & (x011 | !x013 | !x022) & (!x030 | !x039 | !x008) & (!x001 | !x017 | x018) & (!x015 | !x028 | !x008) & (x017 | !x005 | !x040) & (x024 | x035 | !x032) & (x031 | !x035 | x011) & (x040 | !x002 | !x026) & (x023 | !x016 | !x011) & (!x024 | x004 | !x005) & (x025 | x040 | x020) & (!x031 | x028 | x005) & (x009 | x032 | !x021) & (x028 | !x007 | !x005) & (!x001 | x017 | x018) & (x011 | x009 | x031) & (x022 | x036 | x018) & (!x019 | x027 | !x033) & (x030 | x036 | x035) & (!x003 | x019 | x013) & (x022 | x034 | x031) & (!x029 | !x017 | !x011) & (!x016 | x021 | x012) & (!x015 | !x030 | x016) & (x014 | !x028 | x003) & (!x022 | x040 | !x014) & (!x024 | !x023 | !x009) & (x034 | x038 | x030)";

        // 11437
        // 1   0   0   1   1   1   1   1   1   1   1   1   1   1   1   1   1   0   1   1   1   1   1   1   1   1   1   0   0   0   1   1   0   0   0   0   1   0   1   0   1   0   1   0   1   1   1   0   1   0   0   1   1   1   0   0   1   1   0   1   1   0   0   1   1   1   1   0   0   0   1   1   0   0   1   1   1   1   1   1
        private const string inputG = "80 354 162 368 258 308 320 9 306 310 312 3 61 209 377 310 105 29 86 380 394 263 14 300 32 186 196 385 214 5 201 178 282 170 357 17 257 177 246 207 6 166 171 370 159 286 228 376 13 73 317 165 100 55 299 332 340 262 336 73 112 334 194 302 110 135 120 290 252 361 54 328 182 279 375 229 93 44 141 146 357 (!x006 | !x056 | !x029) & (!x036 | x059 | !x049) & (x035 | !x039 | x012) & (!x051 | !x074 | x021) & (x061 | !x036 | x029) & (x013 | !x015 | !x044) & (x052 | x062 | x005) & (x049 | !x010 | !x032) & (x011 | x074 | x054) & (!x013 | x034 | !x048) & (!x080 | !x029 | x036) & (x020 | !x076 | x040) & (!x069 | !x046 | !x003) & (x027 | !x078 | x032) & (!x032 | !x053 | !x056) & (x028 | x053 | x014) & (!x077 | !x070 | x027) & (x073 | !x061 | x032) & (!x052 | x067 | x075) & (x050 | x047 | !x034) & (x043 | x038 | x047) & (!x030 | x027 | x052) & (x006 | !x068 | x056) & (!x028 | !x040 | x026) & (!x067 | x036 | !x044) & (!x040 | x028 | x031) & (!x060 | !x050 | !x040) & (x066 | !x074 | !x032) & (!x047 | x056 | !x073) & (x004 | x077 | !x036) & (x053 | !x011 | !x080) & (!x074 | !x001 | x080) & (!x047 | !x002 | x056) & (x017 | !x037 | !x053) & (x007 | x063 | x021) & (x008 | !x013 | x078) & (x046 | !x013 | !x052) & (!x040 | !x071 | x064) & (x071 | !x026 | x010) & (!x020 | x039 | !x037) & (x013 | !x039 | !x055) & (!x074 | !x060 | x016) & (!x019 | x065 | !x069) & (x038 | !x047 | x080) & (x025 | !x078 | !x020) & (!x008 | !x022 | x014) & (x068 | !x023 | x009) & (x017 | !x072 | !x071) & (x035 | !x050 | !x080) & (!x047 | !x040 | x030) & (x076 | !x039 | !x007) & (x058 | !x035 | !x047) & (!x039 | x015 | !x028) & (!x011 | x031 | !x016) & (x020 | !x066 | x016) & (x053 | !x059 | x062) & (x077 | x079 | x005) & (!x010 | !x056 | x015) & (x030 | !x045 | !x038) & (!x074 | !x066 | !x043) & (x064 | x019 | x080) & (!x001 | x080 | !x014) & (!x076 | !x037 | x021) & (x021 | !x019 | !x027) & (x056 | !x061 | x005) & (x021 | x072 | x038) & (!x015 | x064 | !x069) & (x018 | !x022 | !x050) & (!x031 | x021 | x012) & (!x016 | x039 | !x072) & (x011 | !x062 | x060) & (x052 | !x032 | !x025) & (!x074 | x045 | x005) & (x004 | x012 | !x008) & (x070 | !x060 | x022) & (x057 | !x073 | x018) & (!x035 | !x050 | !x018) & (!x016 | x063 | !x050) & (!x067 | x034 | !x035) & (!x028 | !x077 | !x022) & (x008 | !x003 | x058) & (x065 | !x074 | x076) & (!x040 | x073 | !x007) & (x009 | !x036 | x057) & (x039 | !x005 | x051) & (x078 | x017 | x079) & (x009 | !x013 | !x043) & (x078 | !x079 | x042) & (!x035 | !x070 | !x013) & (x052 | !x027 | !x018) & (x060 | x023 | x079) & (!x022 | !x041 | x072) & (x053 | !x037 | !x068) & (!x005 | !x063 | !x035) & (x049 | x014 | !x038) & (x066 | x067 | x004) & (!x050 | !x006 | !x052) & (!x051 | !x074 | !x006) & (x024 | !x029 | !x057) & (!x005 | x001 | !x043) & (!x074 | !x024 | !x075) & (x015 | x020 | x074) & (x046 | x074 | !x021) & (!x073 | x011 | x012) & (x033 | x049 | x019) & (x005 | x022 | !x078) & (!x035 | x061 | !x053) & (!x049 | x016 | !x009) & (x061 | x046 | !x045) & (!x049 | !x055 | !x026) & (x076 | x009 | !x025) & (x059 | x016 | x049) & (!x079 | x007 | x057) & (x013 | x002 | x032) & (!x051 | x048 | x057) & (x010 | !x046 | !x034) & (!x049 | !x036 | x069) & (x052 | x017 | !x001) & (!x016 | !x036 | !x021) & (!x069 | x073 | x041) & (!x019 | !x068 | !x009) & (x001 | !x036 | !x010) & (x058 | !x013 | !x060) & (!x034 | !x047 | x041) & (!x055 | x025 | x045) & (x057 | x008 | x029) & (!x038 | x023 | !x021) & (x019 | !x062 | !x003) & (x049 | x071 | !x078) & (x045 | x002 | x023) & (!x039 | x045 | !x008) & (x042 | !x007 | x022) & (x041 | !x020 | x045) & (!x055 | !x054 | x047) & (!x010 | x017 | !x059) & (x023 | !x031 | x075) & (!x027 | !x023 | !x050) & (!x062 | !x065 | !x078) & (!x071 | !x073 | !x034) & (x079 | x008 | !x040) & (!x034 | !x012 | !x015) & (x049 | x065 | !x061) & (!x069 | !x004 | !x066) & (x026 | x020 | x068) & (x030 | x077 | x016) & (!x039 | !x007 | !x074) & (!x018 | !x010 | !x072) & (x044 | !x006 | !x069) & (x015 | !x040 | x038) & (!x004 | x008 | !x064) & (x034 | x049 | x022) & (!x061 | x011 | !x048) & (x015 | x028 | !x024) & (!x024 | !x008 | !x062) & (x059 | !x024 | x066) & (x036 | x068 | !x044) & (x025 | x068 | x006) & (!x041 | !x066 | x064) & (!x010 | x045 | x070) & (!x049 | !x027 | !x030) & (x059 | x070 | !x063) & (!x009 | !x040 | !x010) & (x006 | !x036 | !x001) & (x078 | !x057 | x038) & (x041 | !x014 | !x064) & (x035 | x005 | !x023) & (x023 | x013 | x034) & (!x075 | !x038 | !x007) & (!x065 | !x070 | x069) & (!x005 | !x055 | !x046) & (!x028 | !x080 | !x010) & (x064 | !x079 | !x052) & (!x052 | !x062 | !x077) & (!x059 | !x013 | x062) & (!x063 | !x051 | x034) & (!x050 | !x052 | x072) & (x006 | !x068 | !x007) & (x023 | x070 | x031) & (!x073 | !x074 | !x027) & (!x061 | x004 | !x080) & (x044 | !x002 | !x061) & (!x055 | !x037 | x073) & (x077 | !x007 | x004) & (!x040 | x042 | x056) & (x024 | x010 | x002) & (x068 | x007 | x080) & (!x045 | !x033 | x055) & (x025 | x068 | x065) & (!x064 | !x012 | x032) & (!x002 | !x026 | !x056) & (!x061 | x068 | !x003) & (!x080 | x039 | x075) & (!x003 | x076 | !x011) & (x020 | !x027 | x038) & (!x025 | x058 | !x013) & (x077 | !x066 | x011) & (!x007 | x076 | !x074) & (!x057 | x078 | x032) & (!x044 | x053 | !x020) & (x016 | !x011 | x023) & (x068 | !x033 | x018) & (x065 | !x003 | !x024) & (x033 | !x051 | !x008) & (x016 | x039 | !x001) & (!x042 | !x069 | !x067) & (!x021 | x080 | x036) & (!x051 | x020 | x048) & (!x052 | !x003 | !x067) & (!x046 | !x002 | !x007) & (!x028 | x031 | !x039) & (!x031 | x036 | !x033) & (x062 | x049 | x018) & (x046 | x057 | x066) & (!x045 | x058 | x029) & (x075 | x046 | !x058) & (x073 | !x038 | !x063) & (!x067 | x045 | x008) & (x017 | !x023 | x025) & (x020 | x003 | !x065) & (x076 | x041 | x011) & (!x006 | x049 | !x034) & (!x053 | !x021 | x031) & (x038 | x043 | x065) & (x003 | !x042 | !x047) & (x045 | !x051 | x033) & (x057 | x041 | !x030) & (!x015 | x025 | x063) & (!x060 | !x070 | !x025) & (x044 | x006 | x076) & (!x037 | x042 | !x029) & (!x063 | !x066 | x068) & (!x027 | !x058 | !x059) & (!x067 | !x055 | !x025) & (!x070 | x050 | !x040) & (!x034 | !x059 | x064) & (!x077 | x039 | !x079) & (x064 | !x030 | x003) & (x021 | x065 | !x015) & (!x014 | x058 | !x078) & (x004 | x037 | x059)";
    }

}