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
	: ModuleDecl
	| NamedModuleDecl
	| TestDecl
	| ImplementationDecl
	;

/* Module Expression */
ModuleExpr
	: HideExpr
	| AssertExpr
	| AssumeExpr
	| ExportExpr
	| SafeExpr
	| RenameExpr
	| ComposeExpr
	| ID								{ PushModuleName($1.str, ToSpan(@1)); }
	;

/* Named Module Expr */
NamedModuleDecl
	: MODULE ID ASSIGN ModuleExpr SEMICOLON			{ AddModuleDef($2.str, ToSpan(@2), ToSpan(@1)); }
	;
/* Module */
ModuleDecl
	: MODULE ID ModulePrivateEvents LCBRACE MachineNamesList RCBRACE			{ AddModuleDecl($2.str, ToSpan(@2), ToSpan(@1)); }
	;

MachineNamesList
	: ID							{ AddToMachineNamesList($1.str, ToSpan(@1)); }
	| ID COMMA MachineNamesList		{ AddToMachineNamesList($1.str, ToSpan(@1)); }
	;

ModulePrivateEvents
	: PRIVATE NonDefaultEventList SEMICOLON		{ AddPrivatesList(ToSpan(@1)); }
	| PRIVATE SEMICOLON
	;

/* Composition */
ComposeExpr
	:  ModuleExpr LOR ModuleExpr		{ PushComposeExpr(ToSpan(@1)); }
	;

/* Hide */
HideExpr
	: LPAREN HIDE NonDefaultEventList IN ModuleExpr RPAREN		{ PushHideExpr(ToSpan(@1)); }
	;

/* Safe */
SafeExpr
	: LPAREN SAFE ModuleExpr RPAREN		{ PushSafeExpr(ToSpan(@1)); }
	;
/* Assert */
AssertExpr
	: LPAREN ASSERT MonitorNameList IN ModuleExpr RPAREN		{ PushAssertExpr(ToSpan(@1)); }
	;

/* Assume */
AssumeExpr
	: LPAREN ASSUME MonitorNameList IN ModuleExpr RPAREN		{ PushAssumeExpr(ToSpan(@1)); }
	;

/* Export */
ExportExpr
	: LPAREN EXPORT ID AS ID IN ModuleExpr RPAREN		{ PushExportExpr($3.str, $5.str, ToSpan(@3), ToSpan(@5), ToSpan(@1)); }
	;

/* Rename */
RenameExpr
	: LPAREN RENAME ID TO ID IN ModuleExpr RPAREN		{ PushRenameExpr($3.str, ToSpan(@3), $5.str, ToSpan(@5), ToSpan(@1)); }
	;

/* MonitorNameList */
MonitorNameList 
	: ID							{ PushMonitorName($1.str, ToSpan(@1), true); }
	| ID COMMA MonitorNameList		{ PushMonitorName($1.str, ToSpan(@1), false); }
	;

/* Test Declaration */
TestDecl
	: TEST ID COLON ModuleExpr SEMICOLON							{ AddTestDeclaration($2.str, ToSpan(@2), ToSpan(@1)); }
	| TEST ID COLON ModuleExpr REFINES ModuleExpr SEMICOLON		{ AddRefinementDeclaration($2.str, ToSpan(@2), ToSpan(@1)); }
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