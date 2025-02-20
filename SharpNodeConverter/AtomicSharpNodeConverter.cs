using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Humanizer;
using System.Text.Json.Serialization;

namespace SharpNodeConverterGenerator.SharpNodeConverter
{
	public abstract class AtomicSharpNodeConverter
	{
		[JsonProperty("$type",Order = -100)]
		public abstract string TypeShortName { get; }
		public abstract NodeModel Convert(NodeModel node);
	}

	[Serializable]
	public class NodeTypeRemappingConverter : AtomicSharpNodeConverter
	{
		[JsonProperty] public required string To;

		public override string TypeShortName { get => "NodeTypeRemapping"; }

		public override NodeModel Convert(NodeModel node)
		{
			node.TypeUID = To;
			return node;
		}
	}

	[Serializable]
	public class NodePropertyRemappingConverter : AtomicSharpNodeConverter
	{
		[JsonProperty] public required string OriginalKey;
		[JsonProperty] public required string NewKey;

		public override string TypeShortName { get => "NodePropertyRemapping"; }

		public override NodeModel Convert(NodeModel node)
		{
			foreach (var prop in node.Captures)
			{
				if (prop.Key == OriginalKey)
				{
					prop.Key = NewKey;
				}
			}
			return node;
		}
	}

	[Serializable]
	public class FormatPropertyConverter : AtomicSharpNodeConverter
	{
		public override string TypeShortName { get => "FormatProperty"; }

		public override NodeModel Convert(NodeModel node)
		{
			foreach (var prop in node.Captures)
			{
				prop.Key = prop.Key
					.ToLower()
					.Replace("(", " ")
					.Replace(")", "")
					.Replace("'", "")
					.Replace(",", " ")
					.Underscore();
			}
			return node;
		}
	}

	[Serializable]
	public class StripNamespaceInTypeConverter : AtomicSharpNodeConverter
	{
		public override string TypeShortName { get => "StripNamespaceInType"; }

		public override NodeModel Convert(NodeModel node)
		{
			node.TypeUID = node.TypeUID.Split(',')[0].Split('.')[^1];
			return node;
		}
	}

	[Serializable]
	public class DefaultSharpNodeFormatConverter : AtomicSharpNodeConverter{
		private readonly FormatPropertyConverter formatPropertyConverter = new();
		private readonly StripNamespaceInTypeConverter stripNamespaceInTypeConverter = new();

		public override string TypeShortName { get => "Default"; }

		public override NodeModel Convert(NodeModel node){
			node = formatPropertyConverter.Convert(node);
			node = stripNamespaceInTypeConverter.Convert(node);
			return node;
		}
	}
}
