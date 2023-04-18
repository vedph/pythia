grammar pythia;

// PARSER

// query
query: corSet? docSet? txtExpr;

// sets
// corpus set: once for whole query, defines the corpora ID(s)
// to limit the search into
corSet: delim=CSET1C ID+ delim=CSET0;

// documents set: once for the whole query, defines the document pairs
// to match so that search is limited only within the set of matching documents
docSet: delim=CSET1D docExpr delim=CSET0;

// expressions
docExpr: docExpr (AND | OR | ANDNOT | ORNOT) docExpr
       | LPAREN docExpr RPAREN
       | LSQUARE tpair RSQUARE;

// #... are labels to let visitor/listener distinguish between alternatives;
// use yourType.IDENTIFIER() in your code to access these labels
txtExpr: txtExpr (AND | OR | ANDNOT | ORNOT) txtExpr #teLogical
       | txtExpr locop txtExpr #teLocation
       | LPAREN txtExpr RPAREN #teParen
       | pair #tePair;

// pairs
pair: LSQUARE (tpair | spair) RSQUARE;

// structure pair: can be name only referring to non-privileged attribute,
// e.g. [value="philosophia" AND $l] meaning that the token must be inside
// a structure named l.
spair: name=SID (operator=(EQ | NEQ | CONTAINS | STARTSWITH | ENDSWITH | REGEXP | WILDCARDS | SIMILAR | EQN | NEQN | LT | LTEQ | GT | GTEQ) value=QVALUE)?;

// token pair: can be name only when referring to non-privileged attribute,
// e.g. [value="philosophia" AND pn] meaning that the token must have a pn
// attribute.
tpair: name=ID (operator=(EQ | NEQ | CONTAINS | STARTSWITH | ENDSWITH | REGEXP | WILDCARDS | SIMILAR | EQN | NEQN | LT | LTEQ | GT | GTEQ) value=QVALUE)?;

locop: (operator=(NEAR | BEFORE | AFTER | INSIDE | OVERLAPS | LALIGN | RALIGN | NOTNEAR | NOTBEFORE | NOTAFTER | NOTINSIDE | NOTOVERLAPS | NOTLALIGN | NOTRALIGN) LPAREN (locnArg(',' locnArg)*(',' locsArg)?)? RPAREN);

locnArg: ('n' | 'm' | 'ns' | 'ms' | 'ne' | 'me') '=' INT;
locsArg: 's' '=' ID;

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

// corpus ID, document / token attribute name
ID: ID_HEAD (ID_HEAD | ID_BODY)*;
fragment ID_HEAD: [a-zA-Z_];
fragment ID_BODY: [-0-9];

// structure attribute name
SID: '$' (ID_HEAD | ID_BODY)*;

INT: [0-9]+;

// parentheses
LPAREN: '(';
RPAREN: ')';
LSQUARE: '[';
RSQUARE: ']';

// whitespace
WS: [ \r\n\t]+ -> skip;