parser grammar PParser;
options { tokenVocab=PLexer; }

program : (topDecl | annotationSet)* ;

type : BOOL | INT | FLOAT | EVENT | MACHINE | DATA | Iden
     | ANY (LT eventSet=Iden GT)?
     | SEQ LBRACK type RBRACK
     | MAP LBRACK type COMMA type RBRACK
     | LPAREN typeList RPAREN
     | LPAREN idenTypeList RPAREN
     ;

typeList : type (COMMA type)* ;

idenTypeList : idenType (COMMA idenType)* ;
idenType : Iden COLON type ;

topDecl : typeDefDecl
        | enumTypeDefDecl
        | eventDecl
        | eventSetDecl
        | interfaceDecl
        | implMachineDecl
        | implMachineProtoDecl
        | specMachineDecl
        | funDecl
        | funProtoDecl
        ;

annotationSet : LBRACK annotationList? RBRACK;
annotationList : annotation (',' annotation)* ;
annotation : Iden ASSIGN NullLiteral
           | Iden ASSIGN BoolLiteral
           | Iden ASSIGN IntLiteral
           | Iden ASSIGN Iden
           ;

typeDefDecl : TYPE Iden (ASSIGN type)? SEMI ;

enumTypeDefDecl : ENUM Iden LBRACE idenList RBRACE
                | ENUM Iden LBRACE numberedEnumElemList RBRACE
                ;
idenList : Iden (COMMA Iden)* ;
numberedEnumElemList : numberedEnumItem (COMMA numberedEnumItem)* ;
numberedEnumItem : Iden ASSIGN IntLiteral ;

eventDecl : EVENT Iden cardinality? (COLON type)? annotationSet? SEMI;
cardinality : ASSERT IntLiteral
            | ASSUME IntLiteral
            ;

eventSetDecl : EVENTSET Iden ASSIGN LBRACE nonDefaultEventList RBRACE SEMI ;

interfaceDecl : TYPE Iden LPAREN type? RPAREN ASSIGN Iden SEMI
              | TYPE Iden LPAREN type? RPAREN ASSIGN LBRACE nonDefaultEventList RBRACE SEMI
              ;

nonDefaultEventList : nonDefaultEventId (COMMA nonDefaultEventId)* ;
nonDefaultEventId : HALT | Iden ;

implMachineDecl : MACHINE Iden cardinality? annotationSet? (COLON idenList)? receivesSends* LBRACE machineBody RBRACE ;
receivesSends : RECEIVES nonDefaultEventList? SEMI
              | SENDS nonDefaultEventList? SEMI
              ;

implMachineProtoDecl : EXTERN MACHINE Iden LPAREN type? RPAREN SEMI;

specMachineDecl : SPEC Iden OBSERVES nonDefaultEventList LBRACE machineBody RBRACE ;

machineBody : machineEntry*;
machineEntry : varDecl
             | funDecl
             | group
             | stateDecl
             ;

varDecl : VAR idenList COLON type annotationSet? SEMI ;

funDecl : FUN Iden funParams (COLON type)? annotationSet? (SEMI | statementBlock) ;
funProtoDecl : EXTERN FUN Iden (CREATES idenList? SEMI)? funParams (COLON type)? annotationSet? SEMI;
funParams : LPAREN idenTypeList? RPAREN;

group : GROUP Iden LBRACE groupItem* RBRACE ;
groupItem : stateDecl
          | group
          ;

stateDecl : START? (HOT | COLD)? STATE Iden annotationSet? LBRACE stateBodyItem* RBRACE ;

stateBodyItem : ENTRY anonEventHandler
              | ENTRY Iden SEMI
              | EXIT statementBlock
              | EXIT Iden SEMI
              | DEFER nonDefaultEventList annotationSet? SEMI
              | IGNORE nonDefaultEventList annotationSet? SEMI
              | ON eventList DO Iden annotationSet? SEMI
              | ON eventList DO annotationSet? anonEventHandler // allow optional SEMI here?
              | ON eventList PUSH qualifiedName annotationSet? SEMI
              | ON eventList GOTO qualifiedName annotationSet? SEMI
              | ON eventList GOTO qualifiedName annotationSet? WITH anonEventHandler // allow optional SEMI here?
              | ON eventList GOTO qualifiedName annotationSet? WITH Iden SEMI
              ;

eventList : eventId (COMMA eventId)* ;
eventId : NullLiteral | HALT | Iden ;

qualifiedName : Iden (DOT Iden)* ;

statementBlock : LBRACE varDecl* statement* RBRACE ;
statement : LBRACE statement* RBRACE
          | POP SEMI
          | ASSERT expr (COMMA StringLiteral)? SEMI
          | PRINT StringLiteral (COMMA exprArgList)? SEMI
          | RETURN expr? SEMI
          | lvalue ASSIGN exprArg SEMI
          | lvalue INSERT exprArg SEMI
          | lvalue REMOVE expr SEMI
          | WHILE LPAREN expr RPAREN statement
          | IF LPAREN expr RPAREN statement (ELSE statement)?
          | NEW Iden LPAREN exprArgList? RPAREN SEMI
          | Iden LPAREN exprArgList? RPAREN SEMI
          | RAISE expr (COMMA exprArgList)? SEMI
          | SEND expr COMMA expr (COMMA exprArgList)? SEMI
          | ANNOUNCE expr (COMMA exprArgList)? SEMI
          | GOTO qualifiedName (COMMA exprArgList)? SEMI
          | RECEIVE LBRACE recvCase+ RBRACE
          | SEMI
          ;

lvalue : Iden
       | lvalue DOT Iden
       | lvalue DOT IntLiteral
       | lvalue LBRACK expr RBRACK
       ;

recvCase : CASE eventList COLON anonEventHandler ;
anonEventHandler : payloadVarDecl? statementBlock ;
payloadVarDecl : LPAREN Iden COLON type RPAREN ;

expr : primitive
     | LPAREN unnamedTupleBody RPAREN
     | LPAREN namedTupleBody RPAREN
     | LPAREN expr RPAREN
     | expr DOT field=Iden
     | expr DOT field=IntLiteral
     | expr LBRACK expr RBRACK
     | KEYS LPAREN expr RPAREN
     | VALUES LPAREN expr RPAREN
     | SIZEOF LPAREN expr RPAREN
     | DEFAULT LPAREN type RPAREN
     | NEW Iden LPAREN exprArgList? RPAREN
     | funName=Iden LPAREN exprArgList? RPAREN
     | unop=(SUB | LNOT) expr
     | expr binop=(MUL | DIV) expr
     | expr binop=(ADD | SUB) expr
     | expr cast=(AS | TO) type
     | expr binop=(LT | GT | GE | LE | IN) expr
     | expr binop=(EQ | NE) expr
     | expr binop=LAND expr
     | expr binop=LOR expr
     ;

primitive : Iden
          | floatLiteral
          | BoolLiteral
          | IntLiteral
          | NullLiteral
          | NONDET
          | FAIRNONDET
          | HALT
          | THIS
          ;

floatLiteral : IntLiteral DOT IntLiteral
             | DOT IntLiteral
             | FLOAT LPAREN IntLiteral COMMA IntLiteral RPAREN
             ;

unnamedTupleBody : exprArg COMMA
                 | exprArg (COMMA exprArg)*
                 ;

namedTupleBody : namedTupleField COMMA
               | namedTupleField (COMMA namedTupleField)+
               ;

namedTupleField : Iden ASSIGN exprArg ;

exprArgList : exprArg (COMMA exprArg)* ;
exprArg : Iden linear=(SWAP | MOVE)
        | expr
        ;
