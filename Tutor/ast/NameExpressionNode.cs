using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronPython.Compiler.Ast;

namespace Tutor.ast
{
    class NameExpressionNode : LeafNode
    {
        public NameExpressionNode(Node innerNode) : base(innerNode)
        {
        }

        protected override bool IsEqualToInnerNode(Node node)
        {
            var inner = InnerNode as NameExpression;
            var comparedNode = node as NameExpression;
            if (comparedNode == null) return false;
            return inner.Name.Equals(comparedNode.Name);
        }

        public override PythonNode Clone()
        {
            var pythonNode = new NameExpressionNode(InnerNode);
            pythonNode.Id = Id;
            if (Value != null) pythonNode.Value = Value;
            return pythonNode;
        }
    }
}
