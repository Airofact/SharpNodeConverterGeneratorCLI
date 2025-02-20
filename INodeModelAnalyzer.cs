using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpNodeConverterGenerator
{
	public interface INodeModelAnalyzer
	{
		public NodeModel Analyze(string path);
	}
}
