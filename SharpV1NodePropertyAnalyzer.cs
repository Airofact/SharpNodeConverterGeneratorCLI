using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Spectre.Console;

namespace SharpNodeConverterGenerator
{
	public class SharpV1NodePropertyAnalyzer : INodeModelAnalyzer
	{
		private static readonly SharpV1NodePropertyAnalyzer _instance = new();

		private SharpV1NodePropertyAnalyzer() { }

		public static SharpV1NodePropertyAnalyzer Instance => _instance;

		public NodeModel Analyze(string path)
		{
			string[] pathSlice = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			string category = pathSlice[^2];
			string fileName = Path.GetFileNameWithoutExtension(path);
			NodeModel model = new();
			StreamReader sr = new(path);
			string code = sr.ReadToEnd();
			SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
			CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
			var FirstClass = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
			if (FirstClass != null)
			{
				string identifier = FirstClass.Identifier.ToString();
				if (identifier != fileName)
				{
					AnsiConsole.MarkupLine("[orange]File name does not match class name[/]");
				}
				model.TypeUID = $".{category}.{fileName}, ";
				var properties = FirstClass.Members.OfType<PropertyDeclarationSyntax>().ToList();
				foreach (var property in properties)
				{
					string? name = AnalyzeProperty(property);
					NodeProperty item;
					if (name == null)
					{
						item = new() { Key = "Property Analyze Failed" };
					}else
					{
						item = new() { Key = name };
					}
					model.Captures.Add(item);
				}
			}
			return model;
		}

		public static string? AnalyzeProperty(PropertyDeclarationSyntax property)
		{
			if (property.AccessorList == null)
				return null;
			var accessor = property.AccessorList.Accessors.FirstOrDefault();
			if (accessor == null)
				return null;
			if (accessor.ExpressionBody == null)
				return null;
			var expression = accessor.ExpressionBody.Expression;
			if (expression == null)
				return null;
			if(expression is MemberAccessExpressionSyntax memberAccess){
				var target = memberAccess.Expression;
				if (target is InvocationExpressionSyntax invocation)
				{
					var arguments = invocation.ArgumentList.Arguments;
					for (int i = 0; i < arguments.Count; i++)
					{
						var argument = arguments[i];
						if (
							(argument.Expression is LiteralExpressionSyntax literal) &&
							(argument.NameColon?.Name.Identifier.Text == "name")
						)
						{
							return literal.Token.ValueText;
						}
					}
					if (arguments.Count >= 3)
					{
						var argument = arguments[2];
						if (argument.Expression is LiteralExpressionSyntax literal)
						{
							return literal.Token.ValueText;
						}
					}
				}
			}
			return property.Identifier.ToString();
		}
	}
}
