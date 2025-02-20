using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace SharpNodeConverterGenerator
{
	public class SharpV2NodePropertyAnalyzer : INodeModelAnalyzer
	{
		private static readonly SharpV2NodePropertyAnalyzer _instance = new();
		public static SharpV2NodePropertyAnalyzer Instance => _instance;
		private SharpV2NodePropertyAnalyzer() { }

		public NodeModel Analyze(string path)
		{
			StreamReader sr = new(path);
			string json = sr.ReadToEnd();
			var v2node = JsonConvert.DeserializeObject<NodeModel>(json) ?? throw new Exception("Invalid JSON");
			return v2node;
		}
	}
}