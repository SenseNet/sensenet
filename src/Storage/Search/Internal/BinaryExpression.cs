using System;
using System.Collections.Generic;
using System.Text;
using SenseNet.ContentRepository.Storage.Schema;
using System.Xml;
using System.Globalization;

namespace SenseNet.ContentRepository.Storage.Search.Internal
{
    public class BinaryExpression : Expression
    {
        private Operator _operator;
        private PropertyLiteral _leftValue;
        private Literal _rightValue;


        public Operator Operator
        {
            get { return _operator; }
        }
        public PropertyLiteral LeftValue
        {
            get { return _leftValue; }
        }
        public Literal RightValue
        {
            get { return _rightValue; }
        }


        public BinaryExpression(PropertyType property, Operator op, object value)
        {
            _leftValue = new PropertyLiteral(property);
            _operator = op;
            _rightValue = new Literal(value);
        }
        public BinaryExpression(PropertyType property, Operator op, PropertyType value)
        {
            _leftValue = new PropertyLiteral(property);
            _operator = op;
            _rightValue = new Literal(value);
        }
        public BinaryExpression(PropertyType property, Operator op, NodeAttribute value)
        {
            _leftValue = new PropertyLiteral(property);
            _operator = op;
            _rightValue = new Literal(value);
        }
        public BinaryExpression(NodeAttribute property, Operator op, object value)
        {
            _leftValue = new PropertyLiteral(property);
            _operator = op;
            _rightValue = new Literal(value);
        }
        public BinaryExpression(NodeAttribute property, Operator op, PropertyType value)
        {
            _leftValue = new PropertyLiteral(property);
            _operator = op;
            _rightValue = new Literal(value);
        }
        public BinaryExpression(NodeAttribute property, Operator op, NodeAttribute value)
        {
            _leftValue = new PropertyLiteral(property);
            _operator = op;
            _rightValue = new Literal(value);
        }


        public static Operator GetOperator(StringOperator op)
        {
            switch (op)
            {
                case StringOperator.Equal:
                    return Operator.Equal;
                case StringOperator.NotEqual:
                    return Operator.NotEqual;
                case StringOperator.LessThan:
                    return Operator.LessThan;
                case StringOperator.GreaterThan:
                    return Operator.GreaterThan;
                case StringOperator.LessThanOrEqual:
                    return Operator.LessThanOrEqual;
                case StringOperator.GreaterThanOrEqual:
                    return Operator.GreaterThanOrEqual;
                case StringOperator.StartsWith:
                    return Operator.StartsWith;
                case StringOperator.EndsWith:
                    return Operator.EndsWith;
                case StringOperator.Contains:
                    return Operator.Contains;
                default:
                    throw new SnNotSupportedException(String.Format(CultureInfo.CurrentCulture, SR.Exceptions.Search.Msg_UnknownStringOperator_1, op));
            }
        }
        public static Operator GetOperator(ValueOperator op)
        {
            switch (op)
            {
                case ValueOperator.Equal:
                    return Operator.Equal;
                case ValueOperator.NotEqual:
                    return Operator.NotEqual;
                case ValueOperator.LessThan:
                    return Operator.LessThan;
                case ValueOperator.GreaterThan:
                    return Operator.GreaterThan;
                case ValueOperator.LessThanOrEqual:
                    return Operator.LessThanOrEqual;
                case ValueOperator.GreaterThanOrEqual:
                    return Operator.GreaterThanOrEqual;
                default:
                    throw new SnNotSupportedException(String.Format(CultureInfo.CurrentCulture, SR.Exceptions.Search.Msg_UnknownValueOperator_1, op));
            }
        }
        public static NodeAttribute GetNodeAttribute(StringAttribute attr)
        {
            switch (attr)
            {
                case StringAttribute.Name:
                    return NodeAttribute.Name;
                case StringAttribute.Path:
                    return NodeAttribute.Path;
                case StringAttribute.ETag:
                    return NodeAttribute.ETag;
                case StringAttribute.LockToken:
                    return NodeAttribute.LockToken;
                default:
                    throw new SnNotSupportedException("Unknown StringAttribute");
            }
        }
        public static NodeAttribute GetNodeAttribute(IntAttribute attr)
        {
            switch (attr)
            {
                case IntAttribute.Id:
                    return NodeAttribute.Id;
                case IntAttribute.IsDeleted:
                    return NodeAttribute.IsDeleted;
                case IntAttribute.IsInherited:
                    return NodeAttribute.IsInherited;
                case IntAttribute.Index:
                    return NodeAttribute.Index;
                case IntAttribute.Locked:
                    return NodeAttribute.Locked;
                case IntAttribute.LockType:
                    return NodeAttribute.LockType;
                case IntAttribute.LockTimeout:
                    return NodeAttribute.LockTimeout;
                case IntAttribute.MajorVersion:
                    return NodeAttribute.MajorVersion;
                case IntAttribute.MinorVersion:
                    return NodeAttribute.MinorVersion;
                case IntAttribute.FullTextRank:
                    return NodeAttribute.FullTextRank;
                case IntAttribute.ParentId:
                    return NodeAttribute.ParentId;
                case IntAttribute.LockedById:
                    return NodeAttribute.LockedById;
                case IntAttribute.CreatedById:
                    return NodeAttribute.CreatedById;
                case IntAttribute.ModifiedById:
                    return NodeAttribute.ModifiedById;
                case IntAttribute.IsSystem:
                    return NodeAttribute.IsSystem;
                case IntAttribute.OwnerId:
                    return NodeAttribute.OwnerId;
                case IntAttribute.SavingState:
                    return NodeAttribute.SavingState;
                default:
                    throw new SnNotSupportedException("Unknown IntAttribute");
            }
        }
        public static NodeAttribute GetNodeAttribute(DateTimeAttribute attr)
        {
            switch (attr)
            {
                case DateTimeAttribute.LockDate:
                    return NodeAttribute.LockDate;
                case DateTimeAttribute.LastLockUpdate:
                    return NodeAttribute.LastLockUpdate;
                case DateTimeAttribute.CreationDate:
                    return NodeAttribute.CreationDate;
                case DateTimeAttribute.ModificationDate:
                    return NodeAttribute.ModificationDate;
                default:
                    throw new SnNotSupportedException("Unknown DateTimeAttribute");
            }
        }
        public static NodeAttribute GetNodeAttribute(ReferenceAttribute attr)
        {
            switch (attr)
            {
                case ReferenceAttribute.Parent:
                    return NodeAttribute.Parent;
                case ReferenceAttribute.LockedBy:
                    return NodeAttribute.LockedBy;
                case ReferenceAttribute.CreatedBy:
                    return NodeAttribute.CreatedBy;
                case ReferenceAttribute.ModifiedBy:
                    return NodeAttribute.ModifiedBy;
                default:
                    throw new SnNotSupportedException("Unknown ReferenceAttribute");
            }
        }

        internal override void WriteXml(System.Xml.XmlWriter writer)
        {
            throw new SnNotSupportedException();
        }

    }
}