using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpNodeConverterGenerator
{
	public class NodeProperty
	{
		public required string Key { get; set; } = string.Empty;

		public NodeProperty(){

		}

		public NodeProperty(string Key)
		{
			this.Key = Key;
		}

	}
}
