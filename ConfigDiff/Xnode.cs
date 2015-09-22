using ConfigDiff.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ConfigDiff
{
    /// <summary>
    /// Type (or strength) of a node match between trees
    /// </summary>
    public enum MatchType
    {
        Match,
        Change,
        NoMatch
    }
    
    public class XNode
    {
        /// <summary>
        /// The respective System.Xml.XmlNode instance that this XNode maps to from the original XmlDocument.
        /// </summary>
        public XmlNode XmlNode { get; set; }

        /// <summary>
        /// Path from the root to this node in the tree - does not include order
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Path from the root to this node in the tree - does not include order
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
        /// Collection of child attribute nodes
        /// </summary>
        public List<XNode> Attributes { get; private set; }

        /// <summary>
        /// Collection of child attribute nodes
        /// </summary>
        public List<XNode> Elements { get; private set; }

        /// <summary>
        /// Collection of child text nodes
        /// </summary>
        public List<XNode> Texts { get; set; }

        /// <summary>
        /// Node in other tree that this node is matched with
        /// </summary>
        public XNode Matching { get; set; }

        /// <summary>
        /// Type (or strength) of match that this node has with it's 'Match' node
        /// </summary>
        public MatchType Match { get; set; }

        /// <summary>
        /// Collection of child nodes
        /// </summary>
        public IEnumerable<XNode> Children 
        { 
            get 
            {
                foreach (var att in Attributes)
                    yield return att;

                foreach (var elem in Elements)
                    yield return elem;

                foreach (var text in Texts)
                    yield return text;
            } 
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="node">The respective System.Xml.XmlNode instance that this XNode maps to from the original XmlDocument.</param>
        /// <param name="parent">The parent XNode of this node in the XML Hierarchy</param>
        public XNode(XmlNode node, XNode parent)
        {
            this.XmlNode = node;
            this.Parent = parent;
            //this.Children = new List<XNode>();
        }

        public bool IsAttribute()
        {
            return XmlNode.NodeType == XmlNodeType.Attribute;
        }

        public bool IsText()
        {
            return XmlNode.NodeType == XmlNodeType.Text || return XmlNode.NodeType == XmlNodeType.CDATA;
        }

        public bool IsElement()
        {
            return XmlNode.NodeType == XmlNodeType.Element;
        }

        public static XNode Build(XmlNode node, XNode parent, string path)
        {
            if (node.NodeType == XmlNodeType.Attribute)
            {
                var xnode = new XNode(node, parent);
                var name = "@" + node.Name.ToLowerInvariant();
                xnode.Name = name;
                xnode.Path = string.Format("{0}/{1}", path, name);
                xnode.Hash = Murmur3Hasher.HashString(name + "/" + (node.Value ?? string.Empty));
                return xnode;
            }

            else if (node.NodeType == XmlNodeType.Text || node.NodeType == XmlNodeType.CDATA)
            {
                var xnode = new XNode(node, parent);
                var name = "#text";
                xnode.Name = name;
                xnode.Path = string.Format("{0}/{1}", path, name);
                xnode.Hash = Murmur3Hasher.HashString(name + "/" + (node.Value ?? string.Empty));

                return xnode;
            }

            else if (node.NodeType == XmlNodeType.Element)
            {
                var xnode = new XNode(node, parent);
                var name = node.Name.ToLowerInvariant();
                xnode.Name = name;
                xnode.Path = string.Format("{0}/{1}", path, name);
                var hashes = new List<byte[]>();
                hashes.Add(Murmur3Hasher.HashString(name + "/"));

                // Add attributes
                for (int i = 0; i < node.Attributes.Count; i++)
                    AddChild(node.Attributes[i], xnode, hashes);
                
                // Add child elements and Text nodes
                for (int i = 0; i < node.ChildNodes.Count; i++)
                    AddChild(node.ChildNodes[i], xnode, hashes);

                // Sort and concatenate child hashes and then compute the hash
                var joined = ConcatAll(hashes.OrderBy(h => h, new ByteArrayComparer())
                    .ToList(), Murmur3Hasher.OUTPUT_LENGTH);
                xnode.Hash = Murmur3Hasher.HashBytes(joined);
                
                return xnode;
            }

            return null;
        }

        private static void AddChild(XmlNode child, XNode parent, List<byte[]> hashes)
        {
            var xChild = XNode.Build(child, parent, parent.Path);
            if (xChild != null)
            {
                if (xChild.IsElement())
                    parent.Elements.Add(xChild);
                if(xChild.IsAttribute())
                    parent.Attributes.Add(xChild);
                if (xChild.IsText())
                    parent.Texts.Add(xChild);

                hashes.Add(xChild.Hash);
            }
        }

        private static byte[] ConcatAll(List<byte[]> arrays, int length)
        {
            var result = new byte[arrays.Count * length];
            for(int i = 0; i < arrays.Count; i++)
                arrays[i].CopyTo(result, i * length);
            
            return result;
        }
    }
}
