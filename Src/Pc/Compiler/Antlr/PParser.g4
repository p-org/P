parser grammar PParser;
options { tokenVocab=PLexer; }

program : (topDecl | annotationSet)* ;

type : ANY LT eventSet=Iden GT    # BoundedType
     | SEQ LBRACK type RBRACK     # SeqType
     | MAP LBRACK keyType=type COMMA valueType=type RBRACK  # MapType
     | LPAREN tupTypes+=type (COMMA tupTypes+=type)* RPAREN # TupleType
     | LPAREN idenTypeList RPAREN # NamedTupleType
     | BOOL    # PrimitiveType
     | INT     # PrimitiveType
     | FLOAT   # PrimitiveType
     | EVENT   # PrimitiveType
     | MACHINE # PrimitiveType
     | DATA    # PrimitiveType
     | ANY     # PrimitiveType
     | Iden    # NamedType
     ;

idenTypeList : names+=Iden COLON types+=type (COMMA names+=Iden COLON types+=type)* ;

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

annotationSet : LBRACK (annotations+=annotation (',' annotations+=annotation)*)? RBRACK;
annotation : name=Iden ASSIGN value=NullLiteral
           | name=Iden ASSIGN value=BoolLiteral
           | name=Iden ASSIGN value=IntLiteral
           | name=Iden ASSIGN value=Iden
           ;

typeDefDecl : TYPE Iden SEMI # ForeignTypeDef
            | TYPE Iden ASSIGN type SEMI # PTypeDef
            ;

enumTypeDefDecl : ENUM Iden LBRACE idenList RBRACE
                | ENUM Iden LBRACE numberedEnumElemList RBRACE
                ;
idenList : Iden (COMMA Iden)* ;
numberedEnumElemList : names+=Iden ASSIGN values+=IntLiteral (COMMA names+=Iden ASSIGN values+=IntLiteral)* ;

eventDecl : EVENT Iden cardinality? (COLON type)? annotationSet? SEMI;
cardinality : ASSERT IntLiteral
            | ASSUME IntLiteral
            ;

eventSetDecl : EVENTSET Iden ASSIGN LBRACE nonDefaultEventList RBRACE SEMI ;

interfaceDecl : TYPE Iden LPAREN type? RPAREN ASSIGN Iden SEMI
              | TYPE Iden LPAREN type? RPAREN ASSIGN LBRACE nonDefaultEventList RBRACE SEMI
              ;

nonDefaultEventList : events+=(HALT | Iden) (COMMA events+=(HALT | Iden))* ;

implMachineDecl : MACHINE Iden cardinality? annotationSet? (COLON idenList)? receivesSends* machineBody ;
receivesSends : RECEIVES nonDefaultEventList? SEMI
              | SENDS nonDefaultEventList? SEMI
              ;

implMachineProtoDecl : EXTERN MACHINE Iden LPAREN type? RPAREN SEMI;

specMachineDecl : SPEC Iden OBSERVES nonDefaultEventList machineBody ;

machineBody : LBRACE machineEntry* RBRACE;
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

stateDecl : START? temperature=(HOT | COLD)? STATE Iden annotationSet? LBRACE stateBodyItem* RBRACE ;

stateBodyItem : ENTRY anonEventHandler # StateEntry
              | ENTRY Iden SEMI        # StateEntry
              | EXIT statementBlock    # StateExit
              | EXIT Iden SEMI         # StateExit
              | DEFER nonDefaultEventList annotationSet? SEMI    # StateDefer
              | IGNORE nonDefaultEventList annotationSet? SEMI   # StateIgnore
              | ON eventList DO Iden annotationSet? SEMI # OnEventDoAction
              | ON eventList DO annotationSet? anonEventHandler # OnEventDoAction
              | ON eventList PUSH stateName annotationSet? SEMI # OnEventPushState
              | ON eventList GOTO stateName annotationSet? SEMI # OnEventGotoState
              | ON eventList GOTO stateName annotationSet? WITH anonEventHandler # OnEventGotoState
              | ON eventList GOTO stateName annotationSet? WITH Iden SEMI        # OnEventGotoState
              ;

eventList : eventId (COMMA eventId)* ;
eventId : NullLiteral | HALT | Iden ;

stateName : Iden (DOT Iden)* ;

statementBlock : LBRACE varDecl* statement* RBRACE ;
statement : LBRACE statement* RBRACE
          | POP SEMI
          | ASSERT expr (COMMA StringLiteral)? SEMI
          | PRINT StringLiteral (COMMA rvalueList)? SEMI
          | RETURN expr? SEMI
          | lvalue ASSIGN rvalue SEMI
          | lvalue INSERT rvalue SEMI
          | lvalue REMOVE expr SEMI
          | WHILE LPAREN expr RPAREN statement
          | IF LPAREN expr RPAREN statement (ELSE statement)?
          | NEW Iden LPAREN rvalueList? RPAREN SEMI
          | Iden LPAREN rvalueList? RPAREN SEMI
          | RAISE expr (COMMA rvalueList)? SEMI
          | SEND expr COMMA expr (COMMA rvalueList)? SEMI
          | ANNOUNCE expr (COMMA rvalueList)? SEMI
          | GOTO stateName (COMMA rvalueList)? SEMI
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

expr : primitive # PrimitiveExpr
     | LPAREN unnamedTupleBody RPAREN # UnnamedTupleExpr
     | LPAREN namedTupleBody RPAREN # NamedTupleExpr
     | LPAREN expr RPAREN # ParenExpr
     | expr DOT field=Iden # TupleAccessExpr
     | expr DOT field=IntLiteral # TupleAccessExpr
     | expr LBRACK expr RBRACK # SeqAccessExpr
     | fun=KEYS LPAREN expr RPAREN # KeywordExpr
     | fun=VALUES LPAREN expr RPAREN # KeywordExpr
     | fun=SIZEOF LPAREN expr RPAREN # KeywordExpr
     | fun=DEFAULT LPAREN type RPAREN # KeywordExpr
     | NEW Iden LPAREN rvalueList? RPAREN #CtorExpr
     | Iden LPAREN rvalueList? RPAREN # FunCallExpr
     | op=(SUB | LNOT) expr # UnaryExpr
     | expr op=(MUL | DIV) expr # BinExpr
     | expr op=(ADD | SUB) expr # BinExpr
     | expr cast=(AS | TO) type # CastExpr
     | expr op=(LT | GT | GE | LE | IN) expr # BinExpr
     | expr op=(EQ | NE) expr # BinExpr
     | expr op=LAND expr # BinExpr
     | expr op=LOR expr # BinExpr
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

floatLiteral : pre=IntLiteral? DOT post=IntLiteral # DecimalFloat
             | FLOAT LPAREN base=IntLiteral COMMA exp=IntLiteral RPAREN # ExpFloat
             ;

unnamedTupleBody : fields+=rvalue COMMA
                 | fields+=rvalue (COMMA fields+=rvalue)+
                 ;

namedTupleBody : names+=Iden ASSIGN values+=rvalue COMMA
               | names+=Iden ASSIGN values+=rvalue (COMMA names+=Iden ASSIGN values+=rvalue)+
               ;

rvalueList : rvalue (COMMA rvalue)* ;
rvalue : Iden linear=(SWAP | MOVE)
       | expr
       ;
