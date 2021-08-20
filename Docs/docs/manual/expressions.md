???+ note "P Expressions Grammar"

    ```
    expr : primitive                                      # PrimitiveExpr
     | LPAREN unnamedTupleBody RPAREN                 # UnnamedTupleExpr
     | LPAREN namedTupleBody RPAREN                   # NamedTupleExpr
     | LPAREN expr RPAREN                             # ParenExpr
     | expr DOT field=iden                            # NamedTupleAccessExpr
     | expr DOT field=int                             # TupleAccessExpr
     | seq=expr LBRACK index=expr RBRACK              # SeqAccessExpr
     | fun=KEYS LPAREN expr RPAREN                    # KeywordExpr
     | fun=VALUES LPAREN expr RPAREN                  # KeywordExpr
     | fun=SIZEOF LPAREN expr RPAREN                  # KeywordExpr
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

    formatedString	:	StringLiteral
                    |	FORMAT LPAREN StringLiteral (COMMA rvalueList)? RPAREN 
                    ;	 

    primitive : iden
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

    namedTupleBody : names+=iden ASSIGN values+=rvalue COMMA
                | names+=iden ASSIGN values+=rvalue (COMMA names+=iden ASSIGN values+=rvalue)+
                ;
    ```