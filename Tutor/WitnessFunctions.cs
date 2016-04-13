﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.Extraction.Text.Semantics;
using Microsoft.ProgramSynthesis.Learning;
using Microsoft.ProgramSynthesis.Rules;
using Microsoft.ProgramSynthesis.Specifications;
using Microsoft.ProgramSynthesis.Utils;
using static Tutor.Transformation.RegexUtils;

namespace Tutor.Transformation
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public static class WitnessFunctions
    {
        [WitnessFunction("Apply", 1)]
        public static ExampleSpec WitnessPatch(GrammarRule rule, int parameter, ExampleSpec spec)
        {
            var patchExamples = new Dictionary<State, object>();
            foreach (var input in spec.ProvidedInputs)
            {
                var before = (PythonNode)input[rule.Body[0]];
                var after = (PythonNode)spec.Examples[input];

                var zss = new PythonZss(before, after);
                var editDistance = zss.Compute();
                patchExamples[input] = editDistance.Item2.First();
            }
            return  new ExampleSpec(patchExamples);
        }

        [WitnessFunction("Patch", 0)]
        public static ExampleSpec WitnessEdit(GrammarRule rule, int parameter, ExampleSpec spec)
        {
            var editExamples = new Dictionary<State, object>();
            foreach (var input in spec.ProvidedInputs)
            {
                var operation = spec.Examples[input] as Operation;
                editExamples[input] = operation;
            }
            return new ExampleSpec(editExamples);
        }

        [WitnessFunction("Patch", 1)]
        public static DisjunctiveExamplesSpec WitnessContext(GrammarRule rule, int parameter, ExampleSpec spec)
        {
            var contextExamples = new Dictionary<State, IEnumerable<object>>();
            foreach (State input in spec.ProvidedInputs)
            {
                var operation = spec.Examples[input] as Operation;
                if (operation != null)
                {
                    var target = operation.Target;
                    contextExamples[input] = new List<object>() { target };
                }
            }
            return DisjunctiveExamplesSpec.From(contextExamples);
        }

        [WitnessFunction("Match", 1)]
        public static DisjunctiveExamplesSpec WitnessTemplate(GrammarRule rule, int parameter, ExampleSpec spec)
        {
            var templateExamples = new Dictionary<State, IEnumerable<object>>();
            foreach (var input in spec.ProvidedInputs)
            {
                var templateTrees = new List<object>();
                foreach (PythonNode node in spec.DisjunctiveExamples[input])
                {
                    node.EditId = 1;
                    templateTrees.Add(node.Parent);
                    templateTrees.Add(node.Parent.GetAbstractCopy());
                    templateTrees.Add(node);
                    templateTrees.Add(node.GetAbstractCopy());
                }
                templateExamples[input] = templateTrees;
            }
            return DisjunctiveExamplesSpec.From(templateExamples);
        }

        [WitnessFunction("SubStr", 1)]
        public static DisjunctiveExamplesSpec WitnessPositionPair(GrammarRule rule, int parameter,
                                                                  ExampleSpec spec)
        {
            var ppExamples = new Dictionary<State, IEnumerable<object>>();
            foreach (State input in spec.ProvidedInputs)
            {
                var v = (StringRegion) input[rule.Body[0]];
                var desiredOutput = (StringRegion) spec.Examples[input];
                var occurrences = new List<object>();
                for (int i = v.Value.IndexOf(desiredOutput.Value, StringComparison.Ordinal);
                     i >= 0;
                     i = v.Value.IndexOf(desiredOutput.Value, i + 1, StringComparison.Ordinal))
                {
                    occurrences.Add(Tuple.Create(v.Start + (uint?) i, v.Start + (uint?) i + desiredOutput.Length));
                }
                ppExamples[input] = occurrences;
            }
            return DisjunctiveExamplesSpec.From(ppExamples);
        }

        [WitnessFunction("AbsPos", 1)]
        public static DisjunctiveExamplesSpec WitnessK(GrammarRule rule, int parameter,
                                                       DisjunctiveExamplesSpec spec)
        {
            var kExamples = new Dictionary<State, IEnumerable<object>>();
            foreach (State input in spec.ProvidedInputs)
            {
                var v = (StringRegion) input[rule.Body[0]];
                var positions = new List<object>();
                foreach (uint pos in spec.DisjunctiveExamples[input])
                {
                    positions.Add((int) pos + 1 - (int) v.Start);
                    positions.Add((int) pos - (int) v.End - 1);
                }
                kExamples[input] = positions;
            }
            return DisjunctiveExamplesSpec.From(kExamples);
        }

        [WitnessFunction("RegPos", 1)]
        public static DisjunctiveExamplesSpec WitnessRegexPair(GrammarRule rule, int parameter,
                                                               DisjunctiveExamplesSpec spec)
        {
            var rrExamples = new Dictionary<State, IEnumerable<object>>();
            foreach (State input in spec.ProvidedInputs)
            {
                var v = (StringRegion) input[rule.Body[0]];
                var regexes = new List<Tuple<RegularExpression, RegularExpression>>();
                foreach (uint pos in spec.DisjunctiveExamples[input])
                {
                    Dictionary<Token, TokenMatch> rightMatches;
                    if (!v.Cache.TryGetAllMatchesStartingAt(pos, out rightMatches)) continue;
                    Dictionary<Token, TokenMatch> leftMatches;
                    if (!v.Cache.TryGetAllMatchesEndingAt(pos, out leftMatches)) continue;
                    var leftRegexes = leftMatches.Keys.Select(RegularExpression.Create).Append(Epsilon);
                    var rightRegexes = rightMatches.Keys.Select(RegularExpression.Create).Append(Epsilon);
                    var regexPairs = from l in leftRegexes from r in rightRegexes select Tuple.Create(l, r);
                    regexes.AddRange(regexPairs);
                }
                rrExamples[input] = regexes;
            }
            return DisjunctiveExamplesSpec.From(rrExamples);
        }

        [WitnessFunction("RegPos", 2, DependsOnParameters = new[] { 1 })]
        public static DisjunctiveExamplesSpec WitnessRegexCount(GrammarRule rule, int parameter,
                                                                DisjunctiveExamplesSpec spec,
                                                                ExampleSpec regexBinding)
        {
            var kExamples = new Dictionary<State, IEnumerable<object>>();
            foreach (State input in spec.ProvidedInputs)
            {
                var v = (StringRegion) input[rule.Body[0]];
                var rr = (Tuple<RegularExpression, RegularExpression>) regexBinding.Examples[input];
                var ks = new List<object>();
                foreach (uint pos in spec.DisjunctiveExamples[input])
                {
                    var ms = rr.Item1.Run(v).Where(m => rr.Item2.MatchesAt(v, m.Right)).ToArray();
                    int index = ms.BinarySearchBy(m => m.Right.CompareTo(pos));
                    if (index < 0) return null;
                    ks.Add(index + 1);
                    ks.Add(index - ms.Length);
                }
                kExamples[input] = ks;
            }
            return DisjunctiveExamplesSpec.From(kExamples);
        }
    }
}
