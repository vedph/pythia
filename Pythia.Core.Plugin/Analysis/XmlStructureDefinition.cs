using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Corpus.Core;

namespace Pythia.Core.Plugin.Analysis
{
    /// <summary>
    /// The definition of an XML-based structure for an
    /// <see cref="XmlStructureParser"/>.
    /// </summary>
    public sealed class XmlStructureDefinition
    {
        private string _valueTemplate;
        private HashSet<string> _valueTemplateArgNames;

        private Dictionary<string, string> _args;
        private XmlStructureValueArg[] _valueTemplateArgs;

        /// <summary>
        /// Gets or sets the XPath expression to look for the attribute value(s)
        /// in the source XML document. This is a full XPath expression, and
        /// can use namespace prefixes.
        /// </summary>
        public string XPath { get; set; }

        /// <summary>
        /// Gets or sets the XPath expression(s) to look for the structure
        /// value, relative to the structure's target element. For instance,
        /// <c>./@n</c> would get an <c>n</c> attribute from the target
        /// element of <see cref="XPath"/>. Each of these expressions is keyed
        /// to an argument name e.g. <c>n</c> for <c>./@n</c>.
        /// These arguments will then be used to build the target structure's
        /// value via <see cref="ValueTemplate"/>.
        /// </summary>
        public XmlStructureValueArg[] ValueTemplateArgs
        {
            get { return _valueTemplateArgs; }
            set
            {
                _valueTemplateArgs = value;
                _args?.Clear();

                if (value?.Length > 0)
                {
                    if (_args == null) _args = new Dictionary<string, string>();
                    foreach (var a in value) _args[a.Name] = a.Value;
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
        public string ValueTemplate
        {
            get { return _valueTemplate; }
            set
            {
                _valueTemplate = value;
                _valueTemplateArgNames?.Clear();

                if (!string.IsNullOrEmpty(value))
                {
                    if (_valueTemplateArgNames == null)
                        _valueTemplateArgNames = new HashSet<string>();

                    foreach (Match m in Regex.Matches(value, @"\{([^}]+)\}"))
                        _valueTemplateArgNames.Add(m.Groups[1].Value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the structure name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the structure value type.
        /// </summary>
        public AttributeType Type { get; set; }

        /// <summary>
        /// Gets or sets the name of the token target. When this is not null,
        /// it means that the structure definition targets a token rather than
        /// a structure.
        /// This happens when you want to add attributes to those tokens which
        /// appear inside specific structures, but you do not want these
        /// structures to be stored as such, as their only purpose is marking
        /// the included tokens.
        /// For instance, a structure like "foreign" in TEI marks a token as
        /// a foreign word, but should not be stored among structures.
        /// </summary>
        public string TokenTargetName { get; set; }

        /// <summary>
        /// Gets the list of all the unique argument names used in
        /// <see cref="ValueTemplate"/>.
        /// </summary>
        /// <returns>List of names or null.</returns>
        public IList<string> GetUsedArgNames() => _valueTemplateArgNames?.ToList();

        /// <summary>
        /// Gets the XPath expression corresponding to the argument with the
        /// specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The XPath expression or null if no such argument.</returns>
        public string GetArgXPath(string name) => _args?.ContainsKey(name) == true?
            _args[name] : null;

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(Name);

            if (TokenTargetName != null) sb.Append(':').Append(TokenTargetName);
            sb.Append(Type == AttributeType.Number ? "#" : "")
                .Append('=').Append(XPath);

            return sb.ToString();
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
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the argument value, which is an XPath expression
        /// relative to the target element.
        /// </summary>
        public string Value { get; set; }

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
}
