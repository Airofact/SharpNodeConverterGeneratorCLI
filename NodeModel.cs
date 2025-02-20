using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace SharpNodeConverterGenerator
{
	[Serializable]
	public class NodeModel
	{
		[JsonProperty] public string TypeUID = "Unknown Node";
		[JsonProperty] public List<NodeProperty> Captures = [];
	}
}
