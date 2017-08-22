parser grammar PParser;
options { tokenVocab=PLexer; }

// A small overview of ANTLRs parser rules:
//
// Parser rules begin with a lower case letter, lexer rules begin 
// with an Uppercase letter. To create a parser rule, write the name
// followed by a colon (:) and then a list of alternatives, separated
// by pipe (|) characters. You can use parenthesis for sub-expressions,
// alternatives within those sub-expressions, and kleene * or + on any
// element in a rule.
//
// Every production rule corresponds to a class that gets generated
// in the target language for the ANTLR generator. If we use alternative
// labels, as in `type`, then subclasses of the rule-class will be created
// for each label. If one alternative is labelled, then they all must be.
// The purpose of labels is to call different functions in the generated
// listeners and visitors for the results of these productions.
//
// Lastly, ANTLR's DSL contains a feature that allows us to name the matched
// tokens and productions in an alternative (name=part) or collect multiple
// tokens or productions of the same type into a list (list+=part). The `type`
// production below uses this feature, too.

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

eventSetDecl : EVENTSET name=Iden ASSIGN LBRACE eventSetLiteral RBRACE SEMI ;

interfaceDecl : TYPE name=Iden LPAREN type? RPAREN ASSIGN eventSet=Iden SEMI
              | TYPE name=Iden LPAREN type? RPAREN ASSIGN LBRACE eventSetLiteral RBRACE SEMI
              ;

eventSetLiteral : events+=(HALT | Iden) (COMMA events+=(HALT | Iden))* ;

// has scope
implMachineDecl : MACHINE name=Iden cardinality? annotationSet? (COLON idenList)? receivesSends* machineBody ;
idenList : names+=Iden (COMMA names+=Iden)* ;
receivesSends : RECEIVES eventSetLiteral? SEMI # MachineReceive
              | SENDS eventSetLiteral? SEMI    # MachineSend
              ;

implMachineProtoDecl : EXTERN MACHINE name=Iden LPAREN type? RPAREN SEMI;

specMachineDecl : SPEC name=Iden OBSERVES eventSetLiteral machineBody ;

machineBody : LBRACE machineEntry* RBRACE;
machineEntry : varDecl
             | funDecl
             | group
             | stateDecl
             ;

varDecl : VAR idenList COLON type annotationSet? SEMI ;

funDecl : FUN name=Iden LPAREN funParamList? RPAREN (COLON type)? annotationSet? (SEMI | functionBody) ;
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

nonDefaultEventList : events+=(HALT | Iden) (COMMA events+=(HALT | Iden))* ;
eventList : eventId (COMMA eventId)* ;
eventId : NullLiteral | HALT | Iden ;

stateName : (groups+=Iden DOT)* state=Iden ; // First few Idens are groups

functionBody : LBRACE varDecl* statement* RBRACE ;
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

lvalue : name=Iden
       | lvalue DOT field=Iden
       | lvalue DOT IntLiteral
       | lvalue LBRACK expr RBRACK
       ;

recvCase : CASE eventList COLON anonEventHandler ;
anonEventHandler : (LPAREN funParam RPAREN)? functionBody ;
noParamAnonEventHandler : functionBody;

expr : primitive # PrimitiveExpr
     | LPAREN unnamedTupleBody RPAREN # UnnamedTupleExpr
     | LPAREN namedTupleBody RPAREN # NamedTupleExpr
     | LPAREN expr RPAREN # ParenExpr
     | expr DOT field=Iden # NamedTupleAccessExpr
     | expr DOT field=IntLiteral # TupleAccessExpr
     | seq=expr LBRACK index=expr RBRACK # SeqAccessExpr
     | fun=KEYS LPAREN expr RPAREN # KeywordExpr
     | fun=VALUES LPAREN expr RPAREN # KeywordExpr
     | fun=SIZEOF LPAREN expr RPAREN # KeywordExpr
     | fun=DEFAULT LPAREN type RPAREN # KeywordExpr
     | NEW machineName=Iden LPAREN rvalueList? RPAREN #CtorExpr
     | fun=Iden LPAREN rvalueList? RPAREN # FunCallExpr
     | op=(SUB | LNOT) expr # UnaryExpr
     | lhs=expr op=(MUL | DIV) rhs=expr # BinExpr
     | lhs=expr op=(ADD | SUB) rhs=expr # BinExpr
     | expr cast=(AS | TO) type # CastExpr
     | lhs=expr op=(LT | GT | GE | LE | IN) rhs=expr # BinExpr
     | lhs=expr op=(EQ | NE) rhs=expr # BinExpr
     | lhs=expr op=LAND rhs=expr # BinExpr
     | lhs=expr op=LOR rhs=expr # BinExpr
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
