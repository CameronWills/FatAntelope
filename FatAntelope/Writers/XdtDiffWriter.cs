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
    /// This implementation makes some assumptions about common XML in the config file, and is a little hacky.
    /// May not produce the best result with placement of xdt:Transform and xdt:Locator attributes.
    /// </remarks>
    public class XdtDiffWriter : BaseDiffWriter
    {
        #region Helper Classes
        
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

        /// <summary>
        /// Stores unique trait/s for an element
        /// </summary>
        private class Trait
        {
            public int Index { get; set; }
            public XNode Attribute { get; set; }
            //public XNode Element { get; set; }
            //public string Value { get; set; }

            public Trait()
            {
                Index = -1;
            }
        }

        #endregion

        /// <summary>
        /// Types of xdt transforms
        /// </summary>
        private enum TransformType
        {
            None = 0,
            RemoveAttributes = 1,
            SetAttributes = 2,
            RemoveAndSetAttributes = 3, // HACK: non-standard transform to get around SetAttributes not supporting remove
            Insert = 4,
            InsertBefore = 5,
            InsertAfter = 6,
            Remove = 7,
            RemoveAll = 8,
            Replace = 9
        }

        private const string XdtNamespace = "http://schemas.microsoft.com/XML-Document-Transform";
        private const string XdtPrefix = "xdt";
        private const string XdtTransform = "Transform";
        private const string XdtLocator = "Locator";
        private const string XdtMatch = "Match({0})";
        private const string XdtXPath = "XPath({0})";
        private const string XdtCondition = "Condition({0})";
        private const string XdtSetAttributes = "SetAttributes({0})";
        private const string XdtRemoveAttributes = "RemoveAttributes({0})";
        private const string XPathPredicate = "[{0}='{1}']";
        private const string XPathIndexPredicate = "[{0}]";

        /// <summary>
        /// Write the diff / patch to the given file
        /// </summary>
        public override void WriteDiff(XTree tree, string file)
        {
            var doc = GetDiff(tree);
            doc.Save(file);
        }

        /// <summary>
        /// Get the diff patch for the tree
        /// </summary>
        public XmlDocument GetDiff(XTree tree)
        {
            var doc = new XmlDocument();
            var root = WriteElement(tree.Root.Matching, tree.Root, doc, string.Empty);

            var attr = doc.CreateAttribute("xmlns", XdtPrefix, "http://www.w3.org/2000/xmlns/");
            attr.Value = XdtNamespace;
            root.Attributes.Append(attr);

            return doc;
        }

        /// <summary>
        /// Append the changed element to the new config transform. The given element may have been updated, inserted or deleted.
        /// </summary>
        private XmlNode WriteElement(XNode oldElement, XNode newElement, XmlNode target, string path)
        {

            XmlNode element = null;
            var transform = GetTransformType(oldElement, newElement);
            if (transform == TransformType.Insert)  // Insert
            {
                element = CopyNode(newElement, target);
                AddTransform(element, transform.ToString());
                return element;
            }

            var trait = GetUniqueTrait(oldElement);
            path = GetPath(path, oldElement, trait);

            if (transform == TransformType.Replace)  // Replace
            {
                element = CopyNode(newElement, target);
                AddTransform(element, transform.ToString());
                AddLocator(element, trait, false);
                return element;
            }

            element = AddElement(target, oldElement.XmlNode.Name);
            AddLocator(element, trait, true);
            if (transform == TransformType.Remove)  // Remove
            {
                AddTransform(element, TransformType.Remove.ToString());
                return element;
            }
            else if (transform == TransformType.RemoveAttributes || transform == TransformType.RemoveAndSetAttributes)  // RemoveAttributes
            {
                var builder = new StringBuilder();
                var first = true;
                foreach (var attr in oldElement.Attributes)
                {
                    if (attr.Match == MatchType.NoMatch)
                        builder.Append((first ? string.Empty : ",") + attr.XmlNode.Name);
                }
                AddTransform(element, string.Format(XdtRemoveAttributes, builder.ToString()));

                if (transform == TransformType.RemoveAndSetAttributes)   // RemoveAndSetAttributes
                {
                    var element2 = AddElement(target, oldElement.XmlNode.Name);
                    AddLocator(element2, trait, true);
                    var attributeList = CopyAttributes(newElement, element2);
                    AddTransform(element2, string.Format(XdtSetAttributes, attributeList));
                }
            }
            else if (transform == TransformType.SetAttributes)  // SetAttributes
            {
                var attributeList = CopyAttributes(newElement, element);
                AddTransform(element, string.Format(XdtSetAttributes, attributeList));
            }
            
            // Finally, process child elements
            foreach (var child in newElement.Elements)
            {
                if (child.Match == MatchType.Change || child.Match == MatchType.NoMatch)
                    WriteElement(child.Matching, child, element, path);
            }
            foreach(var child in oldElement.Elements)
            {
                if (child.Match == MatchType.NoMatch)
                    WriteElement(child, null, element, path);
            }

            return element;
        }

        /// <summary>
        /// Copy all inserted or updated attributes to the given element. 
        /// </summary>
        private string CopyAttributes(XNode node, XmlNode element)
        {
            var builder = new StringBuilder();
            var first = true;
            foreach (var attr in node.Attributes)
            {
                if (attr.Match == MatchType.Change || attr.Match == MatchType.NoMatch)
                {
                    var attribute = CopyAttribute(attr, element);
                    builder.Append((first ? string.Empty : ",") + attr.XmlNode.Name);
                    first = false;
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Copy an attribute to the given element. 
        /// </summary>
        private XmlNode CopyAttribute(XNode node, XmlNode element)
        {
            var child = element.OwnerDocument.ImportNode(node.XmlNode, true);
            element.Attributes.Append(child as XmlAttribute);

            return child;
        }

        /// <summary>
        /// Copy an element or text node to the given element. 
        /// </summary>
        private XmlNode CopyNode(XNode node, XmlNode element)
        {
            var child = element.OwnerDocument.ImportNode(node.XmlNode, true);
            element.AppendChild(child);

            return child;
        }

        /// <summary>
        /// Append a new attribute to the given element. 
        /// </summary>
        private XmlAttribute AddAttribute(XmlNode element, string prefix, string name, string namespaceUri, string value)
        {
            var attr = element.OwnerDocument.CreateAttribute(prefix, name, namespaceUri);
            attr.Value = value;

            return element.Attributes.Append(attr);
        }

        /// <summary>
        /// Append a new element with the given name. 
        /// </summary>
        private XmlElement AddElement(XmlNode parent, string name)
        {
            var elem = (parent.OwnerDocument ?? (XmlDocument)parent).CreateElement(name);
            parent.AppendChild(elem);

            return elem;
        }

        /// <summary>
        /// Add the xdt:Locator attribute to the given element when necessary. 
        /// Will use the Match(attribute) option instead of a Condition when possible.
        /// </summary>
        private XmlAttribute AddLocator(XmlNode element, Trait trait, bool addAttribute)
        {
            if (trait != null)
            {
                if(trait.Attribute != null)
                { 
                    var attribute = trait.Attribute;
                    if (attribute.Match == MatchType.Match)
                    {
                        if (addAttribute)
                            CopyAttribute(attribute, element);

                        return AddAttribute(element, XdtPrefix, XdtLocator, XdtNamespace, string.Format(XdtMatch, attribute.XmlNode.Name));
                    }

                    return AddAttribute(element, XdtPrefix, XdtLocator, XdtNamespace, string.Format(XdtCondition, BuildPredicate(attribute.XmlNode)));
                }

                return AddAttribute(element, XdtPrefix, XdtLocator, XdtNamespace, string.Format(XdtCondition, trait.Index));

            }
            return null;
        }

        private string BuildPredicate(XmlNode node)
        {
            return string.Format(XPathPredicate, (node.NodeType == XmlNodeType.Attribute ? "@" : string.Empty) + node.Name, node.Value);
        }

        /// <summary>
        /// Add the xdt:Transform attribute to the given element
        /// </summary>
        private XmlAttribute AddTransform(XmlNode element, string value)
        {
            return AddAttribute(element, XdtPrefix, XdtTransform, XdtNamespace, value);
        }

        /// <summary>
        /// Calculate unique traits for the given element  - unique index or attribute
        /// </summary>
        private Trait GetUniqueTrait(XNode element)
        {
            var duplicates = new List<XNode>();
            var parent = element.Parent;
            if (parent != null)
            {
                var index = -1;

                // Check for siblings with the same name
                foreach(var child in parent.Elements)
                {
                    if (child.Name == element.Name)
                    {
                        duplicates.Add(child);
                        if (child == element)
                            index = duplicates.Count;
                    }
                }

                // Mulitple elements with the same name 
                if (duplicates.Count > 1)
                {
                    // try and find unique attribute
                    foreach (var attribute in element.Attributes)
                    {
                        var value = attribute.XmlNode.Value;
                        var unique = true;
                        foreach (var child in duplicates)
                        {
                            foreach (var childAttr in child.Attributes)
                            {
                                if (childAttr.Name == attribute.Name)
                                {
                                    if (value == childAttr.XmlNode.Value && childAttr != attribute)
                                        unique = false;
                                    break;
                                }
                            }

                            if (!unique)
                                break;
                        }

                        if (unique)
                            return new Trait() { Attribute = attribute };
                    }

                    // No unique attributes, so use index
                    return new Trait() { Index = index };
                }
            }

            return null;
        }

        /// <summary>
        /// Calculate the type of xdt:Transform attribute to write with this element 
        /// </summary>
        private TransformType GetTransformType(XNode oldElement, XNode newElement)
        {
            if (oldElement != null && oldElement.Match == MatchType.NoMatch)
                return TransformType.Remove;

            if (newElement != null && newElement.Match == MatchType.NoMatch)
                return TransformType.Insert;

            // if text nodes have changed, then we must replace
            var texts = GetCounts(oldElement.Texts, newElement.Texts);
            if (texts.HasChanges())
                return TransformType.Replace;

            var attributes = GetCounts(oldElement.Attributes, newElement.Attributes);
            var elements = GetCounts(oldElement.Elements, newElement.Elements);

            // If mostly only element inserts & deletes
            if (elements.Deletes + elements.Inserts > 0 
                && elements.Unchanged + elements.Updates == 0 
                && attributes.Unchanged < elements.TotalChanges())
                return TransformType.Replace;

            // If has attribute changes
            if(attributes.HasChanges())
            { 
                // If only attribute deletes, mark attributes for removal
                if(attributes.IsDeletesOnly(true))
                    return TransformType.RemoveAttributes;
                    
                // If both removing and changing / inserting some attributes.
                if(attributes.Deletes > 0)
                    return TransformType.RemoveAndSetAttributes;

                return TransformType.SetAttributes;
            }

            return TransformType.None;
        }

        /// <summary>
        /// Calculate the inserted, updated, deleted and unchanged counts for the given nodes
        /// </summary>
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

        /// <summary>
        /// Get the absolute XPath for the given element
        /// </summary>
        private string GetPath(string path, XNode element, Trait trait)
        {
            var newPath = path + "/" + element.XmlNode.Name;
            if(trait != null)
            { 
                if(trait.Attribute != null)
                    return newPath + BuildPredicate(trait.Attribute.XmlNode);

                if(trait.Index >= 0)
                    return newPath + string.Format(XPathIndexPredicate, trait.Index);
            }

            return newPath;
        }
    }
}
