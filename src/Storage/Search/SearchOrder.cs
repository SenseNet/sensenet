using System;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Search.Internal;

namespace SenseNet.ContentRepository.Storage.Search
{
    public class SearchOrder
    {
        private OrderDirection _direction;
        private PropertyLiteral _propertyToOrder;

        public string PropertyName { get { return PropertyToOrder.Name; } }
        public OrderDirection Direction
        {
            get { return _direction; }
        }
        public PropertyLiteral PropertyToOrder
        {
            get { return _propertyToOrder; }
        }

        public SearchOrder(PropertyType propertyToOrder)
        {
            if (propertyToOrder == null)
                throw new ArgumentNullException("propertyToOrder");
            _propertyToOrder = new PropertyLiteral(propertyToOrder);
            _direction = OrderDirection.Asc;
        }
        public SearchOrder(PropertyType propertyToOrder, OrderDirection direction)
        {
            if (propertyToOrder == null)
                throw new ArgumentNullException("propertyToOrder");
            _propertyToOrder = new PropertyLiteral(propertyToOrder);
            _direction = direction;
        }

        public SearchOrder(StringAttribute propertyToOrder) : this(propertyToOrder, OrderDirection.Asc) { }
        public SearchOrder(IntAttribute propertyToOrder) : this(propertyToOrder, OrderDirection.Asc) { }
        public SearchOrder(DateTimeAttribute propertyToOrder) : this(propertyToOrder, OrderDirection.Asc) { }
        public SearchOrder(ReferenceAttribute propertyToOrder) : this(propertyToOrder, OrderDirection.Asc) { }

        public SearchOrder(StringAttribute propertyToOrder, OrderDirection direction) : this((NodeAttribute)propertyToOrder, direction) { }
        public SearchOrder(IntAttribute propertyToOrder, OrderDirection direction) : this((NodeAttribute)propertyToOrder, direction) { }
        public SearchOrder(DateTimeAttribute propertyToOrder, OrderDirection direction) : this((NodeAttribute)propertyToOrder, direction) { }
        public SearchOrder(ReferenceAttribute propertyToOrder, OrderDirection direction) : this((NodeAttribute)propertyToOrder, direction) { }

        internal SearchOrder(NodeAttribute propertyToOrder, OrderDirection direction)
        {
            _propertyToOrder = new PropertyLiteral(propertyToOrder);
            _direction = direction;
        }
    }
}