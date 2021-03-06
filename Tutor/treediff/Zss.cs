using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using CsQuery.ExtensionMethods.Internal;
using IronPython.Compiler.Ast;

namespace Tutor
{
    /// <summary>
    /// Class that implements the Zhang and Shasha algoritm for tree edit distance
    /// Reference: http://research.cs.queensu.ca/TechReports/Reports/1995-372.pdf
    /// </summary>
    public abstract class Zss
    {
        /// <summary>
        /// ASTs in some representation T of the programs before and after the change
        /// </summary>
        protected PythonNode PreviousTree, CurrentTree; 

        /// <summary>
        /// AST of the previous and the current programs wrapped in the ZssNode class
        /// </summary>
        protected List<PythonNode> A, B;
        /// <summary>
        /// list of vertices of previous and current trees sorted by postorder traversal. 
        /// Each vertice is represented by an interger. 
        /// </summary>
        protected int[] T1, T2;
        /// <summary>
        /// the leftmost leaf descendant of the subtree rooted at i
        /// </summary>
        private int[] _l1,_l2;
        /// <summary>
        /// keyroots of the asts before and after the change
        /// </summary>
        private List<Int32> _k1, _k2;
        /// <summary>
        /// dynamic programming table with the edit distances as Tuple. The first item is the cost
        /// the second item is the list of edit operations 
        /// </summary>
        private EditDistance[,] _treedists;


        /// <summary>
        /// Create an object to compute the edit distance given two trees
        /// </summary>
        /// <param name="previousTree">Tree before the change</param>
        /// <param name="currentTree">Tree after the change</param>
        protected Zss(PythonNode previousTree, PythonNode currentTree)
        {
            PreviousTree = previousTree;
            CurrentTree = currentTree;
        }


        /// <summary>
        /// Abstract method that wraps the given trees into the ZssNode class and generate 
        /// the list of vertices of the trees
        /// </summary>
        /// <param name="t1">Tree before the change</param>
        /// <param name="t2">Tree after the change</param>
        protected abstract void GenerateNodes(PythonNode t1, PythonNode t2);

        /// <summary>
        /// Generate keyroots of tree T , K(T) = { k E T | !e k' > k with l(k') = l(k) }
        /// </summary>
        /// <param name="tree">list of vertices of the tree sorted in post order</param>
        /// <param name="l">the leftmost leaf descendant of the subtree rooted at i</param>
        /// <returns></returns>
        private List<Int32> ComputeK(int[] tree,  
            int[] l)
        {
            var result = new List<Int32>();
            //for each vertice, checks if the keyroot condition is valid. If so, add it
            //to the keyroot list
            for (var i = 0; i < tree.Length; i++)
            {
                var isKeyRoot = true;
                if (i < tree.Length - 1)
                {
                    for (var j = i + 1; j < tree.Length; j++)
                    {
                        if (l[tree[i]] == l[tree[j]])
                            isKeyRoot = false;
                    }
                }
                if (isKeyRoot)
                    result.Add(tree[i]);
            }
            return result;
        }

        /// <summary>
        /// Compute the list l, where l(i) is the leftmost leaf descendant of the subtree rooted at i
        /// </summary>
        /// <param name="t1">tree sorted in post order</param>
        /// <param name="tree">actual tree to get the left most descendant for each i</param>
        /// <returns></returns>
        private int[] ComputeL(int[] t1, List<PythonNode>  tree)
        {
            var result = new int[t1.Length+1];
            result[0] = 0;
            foreach (var node in t1)
            {
                var currentNode = tree[node-1];
                result[node] = tree.IndexOf(currentNode.GetLeftMostDescendant()) + 1;
            }
            return result;
        }

       /// <summary>
       /// Compute the tree edit distance
       /// </summary>
       /// <returns>Returns a tuple. The first item is the cost. The second item is the 
       /// sequence of edit operations</returns>
        public EditDistance  Compute()
        {
            GenerateNodes(PreviousTree, CurrentTree);
            _l1 = ComputeL(T1, A);
            _l2 = ComputeL(T2, B);
            _k1 = ComputeK(T1, _l1);
            _k2 = ComputeK(T2, _l2);

            _treedists = new EditDistance[T1.Length + 1, T2.Length + 1];

            _treedists[0, 0] = new EditDistance() {Distance = 0};
                 
            foreach (var x in _k1)
            {
                foreach (var y in _k2)
                {
                    Treedists(x, y);
                }
            }
            
            return _treedists[T1.Length,T2.Length];
        }

        private void Treedists(int i, int j)
        {
            var m = i - _l1[i] + 2;
            var n = j - _l2[j] + 2;

            var fd = new EditDistance[m, n];
            fd[0, 0] = new EditDistance() {Distance = 0};
            var ioff = _l1[i] - 1;
            var joff = _l2[j] - 1;

            for (int x = 1; x < m; x++)
            {
                var edits = new List<Edit>(fd[x - 1, 0].Edits);
                var pythonNode = A[x +ioff -1];
                edits.Add(new Delete(pythonNode, pythonNode.Parent));
                fd[x, 0] =  new EditDistance() {Distance = fd[x - 1, 0].Distance+ 1 , Edits = edits, Mapping = new Dictionary<PythonNode, PythonNode>(fd[x - 1, 0].Mapping) }; 
            }
            for (int y = 1; y < n; y++)
            {
                var node = B[y - 1 + joff];
                var edits = new List<Edit>(fd[0, y - 1].Edits);
                edits.Add(new Insert(node, node.Parent));
                fd[0, y] = new EditDistance() { Distance = fd[0, y - 1].Distance + 1, Edits = edits, Mapping = new Dictionary<PythonNode, PythonNode>(fd[0, y - 1].Mapping)};
            }

            for (int x = 1; x < m; x++)
            {
                for (int y = 1; y < n; y++)
                {
                    if (_l1[i] == _l1[x + ioff] && _l2[j] == _l2[y + joff])
                    {
                        var value = Math.Min(Math.Min(fd[x - 1, y].Distance + 1, //cost to remove is 1
                            fd[x, y - 1].Distance + 1), //cost to insert is 1
                            fd[x-1,y-1].Distance + CostUpdate(A[x+ioff-1], B[y+joff-1])); //cost to Edit depends

                        List<Edit> edits;
                        Dictionary<PythonNode, PythonNode> mapping;
                        if (value == fd[x - 1, y].Distance + 1)
                        {
                            var node = A[x - 1 + ioff];
                            edits = new List<Edit>(fd[x - 1, y].Edits) {new Delete(node, node.Parent)};
                            mapping = new Dictionary<PythonNode, PythonNode>(fd[x - 1, y].Mapping);
                        } else if (value == fd[x, y - 1].Distance + 1)
                        {
                            var node = B[y - 1 + joff];
                            edits = new List<Edit>(fd[x, y - 1].Edits) { new Insert(node, node.Parent) };
                            mapping = new Dictionary<PythonNode, PythonNode>(fd[x, y - 1].Mapping);
                        }
                        else
                        {
                            edits = new List<Edit>(fd[x - 1, y - 1].Edits);
                            var oldNode = A[x + ioff - 1];
                            var newNode = B[y + joff - 1];
                            
                            if (CostUpdate(oldNode, newNode) > 0)
                                edits.Add(new Update(newNode, oldNode));

                            mapping = new Dictionary<PythonNode, PythonNode>(fd[x - 1, y - 1].Mapping);

                            if (mapping.ContainsKey(newNode))
                                mapping.Remove(newNode);

                            mapping.Add(newNode, oldNode);
                        }

                        fd[x, y] = new EditDistance() {Distance = value, Edits = edits, Mapping = mapping};
                        _treedists[x + ioff, y + joff] = fd[x, y];
                    }
                    else
                    {
                        var p = _l1[x + ioff] - 1 - ioff;
                        var q = _l2[y + joff] - 1 - joff;

                        var value = Math.Min(fd[p, q].Distance + _treedists[x + ioff, y + joff].Distance, 
                                Math.Min(fd[x - 1, y].Distance + 1, fd[x, y - 1].Distance + 1));

                        List<Edit> edits;
                        Dictionary<PythonNode, PythonNode> mapping; 
                        if (value == fd[p, q].Distance + _treedists[x + ioff, y + joff].Distance)
                        {
                            edits = new List<Edit>(fd[p, q].Edits);
                            edits.AddRange(_treedists[x + ioff, y + joff].Edits);
                            mapping = new Dictionary<PythonNode, PythonNode>(fd[p, q].Mapping);
                            foreach (var keyValuePair in _treedists[x + ioff, y + joff].Mapping)
                            {
                                //todo there is problem a bug here
                                //it should check whether the maps come from each one of them.
                                if (mapping.ContainsKey(keyValuePair.Key))
                                    mapping.Remove(keyValuePair.Key);

                                PythonNode key = null;
                                foreach (var keyValue in mapping)
                                {
                                    if (keyValue.Value.Equals(keyValuePair.Value))
                                        key = keyValue.Key;
                                }
                                if (key != null) mapping.Remove(key);

                                mapping.Add(keyValuePair.Key, keyValuePair.Value);
                            }
                        }
                        else if (value == fd[x - 1, y].Distance + 1)
                        {
                            edits = new List<Edit>(fd[x - 1, y].Edits);
                            var pythonNode = A[x - 1 + ioff];
                            edits.Add(new Delete(pythonNode, pythonNode.Parent));
                            mapping = new Dictionary<PythonNode, PythonNode>(fd[x - 1, y].Mapping);
                        }
                        else
                        {
                            edits = new List<Edit>(fd[x, y - 1].Edits);
                            var pythonNode = B[y - 1 + joff];
                            edits.Add(new Insert(pythonNode, pythonNode.Parent));
                            mapping = new Dictionary<PythonNode, PythonNode>(fd[x, y - 1].Mapping);
                        }

                        fd[x, y] = new EditDistance() {Distance = value, Edits = edits, Mapping = mapping};
                    }
                }
            }
        }

        private int CostUpdate(PythonNode zssNode, PythonNode node1)
        {
            return (zssNode.Similar(node1)) ? 0 : 1;
        }
    }

    public class EditDistance
    {
        public List<Edit> Edits { get; set; }

        public int Distance { get; set; }

        public Dictionary<PythonNode, PythonNode> Mapping { get; set; } 

        public EditDistance()
        {
            Edits = new List<Edit>();
            Mapping = new Dictionary<PythonNode, PythonNode>();
        }
    }

    public class PythonZss : Zss
    {
        public PythonZss(PythonNode previousTree, PythonNode currentTree)
            : base(previousTree,currentTree)
        {
        }

        protected override void GenerateNodes(PythonNode t1, PythonNode t2)
        {
            var visitor = new SortedTreeVisitor();
            t1.PostWalk(visitor);
            A = visitor.Nodes;
            T1 = Enumerable.Range(1, A.Count).ToArray();
            visitor = new SortedTreeVisitor();
            t2.PostWalk(visitor);
            B = visitor.Nodes;
            T2 = Enumerable.Range(1, B.Count).ToArray();
        }
    }

    public class SortedTreeVisitor
    {
        public List<PythonNode> Nodes { get; }

        public SortedTreeVisitor()
        {
            Nodes = new List<PythonNode>();
        } 
    }


    public abstract class ZssNode<T>
    {
        public string Label { set; get; }

        public T InternalNode { set; get; }

        public abstract bool Similar(ZssNode<T> other);

        public abstract ZssNode<T> GetLeftMostDescendant();

        public abstract string AbstractType();
    }


    //class PythonZssNode : ZssNode<PythonNode>
    //{
    //    protected bool Equals(PythonZssNode other)
    //    {
    //        return string.Equals(Label, other.Label) && Equals(InternalNode, other.InternalNode);
    //    }

    //    public override bool Equals(object obj)
    //    {
    //        if (ReferenceEquals(null, obj)) return false;
    //        if (ReferenceEquals(this, obj)) return true;
    //        if (obj.GetType() != GetType()) return false;
    //        return Equals((PythonZssNode)obj);
    //    }

    //    public override ZssNode<PythonNode> GetLeftMostDescendant()
    //    {
    //        var walker = new PostOrderWalker();
    //        walker.Nodes = new List<ZssNode<PythonNode>>();
    //        InternalNode.Walk(walker);
    //        if (walker.Nodes.Count == 0)
    //            throw new Exception("list should not be empty");
    //        return walker.Nodes.First();
    //    }

    //    public override string AbstractType()
    //    {
    //        return InternalNode.InnerNode.NodeName;
    //    }

    //    public override string ToString()
    //    {
    //        return InternalNode.InnerNode.NodeName + "-" + Label;
    //    }

    //    public override int GetHashCode()
    //    {
    //        unchecked
    //        {
    //            return ((Label != null ? Label.GetHashCode() : 0) * 397) ^ (InternalNode != null ? InternalNode.GetHashCode() : 0);
    //        }
    //    }

    //    public override bool Similar(ZssNode<PythonNode> node1)
    //    {
    //        return ToString().Equals(node1.ToString());
    //    }
    //}

    //class PostOrderWalker : PythonWalker
    //{
    //    public List<ZssNode<Node>> Nodes {set; get;}


    //    /// <summary>
    //    /// Wraps an IronPython ZssNode to a ZssNode with a label related to specific properties of each ZssNode
    //    /// </summary>
    //    /// <param name="label"></param>
    //    /// <param name="node"></param>
    //    private void AddNode(string label, Node node)
    //    {
    //        Nodes.Add(new PythonZssNode()
    //        {
    //            Label = label,
    //            InternalNode = node
    //        });
    //    }

    //    public override void PostWalk(BinaryExpression node)
    //    {
    //        AddNode(node.Operator.ToString(), node);
    //        base.PostWalk(node);
    //    }

    //    public override void PostWalk(ConstantExpression node)
    //    {
    //        var label = node.Type.FullName + ": " + node.Value;
    //        AddNode(label, node);
    //        base.PostWalk(node);
    //    }

    //    public override void PostWalk(NameExpression node)
    //    {
    //        var label = node.Name;
    //        AddNode(label, node);
    //        base.PostWalk(node);
    //    }

    //    public override void PostWalk(ExpressionStatement node)
    //    {
    //        var label = node.Expression.Type.FullName;
    //        AddNode(label, node);
    //    }

    //    public override void PostWalk(AndExpression node)
    //    {
    //        var label = node.NodeName;
    //        AddNode(label, node);
    //    }

    //    public override void PostWalk(BackQuoteExpression node)
    //    {
    //        var label = node.NodeName;
    //        AddNode(label, node);
    //    }


    //    public override void PostWalk(CallExpression node)
    //    {
    //        var label = node.TargetNode.NodeName;
    //        AddNode(label, node);
    //    }

    //    public override void PostWalk(ConditionalExpression node)
    //    {
    //        var label = node.NodeName;
    //        AddNode(label, node);
    //    }


    //    public override void PostWalk(IndexExpression node)
    //    {
    //        var label = node.Index.NodeName;
    //        AddNode(label, node);
    //    }

    //    public override void PostWalk(LambdaExpression node)
    //    {
    //        var label = node.Function.Name;
    //        AddNode(label, node);
    //    }

    //    public override void PostWalk(ListComprehension node)
    //    {
    //        var label = node.NodeName;
    //        AddNode(label, node);
    //    }

    //    public override void PostWalk(ListExpression node)
    //    {
    //        var label = node.NodeName;
    //        AddNode(label, node);
    //    }

    //    public override void PostWalk(MemberExpression node)
    //    {
    //        var label = node.TargetNode.Type.FullName + ": " + node.Name;
    //        AddNode(label, node);
    //    }
        
    //    public override void PostWalk(OrExpression node)
    //    {
    //        var label = node.NodeName;
    //        AddNode(label, node);
    //    }

    //    public override void PostWalk(ParenthesisExpression node)
    //    {
    //        var label = node.NodeName;
    //        AddNode(label, node);
    //    }

    //    public override void PostWalk(SetComprehension node)
    //    {
    //        var label = node.NodeName;
    //        AddNode(label, node);
    //    }

    //    public override void PostWalk(SetExpression node)
    //    {
    //        var label = node.NodeName;
    //        AddNode(label, node);
    //    }

    //    public override void PostWalk(SliceExpression node)
    //    {
    //        var label = node.NodeName;
    //        AddNode(label, node);
    //    }

    //    public override void PostWalk(TupleExpression node)
    //    {
    //        var label = node.NodeName;
    //        AddNode(label, node);
    //    }

    //    public override void PostWalk(UnaryExpression node)
    //    {
    //        var label = node.NodeName + "" +  node.Op.ToString();
    //        AddNode(label, node);
    //    }

    //    public override void PostWalk(YieldExpression node)
    //    {
    //        var label = node.NodeName; 
    //        AddNode(label, node);
    //    }

    //    public override void PostWalk(AssertStatement node)
    //    {
    //        var label = node.NodeName;
    //        AddNode(label, node);
    //    }

    //    public override void PostWalk(AssignmentStatement node)
    //    {
    //        var label = node.NodeName;
    //        AddNode(label, node);
    //    }

    //    public override void PostWalk(Arg node)
    //    {
    //        var label = node.NodeName;
    //        AddNode(label, node);
    //    }

    //    public override void PostWalk(AugmentedAssignStatement node)
    //    {
    //        var label = node.NodeName + ": " +  node.Operator.ToString();
    //        AddNode(label, node);
    //    }

    //    public override void PostWalk(BreakStatement node)
    //    {
    //        var label = node.NodeName;
    //        AddNode(label, node);
    //    }

    //    public override void PostWalk(PythonAst node)
    //    {
    //        var label = node.NodeName;
    //        AddNode(label, node);
    //    }

    //    public override void PostWalk(PrintStatement node)
    //    {
    //        var label = node.NodeName;
    //        AddNode(label, node);
    //    }

    //    public override void PostWalk(ClassDefinition node)
    //    {

    //        var label = node.Name;
    //        AddNode(label, node);
    //    }

    //    public override void PostWalk(WhileStatement node)
    //    {
    //        var label = node.NodeName;
    //        AddNode(label, node);
    //        base.PostWalk(node);
    //    }

    //    public override void PostWalk(ComprehensionFor node)
    //    {
    //        var label = node.NodeName;
    //        AddNode(label, node);
    //        base.PostWalk(node);
    //    }

    //    public override void PostWalk(Parameter node)
    //    {
    //        var label = node.Name;
    //        AddNode(label, node);
    //        base.PostWalk(node);
    //    }

    //    public override void PostWalk(IfStatementTest node)
    //    {
    //        var label = node.NodeName;
    //        AddNode(label, node);
    //        base.PostWalk(node);
    //    }

    //    public override void PostWalk(ComprehensionIf node)
    //    {
    //        var label = node.NodeName;
    //        AddNode(label, node);
    //        base.PostWalk(node);
    //    }

    //    public override void PostWalk(ContinueStatement node)
    //    {
    //        var label = node.NodeName;
    //        AddNode(label, node);
    //        base.PostWalk(node);
    //    }

    //    public override void PostWalk(EmptyStatement node)
    //    {
    //        var label = node.NodeName;
    //        AddNode(label, node);
    //        base.PostWalk(node);
    //    }

    //    public override void PostWalk(ForStatement node)
    //    {
    //        var label = node.NodeName;
    //        AddNode(label, node);
    //        base.PostWalk(node);
    //    }

    //    public override void PostWalk(IfStatement node)
    //    {
    //        var label = node.NodeName;
    //        AddNode(label, node);
    //        base.PostWalk(node);
    //    }

    //    public override void PostWalk(ReturnStatement node)
    //    {
    //        var label = node.NodeName;
    //        AddNode(label, node);
    //        base.PostWalk(node);
    //    }

    //    public override void PostWalk(FunctionDefinition node)
    //    {
    //        var label = node.Name;
    //        AddNode(label, node);
    //        base.PostWalk(node);
    //    }
    //}

}