using System;
using System.Collections.Generic;
using System.Text;
using SenseNet.ContentRepository.Storage.Schema;

namespace  SenseNet.ContentRepository.Schema
{
	internal class NodeTypeRegistration
	{
		public Type Type { get; set; }
		public string Name { get { return this.Type.FullName; } }
		public string ParentName { get; private set; }
		public List<PropertyTypeRegistration> PropertyTypeRegistrations { get; private set; }

		public NodeTypeRegistration(Type type, string parentTypeName, List<PropertyTypeRegistration> ptRegs)
		{
			this.Type = type;
			this.ParentName = parentTypeName;
			this.PropertyTypeRegistrations = ptRegs;

			foreach (PropertyTypeRegistration propReg in this.PropertyTypeRegistrations)
				propReg.Parent = this;
		}

		public PropertyTypeRegistration PropertyTypeRegistrationByName(string name)
		{
			foreach (PropertyTypeRegistration propReg in this.PropertyTypeRegistrations)
			{
				string pName = propReg.Name;
				if (pName == name)
					return propReg;
				if (pName.EndsWith(name) && pName[pName.Length - name.Length - 1] == '.')
					return propReg;
			}
			return null;
		}

		public override string ToString()
		{
			return String.Concat("NodeTypeRegistration: '", this.Name, "'");
		}
	}
}