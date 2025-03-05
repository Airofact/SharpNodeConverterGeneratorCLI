using Newtonsoft.Json;
using SharpNodeConverterGenerator.SharpNodeConverter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpNodeConverterGenerator.SharpNodeConverter
{
	[Serializable]
	public class SharpNodeConverterModel : AtomicSharpNodeConverter
	{
		[JsonProperty] public string SharpName = "Unknown Node";
		[JsonProperty] public List<AtomicSharpNodeConverter> Converters = [];

		[JsonIgnore]
		public override string TypeShortName { get => "Composite"; }

		public override NodeModel Convert(NodeModel node)
		{
			var newNode = node;
			foreach (var converter in Converters)
			{
				newNode = converter.Convert(newNode);
			}
			return newNode;
		}
	}
}
