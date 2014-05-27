%namespace Microsoft.Pc.Parser
%using Microsoft.Pc.Domains;
%visibility internal
%YYSTYPE LexValue
%partial

%union {
	public string str;
}

%token INT BOOL FOREIGN ANY SEQ MAP ID
%token MAIN EVENT MACHINE MONITOR ASSUME

%token VAR START STABLE MODEL STATE FUN ACTION MAXQUEUE SUBMACHINE

%token ENTRY EXIT DEFER IGNORE GOTO ON DO PUSH

%token IF WHILE THIS TRIGGER PAYLOAD ARG NEW RETURN FAIR ID LEAVE ASSERT CALL INVOKE RAISE SEND DEFAULT DELETE NULL ELSE
%token LPAREN RPAREN LCBRACE RCBRACE LBRACKET RBRACKET SIZEOF KEYS

%token TRUE FALSE

%token ASSIGN
%token EQ NE LT GT LE GE IN
%left LAND LNOT LOR FAIRNONDET

%token DOT COLON COMMA
%left  SEMICOLON

%token INT REAL BOOL

%left  PLUS MINUS
%left  DIV
%left  MUL 
%left  UMINUS

%token maxParseToken 
%token LEX_WHITE LEX_ERROR LEX_COMMENT

%%

Program
    : EOF
	| TopDeclList
	;

TopDeclList
    : TopDecl
	| TopDecl TopDeclList
	;

TopDecl
    : EventDecl
	;

/******************* Event Declarations *******************/ 
EventDecl
	: EVENT ID CardOrNone TypeOrNone SEMICOLON      { AddEvent($2.str, ToSpan(@2), ToSpan(@1)); }
	;

CardOrNone
	: ASSERT INT									{ SetEventCard($2.str, true,  ToSpan(@1)); }
	| ASSUME INT									{ SetEventCard($2.str, false, ToSpan(@1)); }
	|												{ }
	;

TypeOrNone
	: COLON Type									{ SetEventType(ToSpan(@1)); }
	|												{ }
	;

/******************* Type Expressions *******************/

Type
	: NULL    { PushTypeExpr(MkBaseType(P_Root.UserCnstKind.NULL,    ToSpan(@1))); }
	| BOOL    { PushTypeExpr(MkBaseType(P_Root.UserCnstKind.BOOL,    ToSpan(@1))); }
	| INT     { PushTypeExpr(MkBaseType(P_Root.UserCnstKind.INT,     ToSpan(@1))); }
	| EVENT   { PushTypeExpr(MkBaseType(P_Root.UserCnstKind.EVENT,   ToSpan(@1))); }
	| MACHINE { PushTypeExpr(MkBaseType(P_Root.UserCnstKind.MACHINE, ToSpan(@1))); }						
	| MODEL   { PushTypeExpr(MkBaseType(P_Root.UserCnstKind.MODEL,   ToSpan(@1))); }						
	| FOREIGN { PushTypeExpr(MkBaseType(P_Root.UserCnstKind.FOREIGN, ToSpan(@1))); }						
	| ANY     { PushTypeExpr(MkBaseType(P_Root.UserCnstKind.ANY,     ToSpan(@1))); }
	| SEQ LBRACKET Type RBRACKET            { PushSeqType(ToSpan(@1)); }
	| MAP LBRACKET Type COMMA Type RBRACKET { PushMapType(ToSpan(@1)); }
	| LPAREN TupTypeList RPAREN	
	| LPAREN NmdTupTypeList RPAREN	
	;

TupTypeList
	: Type						{ PushTupType(ToSpan(@1), true);  }
	| Type COMMA TupTypeList	{ PushTupType(ToSpan(@1), false); }
	;

NmdTupTypeList
	: ID COLON Type						  { PushNmdTupType($1.str, ToSpan(@1), true);  }			
	| ID COLON Type COMMA NmdTupTypeList  { PushNmdTupType($1.str, ToSpan(@1), false); }	
	;
%%