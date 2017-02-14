using System;
using System.Collections.Generic;
using System.Linq;

namespace PAA
{
    public class InputGenerator
    {
        private Random random;
        private HashSet<string> allLiterals;

        public InputGenerator()
        {
            random = new Random();
        }

        private int [] GenerateWeights(int literalsCount, int maxWeight)
        {
            var weights = new int[literalsCount];

            for (int i = 0; i < literalsCount; i++)
            {
                weights[i] = random.Next(1, maxWeight);
            }

            return weights;
        }

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

        public string GenerateInput(int literalsCount, int clausulesCount, int maxWeight)
        {
            if(literalsCount < 3 || clausulesCount < Math.Ceiling((double)literalsCount / 3))
            {
                throw new ArgumentException("Neplatné parametry");
            }
            allLiterals = new HashSet<string>();

            var formula = GenerateFormula(literalsCount + 1, clausulesCount);
            var weights = GenerateWeights(allLiterals.Count(), maxWeight);

            //for (int i = 0 i < weights.Length i++)
            //{
            //    Console.Write(string.Format("{0}: {1},", allLiterals.OrderBy(x => x).ElementAt(i), weights[i]));
            //}
            //Console.WriteLine();
            //Console.WriteLine(formula);

            return string.Format("{0} {1} {2}", allLiterals.Count(), string.Join(" ", weights), formula);
        }
    }
}
