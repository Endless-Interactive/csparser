using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSParser;

public partial class Generator
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

		// This is a hack to find a file scope namespace since roslyn doesn't seem to support it.
		if (namespaces.Count == 0)
		{
			var match = NamespaceRegex().Match(code);

			if (match.Success)
			{
				var fileScopeNamespace = match.Value.Replace("namespace ", "").Replace(";", "");

				namespaces.Add(SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(fileScopeNamespace)));
			}
		}

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
			@class.Properties = GetProperties(cd);
			@class.Fields = GetFields(cd);

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
			var modifiers = md.Modifiers.ToString();

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

			method.SetModifiers(modifiers);

			if (_exclusions.IsMethodExcluded(method))
			{
				Log($"Excluding method {method.Name}");
				continue;
			}

			method.XmlDoc = GetXMLDocumentation(md);

			methodList.Add(method);
		}

		return methodList;
	}

	private List<CSProperty> GetProperties(SyntaxNode root)
	{
		var propertyList = new List<CSProperty>();

		var properties = root.DescendantNodes().OfType<PropertyDeclarationSyntax>().ToList();

		foreach (var pd in properties)
		{
			var modifiers = pd.Modifiers.ToString();

			var property = new CSProperty
			{
				Name = pd.Identifier.ToString(),
				Type = pd.Type.ToString(),
				DefaultValue = pd.Initializer?.Value.ToString().Trim('"') ?? ""
			};

			property.SetModifiers(modifiers);

			if (_exclusions.IsPropertyExcluded(property))
			{
				Log($"Excluding property {property.Name}");
				continue;
			}

			property.XmlDoc = GetXMLDocumentation(pd);

			propertyList.Add(property);
		}

		return propertyList;
	}

	private List<CSField> GetFields(SyntaxNode root)
	{
		var fieldList = new List<CSField>();

		var fields = root.DescendantNodes().OfType<FieldDeclarationSyntax>().ToList();

		foreach (var fd in fields)
		{
			var modifiers = fd.Modifiers.ToString();


			var field = new CSField
			{
				Name = fd.Declaration.Variables.First().Identifier.ToString(),
				Type = fd.Declaration.Type.ToString(),
				DefaultValue = fd.Declaration.Variables.First().Initializer?.Value.ToString().Trim('"') ?? ""
			};

			field.SetModifiers(modifiers);

			if (_exclusions.IsFieldExcluded(field))
			{
				Log($"Excluding field {field.Name}");
				continue;
			}

			field.XmlDoc = GetXMLDocumentation(fd);

			fieldList.Add(field);
		}

		return fieldList;
	}

	private XMLDoc GetXMLDocumentation(CSharpSyntaxNode md)
	{
		return GetXMLDocumentation(md.GetLeadingTrivia()
			.Where(x => x.Kind() == SyntaxKind.SingleLineDocumentationCommentTrivia ||
			            x.Kind() == SyntaxKind.MultiLineDocumentationCommentTrivia).Select(x => x.ToString()).ToList());
	}

	private string RemoveXMLSyntax(string name, string xml)
	{
		var regex = new Regex(@"^<" + name + ">(\\r|\\n|\\t){0,3}");

		return regex.Replace(xml, "").Replace($"</{name}>", "").Trim();
	}

	// TODO: Missing support for TypeParameters, TypeParameterReference, ParamReferences, InheritDoc, and Include
	private XMLDoc GetXMLDocumentation(List<string> comments)
	{
		var xmlDoc = new XMLDoc();

		foreach (var comment in comments)
		{
			var xml = XDocument.Parse($"<root>{comment.Replace("///", "")}</root>");

			var summary = xml.Descendants("summary").FirstOrDefault();
			if (summary != null)
				xmlDoc.Summary = RemoveXMLSyntax(summary.Name.ToString(), summary.ToString());

			var remarks = xml.Descendants("remarks").FirstOrDefault();
			if (remarks != null)
				xmlDoc.Remarks = RemoveXMLSyntax(remarks.Name.ToString(), remarks.ToString());

			var returns = xml.Descendants("returns").FirstOrDefault();
			if (returns != null)
				xmlDoc.Returns = RemoveXMLSyntax(returns.Name.ToString(), returns.ToString());

			var param = xml.Descendants("param").ToList();
			foreach (var p in param)
			{
				var name = p.Attribute("name")?.Value;
				var description = p.Value.Trim();

				if (name != null)
					xmlDoc.Parameters.Add(new XMLDoc.XMLDocParam(name, description));
			}

			var paramrefs = xml.Descendants("paramref").ToList();
			foreach (var p in paramrefs)
			{
				var name = p.Attribute("name")?.Value;
				var description = p.Value.Trim();

				if (name != null)
					xmlDoc.ParamRefs.Add(new XMLDoc.XMLParamRef(name, description));
			}

			var exception = xml.Descendants("exception").ToList();
			foreach (var e in exception)
			{
				var cref = e.Attribute("cref")?.Value ?? "";
				var description = RemoveXMLSyntax(e.Name.ToString(), e.ToString());

				xmlDoc.Exceptions.Add(new XMLDoc.XMLException(cref, description));
			}

			var value = xml.Descendants("value").FirstOrDefault();
			if (value != null)
				xmlDoc.Value = value.Value.Trim();

			var see = xml.Descendants("see").ToList();
			foreach (var s in see)
			{
				var cref = s.Attribute("cref")?.Value ?? "";
				var href = s.Attribute("href")?.Value ?? "";
				var langword = s.Attribute("langword")?.Value ?? "";
				var description = s.Value.Trim();

				xmlDoc.See.Add(new XMLDoc.XMLSee(cref, href, description, langword));
			}

			var seealso = xml.Descendants("seealso").ToList();
			foreach (var s in seealso)
			{
				var cref = s.Attribute("cref")?.Value ?? "";
				var href = s.Attribute("href")?.Value ?? "";
				var description = s.Value.Trim();

				xmlDoc.SeeAlso.Add(new XMLDoc.XMLSeeAlso(cref, href, description));
			}

			var examples = xml.Descendants("example").ToList();
			foreach (var e in examples)
			{
				var description = RemoveXMLSyntax(e.Name.ToString(), e.ToString());

				xmlDoc.Examples.Add(description);
			}
		}

		return xmlDoc;
	}

	// This regex is used to find the file scope namespace, but it could fail
	[GeneratedRegex("namespace [\\w|\\d|.]+;")]
	private static partial Regex NamespaceRegex();
}