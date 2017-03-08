using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace SenseNet.Services
{
	[DataContract]
    public class EntityReference
    {
		[DataMember]
        public string Uri { get; set; }
    }

	[DataContract]
	public class EntityReference<T> : EntityReference
    {
    }
}