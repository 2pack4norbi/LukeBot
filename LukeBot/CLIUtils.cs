using System.Collections.Generic;
using System.Text;
using CommandLine;


namespace LukeBot
{
    public static class CLIUtils
    {
        public static void HandleCLIError(IEnumerable<Error> errs, string command, out string msg)
        {
            msg = "";

            bool otherErrorsExist = false;
            foreach (Error e in errs)
            {
                if (e is HelpVerbRequestedError || e is HelpRequestedError || e is NoVerbSelectedError)
                    continue;

                otherErrorsExist = true;
            }

            if (otherErrorsExist)
            {
                msg = "Error while parsing " + command + " command.";
            }
        }

        internal class CLIMessageProxyTextWriter: System.IO.TextWriter
        {
            public override Encoding Encoding => Encoding.Default;

            private CLIMessageProxy mCLI;
            private string mBuffer;

            public CLIMessageProxyTextWriter(CLIMessageProxy proxy)
            {
                mCLI = proxy;
                mBuffer = "";
            }

            public override void Write(char c)
            {
                if (c == '\n')
                {
                    mCLI.Message(mBuffer);
                    mBuffer = "";
                }
                else
                {
                    mBuffer += c;
                }
            }
        }
    }
}