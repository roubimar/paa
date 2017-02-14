using System;
using System.Collections.Generic;
using System.Linq;

namespace PAA
{
    /// <summary>
    /// Generator of desired 3-SAT instances
    /// </summary>
    public class InputGenerator
    {
        private Random random;
        private HashSet<string> allLiterals;

        public InputGenerator()
        {
            random = new Random();
        }

        /// <summary>
        /// Generate weights for number of literals
        /// </summary>
        private int [] GenerateWeights(int literalsCount, int maxWeight)
        {
            var weights = new int[literalsCount];

            for (int i = 0; i < literalsCount; i++)
            {
                weights[i] = random.Next(1, maxWeight + 1 );
            }
            return weights;
        }

        /// <summary>
        /// Generate formula of 3-SAT (for desired count of literals of desired clausules)
        /// For each clausule is generated trio of distinguished literals and with probability of 50% it is negated
        /// </summary>
        private string GenerateFormula(int literalsCount, int clausulesCount)
        {
            var formula = string.Empty;
            var clausules = new List<string>();
            for (int i = 0; i < clausulesCount; i++)
            {
                var literals = new List<string>();
                int[] numbers = new int[3];
                for (int j = 0; j < 3; j++)
                {
                    int number;
                    do {
                        number = random.Next(0, literalsCount);
                    } while (numbers.Contains(number));
                    numbers[j] = number;
                    var name = string.Format("x{0:000}", number);
                    var literal = string.Format("{0}{1}", random.NextDouble() > 0.5 ? "!" : string.Empty, name);
                    literals.Add(literal);
                    if(!allLiterals.Contains(name))
                    {
                        allLiterals.Add(name);
                    }
                }
                var clausule = "(" + string.Join(" | ", literals) + ")";
                clausules.Add(clausule);
            }
            return string.Join(" & ", clausules); 
        }

        /// <summary>
        /// Generate Input for alghoritmhs
        /// </summary>
        /// <param name="literalsCount">desired literals count - this is randomed and could be lower</param>
        /// <param name="clausulesCount">number of clausules</param>
        /// <param name="maxWeight">max weight of one literal</param>
        /// <returns>String inf format: [Number of literals(N)] [Weight_1..N] [(X_1..N | X_1..N | X_1..N) & (...) ]</returns>
        public string GenerateInput(int literalsCount, int clausulesCount, int maxWeight)
        {
            if(literalsCount < 3 || clausulesCount < Math.Ceiling((double)literalsCount / 3))
            {
                throw new ArgumentException("Neplatné parametry");
            }
            allLiterals = new HashSet<string>();

            var formula = GenerateFormula(literalsCount + 1, clausulesCount);
            var weights = GenerateWeights(allLiterals.Count(), maxWeight);
            
            return string.Format("{0} {1} {2}", allLiterals.Count(), string.Join(" ", weights), formula);
        }
    }
}
