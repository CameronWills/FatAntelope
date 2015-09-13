using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ConfigDiff
{
    class Program
    {
        static void Main(string[] args)
        {
            if(args != null && args.Length > 0)
            {
                var tree = BuildTree(args[0]);
                Console.WriteLine("Built");
            }
            else
            {
                Console.WriteLine("Need to pass filename");
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
