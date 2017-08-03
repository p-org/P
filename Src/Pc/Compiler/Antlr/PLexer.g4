lexer grammar PLexer;

// Type names

ANY      : 'any' ;
BOOL     : 'bool' ;
ENUM     : 'enum' ;
EVENT    : 'event' ;
EVENTSET : 'eventset' ;
FLOAT    : 'float' ;
INT      : 'int' ;
MACHINE  : 'machine' ;
MAP      : 'map' ;
SEQ      : 'seq' ;

// Keywords

ANNOUNCE : 'announce' ;
AS       : 'as' ;
ASSERT   : 'assert' ;
ASSUME   : 'assume' ;
CASE     : 'case' ;
COLD     : 'cold' ;
DATA     : 'data' ;
DEFAULT  : 'default' ;
DEFER    : 'defer' ;
DO       : 'do' ;
ELSE     : 'else' ;
ENTRY    : 'entry' ;
EXIT     : 'exit' ;
EXTERN   : 'extern' ;
FUN      : 'fun' ;
GOTO     : 'goto' ;
GROUP    : 'group' ;
HALT     : 'halt' ;
HOT      : 'hot' ;
IF       : 'if' ;
IGNORE   : 'ignore' ;
IN       : 'in' ;
KEYS     : 'keys' ;
MOVE     : 'move' ;
NEW      : 'new' ;
OBSERVES : 'observes' ;
ON       : 'on' ;
POP      : 'pop' ;
PRINT    : 'print' ;
PUSH     : 'push' ;
RAISE    : 'raise' ;
RECEIVE  : 'receive' ;
RECEIVES : 'receives' ;
RETURN   : 'return' ;
SEND     : 'send' ;
SENDS    : 'sends' ;
SIZEOF   : 'sizeof' ;
SPEC     : 'spec' ;
START    : 'start' ;
STATE    : 'state' ;
SWAP     : 'swap' ;
THIS     : 'this' ;
TYPE     : 'type' ;
VALUES   : 'values' ;
VAR      : 'var' ;
WHILE    : 'while' ;
WITH     : 'with' ;

// Linker-specific keywords

COMPOSE        : 'compose' ;
EXPORT         : 'export' ;
HIDE           : 'hide' ;
IMPLEMENTATION : 'implementation' ;
MODULE         : 'module' ;
PRIVATE        : 'private' ;
REFINES        : 'refines' ;
RENAME         : 'rename' ;
SAFE           : 'safe' ;
TEST           : 'test' ;

// Common keywords

CREATES : 'creates' ;
TO      : 'to' ;

// Literals

BoolLiteral : 'true' | 'false' ;

IntLiteral : [0-9]+ ;

NullLiteral : 'null';

StringLiteral : '"' StringCharacters? '"' ;
fragment StringCharacters : StringCharacter+ ;
fragment StringCharacter : ~["\\] | EscapeSequence ;
fragment EscapeSequence : '\\' . ;

// Symbols

FAIRNONDET : '$$' ;
NONDET : '$' ;

LNOT   : '!' ;
LAND   : '&&' ;
LOR    : '||' ;

EQ     : '==' ;
NE     : '!=' ;
LE     : '<=' ;
GE     : '>=' ;
LT     : '<'  ;
GT     : '>'  ;

ASSIGN : '=' ;
INSERT : '+=' ;
REMOVE : '-=' ;

ADD    : '+' ;
SUB    : '-' ;
MUL    : '*' ;
DIV    : '/' ;

LBRACE : '{' ;
RBRACE : '}' ;
LBRACK : '[' ;
RBRACK : ']' ;
LPAREN : '(' ;
RPAREN : ')' ;
SEMI   : ';' ;
COMMA  : ',' ;
DOT    : '.' ;
COLON  : ':' ;

// Identifiers

Iden : PLetter PLetterOrDigit* ;
fragment PLetter : [a-zA-Z_] ;
fragment PLetterOrDigit : [a-zA-Z0-9_] ;

// Non-code regions

Whitespace : [ \t\r\n\f]+ -> skip ;
BlockComment : '/*' .*? '*/' -> channel(HIDDEN) ;
LineComment : '//' ~[\r\n]* -> channel(HIDDEN) ;
