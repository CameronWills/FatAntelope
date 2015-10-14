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
        static void Main(string[] args)
        {
            if (args != null && args.Length == 3)
            {
                Console.WriteLine("Building trees...");
                var tree1 = BuildTree(args[0]);
                var tree2 = BuildTree(args[1]);

                Console.WriteLine("Diffing trees...");
                XDiff.Diff(tree1, tree2);

                Console.WriteLine("Writing Diff...");
                var writer = new XdtDiffWriter();
                writer.WriteDiff(tree2, args[2]);

                Console.WriteLine("Done!");
            }
            else
            {
                Console.WriteLine("Unexpected number of paramters");
                Console.WriteLine("Usage: FatAntelope [source-file] [target-file] [output-file]");
            }

            Console.ReadLine();
        }


        public static XTree BuildTree(string fileName)
        {
            var doc = new XmlDocument();
            doc.Load(fileName);

            return new XTree(doc);
        }
    }
}
