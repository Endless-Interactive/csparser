using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSParser;

public class Generator
{
	public static bool Debug = false;
	private CSExclusions _exclusions = new();
	public List<CSInfo> Namespaces = new();

	public Generator(string path = "")
	{
		if (string.IsNullOrWhiteSpace(path))
			return;

		AddDirectory(path);
	}

	/// <summary>
	///     Excludes any class, method, property or field with the specified access modifier.
	/// </summary>
	/// <param name="modifier"></param>
	public void Exclude(CSAccessModifier modifier)
	{
		_exclusions.Add(modifier);
	}

	/// <summary>
	///     Excludes a namespace or class from being parsed.
	/// </summary>
	/// <param name="type"></param>
	/// <param name="name"></param>
	public void Exclude(CSExclusions.ExcludeType type, string name)
	{
		_exclusions.Add(type, name);
	}

	public void AddDirectory(string path)
	{
		var files = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories);

		foreach (var file in files) AddFile(file);
	}

	public void AddFile(string path)
	{
		var code = File.ReadAllText(path);
		AddCode(code);
	}

	public void AddCode(string code)
	{
		var tree = CSharpSyntaxTree.ParseText(code);
		var root = tree.GetRoot();

		var namespaces = root.DescendantNodes().OfType<NamespaceDeclarationSyntax>().ToList();

		foreach (var nd in namespaces)
		{
			var namespaceName = nd.Name.ToString();

			if (_exclusions.IsNamespaceExcluded(namespaceName))
			{
				Log($"Excluding namespace {namespaceName}");
				continue;
			}

			var existingInfo = Namespaces.FirstOrDefault(x => x.Namespace == namespaceName);

			if (existingInfo != null)
			{
				existingInfo.Classes.AddRange(GetClasses(root));
				continue;
			}

			var csInfo = new CSInfo
			{
				Namespace = namespaceName,
				Classes = GetClasses(root)
			};

			if (csInfo.IsAllExcluded(_exclusions))
			{
				Log($"Excluding namespace {namespaceName} because all classes are excluded");
				continue;
			}

			Namespaces.Add(csInfo);
		}
	}

	private List<CSClass> GetClasses(SyntaxNode root)
	{
		var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();
		var classList = new List<CSClass>();

		foreach (var cd in classes)
		{
			var className = cd.Identifier.ToString();

			var @class = new CSClass
			{
				Name = className,
				Inherits = cd.BaseList?.Types.Select(x => x.Type.ToString()).ToList() ?? new List<string>()
			};

			@class.SetModifiers(cd.Modifiers.ToString());

			if (_exclusions.IsClassExcluded(@class))
			{
				Log($"Excluding class {className}");
				continue;
			}

			@class.XmlDoc = GetXMLDocumentation(cd);
			@class.Methods = GetMethods(cd);

			classList.Add(@class);
		}

		return classList;
	}

	private static void Log(string message)
	{
		if (Debug)
			Console.WriteLine(message);
	}

	private List<CSMethod> GetMethods(SyntaxNode root)
	{
		var methodList = new List<CSMethod>();

		var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>().ToList();

		foreach (var md in methods)
		{
			var method = new CSMethod
			{
				Name = md.Identifier.ToString(),
				ReturnType = md.ReturnType.ToString()
			};

			var parameters = md.ParameterList.Parameters;

			foreach (var ps in parameters)
				method.Parameters.Add(new CSParameter
				{
					Name = ps.Identifier.ToString(),
					Type = ps.Type.ToString(),
					Optional = ps.Default != null,
					DefaultValue = ps.Default?.Value.ToString() ?? ""
				});


			method.XmlDoc = GetXMLDocumentation(md);

			methodList.Add(method);
		}

		return methodList;
	}

	private XMLDoc GetXMLDocumentation(CSharpSyntaxNode md)
	{
		return GetXMLDocumentation(md.GetLeadingTrivia()
			.Where(x => x.Kind() == SyntaxKind.SingleLineDocumentationCommentTrivia ||
			            x.Kind() == SyntaxKind.MultiLineDocumentationCommentTrivia).Select(x => x.ToString()).ToList());
	}

	private XMLDoc GetXMLDocumentation(List<string> comments)
	{
		var xmlDoc = new XMLDoc();

		foreach (var comment in comments)
		{
			var xml = XDocument.Parse($"<root>{comment.Replace("///", "")}</root>");

			var summary = xml.Descendants("summary").FirstOrDefault();
			if (summary != null)
				xmlDoc.Summary = summary.Value.Trim();

			var remarks = xml.Descendants("remarks").FirstOrDefault();
			if (remarks != null)
				xmlDoc.Remarks = remarks.Value.Trim();

			var returns = xml.Descendants("returns").FirstOrDefault();
			if (returns != null)
				xmlDoc.Returns = returns.Value.Trim();

			var param = xml.Descendants("param").ToList();
			foreach (var p in param)
			{
				var name = p.Attribute("name")?.Value;
				var value = p.Value.Trim();

				if (name != null)
					xmlDoc.Parameters.Add(new XMLDoc.XMLDocParam(name, value));
			}
		}

		return xmlDoc;
	}
}