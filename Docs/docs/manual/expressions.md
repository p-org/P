A Function in P can be arbitrary piece of imperative code which enables programmers to capture complex protocol logic in their state machines.
P supports the common imperative programming language expressions (just like in [Java](https://docs.oracle.com/javase/tutorial/java/nutsandbolts/expressions.html)).

???+ note "P Expressions Grammar"

    ```
    expr : 
        | primitiveExpr                                  # PrimitiveExpr
        | (tupleBody)                                    # TupleExpr
        | LPAREN namedTupleBody RPAREN                   # NamedTupleExpr
        | LPAREN expr RPAREN                             # ParenExpr
        | expr DOT field=iden                            # NamedTupleAccessExpr
        | expr DOT field=int                             # TupleAccessExpr
        | seq=expr LBRACK index=expr RBRACK              # SeqAccessExpr
        | keys(expr)                                     # KeywordExpr
        | values(expr)                                   # KeywordExpr
        | sizeof(expr)                                   # KeywordExpr
        | fun=DEFAULT LPAREN type RPAREN                 # KeywordExpr
        | NEW interfaceName=iden LPAREN rvalueList? RPAREN # CtorExpr
        | fun=iden LPAREN rvalueList? RPAREN             # FunCallExpr
        | op=(SUB | LNOT) expr                           # UnaryExpr
        | lhs=expr op=(MUL | DIV) rhs=expr               # BinExpr
        | lhs=expr op=(ADD | SUB) rhs=expr               # BinExpr
        | expr cast=(AS | TO) type                       # CastExpr
        | lhs=expr op=(LT | GT | GE | LE | IN) rhs=expr  # BinExpr
        | lhs=expr op=(EQ | NE) rhs=expr                 # BinExpr
        | lhs=expr op=LAND rhs=expr                      # BinExpr
        | lhs=expr op=LOR rhs=expr                       # BinExpr
        | CHOOSE LPAREN expr? RPAREN					  # ChooseExpr
        | formatedString								  # StringExpr
     ;

    formatedString	
        : StringLiteral
        |	FORMAT LPAREN StringLiteral (COMMA rvalueList)? RPAREN 
        ;	 

    primitiveExpr 
        : iden
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

    tupleBody : fields+=rvalue COMMA
                    | fields+=rvalue (COMMA fields+=rvalue)+
                    ;

    namedTupleBody : names+=iden ASSIGN values+=rvalue COMMA
                | names+=iden ASSIGN values+=rvalue (COMMA names+=iden ASSIGN values+=rvalue)+
                ;
    /* A r-value is an expression that can’t have a value assigned to it which 
means r-value can appear on right but not on left hand side of an assignment operator(=)*/
        rvalue : expr ;
    ```
