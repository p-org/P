module System = 
{
    IClient -> Client, IServer -> ServerImpl
};

module System' = 
{
    IClient -> Client, IServer -> ServerAbs
};


// Similar behavior in union or composition.

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

test test0: (compose ClientModule, ServerImplModule);

test test1: (compose ClientModule, ServerAbsModule);