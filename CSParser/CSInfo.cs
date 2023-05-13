using System.Text.RegularExpressions;

namespace CSParser;

public class CSClass : CSObject
{
	public List<CSField> Fields = new();
	public List<string> Inherits = new();
	public List<CSMethod> Methods = new();
	public string Name;
	public List<CSProperty> Properties = new();
}

public class CSObject
{
	public CSAccessModifier AccessModifier;
	public string Modifier = "";
	public string Name = "";
	public XMLDoc XmlDoc;
	public string FullModifier => $"{GetModifier(AccessModifier)}{GetModifier()}";

	private string GetModifier()
	{
		return Modifier.Length < 1 ? "" : $" {Modifier}";
	}

	public void SetModifiers(string mod)
	{
		var modList = mod.Split(" ");

		if (mod.Length == 0) return;

		var modCheck = modList[0];

		if (modList.Length >= 2)
		{
			var sec = modList[1];

			modCheck = sec is "internal" or "protected" ? $"{modList[0]} {sec}" : modList[0];
		}

		var regex = new Regex(modCheck.ToLower() + @"[ ]?");

		mod = regex.Replace(mod, "");

		AccessModifier = modCheck.ToLower() switch
		{
			"public" => CSAccessModifier.Public,
			"private" => CSAccessModifier.Private,
			"protected" => CSAccessModifier.Protected,
			"internal" => CSAccessModifier.Internal,
			"protected internal" => CSAccessModifier.ProtectedInternal,
			"private protected" => CSAccessModifier.PrivateProtected,
			_ => throw new Exception($"Unknown access modifier: {modCheck}")
		};

		Modifier = mod;
	}

	public static string GetModifier(CSAccessModifier modifier)
	{
		return modifier switch
		{
			CSAccessModifier.PrivateProtected => "private protected",
			CSAccessModifier.ProtectedInternal => "protected internal",
			_ => modifier.ToString().ToLower()
		};
	}

	public override string ToString()
	{
		return $"{FullModifier} {Name}";
	}
}

public class XMLDoc
{
	public string Summary { get; set; } = "";
	public string Remarks { get; set; } = "";
	public string Returns { get; set; } = "";
	public List<XMLDocParam> Parameters { get; set; } = new();
	public string ParamRef { get; set; } = "";
	public string Exception { get; set; } = "";
	public string Value { get; set; } = "";
	public string Para { get; set; } = "";
	public string List { get; set; } = "";
	public string C { get; set; } = "";
	public string Code { get; set; } = "";
	public string Example { get; set; } = "";
	public string InheritDoc { get; set; } = "";
	public string Include { get; set; } = "";
	public string See { get; set; } = "";
	public string SeeAlso { get; set; } = "";
	public string Cref { get; set; } = "";
	public string Href { get; set; } = "";
	public string TypeParam { get; set; } = "";
	public string TypeParamRef { get; set; } = "";

	public override string ToString()
	{
		return $"{Summary}";
	}

	public class XMLDocParam
	{
		public string Description;
		public string Name;

		public XMLDocParam(string name, string description)
		{
			Name = name;
			Description = description;
		}
	}
}

public class CSMethod : CSObject
{
	public List<CSParameter> Parameters = new();
	public string ReturnType;

	public override string ToString()
	{
		return $"{FullModifier} {ReturnType} {Name}({string.Join(", ", Parameters)})";
	}
}

public class CSProperty : CSObject
{
	public string ReturnType;
}

public class CSField : CSObject
{
	public string Type;
}

public class CSParameter
{
	public string Name, Type, DefaultValue;
	public bool Optional;

	public override string ToString()
	{
		var defaultValue = DefaultValue;

		switch (Type)
		{
			case "string":
				defaultValue = $"\"{defaultValue}\"";
				break;
		}

		return $"{Type} {Name}{( Optional ? $" = {defaultValue}" : "" )}";
	}
}

public class CSInfo
{
	public List<CSClass> Classes = new();
	public string? Namespace { get; set; }

	public bool IsAllExcluded(CSExclusions exclusions)
	{
		return Classes.Count != 0 && Classes.All(c =>
			c.Fields.Count != 0 && c.Fields.All(exclusions.IsFieldExcluded) && c.Methods.Count != 0 &&
			c.Methods.All(exclusions.IsMethodExcluded) &&
			c.Properties.Count != 0 && c.Properties.All(exclusions.IsPropertyExcluded));
	}
}

public enum CSAccessModifier
{
	Public,
	Private,
	Protected,
	Internal,
	ProtectedInternal,
	PrivateProtected
}

public class CSExclusions
{
	public enum ExcludeType
	{
		Namespace,
		Class,
		Method
	}

	public List<string> Classes = new();
	public List<string> Methods = new();
	public List<CSAccessModifier> Modifiers = new();
	public List<string> Namespaces = new();

	public void Add(CSAccessModifier modifier)
	{
		Modifiers.Add(modifier);
	}

	/// <summary>
	///     Excludes a namespace or class from being parsed.
	/// </summary>
	/// <param name="type"></param>
	/// <param name="name"></param>
	public void Add(ExcludeType type, string name)
	{
		switch (type)
		{
			case ExcludeType.Namespace:
				Namespaces.Add(name);
				break;
			case ExcludeType.Class:
				Classes.Add(name);
				break;
			case ExcludeType.Method:
				Methods.Add(name);
				break;
		}
	}

	public bool IsNamespaceExcluded(string? _namespace)
	{
		return _namespace != null && Namespaces.Any(ns => new Regex(ns).IsMatch(_namespace));
	}

	public bool IsMethodExcluded(CSMethod method)
	{
		return Methods.Any(m => new Regex(m).IsMatch(method.Name)) || IsAccessModifierExcluded(method.AccessModifier);
	}

	public bool IsClassExcluded(CSClass @class)
	{
		return Classes.Any(cls => new Regex(cls).IsMatch(@class.Name)) || IsAccessModifierExcluded(@class.AccessModifier);
	}

	public bool IsPropertyExcluded(CSProperty property)
	{
		return IsAccessModifierExcluded(property.AccessModifier);
	}

	public bool IsFieldExcluded(CSField field)
	{
		return IsAccessModifierExcluded(field.AccessModifier);
	}

	public bool IsAccessModifierExcluded(CSAccessModifier modifier)
	{
		return Modifiers.Any(mod => mod.Equals(modifier));
	}
}