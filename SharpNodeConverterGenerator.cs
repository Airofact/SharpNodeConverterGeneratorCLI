using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

using TextCopy;
using Spectre.Console;
using Spectre.Console.Json;

namespace SharpNodeConverterGenerator.CLI
{
	public class SharpNodeConverterGenerator
	{
		public static void Main(string[] args)
		{
			//string v1RootPath = AnsiConsole.Prompt(
			//	new TextPrompt<string>("Enter v1 root directory path:")
			//	.ValidationErrorMessage("Invalid path")
			//	.Validate(path => Directory.Exists(path))
			//);

			//string v2RootPath = AnsiConsole.Prompt(
			//	new TextPrompt<string>("Enter v2 root directory path:")
			//	.ValidationErrorMessage("Invalid path")
			//	.Validate(path => Directory.Exists(path))
			//);

			string testPath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "../../../test");

			ProcessPackagePair(Path.Combine(testPath, "V1Nodes"), Path.Combine(testPath, "V2Nodes"));
		}

		private static bool ProcessPackagePair(string v1RootPath, string v2RootPath)
		{
			var v1Files = Directory.GetFiles(v1RootPath, "*.cs", SearchOption.AllDirectories).ToList();
			var v1AncestorFiles = Directory.GetFiles(v1RootPath, "*.cs", SearchOption.TopDirectoryOnly).ToList();
			v1Files = v1Files.Except(v1AncestorFiles).ToList();

			var v2Files = Directory.GetFiles(v2RootPath, "*.cgen", SearchOption.AllDirectories).ToList();

			if (v1Files.Count == 0 || v2Files.Count == 0)
			{
				AnsiConsole.MarkupLine("[red]No JSON files found in one or both directories.[/]");
				return false;
			}

			while (v1Files.Count > 0 && v2Files.Count > 0)
			{
				var v1FileChoices = v1Files.Select(
					path => Path.GetRelativePath(v1RootPath, path)
				).ToList();
				string v1FilePath = Path.Combine(v1RootPath,AnsiConsole.Prompt(
					new SelectionPrompt<string>()
						.Title("Select a v1 file:")
						.AddChoices(v1FileChoices)
				));

				var v2FileChoices = v2Files.Select(
					path => Path.GetRelativePath(v2RootPath, path)
				).ToList();
				string v2FilePath = Path.Combine(v2RootPath,AnsiConsole.Prompt(
					new SelectionPrompt<string>()
						.Title("Select a v2 file:")
						.AddChoices(v2FileChoices)
				));

				if (!ProcessFilePair(v1FilePath, v2FilePath))
				{
					AnsiConsole.MarkupLine($"[red]Failed to process file pair: {v1FilePath} and {v2FilePath}[/]");
					return false;
				}

				v1Files.Remove(v1FilePath);
				v2Files.Remove(v2FilePath);

				if (v1Files.Count == 0 || v2Files.Count == 0)
				{
					break;
				}

				bool continueProcessing = AnsiConsole.Confirm("Do you want to process another file pair?");
				if (!continueProcessing)
				{
					break;
				}
			}

			return true;
		}

		private static bool ProcessFilePair(string v1NodePath, string v2NodePath)
		{
			var v1Node = SharpV1NodePropertyAnalyzer.Instance.Analyze(v1NodePath);
			AnsiConsole.MarkupLine("V1 Node Loaded");
			AnsiConsole.Write(
				new JsonText(JsonConvert.SerializeObject(v1Node))
			);
			AnsiConsole.WriteLine();

			var v2Node = SharpV2NodePropertyAnalyzer.Instance.Analyze(v2NodePath);
			AnsiConsole.MarkupLine("V2 Node Loaded");
			AnsiConsole.Write(
				new JsonText(JsonConvert.SerializeObject(v2Node))
			);
			AnsiConsole.WriteLine();

			var builder = new SharpNodeConverterBuilder(v1Node.TypeUID)
				.AddDefaultSharpNodeFormat();

			string toType = AnsiConsole.Prompt(
				new TextPrompt<string>("Change node type to:")
					.AddChoice(v2Node.TypeUID)
			);

			if (v1Node.TypeUID != toType)
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
