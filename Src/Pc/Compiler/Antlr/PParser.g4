parser grammar PParser;
options { tokenVocab=PLexer; }

program : (topDecl | annotationSet)* ;

type : ANY LT eventSet=Iden GT    # BoundedType
     | SEQ LBRACK type RBRACK     # SeqType
     | MAP LBRACK keyType=type COMMA valueType=type RBRACK  # MapType
     | LPAREN tupTypes+=type (COMMA tupTypes+=type)* RPAREN # TupleType
     | LPAREN idenTypeList RPAREN # NamedTupleType
     | BOOL      # PrimitiveType
     | INT       # PrimitiveType
     | FLOAT     # PrimitiveType
     | EVENT     # PrimitiveType
     | MACHINE   # PrimitiveType
     | DATA      # PrimitiveType
     | ANY       # PrimitiveType
     | name=Iden # NamedType
     ;

idenTypeList : idenType (COMMA idenType)* ;
idenType : name=Iden COLON type ;

funParamList : funParam (COMMA funParam)* ;
funParam : name=Iden COLON type ;

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

annotationSet : LBRACK (annotations+=annotation (COMMA annotations+=annotation)*)? RBRACK;
annotation : name=Iden ASSIGN value=NullLiteral
           | name=Iden ASSIGN value=BoolLiteral
           | name=Iden ASSIGN value=IntLiteral
           | name=Iden ASSIGN value=Iden
           ;

typeDefDecl : TYPE name=Iden SEMI # ForeignTypeDef
            | TYPE name=Iden ASSIGN type SEMI # PTypeDef
            ;

enumTypeDefDecl : ENUM name=Iden LBRACE enumElemList RBRACE
                | ENUM name=Iden LBRACE numberedEnumElemList RBRACE
                ;
enumElemList : enumElem (COMMA enumElem)* ;
enumElem : name=Iden ;
numberedEnumElemList : numberedEnumElem (COMMA numberedEnumElem)* ;
numberedEnumElem : name=Iden ASSIGN value=IntLiteral ;

eventDecl : EVENT name=Iden cardinality? (COLON type)? annotationSet? SEMI;
cardinality : ASSERT IntLiteral
            | ASSUME IntLiteral
            ;

eventSetDecl : EVENTSET name=Iden ASSIGN LBRACE nonDefaultEventList RBRACE SEMI ;

interfaceDecl : TYPE name=Iden LPAREN type? RPAREN ASSIGN eventSet=Iden SEMI
              | TYPE name=Iden LPAREN type? RPAREN ASSIGN LBRACE nonDefaultEventList RBRACE SEMI
              ;

nonDefaultEventList : events+=(HALT | Iden) (COMMA events+=(HALT | Iden))* ;

implMachineDecl : MACHINE name=Iden cardinality? annotationSet? (COLON idenList)? receivesSends* machineBody ;
idenList : names+=Iden (COMMA names+=Iden)* ;
receivesSends : RECEIVES nonDefaultEventList? SEMI
              | SENDS nonDefaultEventList? SEMI
              ;

implMachineProtoDecl : EXTERN MACHINE name=Iden LPAREN type? RPAREN SEMI;

specMachineDecl : SPEC name=Iden OBSERVES nonDefaultEventList machineBody ;

machineBody : LBRACE machineEntry* RBRACE;
machineEntry : varDecl
             | funDecl
             | group
             | stateDecl
             ;

varDecl : VAR idenList COLON type annotationSet? SEMI ;

funDecl : FUN name=Iden LPAREN funParamList? RPAREN (COLON type)? annotationSet? (SEMI | statementBlock) ;
funProtoDecl : EXTERN FUN name=Iden (CREATES idenList? SEMI)? LPAREN funParamList? RPAREN (COLON type)? annotationSet? SEMI;

group : GROUP name=Iden LBRACE groupItem* RBRACE ;
groupItem : stateDecl
          | group
          ;

stateDecl : START? temperature=(HOT | COLD)? STATE name=Iden annotationSet? LBRACE stateBodyItem* RBRACE ;

stateBodyItem : ENTRY anonEventHandler       # StateEntry
              | ENTRY funName=Iden SEMI      # StateEntry
              | EXIT noParamAnonEventHandler # StateExit
              | EXIT funName=Iden SEMI       # StateExit
              | DEFER nonDefaultEventList annotationSet? SEMI    # StateDefer
              | IGNORE nonDefaultEventList annotationSet? SEMI   # StateIgnore
              | ON eventList DO funName=Iden annotationSet? SEMI # OnEventDoAction
              | ON eventList DO annotationSet? anonEventHandler  # OnEventDoAction
              | ON eventList PUSH stateName annotationSet? SEMI  # OnEventPushState
              | ON eventList GOTO stateName annotationSet? SEMI  # OnEventGotoState
              | ON eventList GOTO stateName annotationSet? WITH anonEventHandler  # OnEventGotoState
              | ON eventList GOTO stateName annotationSet? WITH funName=Iden SEMI # OnEventGotoState
              ;

eventList : eventId (COMMA eventId)* ;
eventId : NullLiteral | HALT | Iden ;

stateName : Iden (DOT Iden)* ; // First few Idens are groups

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
noParamAnonEventHandler : statementBlock;
payloadVarDecl : LPAREN funParam RPAREN ;

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
