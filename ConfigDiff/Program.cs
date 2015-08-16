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


        }


        public static XTree BuildTree(string fileName)
        {
            var doc = new XmlDocument();
            doc.Load(fileName);

            doc.Cho

        }

        public static XNode BuildNode(XmlNode node)
        {
            if (node.NodeType == XmlNodeType.Element)
                return BuildElement(XmlNode);

        }

        public static XNode BuildElement(XmlNode node)
        {
            if (node.NodeType == XmlNodeType.Element)
            {

            }
        } 
    }
}
