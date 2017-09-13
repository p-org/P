module System = 
{
    IClient -> Client, IServer -> ServerImpl
};

module System' = 
{
    IClient -> Client, IServer -> ServerAbs
};


// Similar behavior in union.

module ClientModule = 
{
  IClient -> Client
};

module ServerImplModule = 
{
  IServer -> ServerImpl
};

module ServerAbsModule =
{
  IServer -> ServerAbs
};

//same as system
module System = (union ClientModule, ServerImplModule);


//same as system'
module System' = (union ClientModule, ServerAbsModule);