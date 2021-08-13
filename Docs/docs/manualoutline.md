
## P Program
A P program consists of a collection of the following high-level declarations:

???+ Note "P Top Level Declarations Grammar"  
    ```
    topDecl :               #Top-level P Program Declarations
    | typeDef               #UserDefinedTypeDeclaration
    | enumTypeDefDecl       #EnumTypeDeclaration
    | eventDecl             #EventDeclaration
    | interfaceDecl         #InterfaceDeclaration
    | MachineDecl           #MachineDeclaration
    | specDecl              #SpecDeclaration
    | funDecl               #GlobalFunctionDeclaration
    | ModuleDecl            #ModuleDeclaration
    | testDecl              #TestCaseDeclaration
    ;
    ```