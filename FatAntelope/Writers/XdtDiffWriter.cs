using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace FatAntelope.Writers
{
    /// <summary>
    /// An XML diffgram writer for the microsoft Xml-Document-Transform (xdt) format.
    /// </summary>
    /// <remarks>
    /// Implementation makes some assumptions about the XML in the config file, and is a little hacky.
    /// May not produce the best result as far as placement of xdt:Transform and xdt:Locator attributes.
    /// </remarks>
    public class XdtDiffWriter : BaseDiffWriter
    {
        private enum Transform
        {
            None = 0,
            RemoveAttributes = 1,
            SetAttributes = 2,
            Insert = 3,
            InsertBefore = 4,
            InsertAfter = 5,
            Remove = 6,
            RemoveAll = 7,
            Replace = 8
        }
        
        /// <summary>
        /// Store counts of updated, inserted, deleted and unchanged child XML nodes
        /// </summary>
        private class Counts
        {
            public int Updates { get; set; }
            public int Inserts { get; set; }
            public int Deletes { get; set; }
            public int Unchanged { get; set; }

            public bool IsInsertsOnly(bool ignoreUnchanged = false)
            {
                return Inserts > 0
                    && Updates == 0
                    && Deletes == 0
                    && (Unchanged == 0 || ignoreUnchanged);
            }

            public bool IsUpdatesOnly(bool ignoreUnchanged = false)
            {
                return Inserts == 0
                    && Updates > 0
                    && Deletes == 0
                    && (Unchanged == 0 || ignoreUnchanged);
            }

            public bool IsDeletesOnly(bool ignoreUnchanged = false)
            {
                return Inserts == 0
                    && Updates == 0
                    && Deletes > 0
                    && (Unchanged == 0 || ignoreUnchanged);
            }

            public bool HasAny()
            {
                return Updates + Inserts + Deletes + Unchanged > 0;
            }

            public bool HasChanges()
            {
                return Updates + Inserts + Deletes > 0;
            }

            public int TotalChanges()
            {
                return Updates + Inserts + Deletes;
            }

            public int Total()
            {
                return Updates + Inserts + Deletes + Unchanged;
            }
        }

        private const int DescendantsThreshold = 10;
        private const string XdtNamespace = "http://schemas.microsoft.com/XML-Document-Transform";

        public override void WriteDiff(XTree tree, string file)
        {
            var doc = new XmlDocument();
            var root = WriteElement(tree.Root, doc, string.Empty);

            // XDT Namespace
            var attr = doc.CreateAttribute("xmlns", "xdt", "http://www.w3.org/2000/xmlns/");
            attr.Value = XdtNamespace;
            root.Attributes.Append(attr);
            
            doc.Save(file);
        }

        private XmlNode WriteElement(XNode node, XmlNode target, string path)
        {
            XmlNode element = null;
            path = path + "/" + GetUniqueXPath(node);
            var transform = GetTransform(node);
            if (transform == Transform.Replace || transform == Transform.Insert )
            {
                element = CopyNode(node, target);
                AddAttribute(element, "xdt", "Transform", XdtNamespace, transform.ToString());
                if (transform == Transform.Replace)
                    AddAttribute(element, "xdt", "Locator", XdtNamespace, "XPath(" + path + ")");

                return element;
            }
            else
            {
                element = AddElement(target, node.XmlNode.Name);
                if(transform == Transform.SetAttributes)
                {
                    var attributeList = CopyAttributes(node, element);
                    AddAttribute(element, "xdt", "Locator", XdtNamespace, "XPath(" + path + ")");
                    AddAttribute(element, "xdt", "Transform", XdtNamespace, "SetAttributes(" + attributeList + ")");
                }
                if (transform == Transform.RemoveAttributes)
                {
                    var builder = new StringBuilder();
                    var first = true;
                    foreach (var attr in node.Matching.Attributes)
                    {
                        if (attr.Match == MatchType.NoMatch)
                            builder.Append(first ? string.Empty : "," + attr.XmlNode.Name);
                    }
                    AddAttribute(element, "xdt", "Locator", XdtNamespace, "XPath(" + path + ")");
                    AddAttribute(element, "xdt", "Transform", XdtNamespace, "RemoveAttributes(" + builder.ToString() + ")");
                }
            }

            foreach (var child in node.Elements)
            {
                if (child.Match == MatchType.Change || child.Match == MatchType.NoMatch)
                    WriteElement(child, element, path);
            }

            //TODO: Something here to handle deleted elements

            return element;
        }

        private string CopyAttributes(XNode node, XmlNode target)
        {
            var builder = new StringBuilder();
            var first = true;
            foreach (var attr in node.Attributes)
            {
                if (attr.Match == MatchType.Change || attr.Match == MatchType.NoMatch)
                { 
                    var attribute = CopyAttribute(attr, target);
                    builder.Append((first ? string.Empty : ",") + attr.XmlNode.Name);
                    first = false;
                }
            }

            return builder.ToString();
        }

        private XmlNode CopyAttribute(XNode node, XmlNode target)
        {
            var child = target.OwnerDocument.ImportNode(node.XmlNode, true);
            target.Attributes.Append(child as XmlAttribute);

            return child;
        }

        private XmlNode CopyNode(XNode node, XmlNode target)
        {
            var child = target.OwnerDocument.ImportNode(node.XmlNode, true);
            target.AppendChild(child);

            return child;
        }

        private XmlAttribute AddAttribute(XmlNode target, string prefix, string name, string namespaceUri, string value)
        {
            var attr = target.OwnerDocument.CreateAttribute(prefix, name, namespaceUri);
            attr.Value = value;

            return target.Attributes.Append(attr);
        }

        private XmlElement AddElement(XmlNode target, string name)
        {
            var elem = (target.OwnerDocument ?? (XmlDocument)target).CreateElement(name);
            target.AppendChild(elem);

            return elem;
        }

        private Transform GetTransform(XNode node)
        {
            if (node.Match == MatchType.NoMatch)
                return Transform.Insert;

            // if text nodes are changed, then we must replace
            var texts = GetCounts(node.Matching.Texts, node.Texts);
            if (texts.HasChanges())
                return Transform.Replace;

            var attributes = GetCounts(node.Matching.Attributes, node.Attributes);
            var elements = GetCounts(node.Matching.Elements, node.Elements);
            
            // If no child elements
            if(!elements.HasAny())
            {
                // If only attribute deletes, mark attributes for removal
                if(attributes.IsDeletesOnly())
                    return Transform.RemoveAttributes;
                
                // If most attributes unchanged, only set certain attributes
                //  note, if both updating and deleting some attributes, then Replace is necessary
                if(attributes.Unchanged >= attributes.TotalChanges()  && attributes.Deletes == 0)
                    return Transform.SetAttributes;
   
                return Transform.Replace;
            }

            // If mostly only element inserts & deletes
            if((elements.IsInsertsOnly() || elements.IsDeletesOnly())
                && (!attributes.HasChanges() || attributes.Unchanged < attributes.TotalChanges() 
                    || node.GetDescendantCount() - node.Attributes.Length > attributes.Unchanged))
                    return Transform.Replace;

            // If most children have changed, replace
            var changed = attributes.TotalChanges() + elements.TotalChanges();
            if (node.GetDescendantCount() - changed < changed)
                return Transform.Replace;
            
            return Transform.None;
        }

        private Counts GetCounts(XNode[] original, XNode[] updated)
        {
            var counts = new Counts();

            // Check for attribute changes
            foreach (var child in updated)
            {
                if (child.Match == MatchType.Change)
                    counts.Updates++;

                if (child.Match == MatchType.NoMatch)
                    counts.Inserts++;

                if (child.Match == MatchType.Match)
                    counts.Unchanged++;

            }
            foreach (var child in original)
            {
                if (child.Match == MatchType.NoMatch)
                    counts.Deletes++;
            }

            return counts;
        }

        private string GetUniqueXPath(XNode node)
        {
            var duplicates = new List<XNode>();
            var parent = node.Parent;
            if(parent != null)
            {
                // Check for siblings with the same name
                foreach(var child in parent.Elements)
                {
                    if (child.Name == node.Name)
                        duplicates.Add(child);
                }

                // Mulitple elements with the same name 
                if (duplicates.Count > 1)
                {
                    // try and find unique attribute
                    foreach (var attribute in node.Attributes)
                    {
                        var values = new HashSet<string>();
                        //values.Add(attribute.XmlNode.Value);
                        var unique = true;
                        foreach (var child in duplicates)
                        {
                            foreach (var childAttr in child.Attributes)
                            {
                                if (childAttr.Name == attribute.Name)
                                {
                                    if (values.Contains(childAttr.XmlNode.Value))
                                        unique = false;
                                    values.Add(childAttr.XmlNode.Value);
                                    break;
                                }
                            }

                            if (!unique)
                                break;
                        }

                        if (unique)
                            return node.XmlNode.Name + "[@" + attribute.Name + "='" + attribute.XmlNode.Value + "']";
                    }
                }
            }

            return node.Name;
        }
    }
}
