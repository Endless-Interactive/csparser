using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSParser;

public partial class Generator
{
	private static readonly SymbolDisplayFormat FullyQualifiedFormatCustom = new(
		SymbolDisplayGlobalNamespaceStyle.Omitted,
		SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
		SymbolDisplayGenericsOptions.IncludeTypeParameters,
		SymbolDisplayMemberOptions.IncludeContainingType,
		SymbolDisplayDelegateStyle.NameAndSignature,
		SymbolDisplayExtensionMethodStyle.Default,
		SymbolDisplayParameterOptions.None,
		SymbolDisplayPropertyStyle.NameOnly,
		SymbolDisplayLocalOptions.None,
		SymbolDisplayKindOptions.None,
		SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.ExpandNullable);

	public static bool Debug;

	private readonly HashSet<PortableExecutableReference> _references = [];

	private readonly List<SyntaxTree> _syntaxTrees = [];
	public readonly CSExclusions Exclusions = new();
	private CSharpCompilation? _compilation;

	private SyntaxTree _tree;
	public List<CSInfo> Namespaces = [];

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
		Exclusions.Add(modifier);
	}

	/// <summary>
	///     Excludes a namespace or class from being parsed.
	/// </summary>
	/// <param name="type"></param>
	/// <param name="name"></param>
	public void Exclude(CSExclusions.ExcludeType type, string name)
	{
		Exclusions.Add(type, name);
	}

	public void AddAssemblyReference(string assemblyPath)
	{
		if (!File.Exists(assemblyPath))
			throw new FileNotFoundException($"Assembly not found: {assemblyPath}");

		var reference = MetadataReference.CreateFromFile(assemblyPath);
		_references.Add(reference);

		_compilation = _compilation?.AddReferences(reference);
	}

	public void AddAssemblyReferences(string directoryPath, string searchPattern = "*.dll")
	{
		if (!Directory.Exists(directoryPath))
			throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");

		var assemblyFiles = Directory.GetFiles(directoryPath, searchPattern, SearchOption.AllDirectories);

		foreach (var assemblyFile in assemblyFiles)
			try
			{
				AddAssemblyReference(assemblyFile);
			}
			catch (Exception ex)
			{
				Log($"Failed to load assembly {assemblyFile}: {ex.Message}");
			}
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
		_tree = CSharpSyntaxTree.ParseText(code);
		_syntaxTrees.Add(_tree);

		_compilation = CSharpCompilation.Create(
			"CSParserCompilation",
			_syntaxTrees,
			_references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		ProcessSyntaxTree(_tree, code);
	}

	private void ProcessSyntaxTree(SyntaxTree tree, string code)
	{
		var semanticModel = _compilation!.GetSemanticModel(tree);
		var root = tree.GetRoot();

		var namespaces = root.DescendantNodes().OfType<NamespaceDeclarationSyntax>().ToList();
		var namespaceFileScoped = false;

		if (namespaces.Count == 0)
		{
			var match = NamespaceRegex().Match(code);
			if (match.Success)
			{
				namespaceFileScoped = true;
				var fileScopeNamespace = match.Value.Replace("namespace ", "").Replace(";", "");
				namespaces.Add(SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(fileScopeNamespace)));
			}
		}

		foreach (var nd in namespaces)
		{
			var namespaceName = nd.Name.ToString();
			var node = namespaceFileScoped ? root : nd;
			if (Exclusions.IsNamespaceExcluded(namespaceName))
			{
				Log($"Excluding namespace {namespaceName}");
				continue;
			}

			var existingInfo = Namespaces.FirstOrDefault(x => x.Namespace == namespaceName);

			if (existingInfo != null)
			{
				var classes = GetClasses(node, semanticModel);
				foreach (var @class in classes)
				{
					var existingClass = existingInfo.Classes.FirstOrDefault(x => x.Name == @class.Name);
					if (existingClass != null)
					{
						existingClass.Methods.AddRange(@class.Methods);
						existingClass.Properties.AddRange(@class.Properties);
						existingClass.Fields.AddRange(@class.Fields);
						continue;
					}

					existingInfo.Classes.Add(@class);
				}

				var enums = GetEnums(node, semanticModel);
				foreach (var @enum in enums)
				{
					var existingEnum = existingInfo.Enums.FirstOrDefault(x => x.Name == @enum.Name);
					if (existingEnum != null)
					{
						existingEnum.Values.AddRange(@enum.Values);
						continue;
					}

					existingInfo.Enums.Add(@enum);
				}

				var interfaces = GetInterfaces(node, semanticModel);
				foreach (var @interface in interfaces)
				{
					var existingInterface = existingInfo.Interfaces.FirstOrDefault(x => x.Name == @interface.Name);
					if (existingInterface != null)
					{
						existingInterface.Methods.AddRange(@interface.Methods);
						existingInterface.Properties.AddRange(@interface.Properties);
						continue;
					}

					existingInfo.Interfaces.Add(@interface);
				}

				var delegates = GetDelegates(node, semanticModel);
				foreach (var @delegate in delegates) existingInfo.Delegates.Add(@delegate);

				continue;
			}

			var csInfo = new CSInfo
			{
				Namespace = namespaceName,
				Classes = GetClasses(node, semanticModel),
				Enums = GetEnums(node, semanticModel),
				Interfaces = GetInterfaces(node, semanticModel),
				Delegates = GetDelegates(node, semanticModel)
			};

			if (csInfo.IsAllExcluded(Exclusions))
			{
				Log($"Excluding namespace {namespaceName} because all classes are excluded");
				continue;
			}

			Namespaces.Add(csInfo);
		}
	}

	private List<CSClass> GetClasses(SyntaxNode root, SemanticModel semanticModel)
	{
		var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();
		var classList = new List<CSClass>();

		foreach (var cd in classes)
		{
			var className = cd.Identifier.ToString();
			var classSymbol = semanticModel.GetDeclaredSymbol(cd);

			var inherits = new List<string>();
			if (classSymbol != null)
			{
				if (classSymbol.BaseType != null && classSymbol.BaseType.SpecialType != SpecialType.System_Object)
					inherits.Add(classSymbol.BaseType.ToDisplayString(FullyQualifiedFormatCustom));
				inherits.AddRange(classSymbol.Interfaces.Select(i => i.ToDisplayString(FullyQualifiedFormatCustom)));
			}
			else
			{
				inherits = cd.BaseList?.Types.Select(x => x.Type.ToString()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList() ??
				           new List<string>();
			}

			var @class = new CSClass
			{
				Name = className,
				Inherits = inherits
			};

			@class.SetModifiers(cd.Modifiers.ToString());

			if (Exclusions.IsClassExcluded(@class))
			{
				Log($"Excluding class {className}");
				continue;
			}

			@class.XmlDoc = GetXMLDocumentation(cd);

			var existingClass = classList.FirstOrDefault(x => x.Name == className);

			if (existingClass != null)
			{
				existingClass.Methods.AddRange(GetMethods(cd, semanticModel));
				existingClass.Properties.AddRange(GetProperties(cd, semanticModel));
				existingClass.Fields.AddRange(GetFields(cd, semanticModel));
				existingClass.Events.AddRange(GetEvents(cd, semanticModel));
				continue;
			}

			@class.Methods = GetMethods(cd, semanticModel);
			@class.Properties = GetProperties(cd, semanticModel);
			@class.Fields = GetFields(cd, semanticModel);
			@class.Events = GetEvents(cd, semanticModel);

			classList.Add(@class);
		}

		return classList;
	}

	private List<CSEnum> GetEnums(SyntaxNode root, SemanticModel semanticModel)
	{
		var enumList = new List<CSEnum>();
		var enums = root.DescendantNodes().OfType<EnumDeclarationSyntax>().ToList();

		foreach (var ed in enums)
		{
			var enumName = ed.Identifier.ToString();

			var enumSymbol = semanticModel.GetDeclaredSymbol(ed);
			var underlyingTypeSymbol = enumSymbol?.EnumUnderlyingType;
			var underlyingTypeSyntax = ed.BaseList?.Types.FirstOrDefault()?.Type;

			var enumType = underlyingTypeSymbol is not null
				? underlyingTypeSymbol.ToDisplayString(FullyQualifiedFormatCustom)
				: underlyingTypeSyntax?.ToString() ?? "int";

			var csEnum = new CSEnum
			{
				Name = enumSymbol is not null
					? enumSymbol.ToDisplayString(FullyQualifiedFormatCustom)
					: enumName,
				Type = enumType
			};

			csEnum.SetModifiers(ed.Modifiers.ToString());

			if (Exclusions.IsEnumExcluded(csEnum))
			{
				Log($"Excluding enum {enumName}");
				continue;
			}

			csEnum.XmlDoc = GetXMLDocumentation(ed);

			foreach (var ev in ed.Members.OfType<EnumMemberDeclarationSyntax>())
				csEnum.Values.Add(new CSEnumValue
				{
					Name = ev.Identifier.ToString(),
					Value = ev.EqualsValue?.Value.ToString() ?? ""
				});

			enumList.Add(csEnum);
		}

		return enumList;
	}

	private List<CSInterface> GetInterfaces(SyntaxNode root, SemanticModel semanticModel)
	{
		var interfaceList = new List<CSInterface>();
		var interfaces = root.DescendantNodes().OfType<InterfaceDeclarationSyntax>().ToList();

		foreach (var id in interfaces)
		{
			var interfaceName = id.Identifier.ToString();

			var csInterface = new CSInterface
			{
				Name = interfaceName
			};

			csInterface.SetModifiers(id.Modifiers.ToString());

			if (Exclusions.IsInterfaceExcluded(csInterface))
			{
				Log($"Excluding interface {interfaceName}");
				continue;
			}

			csInterface.XmlDoc = GetXMLDocumentation(id);
			csInterface.Methods = GetMethods(id, semanticModel);
			csInterface.Properties = GetProperties(id, semanticModel);

			interfaceList.Add(csInterface);
		}

		return interfaceList;
	}

	private List<CSDelegate> GetDelegates(SyntaxNode root, SemanticModel semanticModel)
	{
		var delegateList = new List<CSDelegate>();
		var delegates = root.DescendantNodes().OfType<DelegateDeclarationSyntax>().ToList();

		foreach (var dd in delegates)
		{
			var delegateName = dd.Identifier.ToString();
			var delegateSymbol = semanticModel.GetDeclaredSymbol(dd);
			var returnType = delegateSymbol?.DelegateInvokeMethod?.ReturnType.ToDisplayString(FullyQualifiedFormatCustom)
			                 ?? dd.ReturnType.ToString();

			var csDelegate = new CSDelegate
			{
				Name = delegateName,
				Parameters = dd.ParameterList.Parameters.Select(ps =>
				{
					var paramSymbol = semanticModel.GetDeclaredSymbol(ps);
					return new CSParameter
					{
						Name = ps.Identifier.ToString(),
						Type = paramSymbol?.Type.ToDisplayString(FullyQualifiedFormatCustom) ?? ps.Type?.ToString(),
						Optional = ps.Default != null,
						DefaultValue = ps.Default?.Value.ToString() ?? ""
					};
				}).ToList(),
				ReturnType = returnType
			};

			csDelegate.SetModifiers(dd.Modifiers.ToString());

			csDelegate.XmlDoc = GetXMLDocumentation(dd);

			delegateList.Add(csDelegate);
		}

		return delegateList;
	}

	private static void Log(string message)
	{
		if (!Debug) return;
		Console.WriteLine(message);
	}

	private List<CSMethod> GetMethods(SyntaxNode root, SemanticModel semanticModel)
	{
		var methodList = new List<CSMethod>();
		var methods = root.ChildNodes().OfType<MethodDeclarationSyntax>().ToList();

		foreach (var md in methods)
		{
			var modifiers = md.Modifiers.ToString();

			var methodSymbol = semanticModel.GetDeclaredSymbol(md);
			var returnType = methodSymbol?.ReturnType.ToDisplayString(FullyQualifiedFormatCustom) ??
			                 md.ReturnType.ToString();

			var method = new CSMethod
			{
				Name = md.Identifier.ToString(),
				ReturnType = returnType
			};

			var parameters = md.ParameterList.Parameters;

			foreach (var ps in parameters)
			{
				var paramSymbol = semanticModel.GetDeclaredSymbol(ps);
				var param = new CSParameter
				{
					Name = ps.Identifier.ToString(),
					Optional = ps.Default != null,
					DefaultValue = ps.Default?.Value.ToString() ?? "",
					Type = paramSymbol is not null
						? paramSymbol.Type.ToDisplayString(FullyQualifiedFormatCustom)
						: ps.Type?.ToString()
				};

				method.Parameters.Add(param);
			}

			method.SetModifiers(modifiers);

			if (Exclusions.IsMethodExcluded(method))
			{
				Log($"Excluding method {method.Name}");
				continue;
			}

			method.XmlDoc = GetXMLDocumentation(md);

			methodList.Add(method);
		}

		return methodList;
	}

	private List<CSProperty> GetProperties(SyntaxNode root, SemanticModel semanticModel)
	{
		var propertyList = new List<CSProperty>();
		var properties = root.ChildNodes().OfType<PropertyDeclarationSyntax>().ToList();

		foreach (var pd in properties)
		{
			var modifiers = pd.Modifiers.ToString();
			var symbol = semanticModel.GetDeclaredSymbol(pd)?.Type;
			var propertyType = symbol is not null
				? symbol.ToDisplayString(FullyQualifiedFormatCustom)
				: pd.Type.ToString();

			var property = new CSProperty
			{
				Name = pd.Identifier.ToString(),
				DefaultValue = pd.Initializer?.Value.ToString().Trim('"') ?? "",
				Type = propertyType
			};

			property.SetModifiers(modifiers);

			if (Exclusions.IsPropertyExcluded(property))
			{
				Log($"Excluding property {property.Name}");
				continue;
			}

			property.XmlDoc = GetXMLDocumentation(pd);

			propertyList.Add(property);
		}

		return propertyList;
	}

	private List<CSField> GetFields(SyntaxNode root, SemanticModel semanticModel)
	{
		var fieldList = new List<CSField>();
		var fields = root.ChildNodes().OfType<FieldDeclarationSyntax>().ToList();

		foreach (var fd in fields)
		{
			var modifiers = fd.Modifiers.ToString();

			foreach (var variable in fd.Declaration.Variables)
			{
				var field = new CSField
				{
					Name = variable.Identifier.ToString(),
					DefaultValue = variable.Initializer?.Value.ToString().Trim('"') ?? ""
				};

				var typeSymbol = ( semanticModel.GetDeclaredSymbol(variable) as IFieldSymbol )?.Type;

				field.Type = typeSymbol is not null
					? typeSymbol.ToDisplayString(FullyQualifiedFormatCustom)
					: fd.Declaration.Type.ToString();

				field.SetModifiers(modifiers);

				if (Exclusions.IsFieldExcluded(field))
				{
					Log($"Excluding field {field.Name}");
					continue;
				}

				field.XmlDoc = GetXMLDocumentation(fd);
				fieldList.Add(field);
			}
		}

		return fieldList;
	}

	private List<CSEvent> GetEvents(SyntaxNode root, SemanticModel semanticModel)
	{
		var eventList = new List<CSEvent>();
		var eventFields = root.ChildNodes().OfType<EventFieldDeclarationSyntax>();
		foreach (var ef in eventFields)
		{
			var modifiers = ef.Modifiers.ToString();
			var typeSyntax = ef.Declaration.Type;
			foreach (var variable in ef.Declaration.Variables)
			{
				var eventSymbol = semanticModel.GetDeclaredSymbol(variable) as IEventSymbol;
				var eventType = eventSymbol?.Type.ToDisplayString(FullyQualifiedFormatCustom) ?? typeSyntax.ToString();

				var csEvent = new CSEvent
				{
					Name = variable.Identifier.ToString()
				};

				if (eventSymbol?.Type is INamedTypeSymbol { DelegateInvokeMethod: not null } namedTypeSymbol)
					csEvent.Delegate = new CSDelegate
					{
						Name = namedTypeSymbol.Name,
						ReturnType = namedTypeSymbol.DelegateInvokeMethod.ReturnType.ToDisplayString(FullyQualifiedFormatCustom),
						Parameters = namedTypeSymbol.DelegateInvokeMethod.Parameters.Select(ps => new CSParameter
						{
							Name = ps.Name,
							Type = ps.Type.ToDisplayString(FullyQualifiedFormatCustom),
							Optional = ps.IsOptional,
							DefaultValue = ps.HasExplicitDefaultValue ? ps.ExplicitDefaultValue?.ToString() ?? string.Empty : string.Empty
						}).ToList()
					};
				else
					csEvent.Delegate = new CSDelegate
					{
						Name = eventType,
						ReturnType = "void",
						Parameters = []
					};

				csEvent.SetModifiers(modifiers);

				if (Exclusions.IsEventExcluded(csEvent))
				{
					Log($"Excluding event {csEvent.Name}");
					continue;
				}

				csEvent.XmlDoc = GetXMLDocumentation(ef);
				eventList.Add(csEvent);
			}
		}

		var eventDeclarations = root.ChildNodes().OfType<EventDeclarationSyntax>();
		foreach (var ed in eventDeclarations)
		{
			var modifiers = ed.Modifiers.ToString();
			var eventSymbol = semanticModel.GetDeclaredSymbol(ed);
			var eventType = eventSymbol?.Type.ToDisplayString(FullyQualifiedFormatCustom) ?? ed.Type.ToString();

			var csEvent = new CSEvent
			{
				Name = ed.Identifier.ToString()
			};

			if (eventSymbol?.Type is INamedTypeSymbol { DelegateInvokeMethod: not null } namedTypeSymbol)
				csEvent.Delegate = new CSDelegate
				{
					Name = namedTypeSymbol.Name,
					ReturnType = namedTypeSymbol.DelegateInvokeMethod.ReturnType.ToDisplayString(FullyQualifiedFormatCustom),
					Parameters = namedTypeSymbol.DelegateInvokeMethod.Parameters.Select(ps => new CSParameter
					{
						Name = ps.Name,
						Type = ps.Type.ToDisplayString(FullyQualifiedFormatCustom),
						Optional = ps.IsOptional,
						DefaultValue = ps.HasExplicitDefaultValue ? ps.ExplicitDefaultValue?.ToString() ?? string.Empty : string.Empty
					}).ToList()
				};
			else
				csEvent.Delegate = new CSDelegate
				{
					Name = eventType,
					ReturnType = "void",
					Parameters = []
				};

			csEvent.SetModifiers(modifiers);

			if (Exclusions.IsEventExcluded(csEvent))
			{
				Log($"Excluding event {csEvent.Name}");
				continue;
			}

			csEvent.XmlDoc = GetXMLDocumentation(ed);
			eventList.Add(csEvent);
		}

		return eventList;
	}

	private XMLDoc GetXMLDocumentation(CSharpSyntaxNode md)
	{
		return GetXMLDocumentation(md.GetLeadingTrivia()
			.Where(x => x.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
			            x.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia)).Select(x => x.ToString()).ToList());
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
		var groupedComments = comments.Aggregate("", (current, comment) => current + $"{comment}\n");

		if (comments.Count == 0)
			return xmlDoc;

		var xml = XDocument.Parse($"<root>{groupedComments.Replace("///", "")}</root>");

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

		return xmlDoc;
	}

	[GeneratedRegex("namespace [\\w|\\d|.]+;")]
	private static partial Regex NamespaceRegex();
}