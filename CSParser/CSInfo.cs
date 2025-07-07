using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace CSParser;

public class CSClass : CSObject
{
	public List<CSField> Fields = new();
	public List<string> Inherits = new();
	public List<CSMethod> Methods = new();
	public List<CSProperty> Properties = new();
	public bool IsPartial { get; private set; }

	protected override void SetupModifiers(string[] mods)
	{
		var replaceMods = mods;

		if (mods.Contains("partial"))
		{
			IsPartial = true;
			replaceMods = mods.Where(mod => mod != "partial").ToArray();
		}

		base.SetupModifiers(replaceMods);
	}

	protected override void SetupAccessModifier(string accessModifier)
	{
		if (accessModifier is "partial" or "file")
		{
			AccessModifier = CSAccessModifier.Private;
			return;
		}

		base.SetupAccessModifier(accessModifier);
	}

	public bool IsEmpty()
	{
		return Fields.Count == 0 && Methods.Count == 0 && Properties.Count == 0;
	}

	public bool IsExcluded(CSExclusions exclusions)
	{
		return exclusions.IsClassExcluded(this) || exclusions.IsAccessModifierExcluded(AccessModifier);
	}
}

public class CSEnum : CSObjectWithType
{
	public List<CSEnumValue> Values = new();

	public override string ToString()
	{
		return !string.IsNullOrWhiteSpace(Type) ? $"{FullModifier} enum {Name} : {Type}" : $"{FullModifier} enum {Name}";
	}

	public bool IsEmpty()
	{
		return Values.Count == 0;
	}

	public bool IsExcluded(CSExclusions exclusions)
	{
		return exclusions.IsEnumExcluded(this) || exclusions.IsAccessModifierExcluded(AccessModifier);
	}
}

public class CSEnumValue
{
	public string Name = "";
	public string Value = "";

	public override string ToString()
	{
		return Value.Length == 0 ? Name : $"{Name} = {Value}";
	}
}

public class CSInterface : CSObjectWithType
{
	public List<CSMethod> Methods = new();
	public List<CSProperty> Properties = new();

	public override string ToString()
	{
		return !string.IsNullOrWhiteSpace(Type) ? $"{FullModifier} interface {Name} : {Type}" : $"{FullModifier} interface {Name}";
	}

	public bool IsEmpty()
	{
		return Methods.Count == 0 && Properties.Count == 0;
	}

	public bool IsExcluded(CSExclusions exclusions)
	{
		return exclusions.IsInterfaceExcluded(this) || exclusions.IsAccessModifierExcluded(AccessModifier);
	}
}

public class CSObjectWithType : CSObject
{
	public string Type = "";
}

public class CSObject
{
	public CSAccessModifier AccessModifier;
	public CSModifier Modifiers = CSModifier.None;
	public string ModifierString = "";
	public string Name = "";
	public XMLDoc XmlDoc = new();
	public string FullModifier => $"{GetModifier(AccessModifier)}{GetModifiers()}";

	private string GetModifiers()
	{
		return ModifierString.Length < 1 ? "" : $" {ModifierString}";
	}

	public static string GetTypeAsString(string? type, string value)
	{
		var defaultValue = value;

		switch (type)
		{
			case "string":
				defaultValue = $"\"{value}\"";
				break;
		}

		return defaultValue;
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

		ModifierString = mod;

		var accessModifier = modCheck.ToLower();

		SetupAccessModifier(accessModifier);

		if (mod.Length == 0) return;

		var mods = mod.Split(" ");

		if (mods.Length == 0) return;

		SetupModifiers(mods);
	}

	protected virtual void SetupAccessModifier(string accessModifier)
	{
		if (ConvertModifier(accessModifier) != CSModifier.None || accessModifier.Length == 0)
			AccessModifier = CSAccessModifier.Private;
		else
			AccessModifier = accessModifier switch
			{
				"file" => CSAccessModifier.File,
				"public" => CSAccessModifier.Public,
				"private" => CSAccessModifier.Private,
				"protected" => CSAccessModifier.Protected,
				"internal" => CSAccessModifier.Internal,
				"protected internal" => CSAccessModifier.ProtectedInternal,
				"private protected" => CSAccessModifier.PrivateProtected,
				_ => throw new Exception($"Unknown access modifier: {accessModifier}")
			};
	}

	public static CSModifier ConvertModifier(string mod)
	{
		return mod.ToLower() switch
		{
			"abstract" => CSModifier.Abstract,
			"async" => CSModifier.Async,
			"const" => CSModifier.Const,
			"event" => CSModifier.Event,
			"extern" => CSModifier.Extern,
			"in" => CSModifier.In,
			"new" => CSModifier.New,
			"out" => CSModifier.Out,
			"override" => CSModifier.Override,
			"readonly" => CSModifier.Readonly,
			"sealed" => CSModifier.Sealed,
			"static" => CSModifier.Static,
			"unsafe" => CSModifier.Unsafe,
			"virtual" => CSModifier.Virtual,
			"volatile" => CSModifier.Volatile,
			_ => CSModifier.None
		};
	}

	protected virtual void SetupModifiers(string[] mods)
	{
		foreach (var modifier in mods)
			Modifiers |= ConvertModifier(modifier);

		if (Modifiers.HasFlag(CSModifier.None))
			Modifiers &= ~CSModifier.None;
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
	public List<XMLDocParam> Parameters { get; } = new();
	public List<XMLParamRef> ParamRefs { get; } = new();
	public List<XMLException> Exceptions { get; } = new();
	public string Value { get; set; } = "";
	public List<string> Examples { get; } = new();
	public string InheritDoc { get; set; } = "";
	public string Include { get; set; } = "";
	public List<XMLSee> See { get; } = new();
	public List<XMLSeeAlso> SeeAlso { get; } = new();
	public string TypeParam { get; set; } = "";
	public string TypeParamRef { get; set; } = "";

	public override string ToString()
	{
		return $"{Summary}";
	}

	public class XMLDocParam
	{
		public string Description, Name;

		public XMLDocParam(string name, string description)
		{
			Name = name;
			Description = description;
		}
	}

	public class XMLParamRef
	{
		public string Description, Name;

		public XMLParamRef(string name, string description)
		{
			Name = name;
			Description = description;
		}
	}

	public class XMLException
	{
		public string Cref, Description;

		public XMLException(string cref, string description)
		{
			Cref = cref;
			Description = description;
		}
	}

	public class XMLSee
	{
		public string Cref, Href, Description, LangWord;

		public XMLSee(string cref, string href, string description, string langWord)
		{
			Cref = cref;
			Href = href;
			Description = description;
			LangWord = langWord;
		}
	}

	public class XMLSeeAlso
	{
		public string Cref, Href, Description;

		public XMLSeeAlso(string cref, string href, string description)
		{
			Cref = cref;
			Href = href;
			Description = description;
		}
	}
}

public class CSMethod : CSObject
{
	public List<CSParameter> Parameters = new();
	public string ReturnType = "";

	public override string ToString()
	{
		return $"{FullModifier} {ReturnType} {Name}({string.Join(", ", Parameters)})";
	}
}

public class CSProperty : CSObject
{
	public string DefaultValue = "";
	public string Type = "";

	public override string ToString()
	{
		return $"{FullModifier} {Type} {Name}";
	}
}

public class CSField : CSObject
{
	public string DefaultValue = "";
	public string? Type = "";

	public override string ToString()
	{
		var defaultText = DefaultValue.Length > 0 ? $" = {GetTypeAsString(Type, DefaultValue)}" : "";
		return $"{FullModifier} {Type} {Name}{defaultText}";
	}
}

public class CSParameter
{
	public string DefaultValue = "";
	public string Name = "";
	public bool Optional;
	public string? Type = "";

	public override string ToString()
	{
		var defaultValue = CSObject.GetTypeAsString(Type, DefaultValue);

		return $"{Type} {Name}{( Optional ? $" = {defaultValue}" : "" )}";
	}
}

public class CSInfo
{
	public List<CSClass> Classes = new();
	public List<CSEnum> Enums = new();
	public List<CSInterface> Interfaces = new();
	public string? Namespace { get; set; }

	public bool IsAllExcluded(CSExclusions exclusions)
	{
		if (Classes.Count != 0 && Enums.Count != 0 && Interfaces.Count != 0)
			return false;

		return Classes.All(c => c.IsExcluded(exclusions) || c.IsEmpty()) &&
		       Enums.All(e => e.IsExcluded(exclusions) || e.IsEmpty()) &&
		       Interfaces.All(i => i.IsExcluded(exclusions) || i.IsEmpty());
	}
}

public enum CSAccessModifier
{
	File,
	Public,
	Private,
	Protected,
	Internal,
	ProtectedInternal,
	PrivateProtected
}

[Flags]
public enum CSModifier
{
	None,
	Abstract,
	Async,
	Const,
	Event,
	Extern,
	In,
	New,
	Out,
	Override,
	Readonly,
	Sealed,
	Static,
	Unsafe,
	Virtual,
	Volatile
}

public class CSExclusions
{
	public enum ExcludeType
	{
		Namespace,
		Class,
		Method,
		Enum,
		Interface
	}

	public List<string> Classes = new();
	public List<string> Enums = new();
	public List<string> Interfaces = new();
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
			case ExcludeType.Enum:
				Enums.Add(name);
				break;
			case ExcludeType.Interface:
				Interfaces.Add(name);
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

	public bool IsEnumExcluded(CSEnum @enum)
	{
		return Classes.Any(cls => new Regex(cls).IsMatch(@enum.Name)) || IsAccessModifierExcluded(@enum.AccessModifier);
	}

	public bool IsInterfaceExcluded(CSInterface @interface)
	{
		return Classes.Any(cls => new Regex(cls).IsMatch(@interface.Name)) || IsAccessModifierExcluded(@interface.AccessModifier);
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