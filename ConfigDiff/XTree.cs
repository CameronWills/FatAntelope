using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ConfigDiff
{
    public class XTree
    {
        public XNode Root { get; set; }

        public XTree(XmlDocument doc)
        {
            Root = XNode.Build(doc.DocumentElement, null, string.Empty);
        }
    }
}
