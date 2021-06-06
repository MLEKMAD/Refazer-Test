using System;
using System.Collections.Generic;
using System.Linq;
using Refazer.Core;
using Tutor;
namespace Test_project
{
    class Program
    {
        static void Main(string[] args)
        {
            var before = "x = 0;";
            var after = @"x = 1;";
            List<Tuple<string, string>> examples = new List<Tuple<string, string>>() { Tuple.Create(before, after) };
            var refazer = new Refazer4Python();
            var transformation = refazer.LearnTransformations(examples.ToList()).First();
            foreach (var mistake in examples)
            {
                var output = refazer.Apply(transformation, mistake.Item1);
                Console.Out.WriteLine("OUTPUT: ",output);
                var isFixed = false;
                foreach (var newCode in output)
                {
                    var unparser = new Unparser();
                    isFixed = mistake.Item2.Equals(newCode);
                    if (isFixed)
                        break;
                }
            }
        }
    }
}