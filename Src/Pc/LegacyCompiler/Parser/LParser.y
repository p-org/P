%namespace Microsoft.Pc.Parser
%using Microsoft.Pc.Domains;

%visibility internal
%YYSTYPE LexValue
%partial
%importtokens = tokensTokens.dat
%tokentype PTokens
%parsertype LParser

%%

LinkerProgram
	: EOF
	| TopDeclList
	;

TopDeclList
    : TopDecl
	| TopDeclList TopDecl 
	;

TopDecl
	: NamedModuleDecl
	| TestDecl
	| ImplementationDecl
	;

/* Module Expression */
ModuleExpr
	: HideExpr
	| AssertExpr
	| AssumeExpr
	| SafeExpr
	| RenameExpr
	| ComposeExpr
	| UnionExpr
	| PrimitiveModuleExpr
	| ID								{ PushModuleName($1.str, ToSpan(@1)); }
	;

UnionModuleExprList
	: ModuleExpr COMMA ModuleExpr			{ PushUnionExpr(ToSpan(@2)); }
	| ModuleExpr COMMA UnionModuleExprList	{ PushUnionExpr(ToSpan(@2)); }
	;

ComposeModuleExprList
	: ModuleExpr COMMA ModuleExpr				{ PushComposeExpr(ToSpan(@2)); }
	| ModuleExpr COMMA ComposeModuleExprList	{ PushComposeExpr(ToSpan(@2)); }
	;

/* Named Module Expr */
NamedModuleDecl
	: MODULE ID ASSIGN ModuleExpr SEMICOLON			{ AddModuleDef($2.str, ToSpan(@2), ToSpan(@1)); }
	;

/* Primitive Module */
PrimitiveModuleExpr
	: LCBRACE MachineNamesList RCBRACE			{ PushPrimitiveModule(ToSpan(@1)); }
	;

MachineBinds
	: ID							{ AddToMachineBindingList($1.str, $1.str, ToSpan(@1)); }
	| ID BIND ID					{ AddToMachineBindingList($1.str, $3.str, ToSpan(@1)); }
	;

MachineNamesList
	: MachineBinds
	| MachineBinds COMMA MachineNamesList
	;

/* Composition */
ComposeExpr
	:  LPAREN COMPOSE ComposeModuleExprList RPAREN
	;
/* Union */
UnionExpr
	:  LPAREN UNION UnionModuleExprList RPAREN
	;

/* Hide */
HideExpr
	: LPAREN HIDEE NonDefaultEventList IN ModuleExpr RPAREN		{ PushHideEventExpr(ToSpan(@1)); }
	| LPAREN HIDEI StringList IN ModuleExpr RPAREN				{ PushHideInterfaceExpr(ToSpan(@1)); }
	;

/* Safe */
SafeExpr
	: LPAREN SAFE ModuleExpr RPAREN		{ PushSafeExpr(ToSpan(@1)); }
	;

/* Assert */
AssertExpr
	: LPAREN ASSERT StringList IN ModuleExpr RPAREN		{ PushAssertExpr(ToSpan(@1)); }
	;

/* Assume */
AssumeExpr
	: LPAREN ASSUME StringList IN ModuleExpr RPAREN		{ PushAssumeExpr(ToSpan(@1)); }
	;


/* Rename */
RenameExpr
	: LPAREN RENAME ID TO ID IN ModuleExpr RPAREN		{ PushRenameExpr($3.str, ToSpan(@3), $5.str, ToSpan(@5), ToSpan(@1)); }
	;

/* StringList */
StringList 
	: ID							{ PushString($1.str, ToSpan(@1), true); }
	| ID COMMA StringList			{ PushString($1.str, ToSpan(@1), false); }
	;

/* Test Declaration */
TestDecl
	: TEST ID COLON MAIN ID IN ModuleExpr SEMICOLON											{ AddTestDeclaration($2.str, ToSpan(@2), $5.str, ToSpan(@5), ToSpan(@1)); }
	| TEST ID COLON MAIN ID IN ModuleExpr REFINES MAIN ID IN ModuleExpr SEMICOLON			{ AddRefinementDeclaration($2.str, ToSpan(@2), $5.str, ToSpan(@5), $10.str, ToSpan(@10), ToSpan(@1)); }
	;

/* Implementation Declaration */
ImplementationDecl
	: IMPLEMENTATION ModuleExpr SEMICOLON		{ AddImplementationDecl(ToSpan(@1)); }
	;

NonDefaultEventList
	: NonDefaultEventId
	| NonDefaultEventList COMMA NonDefaultEventId 
	;

NonDefaultEventId
	: ID        { AddToEventList($1.str, ToSpan(@1));                      }
	| HALT      { AddToEventList(PLink_Root.UserCnstKind.HALT, ToSpan(@1));    }
	;