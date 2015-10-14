using FatAntelope.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace FatAntelope
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
        private static XNode[] Empty = new XNode[] { };

        /// <summary>
        /// The respective System.Xml.XmlNode instance that this XNode maps to from the original XmlDocument.
        /// </summary>
        public XmlNode XmlNode { get; set; }

        /// <summary>
        /// Name of the node
        /// </summary>
        public string Name { get; set; }

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
        public XNode[] Attributes { get; private set; }

        /// <summary>
        /// Collection of child nodes (elements and text)
        /// </summary>
        public XNode[] Children { get; private set; }

        /// <summary>
        /// Collection of child element nodes
        /// </summary>
        public XNode[] Elements { get; private set; }

        /// <summary>
        /// Collection of child text nodes
        /// </summary>
        public XNode[] Texts { get; private set; }

        /// <summary>
        /// Node in other tree that this node is matched with
        /// </summary>
        public XNode Matching { get; set; }

        /// <summary>
        /// Type (or strength) of match that this node has with it's 'Match' node
        /// </summary>
        public MatchType Match { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="node">The respective System.Xml.XmlNode instance that this XNode maps to from the original XmlDocument.</param>
        /// <param name="parent">The parent XNode of this node in the XML Hierarchy</param>
        public XNode(XmlNode node, XNode parent)
        {
            this.XmlNode = node;
            this.Parent = parent;
            this.Attributes = Empty;
            this.Children = Empty;
            this.Elements = Empty;
            this.Texts = Empty;
        }

        private int descendantCount = -1;
        public int GetDescendantCount()
        {
            if (descendantCount >= 0)
                return descendantCount;

            var count = 0;
            count += Attributes.Length;
            count += Children.Length;

            foreach (var child in Children)
                count += child.GetDescendantCount();

            return descendantCount = count;
        }

        public bool IsAttribute()
        {
            return XmlNode.NodeType == XmlNodeType.Attribute;
        }

        public bool IsText()
        {
            return XmlNode.NodeType == XmlNodeType.Text || XmlNode.NodeType == XmlNodeType.CDATA;
        }

        public bool IsElement()
        {
            return XmlNode.NodeType == XmlNodeType.Element;
        }

        public bool HashEquals(byte[] hash2)
        {
            return ByteArrayComparer.Instance.Compare(Hash, hash2) == 0;
        }

        public static XNode Build(XmlNode node, XNode parent)
        {
            if (node.NodeType == XmlNodeType.Attribute)
            {
                var xnode = new XNode(node, parent);
                xnode.Name = node.Name.ToLowerInvariant();
                var name = "@" + xnode.Name;
                xnode.Hash = Murmur3Hasher.HashString(name + "/" + (node.Value ?? string.Empty));
                return xnode;
            }

            else if (node.NodeType == XmlNodeType.Text || node.NodeType == XmlNodeType.CDATA)
            {
                var xnode = new XNode(node, parent);
                xnode.Name = "#text";
                xnode.Hash = Murmur3Hasher.HashString(xnode.Name + "/" + (node.Value ?? string.Empty));

                return xnode;
            }

            else if (node.NodeType == XmlNodeType.Element)
            {
                var xnode = new XNode(node, parent);
                var name = node.Name.ToLowerInvariant();
                xnode.Name = name;
                var hashes = new List<byte[]>();
                hashes.Add(Murmur3Hasher.HashString(name + "/"));

                // Add attributes
                var attributes = new List<XNode>();
                for (var i = 0; i < node.Attributes.Count; i++)
                { 
                    var child = XNode.Build(node.Attributes[i], xnode);
                    if (child != null)
                    {
                        hashes.Add(child.Hash);
                        attributes.Add(child);
                    }
                }
                xnode.Attributes = attributes.ToArray();

                // Add child elements and text nodes
                var children = new List<XNode>();
                var elements = new List<XNode>();
                var texts = new List<XNode>();
                for (var i = 0; i < node.ChildNodes.Count; i++)
                {
                    var child = XNode.Build(node.ChildNodes[i], xnode);
                    if (child != null)
                    {
                        hashes.Add(child.Hash);
                        children.Add(child);
                        if (child.IsElement())
                            elements.Add(child);
                        else
                            texts.Add(child);
                    }
                }
                xnode.Children = children.ToArray();
                xnode.Elements = elements.ToArray();
                xnode.Texts = texts.ToArray();

                // Sort and concatenate child hashes and then compute the hash
                var joined = ConcatAll(hashes.OrderBy(h => h, ByteArrayComparer.Instance)
                    .ToList(), Murmur3Hasher.OUTPUT_LENGTH);
                xnode.Hash = Murmur3Hasher.HashBytes(joined);
                
                return xnode;
            }

            return null;
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
