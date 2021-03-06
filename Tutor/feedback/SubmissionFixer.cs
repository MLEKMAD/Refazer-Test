using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsQuery.ExtensionMethods.Internal;
using IronPython.Compiler.Ast;
using IronPython.Modules;
using Microsoft.CodeAnalysis.Semantics;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.AST;
using Microsoft.ProgramSynthesis.Compiler;
using Microsoft.ProgramSynthesis.Diagnostics;
using Microsoft.ProgramSynthesis.Learning;
using Microsoft.ProgramSynthesis.Specifications;
using Tutor.ast;
using Tutor.Transformation;

namespace Tutor
{
    public class SubmissionFixer
    {
        public ConcurrentQueue<Tuple<List<Mistake>, ProgramNode>> _classification;
        public List<IEnumerable<ProgramNode>> ProsePrograms { get; }

        public Dictionary<string, int> UsedPrograms { get; }



        public SubmissionFixer(ConcurrentQueue<Tuple<List<Mistake>, ProgramNode>> classification, 
            string pathToGrammar = @"..\..\..\Tutor\synthesis\", string pathToDslLib = @"..\..\Tutor\bin\debug") : this(pathToGrammar, pathToDslLib)
        {
            _classification = classification;
            ProsePrograms = new List<IEnumerable<ProgramNode>>();
            UsedPrograms = new Dictionary<string, int>();
        }

        public SubmissionFixer(string pathToGrammar = @"..\..\..\Tutor\synthesis\", string pathToDslLib = @"..\..\Tutor\bin\debug")
        {
            ProsePrograms = new List<IEnumerable<ProgramNode>>();
            UsedPrograms = new Dictionary<string, int>();
            if (pathToDslLib == null)
            {
                pathToDslLib = ".";
            }
            if (pathToGrammar == null)
            {
                pathToGrammar = "../../../Tutor/synthesis/";
            }
            grammar =
                DSLCompiler.ParseGrammarFromFile(pathToGrammar + @"Transformation.grammar",
                    libraryPaths: new[] { pathToDslLib });
        }

        //public SubmissionFixer Create(Tuple<string, string> example, 
        //    string ranking, int numberOftransformations, string pathToGrammar, string pathToLib)
        //{
        //    var unparser = new Unparser();
        //    var before = unparser.Unparse(NodeWrapper.Wrap(ASTHelper.ParseContent(example.Item1)));
        //    var after = unparser.Unparse(NodeWrapper.Wrap(ASTHelper.ParseContent(example.Item1)));
        //    var mistake = new Mistake() { before = before, after = after };
        //    var list = new List<Mistake>() { mistake };
        //    var queue = new ConcurrentQueue<Tuple<List<Mistake>, ProgramNode>>();
        //    //if ranking is specific, we add more weight for expressions that contains parent context
        //    RankingScore.ScoreForContext = ranking.Equals("specific") ? 100 : -100;
        //    var transformations = LearnPrograms(list, numberOftransformations);

        //    return new SubmissionFixer(queue);
        //}

        public bool Fix(Mistake mistake, Dictionary<string, long> tests, bool leaveOneOut = true)
        {
            //PythonAst ast = null;
            //ast = ASTHelper.ParseContent(mistake.before);
            //var input = State.Create(grammar.Value.InputSymbol, NodeWrapper.Wrap(ast));
            //var unparser = new Unparser();

            //foreach (var tuple in _classification)
            //{
            //    var belongs = false;
            //    foreach (var mistake1 in tuple.Item1)
            //    {
            //        if (mistake.Equals(mistake1))
            //            belongs = true;
            //    }
            //    if (belongs && leaveOneOut)
            //    {
            //        var listWithoutCurrentMistake = tuple.Item1.Where(e => !e.Equals(mistake));
            //        if (!listWithoutCurrentMistake.Any()) return false;
            //        var program = LearnProgram(listWithoutCurrentMistake.ToList());
            //        if (program == null) return false;

            //        var fixedCode = TryFix(tests, program, input, unparser);
            //        if (fixedCode != null)
            //        {
            //            mistake.UsedFix = program.ToString();
            //            mistake.SynthesizedAfter = fixedCode;
            //            return true;
            //        }
            //    }
            //    else
            //    {
            //        var fixedCode = TryFix(tests, tuple.Item2, input, unparser);
            //        if (fixedCode != null)
            //        {
            //            mistake.UsedFix = tuple.Item2.ToString();
            //            mistake.SynthesizedAfter = fixedCode;
            //            return true;
            //        }
            //    }
            //}
            return false;
        }

        private bool TryInParallel(Mistake mistake, Dictionary<string, long> tests, ProgramNode program, State input)
        {
            var  unparser = new Unparser();
            bool isFixed = false;
            var fixedCode = TryFix(tests, program, input, unparser);
            if (fixedCode != null)
            {
                mistake.UsedFix = program.ToString();
                mistake.SynthesizedAfter = fixedCode;
                isFixed = true;
            }
            return isFixed;
        }

        public  string pathToGrammar;
        public  string pathToDslLib;
        public  Result<Grammar> grammar;

        public ProgramNode LearnProgram(List<Mistake> list, Mistake next)
        {
            var mistakes =  (list.Any()) ?  new List<Mistake>() { list.First(),  next } :
                new List<Mistake>() { next };
            if (LearnProgram(mistakes) != null)
            {
                mistakes  = (list.Count > 30) ? new List<Mistake>(list.GetRange(0,30)) { next }:
                    new List<Mistake>(list) { next };
                return LearnProgram(mistakes);
            }
            return null;
        }

        public ProgramNode LearnProgram(List<Mistake> mistakes)
        {
            return LearnPrograms(mistakes,1).First();
        }

        private IEnumerable<ProgramNode> LearnPrograms(List<Mistake> mistakes, int k, string scoreFunction = "Score")
        {
            //var examples = new Dictionary<State, object>();
            //var unparser = new Unparser();
            //foreach (var mistake in mistakes)
            //{
            //    var astBefore = NodeWrapper.Wrap(ASTHelper.ParseContent(mistake.before));
            //    var input = State.Create(grammar.Value.InputSymbol, astBefore);
            //    var astAfter = NodeWrapper.Wrap(ASTHelper.ParseContent(mistake.after));
            //    examples.Add(input, astAfter);
            //}
            //var spec = new ExampleSpec(examples);
            //var prose = new SynthesisEngine(grammar.Value);
            //var learned = prose.LearnGrammarTopK(spec, scoreFunction, k: k);
            //return learned;
            throw  new NotImplementedException();
        }

        public ProgramNode LearnProgram(Mistake mistake, State input)
        {
            //var examples = new Dictionary<State, object>();

            //var astAfter = NodeWrapper.Wrap(ASTHelper.ParseContent(mistake.after));
            //examples.Add(input, astAfter);

            //var spec = new ExampleSpec(examples);
            //var prose = new SynthesisEngine(grammar.Value);
            //var learned = prose.LearnGrammarTopK(spec, "Score", k: 1);
            //return learned.Any() ? learned.First() : null;
            throw new NotImplementedException();
        }


        public string TryFix(Dictionary<string, long> tests, ProgramNode current, 
            State input, Unparser unparser, Tuple<string, List<string>> staticTests = null)
        {            
            object output = null;
            try
            {
                output = current.Invoke(input);
            }
            catch (Exception)
            {
                return null;
            }
            if (output != null)
            {
                var programSet = output as IEnumerable<PythonNode>;
                var range = programSet.Count() < 100 ? programSet.ToList() : programSet.ToList().GetRange(0, 200); 
                foreach (var changedProgram in range)
                {
                    if (staticTests != null && !CheckStaticTests(changedProgram, staticTests))
                        continue; 
                    var newCode = unparser.Unparse(changedProgram);
                    try
                    {
                        var isFixed = IsFixed(tests, newCode);
                        if (isFixed)
                        {
                            if (UsedPrograms.ContainsKey(current.ToString()))
                            {
                                var count = UsedPrograms[current.ToString()];
                                UsedPrograms.Remove(current.ToString());
                                UsedPrograms.Add(current.ToString(), count + 1);
                            }
                            else
                            {
                                UsedPrograms.Add(current.ToString(), 1);
                            }
                            return newCode;
                        }
                    }
                    catch (Exception e)
                    {
                        Trace.TraceError("Exception was thrown when trying to apply fix to submission");
                        Trace.TraceError(e.Message);
                    }
                }
            }
            return null;
        }

        public bool CheckStaticTests(PythonNode changedProgram, Tuple<string, List<string>> staticTests)
        {
            var findFunctionVisitor = new FindFunctionVisitor(staticTests.Item1);
            changedProgram.Walk(findFunctionVisitor);
            if (findFunctionVisitor.Function != null)
            {
                var visitor = new StaticAnalysisTester(staticTests);
                findFunctionVisitor.Function.Walk(visitor);
                return visitor.Passed;
            }
            return false;
        }

        public bool IsFixed(Dictionary<string, long> tests, string newCode)
        {
            var isFixed = true;
            var script = newCode;
            foreach (var test in tests)
            {
               script +=  Environment.NewLine + test.Key;
            }
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("python.exe", "-c \"" + script + "\"")
                {
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                var p = Process.Start(psi);
                if (p == null)
                    isFixed = false;
                else
                {
                    if (!p.HasExited)
                    {
                        p.WaitForExit(1500);
                    }
                    if (!p.HasExited || p.ExitCode != 0)
                        isFixed = false;
                    if (!p.HasExited)
                    {
                        try
                        {
                            p.Kill();
                        }
                        catch (Exception e)
                        {
                            Trace.TraceError("Exception was thrown when running python tests.");
                            Trace.TraceError(e.Message);
                            Console.Out.WriteLine("Exception when trying to kill process");
                            //do nothing   
                        }
                    }
                    p.Close();
                }
            }
            catch (TestCaseException e)
            {
                Trace.TraceError("Exception was thrown when running python tests.");
                Trace.TraceError("Exception was thrown.");
                Trace.TraceError(e.Message);
                isFixed = false;
            }
            catch (RuntimeBinderException e)
            {
                Trace.TraceError("Exception was thrown when running python tests.");
                Trace.TraceError(e.Message);
                isFixed = false;
            }
            return isFixed;
        }

        public bool FixItSelf(Mistake mistake, Dictionary<string, long> tests)
        {
            //var unparser = new Unparser();

            //PythonAst ast = null;
            //try
            //{
            //    ast = ASTHelper.ParseContent(mistake.before);
            //    var cleanCode = unparser.Unparse(NodeWrapper.Wrap(ast));
            //    mistake.before = cleanCode;
            //    ast = ASTHelper.ParseContent(cleanCode);

            //    var astAfter = NodeWrapper.Wrap(ASTHelper.ParseContent(mistake.after));
            //    var cleanedAfter = unparser.Unparse(astAfter);
            //    mistake.after = cleanedAfter;
            //}
            //catch (Exception e)
            //{
            //    Console.Error.WriteLine(e.Message);
            //    return false;
            //}
            //var input = State.Create(grammar.Value.InputSymbol, NodeWrapper.Wrap(ast));
            //var program = LearnProgram(mistake, input);
            //if (program == null) return false;

            //var fixedCode = TryFix(tests, program, input, unparser);
            //if (fixedCode != null)
            //{
            //    return true;
            //}
            return false;
        }
        
    }

    public class StaticAnalysisTester : IVisitor
    {
        private readonly Tuple<string, List<string>> _tests;

        public bool Passed { set; get; } = true;

        public StaticAnalysisTester(Tuple<string, List<string>> tests)
        {
            _tests = tests;
        }

        public bool Visit(PythonNode pythonNode)
        {
            foreach (var test in _tests.Item2)
            {
                switch (test)
                {
                    case "recursion":
                        if (pythonNode is CallExpressionNode)
                        {
                            if (pythonNode.Children.Any() && pythonNode.Children.First() is NameExpressionNode
                                && pythonNode.Children.First().Value.Equals(_tests.Item1))
                                Passed = false; 
                        }
                            break;
                    case "for":
                        if (pythonNode is ForStatementNode)
                            Passed = false;
                        break;
                    case "while":
                        if (pythonNode is WhileStatementNode)
                            Passed = false;
                        break;
                    case "Assign":
                        if (pythonNode is AssignmentStatementNode)
                            Passed = false;
                        break;
                    case "AugAssign":
                        if (pythonNode is AugmentedAssignStatementNode)
                            Passed = false;
                        break;
                }
            }
            return Passed;
        }
    }

    public class FindFunctionVisitor : IVisitor
    {
        private readonly string _name;

        public PythonNode Function { set; get; }

        public FindFunctionVisitor(string name)
        {
            _name = name;
        }

        public bool Visit(PythonNode pythonNode)
        {
            if (pythonNode is FunctionDefinitionNode &&
                pythonNode.Value != null && Equals(pythonNode.Value, _name))
            {
                Function = pythonNode;
            }
            return Function == null;
        }
    }
}
