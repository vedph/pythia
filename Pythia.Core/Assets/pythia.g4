grammar pythia;

// PARSER

// query: the whole input must be consumed
query: corSet? docSet? txtExpr EOF;

// sets
// corpus set: once for whole query, defines the corpora ID(s)
// to limit the search into; IDs which are not valid identifiers
// (e.g. starting with a digit or including a dot) can be quoted
corSet: CSET1C (ID | QVALUE)+ CSET0;

// documents set: once for the whole query, defines the document pairs
// to match so that search is limited only within the set of matching documents
docSet: CSET1D docExpr CSET0;

// expressions: AND and AND NOT bind tighter than OR and OR NOT
// (alternatives listed first have higher precedence)
docExpr: LPAREN docExpr RPAREN #deParen
       | LSQUARE tpair RSQUARE #dePair
       | docExpr (AND | ANDNOT) docExpr #deAnd
       | docExpr (OR | ORNOT) docExpr #deOr;

// #... are labels to let visitor/listener distinguish between alternatives.
// AND and AND NOT bind tighter than OR, which binds tighter than location
// operators. OR NOT is not allowed for text, as it would match any span
// not matching its right operand. Location operators are right-associative,
// so that A BEFORE B BEFORE C means A BEFORE (B BEFORE C): A is filtered by
// B, which in turn is filtered by C.
txtExpr: LPAREN txtExpr RPAREN #teParen
       | pair #tePair
       | txtExpr (AND | ANDNOT) txtExpr #teAnd
       | txtExpr OR txtExpr #teOr
       | <assoc=right> txtExpr locop txtExpr #teLocation;

// pairs
pair: LSQUARE (tpair | spair) RSQUARE;

// structure pair: $NAME alone is a shortcut for $name="NAME", i.e. any structure
// of type NAME; else NAME is any attribute of a structure, e.g. $value="x".
spair: name=SID (operator=(EQ | NEQ | CONTAINS | STARTSWITH | ENDSWITH | REGEXP | WILDCARDS | SIMILAR | EQN | NEQN | LT | LTEQ | GT | GTEQ) value=QVALUE)?;

// token pair: can be name only when referring to non-privileged attribute,
// e.g. [value="philosophia" AND pn] meaning that the token must have a pn
// attribute.
tpair: name=ID (operator=(EQ | NEQ | CONTAINS | STARTSWITH | ENDSWITH | REGEXP | WILDCARDS | SIMILAR | EQN | NEQN | LT | LTEQ | GT | GTEQ) value=QVALUE)?;

// location operator with its arguments (n, m, ns, ms, ne, me with an
// integer value; s with a structure name). Argument names are identifiers
// rather than keywords, so that they do not prevent their use as attribute
// names; they are validated when building SQL.
locop: operator=(NEAR | BEFORE | AFTER | INSIDE | OVERLAPS | LALIGN | RALIGN | NOTNEAR | NOTBEFORE | NOTAFTER | NOTINSIDE | NOTOVERLAPS | NOTLALIGN | NOTRALIGN) LPAREN (locArg (COMMA locArg)*)? RPAREN;

locArg: name=ID EQ (INT | ID);

// TOKENS

// sets
CSET1C: '@@';
CSET1D: '@';
CSET0: ';';

// operators
NOTINSIDE: 'NOT' [ \t\r\n]+ 'INSIDE';
NOTBEFORE: 'NOT' [ \t\r\n]+ 'BEFORE';
NOTAFTER: 'NOT' [ \t\r\n]+ 'AFTER';
NOTNEAR: 'NOT' [ \t\r\n]+ 'NEAR';
NOTOVERLAPS: 'NOT' [ \t\r\n]+ 'OVERLAPS';
NOTLALIGN: 'NOT' [ \t\r\n]+ 'LALIGN';
NOTRALIGN: 'NOT' [ \t\r\n]+ 'RALIGN';

ANDNOT: 'AND' [ \t\r\n]+ 'NOT';
ORNOT: 'OR' [ \t\r\n]+ 'NOT';

INSIDE: 'INSIDE';
BEFORE: 'BEFORE';
AFTER: 'AFTER';
NEAR: 'NEAR';
OVERLAPS: 'OVERLAPS';
LALIGN: 'LALIGN';
RALIGN: 'RALIGN';

AND: 'AND';
OR: 'OR';

// quoted value (the value in a pair)
QVALUE: '"' ~["]* '"';

// pair operators
CONTAINS: '*=';
STARTSWITH: '^=';
ENDSWITH: '$=';
REGEXP: '~=';
WILDCARDS: '?=';
SIMILAR: '%=';
LTEQ: '<=';
GTEQ: '>=';
EQ: '=';
EQN: '==';
NEQ: '<>';
NEQN: '!=';
LT: '<';
GT: '>';

// corpus ID, document / token attribute name, location argument name
ID: ID_HEAD (ID_HEAD | ID_BODY)*;
fragment ID_HEAD: [a-zA-Z_];
fragment ID_BODY: [-0-9];

// structure attribute name
SID: '$' (ID_HEAD | ID_BODY)+;

INT: [0-9]+;

// punctuation
LPAREN: '(';
RPAREN: ')';
LSQUARE: '[';
RSQUARE: ']';
COMMA: ',';

// whitespace
WS: [ \r\n\t]+ -> skip;
