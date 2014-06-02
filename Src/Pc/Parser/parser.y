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

%token VAR START STABLE MODEL STATE FUN ACTION GROUP

%token ENTRY EXIT DEFER IGNORE GOTO ON DO PUSH AS

%token IF WHILE THIS TRIGGER PAYLOAD NEW RETURN ID POP ASSERT CALL INVOKE RAISE SEND DEFAULT HALT NULL 
%token LPAREN RPAREN LCBRACE RCBRACE LBRACKET RBRACKET SIZEOF KEYS VALUES

%token TRUE FALSE

%token ASSIGN REMOVE
%token EQ NE LT GT LE GE IN
%left LAND LNOT LOR NONDET FAIRNONDET

%token DOT COLON COMMA
%left  SEMICOLON

%token INT REAL BOOL

%left  PLUS MINUS
%left  DIV
%left  MUL 
%token ELSE

%token maxParseToken 
%token LEX_WHITE LEX_ERROR LEX_COMMENT

%%

Program
    : EOF
	| TopDeclList
	| AnnotationSet                { AddProgramAnnots(ToSpan(@1)); }
	| AnnotationSet TopDeclList    { AddProgramAnnots(ToSpan(@1)); }
	;

TopDeclList
    : TopDecl
	| TopDeclList TopDecl 
	;

TopDecl
    : EventDecl
	| MachineDecl
	;

/******************* Annotations *******************/ 
AnnotationSet
    : LBRACKET RBRACKET                  { PushAnnotationSet(); }
	| LBRACKET AnnotationList RBRACKET   { PushAnnotationSet(); }
	;

AnnotationList
    : Annotation
	| AnnotationList COMMA Annotation
	;

Annotation
    : ID ASSIGN NULL    { AddAnnotUsrCnstVal($1.str, P_Root.UserCnstKind.NULL, ToSpan(@1), ToSpan(@3));  }
	| ID ASSIGN TRUE    { AddAnnotUsrCnstVal($1.str, P_Root.UserCnstKind.TRUE, ToSpan(@1), ToSpan(@3));  }
	| ID ASSIGN FALSE   { AddAnnotUsrCnstVal($1.str, P_Root.UserCnstKind.FALSE, ToSpan(@1), ToSpan(@3)); }
	| ID ASSIGN ID      { AddAnnotStringVal($1.str, $3.str, ToSpan(@1), ToSpan(@3));                     }
	| ID ASSIGN INT     { AddAnnotIntVal($1.str, $3.str, ToSpan(@1), ToSpan(@3));                        }
	;

/******************* Event Declarations *******************/ 
EventDecl
	: EVENT ID EvCardOrNone EvTypeOrNone EventAnnotOrNone SEMICOLON { AddEvent($2.str, ToSpan(@2), ToSpan(@1)); }
	;

EvCardOrNone
	: ASSERT INT									{ SetEventCard($2.str, true,  ToSpan(@1)); }
	| ASSUME INT									{ SetEventCard($2.str, false, ToSpan(@1)); }
	|												{ }
	;

EvTypeOrNone
	: COLON Type									{ SetEventType(ToSpan(@1));                }
	|												{ }
	;

EventAnnotOrNone
    : AnnotationSet                                 { AddEventAnnots(ToSpan(@1));              }
	|
	;

/******************* Machine Declarations *******************/
MachineDecl
	: IsMain MACHINE ID MachCardOrNone MachAnnotOrNone LCBRACE MachineBody RCBRACE { AddMachine(P_Root.UserCnstKind.REAL, $3.str, ToSpan(@3), ToSpan(@1));    }
	| IsMain MODEL ID MachCardOrNone MachAnnotOrNone LCBRACE MachineBody RCBRACE   { AddMachine(P_Root.UserCnstKind.MODEL, $3.str, ToSpan(@3), ToSpan(@1));   }
	| MONITOR ID MachCardOrNone MachAnnotOrNone LCBRACE MachineBody RCBRACE        { AddMachine(P_Root.UserCnstKind.MONITOR, $2.str, ToSpan(@2), ToSpan(@1)); }
	;

IsMain
	: MAIN											{ SetMachineIsMain(ToSpan(@1)); }
	|												{ }
	;

MachCardOrNone
	: ASSERT INT									{ SetMachineCard($2.str, true,  ToSpan(@1)); }
	| ASSUME INT									{ SetMachineCard($2.str, false, ToSpan(@1)); }
	|												{ }
	;

MachAnnotOrNone
    : AnnotationSet                                 { AddMachineAnnots(ToSpan(@1));              }
	|
	;

/******************* Machine Bodies *******************/
MachineBody
	: MachineBodyItem												
	| MachineBody MachineBodyItem 					
	;

MachineBodyItem
	: VarDecl
	| FunDecl
	| StateDecl
	| Group
	;

/******************* Variable Declarations *******************/
VarDecl
	: VAR VarList COLON Type SEMICOLON	             { AddVarDecls(false, ToSpan(@1)); }
	| VAR VarList COLON Type AnnotationSet SEMICOLON { AddVarDecls(true,  ToSpan(@5)); }
	;

VarList
	: ID                  { AddVarDecl($1.str, ToSpan(@1)); }									
	| ID COMMA VarList    { AddVarDecl($1.str, ToSpan(@1)); }
	;

/******************* Function Declarations *******************/

FunDecl
	: IsModel FUN ID ParamsOrNone RetTypeOrNone FunAnnotOrNone StmtBlock { AddFunction($3.str, ToSpan(@3), ToSpan(@1)); }
	;

IsModel
	: MODEL											{ SetFunKind(P_Root.UserCnstKind.MODEL, ToSpan(@1)); }
	|												{ }
	;

FunAnnotOrNone
    : AnnotationSet { AddFunAnnots(ToSpan(@1)); }
	|
	;

ParamsOrNone
    : LPAREN RPAREN
	| LPAREN NmdTupTypeList RPAREN                  { SetFunParams(ToSpan(@1)); }
	;

RetTypeOrNone
    : COLON Type                                    { SetFunReturn(ToSpan(@1)); }
	| 
	;

/*******************       Group        *******************/
Group
    : GroupName LCBRACE RCBRACE             { AddGroup(); }      
    | GroupName LCBRACE GroupBody RCBRACE   { AddGroup(); }
	;

GroupBody
    : GroupItem
	| GroupBody GroupItem 
	;

GroupItem
    : StateDecl
	| Group
	;

GroupName
    : GROUP ID	{ PushGroup($2.str, ToSpan(@2), ToSpan(@1)); }
	;

/******************* State Declarations *******************/
StateDecl
	: IsStable STATE ID StateAnnotOrNone LCBRACE RCBRACE                  { AddState($3.str, false, ToSpan(@3), ToSpan(@1)); }
	| IsStable STATE ID StateAnnotOrNone LCBRACE StateBody RCBRACE        { AddState($3.str, false, ToSpan(@3), ToSpan(@1)); }	  
	| START IsStable STATE ID StateAnnotOrNone LCBRACE RCBRACE            { AddState($4.str, true,  ToSpan(@4), ToSpan(@1)); }
	| START IsStable STATE ID StateAnnotOrNone LCBRACE StateBody RCBRACE  { AddState($4.str, true,  ToSpan(@4), ToSpan(@1)); }	  
	;

IsStable
	: STABLE        { SetStateIsStable(ToSpan(@1)); }
	|
	;

StateAnnotOrNone
    : AnnotationSet { AddStateAnnots(ToSpan(@1)); }
	|
	;

StateBody
	: StateBodyItem
	| StateBodyItem StateBody 
	;

StateBodyItem
	: ENTRY StmtBlock                                                   { SetStateEntry();                           }				
	| EXIT StmtBlock								                    { SetStateExit();                            }
	| DEFER NonDefaultEventList TrigAnnotOrNone SEMICOLON               { AddDefersOrIgnores(true,  ToSpan(@1));     }			
	| IGNORE NonDefaultEventList TrigAnnotOrNone SEMICOLON			    { AddDefersOrIgnores(false, ToSpan(@1));     }
	| ON EventList DO ID TrigAnnotOrNone SEMICOLON                      { AddAction($4.str, ToSpan(@4), ToSpan(@1)); }
	| ON EventList PUSH QualifiedId TrigAnnotOrNone SEMICOLON           { AddTransition(true, false, ToSpan(@1));    }
 	| ON EventList GOTO QualifiedId TrigAnnotOrNone SEMICOLON           { AddTransition(false, false, ToSpan(@1));   } 
	| ON EventList GOTO QualifiedId TrigAnnotOrNone StmtBlock SEMICOLON { AddTransition(false, true, ToSpan(@1));    }
	;

NonDefaultEventList
	: NonDefaultEventId
	| NonDefaultEventList COMMA NonDefaultEventId 
	;

EventList
	: EventId
	| EventList COMMA EventId
	;

EventId
	: ID        { AddToEventList($1.str, ToSpan(@1));                      }
	| HALT      { AddToEventList(P_Root.UserCnstKind.HALT, ToSpan(@1));    }
	| DEFAULT   { AddToEventList(P_Root.UserCnstKind.DEFAULT, ToSpan(@1)); }
	;

NonDefaultEventId
	: ID        { AddToEventList($1.str, ToSpan(@1));                      }
	| HALT      { AddToEventList(P_Root.UserCnstKind.HALT, ToSpan(@1));    }
	;

TrigAnnotOrNone
    : AnnotationSet  { SetTrigAnnotated(ToSpan(@1)); }
	|
	;

/******************* Type Expressions *******************/

Type
	: NULL                                  { PushTypeExpr(MkBaseType(P_Root.UserCnstKind.NULL,    ToSpan(@1))); }
	| BOOL                                  { PushTypeExpr(MkBaseType(P_Root.UserCnstKind.BOOL,    ToSpan(@1))); }
	| INT                                   { PushTypeExpr(MkBaseType(P_Root.UserCnstKind.INT,     ToSpan(@1))); }
	| EVENT                                 { PushTypeExpr(MkBaseType(P_Root.UserCnstKind.EVENT,   ToSpan(@1))); }
	| MACHINE                               { PushTypeExpr(MkBaseType(P_Root.UserCnstKind.MACHINE, ToSpan(@1))); }						
	| MODEL                                 { PushTypeExpr(MkBaseType(P_Root.UserCnstKind.MODEL,   ToSpan(@1))); }						
	| FOREIGN                               { PushTypeExpr(MkBaseType(P_Root.UserCnstKind.FOREIGN, ToSpan(@1))); }						
	| ANY                                   { PushTypeExpr(MkBaseType(P_Root.UserCnstKind.ANY,     ToSpan(@1))); }
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

/******************* Statements *******************/

Stmt
	: SEMICOLON                                               { PushNulStmt(P_Root.UserCnstKind.SKIP,  ToSpan(@1));      }
	| LCBRACE RCBRACE                                         { PushNulStmt(P_Root.UserCnstKind.SKIP,  ToSpan(@1));      }
	| POP SEMICOLON                                           { PushNulStmt(P_Root.UserCnstKind.POP,   ToSpan(@1));      }
	| LCBRACE StmtList RCBRACE                                { }
	| PUSH QualifiedId SEMICOLON                              { PushPush(ToSpan(@1));                                    }
	| ASSERT Exp SEMICOLON                                    { PushUnStmt(P_Root.UserCnstKind.ASSERT, ToSpan(@1));      }
	| RETURN SEMICOLON                                        { PushReturn(false, ToSpan(@1));                           }
	| RETURN Exp SEMICOLON                                    { PushReturn(true, ToSpan(@1));                            }
	| Exp ASSIGN Exp SEMICOLON                                { PushBinStmt(P_Root.UserCnstKind.ASSIGN, ToSpan(@1));     }
	| Exp REMOVE Exp SEMICOLON                                { PushBinStmt(P_Root.UserCnstKind.REMOVE, ToSpan(@1));     }
	| WHILE LPAREN Exp RPAREN Stmt                            { PushWhile(ToSpan(@1));                                   }
	| IF LPAREN Exp RPAREN Stmt ELSE Stmt %prec ELSE          { PushIte(true, ToSpan(@1));                               }					
	| IF LPAREN Exp RPAREN Stmt		                          { PushIte(false, ToSpan(@1));                              }
	| NEW ID LPAREN RPAREN SEMICOLON                          { PushNewStmt($2.str, false, ToSpan(@2), ToSpan(@1));      }
	| NEW ID LPAREN SingleExprArgList RPAREN SEMICOLON        { PushNewStmt($2.str, true,  ToSpan(@2), ToSpan(@1));      }
	| ID LPAREN RPAREN SEMICOLON                              { PushFunStmt($1.str, false, ToSpan(@1));                  }
	| ID LPAREN ExprArgList RPAREN SEMICOLON                  { PushFunStmt($1.str, true,  ToSpan(@1));                  }						
	| RAISE Exp SEMICOLON                                     { PushRaise(false, ToSpan(@1));                            }
	| RAISE Exp COMMA SingleExprArgList SEMICOLON             { PushRaise(true,  ToSpan(@1));                            }
	| SEND Exp COMMA Exp SEMICOLON                            { PushSend(false, ToSpan(@1));                             }
	| SEND Exp COMMA Exp COMMA SingleExprArgList SEMICOLON    { PushSend(true,  ToSpan(@1));                             }
	| MONITOR ID COMMA Exp SEMICOLON                          { PushMonitor(false, $2.str, ToSpan(@2), ToSpan(@1));      }
	| MONITOR ID COMMA Exp COMMA SingleExprArgList SEMICOLON  { PushMonitor(true, $2.str, ToSpan(@2), ToSpan(@1));       }
	;

StmtBlock
	: LCBRACE RCBRACE                                         { PushNulStmt(P_Root.UserCnstKind.SKIP,  ToSpan(@1));      }    
    | LCBRACE StmtList RCBRACE
	;

StmtList
	: Stmt
	| Stmt StmtList    { PushSeq(); }													
	;

QualifiedId
    : ID                  { Qualify($1.str, ToSpan(@1)); }
	| QualifiedId DOT ID  { Qualify($3.str, ToSpan(@3)); }
	;

/******************* Value Expressions *******************/

Exp
  : Exp_8
  ;

Exp_8 
	: Exp_8 LOR Exp_7	{ PushBinExpr(P_Root.UserCnstKind.OR, ToSpan(@1)); }
	| Exp_7
	;

Exp_7
	: Exp_7 LAND Exp_6	{ PushBinExpr(P_Root.UserCnstKind.AND, ToSpan(@1)); }
	| Exp_6
	;

Exp_6 
	: Exp_5 EQ Exp_5 { PushBinExpr(P_Root.UserCnstKind.EQ,  ToSpan(@1)); }
	| Exp_5 NE Exp_5 { PushBinExpr(P_Root.UserCnstKind.NEQ, ToSpan(@1)); }
	| Exp_5
	;

Exp_5 
	: Exp_4 LT Exp_4 { PushBinExpr(P_Root.UserCnstKind.LT, ToSpan(@1)); }
	| Exp_4 LE Exp_4 { PushBinExpr(P_Root.UserCnstKind.LE, ToSpan(@1)); }
	| Exp_4 GT Exp_4 { PushBinExpr(P_Root.UserCnstKind.GT, ToSpan(@1)); }
	| Exp_4 GE Exp_4 { PushBinExpr(P_Root.UserCnstKind.GE, ToSpan(@1)); }
	| Exp_4 IN Exp_4 { PushBinExpr(P_Root.UserCnstKind.IN, ToSpan(@1)); }
	| Exp_4
	;

Exp_4 
	: Exp_4 AS Type { PushCast(ToSpan(@1)); }	
	| Exp_3
	;

Exp_3 
	: Exp_3 PLUS Exp_2   { PushBinExpr(P_Root.UserCnstKind.ADD, ToSpan(@1)); }	
	| Exp_3 MINUS Exp_2  { PushBinExpr(P_Root.UserCnstKind.SUB, ToSpan(@1)); }
	| Exp_2
	;

Exp_2 
	: Exp_2 MUL Exp_1  { PushBinExpr(P_Root.UserCnstKind.MUL,    ToSpan(@1)); }	
	| Exp_2 DIV Exp_1  { PushBinExpr(P_Root.UserCnstKind.INTDIV, ToSpan(@1)); }
	| Exp_1
	;

Exp_1 
	: MINUS Exp_0 { PushUnExpr(P_Root.UserCnstKind.NEG, ToSpan(@1)); }
	| LNOT  Exp_0 { PushUnExpr(P_Root.UserCnstKind.NOT, ToSpan(@1)); }
	| Exp_0
	;

Exp_0 
    : TRUE                                   { PushNulExpr(P_Root.UserCnstKind.TRUE,       ToSpan(@1)); }
    | FALSE                                  { PushNulExpr(P_Root.UserCnstKind.FALSE,      ToSpan(@1)); }
    | THIS                                   { PushNulExpr(P_Root.UserCnstKind.THIS,       ToSpan(@1)); }
    | TRIGGER                                { PushNulExpr(P_Root.UserCnstKind.TRIGGER,    ToSpan(@1)); }
    | PAYLOAD                                { PushNulExpr(P_Root.UserCnstKind.PAYLOAD,    ToSpan(@1)); }
    | NONDET                                 { PushNulExpr(P_Root.UserCnstKind.NONDET,     ToSpan(@1)); }
    | FAIRNONDET                             { PushNulExpr(P_Root.UserCnstKind.FAIRNONDET, ToSpan(@1)); }
    | NULL                                   { PushNulExpr(P_Root.UserCnstKind.NULL,       ToSpan(@1)); }
    | HALT                                   { PushNulExpr(P_Root.UserCnstKind.HALT,       ToSpan(@1)); }
	| INT                                    { PushIntExpr($1.str, ToSpan(@1));                         }
    | ID                                     { PushName($1.str,    ToSpan(@1));                         }         
	| Exp_0 DOT ID                           { PushField($3.str,   ToSpan(@3));                         }   
	| Exp_0 LBRACKET Exp RBRACKET            { PushBinExpr(P_Root.UserCnstKind.IDX,        ToSpan(@1)); }
	| LPAREN Exp RPAREN                      { }
    | KEYS LPAREN Exp RPAREN                 { PushUnExpr(P_Root.UserCnstKind.KEYS,   ToSpan(@1));      }
    | VALUES  LPAREN Exp RPAREN              { PushUnExpr(P_Root.UserCnstKind.VALUES, ToSpan(@1));      }
    | SIZEOF  LPAREN Exp RPAREN              { PushUnExpr(P_Root.UserCnstKind.SIZEOF, ToSpan(@1));      }
    | DEFAULT LPAREN Type RPAREN             { PushDefaultExpr(ToSpan(@1));                             }
	| NEW ID LPAREN RPAREN                   { PushNewExpr($2.str, false, ToSpan(@2), ToSpan(@1));      }
	| NEW ID LPAREN SingleExprArgList RPAREN { PushNewExpr($2.str, true, ToSpan(@2), ToSpan(@1));       }
	| LPAREN Exp COMMA             RPAREN    { PushTupleExpr(true);                                     }
	| LPAREN Exp COMMA ExprArgList RPAREN    { PushTupleExpr(false);                                    }
	| ID LPAREN RPAREN                       { PushFunExpr($1.str, false, ToSpan(@1));                  }
	| ID LPAREN ExprArgList RPAREN           { PushFunExpr($1.str, true, ToSpan(@1));                   }
	| LPAREN ID ASSIGN Exp COMMA RPAREN      { PushNmdTupleExpr($2.str, ToSpan(@2), true);              }
	| LPAREN ID ASSIGN Exp COMMA 
	  NmdExprArgList       RPAREN            { PushNmdTupleExpr($2.str, ToSpan(@2), false);             }
	;

// An arg list that can be a single expr, or an exprs
SingleExprArgList
	: Exp					      { MoveValToExprs(false); }
	| Exp COMMA SingleExprArgList { PushExprs();           }
	;

// An arg list that is always packed into an exprs.
ExprArgList
	: Exp					{ MoveValToExprs(true);  }
	| Exp COMMA ExprArgList { PushExprs();           }
	;

// A named arg list that is always packed into named exprs.
NmdExprArgList
	: ID ASSIGN Exp		                 { MoveValToNmdExprs($1.str, ToSpan(@1));  }
	| ID ASSIGN Exp COMMA NmdExprArgList { PushNmdExprs($1.str, ToSpan(@1));       }
	;

%%