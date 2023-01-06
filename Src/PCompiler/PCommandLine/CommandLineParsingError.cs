using System;

namespace Plang;

/// <summary>
/// Exception to capture errors when parsing commandline arguments
/// </summary>
public class CommandlineParsingError : Exception
{
    public CommandlineParsingError(string message) : base(message)
    {
    }
}