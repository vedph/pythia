using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System;
using System.Diagnostics;

namespace Corpus.Core.Plugin.Reading;

/// <summary>
/// The definition of an XML-based structure. This is used by
/// <see cref="XmlTextMapper"/> and XML-based structure detectors.
/// </summary>
public class XmlStructureDefinition
{
    private string? _valueTemplate;
    private HashSet<string>? _valueTemplateArgNames;

    private Dictionary<string, string>? _args;
    private XmlStructureValueArg[]? _valueTemplateArgs;

    /// <summary>
    /// Gets or sets the XPath expression to look for the attribute value(s)
    /// in the source XML document. This is a full XPath expression, and
    /// can use namespace prefixes. The expression must be relative to
    /// its parent (or to the document root when defining the root node);
    /// thus, e.g. <c>/tei:TEI/tei:text/tei:body</c> can be a root XPath,
    /// and <c>./tei:div</c> its child node in the map.
    /// </summary>
    public string? XPath { get; set; }

    /// <summary>
    /// Gets or sets the XPath expression(s) to look for the structure
    /// value, relative to the structure's target element. For instance,
    /// <c>./@n</c> would get an <c>n</c> attribute from the target
    /// element of <see cref="XPath"/>. Each of these expressions is keyed
    /// to an argument name e.g. <c>n</c> for <c>./@n</c>.
    /// These arguments will then be used to build the target structure's
    /// value via <see cref="ValueTemplate"/>.
    /// </summary>
    public XmlStructureValueArg[]? ValueTemplateArgs
    {
        get { return _valueTemplateArgs; }
        set
        {
            _valueTemplateArgs = value;
            _args?.Clear();

            if (value?.Length > 0)
            {
                _args ??= [];
                foreach (var a in value) _args[a.Name!] = a.Value ?? "";
            }
        }
    }

    /// <summary>
    /// Gets or sets the target structure's value template. This is either
    /// a constant value, or a template with placeholders between braces.
    /// For instance, <c>Chapter {n}</c> is a template, while <c>New Section</c>
    /// is a constant. Placeholders are replaced by values taken from
    /// <see cref="ValueTemplateArgs"/>, except when they start with a <c>$</c>,
    /// which is reserved for special macros. Currently, the only defined
    /// macro is <c>$_</c>, which gets replaced with a space unless initial
    /// or final, or the template already has a space before it.
    /// </summary>
    public string? ValueTemplate
    {
        get { return _valueTemplate; }
        set
        {
            _valueTemplate = value;
            _valueTemplateArgNames?.Clear();

            if (!string.IsNullOrEmpty(value))
            {
                _valueTemplateArgNames ??= [];

                foreach (Match m in Regex.Matches(value, @"\{([^}]+)\}"))
                    _valueTemplateArgNames.Add(m.Groups[1].Value);
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether value should be trimmed.
    /// </summary>
    public bool ValueTrimming { get; set; }

    /// <summary>
    /// Gets or sets the maximum allowed length of the value. When this
    /// limit is exceeded, the value is cut. Leave this to 0 to disallow
    /// length limits.
    /// </summary>
    public int ValueMaxLength { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to discard nodes having
    /// an empty value, i.e. a value which is either empty or contains
    /// only whitespace(s).
    /// </summary>
    public bool DiscardEmptyValue { get; set; }

    /// <summary>
    /// Gets or sets the structure name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the structure value type.
    /// </summary>
    public AttributeType Type { get; set; }

    /// <summary>
    /// Gets the list of all the unique argument names used in
    /// <see cref="ValueTemplate"/>.
    /// </summary>
    /// <returns>List of names or null.</returns>
    public IList<string>? GetUsedArgNames() => _valueTemplateArgNames?.ToList();

    /// <summary>
    /// Gets the XPath expression corresponding to the argument with the
    /// specified name.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns>The XPath expression or null if no such argument.</returns>
    public string? GetArgXPath(string name) => _args?.ContainsKey(name) == true ?
        _args[name] : null;

    private static string ResolveArgMacros(string value)
    {
        // the unique macro is currently {$_}
        int i = value.LastIndexOf("{$_}");
        if (i == -1) return value;

        StringBuilder sb = new(value);
        do
        {
            // corner case: initial is just removed
            if (i == 0)
            {
                sb.Remove(0, 4);
                break;
            }
            else
            {
                sb.Remove(i, 4);

                // replace with space only if not preceded by space,
                // and not final
                if (!char.IsWhiteSpace(value[i - 1]) && i < sb.Length)
                    sb.Insert(i, ' ');
                i = value.LastIndexOf("{$_}", i - 1);
            }
        } while (i > -1);

        return sb.ToString();
    }

    /// <summary>
    /// Gets the value of this structure from its target element.
    /// </summary>
    /// <param name="target">The target element corresponding to this
    /// structure.</param>
    /// <param name="nsmgr">The optional namespaces manager.</param>
    /// <returns>Value.</returns>
    /// <param name="defaultToName">True to default to the structure
    /// name for the value when <see cref="ValueTemplate"/> is not defined.
    /// </param>
    /// <return>Value or null.</return>
    public string? GetStructureValue(XElement target,
        XmlNamespaceManager? nsmgr = null,
        bool defaultToName = false)
    {
        // use the target name if no value template defined
        if (string.IsNullOrEmpty(ValueTemplate))
        {
            return defaultToName? Name : null;
        }

        // if no placeholder this is a constant value, just ret it
        if (!ValueTemplate.Contains('{')) return ValueTemplate;

        // else we must lookup all the placeholders used in the template:
        // first, get all the used arguments names
        IList<string>? argNames = GetUsedArgNames();
        if (argNames == null) return ValueTemplate;

        // then, collect the value of each used argument
        XPathNavigator nav = target.CreateNavigator();
        Dictionary<string, string> argValues = [];

        foreach (string argName in argNames)
        {
            string? argXPath = GetArgXPath(argName);
            if (argXPath == null) continue;

            try
            {
                // create an XPathExpression with namespace manager if provided
                XPathExpression expr = nsmgr != null
                    ? XPathExpression.Compile(argXPath, nsmgr)
                    : XPathExpression.Compile(argXPath);

                switch (expr.ReturnType)
                {
                    case XPathResultType.NodeSet:
                        var iterator = nav.Select(expr);
                        if (iterator.MoveNext())
                            argValues[argName] = iterator.Current!.Value;
                        break;

                    case XPathResultType.Number:
                    case XPathResultType.String:
                    case XPathResultType.Boolean:
                        var result = nav.Evaluate(expr);
                        argValues[argName] = result?.ToString() ?? "";
                        break;
                }
            }
            catch (XPathException ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        // finally, build the value from the template (excluding {$...})
        string value = Regex.Replace(ValueTemplate, @"\{([^$}][^}]*)\}",
            (Match m) =>
            {
                string argName = m.Groups[1].Value;
                return argValues.ContainsKey(argName) ?
                    argValues[argName] : "";
            });

        // resolve eventual {$...} commands
        value = ResolveArgMacros(value);

        // flatten whitespaces
        value = Regex.Replace(value, @"\s+", " ");

        // trim if requested
        if (ValueTrimming) value = value.Trim();

        // eventually cut the result
        if (ValueMaxLength > 0 && value.Length > ValueMaxLength)
            value = $"{value.AsSpan(0, ValueMaxLength)}...";

        // discard if requested
        if (DiscardEmptyValue && string.IsNullOrWhiteSpace(value))
            return null;

        return value;
    }

    /// <summary>
    /// Returns a <see cref="string" /> that represents this instance.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        return $"{Name}={ValueTemplate}";
    }
}

/// <summary>
/// The argument in a <see cref="XmlStructureDefinition.ValueTemplate"/>.
/// </summary>
public class XmlStructureValueArg
{
    /// <summary>
    /// Initializes a new instance of the <see cref="XmlStructureValueArg"/> class.
    /// </summary>
    public XmlStructureValueArg()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="XmlStructureValueArg"/> class.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="value">The value.</param>
    public XmlStructureValueArg(string name, string value)
    {
        Name = name;
        Value = value;
    }

    /// <summary>
    /// Gets or sets the argument name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the argument value, which is an XPath expression
    /// relative to the target element.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Converts to string.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        return $"{Name}={Value}";
    }
}
