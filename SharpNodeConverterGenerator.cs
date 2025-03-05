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
using System.Security.Cryptography;

namespace SharpNodeConverterGenerator.CLI
{
	public class SharpNodeConverterGenerator
	{
		public static void Main()
		{
			string testPath = Path.Combine(Directory.GetCurrentDirectory(), "../../../test");

			bool processFinished = ProcessPackagePair(
				Path.Combine(testPath, "V1Nodes"),
				Path.Combine(testPath, "V2Nodes"),
				Path.Combine(testPath, "Converters")
			);
			if (processFinished)
			{
				AnsiConsole.MarkupLine("[green]All files have been processed successfully.[/]");
			}
			else
			{
				AnsiConsole.MarkupLine("[red]Unknown error occurred in the process.[/]");
			}
		}

		private static bool ProcessPackagePair(string v1RootPath, string v2RootPath, string? converterPath)
		{
			var v1Directories = Directory.GetDirectories(v1RootPath);
			var v2Directories = Directory.GetDirectories(v2RootPath);

			var v1Files = new Dictionary<string, List<string>>();
			var v2Files = new Dictionary<string, List<string>>();

			foreach (var dir in v1Directories)
			{
				string category = dir.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Last();
				v1Files[category] = Directory.GetFiles(dir, "*.cs").ToList();
			}

			foreach (var dir in v2Directories)
			{
				string category = dir.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Last();
				v2Files[category] = Directory.GetFiles(dir, "*.cgen").ToList();
			}

			if (v1Files.Count == 0 || v2Files.Count == 0)
			{
				AnsiConsole.MarkupLine("[red]No .cs or .cgen files found in the second-level directories.[/]");
				return false;
			}

			int v1RestFileCount = v1Files.Select(x => x.Value.Count).Sum();
			int v2RestFileCount = v2Files.Select(x => x.Value.Count).Sum();

			while (v1RestFileCount > 0 && v2RestFileCount > 0)
			{
				string category = AnsiConsole.Prompt(
					new SelectionPrompt<string>().Title("select a category")
						.AddChoices(v1Files.Select(p => p.Key))
				);

				var v1FileSelectionPrompt = new SelectionPrompt<string>()
					.Title($"Select a v1 file:")
					.AddChoices(v1Files[category].Select(p => Path.GetFileName(p)));

				string v1FilePath = Path.Combine(v1RootPath, category, AnsiConsole.Prompt(v1FileSelectionPrompt));

				if(!v2Files.ToList().Exists(p => p.Key == category))
				{
					AnsiConsole.MarkupLine("[red]provided version of v2 nodelib doesnt contain selected category[/]");
					return false;
				}

				var v2FileSelectionPrompt = new SelectionPrompt<string>()
					.Title($"Select a v2 file:")
					.AddChoices(v2Files[category].Select(p => Path.GetFileName(p)));

				string v2FilePath = Path.Combine(v2RootPath, category, AnsiConsole.Prompt(v2FileSelectionPrompt));

				string? outputPath = null;
				
				if(converterPath != null)
				{
					outputPath = Path.Combine(converterPath, category);
				}

				if (!ProcessFilePair(v1FilePath, v2FilePath, outputPath))
				{
					AnsiConsole.MarkupLine($"[red]Failed to process file pair: {v1FilePath} and {v2FilePath}[/]");
					return false;
				}

				if (v1Files.Any(x => x.Value.Remove(v1FilePath)))
				{
					v1RestFileCount--;
				}else
				{
					AnsiConsole.MarkupLine($"[orange1]Failed to remove file {v1FilePath} from pending list");
				}

				if (v2Files.Any(x => x.Value.Remove(v2FilePath)))
				{
					v2RestFileCount--;
				}else
				{
					AnsiConsole.MarkupLine($"[orange]Failed to remove file {v2FilePath} from pending list");
				}

				bool continueProcessing = AnsiConsole.Confirm("Do you want to process another file pair?");
				if (!continueProcessing)
				{
					break;
				}
			}

			return true;
		}

		private static bool ProcessFilePair(string v1NodePath, string v2NodePath, string? outputPath)
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

			var builder = new SharpNodeConverterBuilder(v1Node.TypeUID);

			bool useDefault = AnsiConsole.Confirm("use default converter?");
			if (useDefault)
				builder.AddDefaultSharpNodeFormat();

			string fromType = v1Node.TypeUID;
			string toType = AnsiConsole.Prompt(
				new TextPrompt<string>($"Change node type from [blue][[{fromType}]][/] to")
					.AddChoice(v2Node.TypeUID)
			);

			if (fromType != toType)
			{
				builder.AddNodeTypeRemapping(toType);
				AnsiConsole.MarkupLine($"[aqua]{fromType} → {toType}[/]");
			}
			else
			{
				AnsiConsole.MarkupLine($"[aqua]type {fromType} stay unchanged[/]");
			}

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
					AnsiConsole.MarkupLine($"[aqua]{originalKey} stay unchanged[/]");
				}
				else
				{
					builder.AddNodePropertyRemapping(originalKey, selectedNewKey);

					AnsiConsole.MarkupLine($"[aqua]{originalKey} → {selectedNewKey}[/]");
				}
			}

			string? converter = builder.Build();

			if (converter == null)
				return false;

			Console.WriteLine(converter);

			if(outputPath != null)
			{
				if(!Path.Exists(outputPath))
				{
					Directory.CreateDirectory(outputPath);
				}
				File.WriteAllText(Path.Combine(outputPath, $"{fromType}.sharpconv"), converter);
				AnsiConsole.MarkupLine("[green]generated converter has been written to target path.[/]");
			}
			else
			{
				Console.WriteLine("Press any key to copy to clipboard");
				Console.ReadKey();

				ClipboardService.SetText(converter);
				AnsiConsole.MarkupLine("[green]generated converter has been written to clipboard.[/]");
			}

			return true;
		}
	}
}
