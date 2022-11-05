using Antlr4.Runtime.Tree;
using System;
using System.Collections.Generic;
using static pythiaParser;

namespace Pythia.Sql
{
    /// <summary>
    /// State of a location expression, whose children are a first pair
    /// followed by any number of locop + pair sequences.
    /// </summary>
    public class LocationState
    {
        private readonly List<char> _pairTypes;
        private LocExprContext? _context;

        /// <summary>
        /// Gets the type of each pair in the location expression. Each type
        /// is represented by T=privileged token, t=non-privileged token,
        /// S=privileged structure, s=non-privileged structure.
        /// </summary>
        public IReadOnlyList<char> PairTypes => _pairTypes;

        /// <summary>
        /// Gets or sets the locExpr context. This is null where there is none,
        /// and not null when the walker is inside a locExpr.
        /// </summary>
        public LocExprContext? Context
        {
            get => _context;
            set
            {
                _context = value;
                UpdatePairTypes();
            }
        }

        /// <summary>
        /// Gets a value indicating whether this location state is active,
        /// i.e. the walker is inside it.
        /// </summary>
        public bool IsActive => _context != null;

        /// <summary>
        /// Gets a value indicating whether <see cref="CurrentPairNumber"/>
        /// is the last pair in the locExpr.
        /// </summary>
        public bool IsLastPair => CurrentPairNumber == _pairTypes.Count;

        /// <summary>
        /// Gets or sets the current pair number inside the locExpr context
        /// (1=first, 2=second, etc.).
        /// </summary>
        public int CurrentPairNumber { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocationState"/> class.
        /// </summary>
        public LocationState()
        {
            _pairTypes = new List<char>();
        }

        /// <summary>
        /// Gets the types for the first and second pairs starting from the
        /// current pair backwards. For instance, if the current pair is 2 (=second),
        /// this method returns the types of pair 1 and pair 2; if it is 3, it returns
        /// the types of pair 2 and 3, etc. The first type is the target node,
        /// while the second is the modifier node.
        /// </summary>
        /// <returns>Tuple with types, or null if out of range (less than 2).</returns>
        public Tuple<char, char>? GetCurrentPairTypes()
        {
            if (_context == null || CurrentPairNumber < 2) return null;
            return Tuple.Create(
                _pairTypes[CurrentPairNumber - 2],
                _pairTypes[CurrentPairNumber - 1]);
        }

        /// <summary>
        /// Resets this state.
        /// </summary>
        public void Reset()
        {
            _context = null;
            _pairTypes.Clear();
            CurrentPairNumber = 0;
        }

        /// <summary>
        /// Gets the type of the colloc pair (token/structure, privileged/not).
        /// </summary>
        /// <param name="pair">The pair to inspect. This is a tpair or spair
        /// (both these types are direct children of pair).</param>
        /// <returns><c>T</c>=token, privileged; <c>t</c>=token, non
        /// privileged; <c>S</c>=structure, privileged; <c>s</c>=structure,
        /// non privileged.</returns>
        private static char GetLocPairType(IRuleNode pair)
        {
            // node is tpair or spair
            char t = pair is SpairContext ? 'S' : 'T';
            ITerminalNode name = (ITerminalNode)pair.GetChild(0);

            if (t == 'S' && string.Compare(name.GetText(), "name", true) != 0)
                return 's';
            if (t == 'T' && !SqlPythiaListener.PrivilegedDocAttrs.Contains(
                name.GetText().ToLowerInvariant()))
            {
                return 't';
            }

            return t;
        }

        private void UpdatePairTypes()
        {
            _pairTypes.Clear();
            if (_context == null) return;

            // scan pairs types (in sequence "pair locop pair...")
            // locExpr / pair / [, => tpair|spair <=, ]
            for (int i = 0; i < _context.ChildCount; i += 2)
            {
                _pairTypes.Add(
                    GetLocPairType((IRuleNode)
                        _context.GetChild<IRuleNode>(i).GetChild(1)));
            }
        }
    }
}
