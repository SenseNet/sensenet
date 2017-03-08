using System;
using System.Collections.Generic;
using System.Text;

namespace SenseNet.ContentRepository.Storage.Search.Internal
{
    public class ExpressionEnumerator : IEnumerator<Expression>
    {
        private Expression _root;
        private List<Expression> _expressionList;
        private IEnumerator<Expression> _innerEnumerator;

        public ExpressionEnumerator(NodeQuery query)
        {
            _root = query;
            _expressionList = new List<Expression>();
            AddExpressionToInnerList(query);
            _innerEnumerator = _expressionList.GetEnumerator();
        }
        private void AddExpressionToInnerList(Expression expression)
        {
            ExpressionList expressionList;
            NotExpression notExpression;
            ReferenceExpression referenceExpression;
            SearchExpression searchExpression;
            TypeExpression typeExpression;
            IBinaryExpressionWrapper binaryExpression;

            if ((expressionList = expression as ExpressionList) != null)
                AddExpressionToInnerList(expressionList);
            if ((notExpression = expression as NotExpression) != null)
                AddExpressionToInnerList(notExpression);
            if ((referenceExpression = expression as ReferenceExpression) != null)
                AddExpressionToInnerList(referenceExpression);
            if ((searchExpression = expression as SearchExpression) != null)
                AddExpressionToInnerList(searchExpression);
            if ((typeExpression = expression as TypeExpression) != null)
                AddExpressionToInnerList(typeExpression);
            if ((binaryExpression = expression as IBinaryExpressionWrapper) != null)
                AddExpressionToInnerList(binaryExpression);
        }
        private void AddExpressionToInnerList(ExpressionList expression)
        {
            _expressionList.Add(expression);
            foreach (Expression exp in expression.Expressions)
                AddExpressionToInnerList(exp);
        }
        private void AddExpressionToInnerList(NotExpression expression)
        {
            _expressionList.Add(expression);
            if (expression.Expression != null)
                AddExpressionToInnerList(expression.Expression);
        }
        private void AddExpressionToInnerList(ReferenceExpression expression)
        {
            _expressionList.Add(expression);
            if (expression.Expression != null)
                AddExpressionToInnerList(expression.Expression);
        }
        private void AddExpressionToInnerList(SearchExpression expression)
        {
            _expressionList.Add(expression);
        }
        private void AddExpressionToInnerList(TypeExpression expression)
        {
            _expressionList.Add(expression);
        }
        private void AddExpressionToInnerList(IBinaryExpressionWrapper expression)
        {
            _expressionList.Add(expression.BinExp);
        }

        public Expression Current
        {
            get { return _innerEnumerator.Current; }
        }

        public void Dispose()
        {
            if (_root != null)
                _root = null;
        }

        object System.Collections.IEnumerator.Current
        {
            get { return _innerEnumerator.Current; }
        }
        public bool MoveNext()
        {
            return _innerEnumerator.MoveNext();
        }
        public void Reset()
        {
            _innerEnumerator.Reset();
        }

    }
}