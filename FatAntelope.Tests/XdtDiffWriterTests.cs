using System;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using FatAntelope;
using Microsoft.Web.XmlTransform;
using FatAntelope.Writers;

namespace FatAntelope.Tests
{
    [TestClass]
    public class XdtWriterTests
    {
        [TestMethod]
        public void SetAttribute()
        {
            var doc1 = @"
                <root>
                    <child1 name1='child1' type1='elem1'>child1</child1>
                    <child2 name1='child2' type1='elem2'>child2</child2>
                </root>";

            var doc2 = @"
                <root>
                    <child1 type1='DIFFERENT' name1='DIFFERENT'>child1</child1>
                    <child2 name1='child2' type1='elem2'>child2</child2>
                </root>";

            AssertCanTransform(doc1, doc2);
        }

        private void AssertCanTransform(string sourceXml, string targetXml)
        {
            var doc1 = new XmlDocument();
            doc1.LoadXml(sourceXml);

            // reordered xml but same values
            var doc2 = new XmlDocument();
            doc2.LoadXml(targetXml);

            var tree1 = new XTree(doc1);
            var tree2 = new XTree(doc2);
            XDiff.Diff(tree1, tree2);

            var writer = new XdtDiffWriter();
            var patch = writer.GetDiff(tree2);
            var transformed = Transform(doc1, patch);

            var transformedTree = new XTree(transformed);

            Assert.IsFalse(Enumerable.SequenceEqual(tree1.Root.Hash, tree2.Root.Hash));
            Assert.IsTrue(Enumerable.SequenceEqual(transformedTree.Root.Hash, tree2.Root.Hash));
        }

        private XmlDocument Transform(XmlDocument sourceXml, XmlDocument patchXml)
        {
            var source = new XmlTransformableDocument();
            source.LoadXml(sourceXml.OuterXml);

            var patch = new XmlTransformation(patchXml.OuterXml, false, null);
            patch.Apply(source);

            return source;
        }
    }
}
