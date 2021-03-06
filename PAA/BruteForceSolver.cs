﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace PAA
{
    public class BruteForceSolver
    {

        internal class BestBruteForceSolution
        {
            public bool[] Parameters { get; set; }
            public int Weight { get; set; }
        }

        private BestBruteForceSolution bestBFSolution;

        Dictionary<string, ParameterExpression> parametersDictionary;
        Delegate evaluationMethod;
        int[] weights;

        public BruteForceSolver(int[] weights, Delegate evaluationMethod, Dictionary<string, ParameterExpression> parametersDictionary)
        {
            this.weights = weights;
            this.evaluationMethod = evaluationMethod;
            this.parametersDictionary = parametersDictionary;
            bestBFSolution = new BestBruteForceSolution() { Weight = 0 };
        }

        /// <summary>
        /// Calculates the all posibilities by brute-force alghorithm
        /// </summary>
        public void BruteForce(int position = 0, bool[] bools = null)
        {
            if (bools == null)
            {
                bools = new bool[parametersDictionary.Count()];
            }
            if (position == parametersDictionary.Count())
            {
                var configuration = bools.Cast<object>().ToList();
                configuration.Add(new Entity(parametersDictionary.Count()));
                var result = evaluationMethod.DynamicInvoke(configuration.ToArray());
                var weight = 0;
                if (((Entity)result).Satisfability)
                {
                    for (int i = 0; i < parametersDictionary.Count(); i++)
                    {
                        if (bools[i]) weight += weights[i];
                    }
                    if (weight > bestBFSolution.Weight)
                    {
                        bestBFSolution.Weight = weight;
                        bestBFSolution.Parameters = bools.ToArray();
                    }
                }
            }
            else
            {
                bools[position] = false;
                BruteForce(position + 1, bools);
                bools[position] = true;
                BruteForce(position + 1, bools);
            }
        }

        /// <summary>
        /// Print of best brute force solution
        /// </summary>
        public bool PrintBestSolution()
        {
            if (bestBFSolution == null || bestBFSolution.Parameters == null)
            {
                Console.WriteLine("There doesn't exist solution");
                return false;
            }
            for (int i = 0; i < parametersDictionary.Count(); i++)
            {
                // print of configuration variables
                Console.Write(string.Format("{0} ", bestBFSolution.Parameters[i] ? "1" : "0"));
            }
            Console.WriteLine("\nWeight: {0}", bestBFSolution.Weight);
            return true;
        }

        /// <summary>
        /// Run the alghoritm
        /// </summary>
        public void Run()
        {
            var time = new Stopwatch();
            var milliseconds = 0L;
            time.Restart();

            BruteForce();
            milliseconds = time.ElapsedMilliseconds;
            Console.WriteLine("\nBruteForce:\t{0} ms\n", milliseconds);
            PrintBestSolution();
        }
    }
}
