using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace FatAntelope.Writers
{
    public class DebugDiffWriter : BaseDiffWriter
    {
        public override void WriteDiff(XTree tree, string file)
        {
            using (var writer = XmlWriter.Create(file, new XmlWriterSettings() { Indent = true }))
            {
                writer.WriteStartDocument();
                WriteElement(tree.Root, writer);
                writer.WriteEndDocument();
            }
        }

        private void WriteElement(XNode node, XmlWriter writer)
        {
            writer.WriteStartElement(node.Name);
            
            foreach (var attr in node.Attributes)
            {
                if (attr.Match == MatchType.Change || attr.Match == MatchType.NoMatch)
                    writer.WriteAttributeString(attr.Name, attr.XmlNode.Value);
            }
            
            foreach (var text in node.Texts)
            {
                if (text.Match == MatchType.Change || text.Match == MatchType.NoMatch)
                    writer.WriteValue(text.XmlNode.Value);
            }

            foreach (var element in node.Elements)
            {
                if (element.Match == MatchType.Change)
                    WriteElement(element, writer);

                if (element.Match == MatchType.NoMatch)
                    writer.WriteRaw(element.XmlNode.OuterXml);
            }

            writer.WriteEndElement();
        }
    }
}
