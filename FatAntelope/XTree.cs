using System.Xml;

namespace FatAntelope
{

    /// <summary>
    /// Top level container of the XTree. Stores metadata about the XML document and it's root node
    /// </summary>
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
