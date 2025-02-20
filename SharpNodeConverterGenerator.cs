using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

using TextCopy;
using Spectre.Console;

namespace SharpNodeConverterGenerator.CLI
{
	public class SharpNodeConverterGenerator
	{
		public static void Main(string[] args)
		{
			while(true){
				if (Process())
					AnsiConsole.WriteLine("Generation complete");
				else
					AnsiConsole.WriteLine("Generation failed");
			}
		}

		private static bool Process(){
			string v1NodePath = AnsiConsole.Prompt(
				new TextPrompt<string>("Enter v1 node definition path:")
				.ValidationErrorMessage("Invalid path")
				.Validate(path => File.Exists(path))
			);

			var v1Node = SharpV1NodePropertyAnalyzer.Instance.Analyze(v1NodePath);
			AnsiConsole.MarkupLine("V1 Node Loaded:");

			string v2NodePath = AnsiConsole.Prompt(
				new TextPrompt<string>("Enter v2 node definition path:")
				.ValidationErrorMessage("Invalid path")
				.Validate(path => File.Exists(path))
			);

			var v2Node = SharpV2NodePropertyAnalyzer.Instance.Analyze(v2NodePath);
			AnsiConsole.MarkupLine("V2 Node Loaded");

			var builder = new SharpNodeConverterBuilder(v1Node.TypeUID)
				.AddDefaultSharpNodeFormat();

			string toType = AnsiConsole.Prompt(
				new TextPrompt<string>("Change node type to:")
					.AddChoice(v2Node.TypeUID)
			);

			if(v1Node.TypeUID != toType)
				builder.AddNodeTypeRemapping(toType);

			AnsiConsole.WriteLine("Remap node property key:");

			List<string> originalKeyList = builder.Check(v1Node).Captures.Select(x => x.Key).ToList();
			List<string> newKeylist = v2Node.Captures.Select(x => x.Key).ToList();

			for (int i = 0; i < v1Node.Captures.Count; i++)
			{
				if (newKeylist.Count == 0)
				{
					break;
				}
				string originalKey = originalKeyList[i];

				string selectedNewKey = AnsiConsole.Prompt(
					new SelectionPrompt<string>()
						.Title($"{originalKey} → ")
						.AddChoices(newKeylist)
				);
				newKeylist.Remove(selectedNewKey);
				if (selectedNewKey == originalKey)
				{
					AnsiConsole.WriteLine($"{originalKey} stay unchanged ");
					continue;
				}

				builder.AddNodePropertyRemapping(originalKey, selectedNewKey);

				AnsiConsole.WriteLine($"{originalKey} → {selectedNewKey}");
			}

			string? converter = builder.Build();

			if (converter == null)
				return false;

			Console.Write(converter);

			Console.WriteLine("Press any key to copy to clipboard");
			Console.ReadKey();

			ClipboardService.SetText(converter);
			return true;
		}
	}
}
