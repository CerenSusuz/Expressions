using System.Collections.Generic;
using System.Linq.Expressions;

namespace ExpressionTrees.Task1.ExpressionsTransformer
{
    public class IncDecExpressionVisitor : ExpressionVisitor
    {
        private readonly Dictionary<string, object> _constants;

        public IncDecExpressionVisitor(Dictionary<string, object> constants)
        {
            _constants = constants;
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (node.NodeType == ExpressionType.Add &&
                node.Right is ConstantExpression constAdd &&
                constAdd.Value?.ToString() == "1")
            {
                return Expression.Increment(Visit(node.Left));
            }

            if (node.NodeType == ExpressionType.Subtract &&
                node.Right is ConstantExpression constSub &&
                constSub.Value?.ToString() == "1")
            {
                return Expression.Decrement(Visit(node.Left));
            }

            return base.VisitBinary(node);
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (_constants.TryGetValue(node.Name, out var value))
            {
                return Expression.Constant(value, node.Type);
            }

            return base.VisitParameter(node);
        }
    }
}
