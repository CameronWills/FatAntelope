using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ConfigDiff
{
    class XNode
    {
        /// <summary>
        /// XPath to this node in the tree
        /// </summary>
        public XmlNode XmlNode { get; set; }
        
        /// <summary>
        /// XPath to this node in the tree
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Hash of the subtree of this node
        /// </summary>
        public byte[] Hash { get; set; }

        /// <summary>
        /// Collection of child nodes
        /// </summary>
        public List<XNode> Children { get; set; }

        public static XNode Build(XTree tree, XNode parent, XmlNode node, int order)
        {
            if (node.NodeType == XmlNodeType.Attribute)
            {
                var xnode = new XNode();
                xnode.XmlNode = node;
                var name = node.Name.ToLowerInvariant();
                xnode.Path = parent.Path + "/@" + name;
                xnode.Hash = Murmur3.Hash(name + "/" + node.Value);
            }


        }
    }
}
