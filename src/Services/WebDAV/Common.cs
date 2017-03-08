using System.Xml;

namespace SenseNet.Services.WebDav
{
	internal class Common
	{
		internal static Depth GetDepth(string depth)
		{
		
			switch(depth)
			{
				case "0":
				{
					return Depth.Current;
				}
				case "1":
				{
					return Depth.Children;	
				}
				case "infinity":
				{
					return Depth.Infinity;	
				}
				default: 
				{
					return Depth.Current;
				}
			}
		}

		internal static XmlTextWriter GetXmlWriter()
		{
			var ms = new System.IO.MemoryStream();
			var writer = new XmlTextWriter(ms, System.Text.Encoding.UTF8);

			writer.WriteStartDocument();
			return writer;
		}
	}
}
