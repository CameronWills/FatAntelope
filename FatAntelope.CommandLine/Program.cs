using FatAntelope.Writers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace FatAntelope.CommandLine
{
    class Program
    {
        internal enum ExitCode : int
        {
            Success = 0,
            InvalidParameters = 1,
            NoDifference = 2,
            RootNodeMismatch = 3,
            UnknownError = 10
        }

        static int Main(string[] args)
        {
            if (args == null || args.Length != 3)
            {
                Console.WriteLine("Unexpected number of paramters");
                Console.WriteLine("Usage: FatAntelope [source-file] [target-file] [output-file]");
                return (int)ExitCode.InvalidParameters;
            }

            Console.WriteLine("Building xml trees...");
            var tree1 = BuildTree(args[0]);
            var tree2 = BuildTree(args[1]);

            Console.WriteLine("Comparing xml trees...");
            XDiff.Diff(tree1, tree2);
            if (tree1.Root.Match == MatchType.Match && tree2.Root.Match == MatchType.Match && tree1.Root.Matching == tree2.Root)
            {
                Console.WriteLine("Warning: No difference found!");
                return (int)ExitCode.NoDifference;
            }

            if (tree1.Root.Match == MatchType.NoMatch || tree2.Root.Match == MatchType.NoMatch)
            {
                Console.Error.WriteLine("Error: Root nodes must have the same name!");
                return (int)ExitCode.RootNodeMismatch;
            }

            Console.WriteLine("Writing XDT transform...");
            var writer = new XdtDiffWriter();
            writer.WriteDiff(tree2, args[2]);
            Console.WriteLine("Done!");
            return (int)ExitCode.Success;
        }


        public static XTree BuildTree(string fileName)
        {
            var doc = new XmlDocument();
            doc.Load(fileName);

            return new XTree(doc);
        }
    }
}
