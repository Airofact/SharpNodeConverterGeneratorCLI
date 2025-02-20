using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpNodeConverterGenerator.SharpNodeConverter;

namespace SharpNodeConverterGenerator
{
	public class SharpNodeConverterBuilder
	{
		private readonly SharpNodeConverterModel converter;
		public SharpNodeConverterBuilder(string typeUID)
		{
			if (string.IsNullOrEmpty(typeUID))
			{
				throw new ArgumentNullException(nameof(typeUID));
			}
			converter = new()
			{
				SharpName = typeUID
			};
		}

		public SharpNodeConverterBuilder AddNodeTypeRemapping(string? toType)
		{
			if (!string.IsNullOrEmpty(toType))
			{
				converter.Converters.Add(new NodeTypeRemappingConverter()
				{
					To = toType
				}); 
			}
			return this;
		}

		public SharpNodeConverterBuilder AddNodePropertyRemapping(string originalKey, string newKey)
		{
			if(!string.IsNullOrEmpty(originalKey) && !string.IsNullOrEmpty(newKey))
			{
				converter.Converters.Add(new NodePropertyRemappingConverter()
				{
					OriginalKey = originalKey,
					NewKey = newKey
				});
			}
			return this;
		}

		public SharpNodeConverterBuilder AddFormatProperty()
		{
			converter.Converters.Add(new FormatPropertyConverter());
			return this;
		}

		public SharpNodeConverterBuilder AddStripNamespaceInType()
		{
			converter.Converters.Add(new StripNamespaceInTypeConverter());
			return this;
		}

		public SharpNodeConverterBuilder AddDefaultSharpNodeFormat()
		{
			converter.Converters.Add(new DefaultSharpNodeFormatConverter());
			return this;
		}

		public string? Build()
		{
			return JsonConvert.SerializeObject(converter, Formatting.Indented);
		}

		public NodeModel Check(NodeModel node){
			return converter.Convert(node);
		}

		public SharpNodeConverterBuilder Info(string info)
		{
			Console.WriteLine(info);
			return this;
		}
	}
}
