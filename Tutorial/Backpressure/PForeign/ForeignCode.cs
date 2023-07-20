/*using System;
using System.Runtime;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Plang.CSharpRuntime;
using Plang.CSharpRuntime.Values;
using Plang.CSharpRuntime.Exceptions;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable 162, 219, 414
namespace PImplementation
{
  public static partial class GlobalFunctions
  {
    System.Random Random = new System.Random();

    public static PrtFloat Temp(PrtFloat lambda)
    {
      float temp = Math.log(1 - Random.NextDouble())/(-lambda.value)
      return (new PrtFloat(temp));
    }
  }
}*/

using System;
using System.Runtime;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Plang.CSharpRuntime;
using Plang.CSharpRuntime.Values;
using Plang.CSharpRuntime.Exceptions;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable 162, 219, 414
namespace PImplementation
{
  public static partial class GlobalFunctions
  {
    static System.Random Random = new System.Random();
    public static PrtFloat Temp(PrtFloat lambda, PMachine pMachine)
    {
      return (new PrtFloat(Math.Log(1 - Random.NextDouble()))/(-lambda));
    }
  }
}