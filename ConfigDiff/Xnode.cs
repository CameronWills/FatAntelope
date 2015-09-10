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
        /// Parent node
        /// </summary>
        public XNode Parent { get; set; }

        /// <summary>
        /// Collection of child nodes
        /// </summary>
        public List<XNode> Children { get; set; }

        public XNode(XmlNode node)
        {
            this.XmlNode = node;
        }

        public static XNode Build(XTree tree, XmlNode node, string path)
        {
            if (node.NodeType == XmlNodeType.Attribute)
            {
                var xnode = new XNode(node);
                var name = node.Name.ToLowerInvariant();
                xnode.Path = string.Format("{0}/@{1}", path, name);
                xnode.Hash = Murmur3.Hash(name + "/" + node.Value);
                tree.Add(xnode, xnode.Path);
                return xnode;
            }

            else if (node.NodeType == XmlNodeType.Element)
            {
                // TODO:
                //  Swapping to unordered diff - ordered may be too sensitive to minor differences in configs
                //  and the vast majority of config nodes are 'unordered' in behaviour
                
                var xnode = new XNode(node);
                var name = node.Name.ToLowerInvariant();
                xnode.Path = string.Format("{0}/{1}", path, name);
                tree.Add(xnode, xnode.Path);

                var hashes = new List<byte[]>();
                hashes.Add(Murmur3.Hash(name + "/"));
                int childOrder = 0;
                for (int i = 0; i < node.ChildNodes.Count; i++)
                {
                    var child = node.ChildNodes[i];
                    var xChild = XNode.Build(tree, child, xnode.Path);
                    if (xChild != null)
                    {
                        xnode.Children.Add(xChild);
                        hashes.Add(xChild.Hash);
                        if (child.NodeType == XmlNodeType.Element)
                            childOrder++;
                    }
                }
                // Join hashes and compute Hash value
                hashes.OrderBy(h => h)
                
                return xnode;
            }

            return null;
        }
    }
}
