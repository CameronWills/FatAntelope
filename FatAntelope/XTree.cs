using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace FatAntelope
{
    public class XTree
    {
        public XmlDocument Document { get; set; }
        public XNode Root { get; set; }

        public XTree(XmlDocument document)
        {
            Document = document;
            Root = XNode.Build(document.DocumentElement, null);
        }
    }
}
