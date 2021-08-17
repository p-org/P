The P module system allows programmers to decompose their complex system into modules to
implement and test the system compositionally. More details about the underlying theory
for the P module system (assume-guarantee style compositional reasoning) is described in
the [research paper](https://ankushdesai.github.io/assets/papers/modp.pdf)

???+ Note "P Modules Grammar"

    ```
    modExpr 
        : LPAREN modExpr RPAREN												  # AnnonymousModuleExpr
		| LBRACE bindslist+=bindExpr (COMMA bindslist+=bindExpr)* RBRACE      # PrimitiveModuleExpr
        | iden                                                                # NamedModule
        | op=COMPOSE mexprs+=modExpr (COMMA mexprs+=modExpr)+				  # ComposeModuleExpr
        | op=UNION   mexprs+=modExpr (COMMA  mexprs+=modExpr)+				  # UnionModuleExpr
        | op=HIDEE  nonDefaultEventList IN modExpr							  # HideEventsModuleExpr
        | op=HIDEI idenList IN modExpr										  # HideInterfacesModuleExpr
        | op=ASSERT  idenList IN modExpr									  # AssertModuleExpr
        ;
    bindExpr : (mName=iden | mName=iden RARROW iName=iden) ;

    namedModuleDecl : MODULE name=iden ASSIGN modExpr SEMI ;
    ```