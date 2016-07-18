using FatAntelope.Writers;
using Microsoft.Web.XmlTransform;
using System;
using System.Reflection;
using System.Xml;

namespace FatAntelope.CommandLine
{
    class Program
    {
        private enum ExitCode : int
        {
            Success = 0,
            InvalidParameters = 1,
            NoDifference = 2,
            RootNodeMismatch = 3,
            UnknownError = 100
        }

        static int Main(string[] args)
        {
            var version = Assembly.GetEntryAssembly().GetName().Version;
            Console.WriteLine(
                  "==================\n"
                + string.Format(" FatAntelope v{0}.{1}\n", version.Major, version.Minor)
                + "==================\n");

            if (args == null || args.Length < 3 || args.Length > 4)
            {
                Console.WriteLine(
                      "Error: Unexpected number of paramters.\n"
                    + "Usage: FatAntelope source-file target-file output-file [transformed-file]\n"
                    + "  source-file : (Input) The original config file\n"
                    + "  target-file : (Input) The final config file\n"
                    + "  output-file : (Output) The output config transform patch file\n"
                    + "  transformed-file : (Optional Output) The config file resulting from applying the output-file to the source-file\n"
                    + "                     This file should be semantically equal to the target-file.\n");
                return (int)ExitCode.InvalidParameters;
            }

            Console.WriteLine("- Building xml trees . . .\n");
            var tree1 = BuildTree(args[0]);
            var tree2 = BuildTree(args[1]);

            Console.WriteLine("- Comparing xml trees . . .\n");
            XDiff.Diff(tree1, tree2);
            if (tree1.Root.Match == MatchType.Match && tree2.Root.Match == MatchType.Match && tree1.Root.Matching == tree2.Root)
            {
                Console.WriteLine("Warning: No difference found!\n");
                return (int)ExitCode.NoDifference;
            }

            if (tree1.Root.Match == MatchType.NoMatch || tree2.Root.Match == MatchType.NoMatch)
            {
                Console.Error.WriteLine("Error: Root nodes must have the same name!\n");
                return (int)ExitCode.RootNodeMismatch;
            }

            Console.WriteLine("- Writing XDT transform . . .\n");
            var writer = new XdtDiffWriter();
            var patch = writer.GetDiff(tree2);
            patch.Save(args[2]);

            if (args.Length > 3)
            {
                Console.WriteLine("- Applying transform to source . . .\n");
                var source = new XmlTransformableDocument();
                source.Load(args[0]);

                var transform = new XmlTransformation(patch.OuterXml, false, null);
                transform.Apply(source);

                source.Save(args[3]);
            }
            Console.WriteLine("- Finished successfully!\n");
            return (int)ExitCode.Success;
        }

        private XmlDocument Transform(XmlDocument sourceXml, XmlDocument patchXml)
        {
            var source = new XmlTransformableDocument();
            source.LoadXml(sourceXml.OuterXml);

            var patch = new XmlTransformation(patchXml.OuterXml, false, null);
            patch.Apply(source);

            return source;
        }

        public static XTree BuildTree(string fileName)
        {
            var doc = new XmlDocument();
            doc.Load(fileName);

            return new XTree(doc);
        }
    }
}
