using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QUT.Gppg;

namespace PParser
{
    public class BaseError
    {
        DSLLoc loc;
        string message;

        public int line
        {
            get { return loc.startLine; }
        }

        public int col
        {
            get { return loc.startColumn; }
        }

        public string msg
        {
            get { return message; }
        }

        public BaseError(DSLLoc l, string m)
        {
            loc = l; message = m;
        }
    }

    public class ParserError : BaseError {
        public ParserError(DSLLoc l, string m) : base(l, m) { }
    }

    internal partial class PParser : ShiftReduceParser<LexValue, LexLocation>
    {

        // Members
        Program root;
        List<ParserError> errorLst;

        public IList<ParserError> errors
        {
            get { return errorLst; }
        }

        public PParser(AbstractScanner<LexValue, LexLocation> s) : base(s)
        {
            root = new Program();
            errorLst = new List<ParserError>();
            ((PScanner)s).parser = this;
        }

        public void error(string msg)
        {
            errorLst.Add(new ParserError(new DSLLoc(null), msg));
        }

        public void error(DSLLoc l, string msg)
        {
            errorLst.Add(new ParserError(l, msg));
        }

        public Program program
        {
            get { return root; }
        }
    }
}
