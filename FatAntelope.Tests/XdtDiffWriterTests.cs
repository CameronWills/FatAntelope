using FatAntelope.Writers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.XmlTransform;
using System.Linq;
using System.Xml;

namespace FatAntelope.Tests
{
    [TestClass]
    public class XdtWriterTests
    {
        [TestMethod]
        public void NoLocatorAndSetAttributes()
        {
            var source = @"
                <root>
                    <child1 name='child1' type='elem1'>child1</child1>
                    <child2 name='child2' type='elem2'>child2</child2>
                </root>";

            var target = @"
                <root>
                    <child1 type='DIFFERENT' name='DIFFERENT'>child1</child1>
                    <child2 name='child2' type='elem2'>child2</child2>
                </root>";

            var patch = GetPatch(source, target);

            // Locator = none
            AssertNoLocator(patch.SelectSingleNode("/root/child1"));

            // Transform = SetAttributes
            AssertTransform(patch.SelectSingleNode("/root/child1"), "SetAttributes(type,name)");

            AssertCanTransform(source, target);
        }

        [TestMethod]
        public void MatchAndSetAttribute()
        {
            var source = @"
                <root>
                    <child name='child1' type='elem1'>child1</child>
                    <child name='child2' type='elem2'>child2</child>
                </root>";

            var target = @"
                <root>
                    <child name='DIFFERENT' type='elem1'>child1</child>
                    <child name='child2' type='elem2'>child2</child>
                </root>";

            var patch = GetPatch(source, target);
            
            // Locator = Match(type)
            AssertLocator(patch.SelectSingleNode("/root/child"), "Match(type)");

            // Transform = SetAttribute(type)
            AssertTransform(patch.SelectSingleNode("/root/child"), "SetAttributes(name)");

            AssertCanTransform(source, target);
        }

        [TestMethod]
        public void InsertBefore()
        {
            var source = @"
                <root>
                    <child name='child1' type='elem1' />
                    <child name='child2' type='elem2' />
                </root>";

            var target = @"
                <root>
                    <clear />
                    <child name='DIFFERENT' type='elem1' />
                    <child name='child2' type='elem2' />
                </root>";

            var patch = GetPatch(source, target);

            // Locator = none
            AssertNoLocator(patch.SelectSingleNode("/root/clear"));

            // Transform = SetAttribute(type)
            AssertTransform(patch.SelectSingleNode("/root/clear"), "InsertBefore(/root/*[1])");

            AssertCanTransform(source, target);
        }

        [TestMethod]
        public void InsertBeforeSingle()
        {
            var source = @"
                <root>
                    <child name='child2' value='123abc' />
                </root>";

            var target = @"
                <root>
                    <child name='child1' value='elem1' />
                    <child name='child2' value='456xyz' />
                </root>";

            var patch = GetPatch(source, target);

            // Values
            AssertValue(patch.SelectSingleNode("/root/child[1]/@name"), "child1");
            AssertValue(patch.SelectSingleNode("/root/child[2]/@name"), "child2");

            // Transform = InsertBefore
            AssertTransform(patch.SelectSingleNode("/root/child[1]"), "InsertBefore(/root/*[1])");

            // Transform = SetAttributes
            AssertTransform(patch.SelectSingleNode("/root/child[2]"), "SetAttributes(value)");

            AssertCanTransform(source, target);
        }

        [TestMethod]
        public void InsertAfterAttribute()
        {
            var source = @"
                <root>
                    <child name='child1' type='elem1' />
                    <child name='child2' type='elem2' />
                </root>";

            var target = @"
                <root>
                    <child name='DIFFERENT' type='elem1' />
                    <clear all='true' />
                    <child name='child2' type='elem2' />
                </root>";

            var patch = GetPatch(source, target);

            // Locator = none
            AssertNoLocator(patch.SelectSingleNode("/root/clear"));

            // Transform = SetAttribute(type)
            AssertTransform(patch.SelectSingleNode("/root/clear"), "InsertAfter(/root/child[(@type='elem1')])");

            AssertCanTransform(source, target);
        }

        [TestMethod]
        public void InsertAfterIndex()
        {
            var source = @"
                <root>
                    <child />
                    <child />
                </root>";

            var target = @"
                <root>
                    <child />
                    <clear all='true' />
                    <child />
                </root>";

            var patch = GetPatch(source, target);

            // Locator = none
            AssertNoLocator(patch.SelectSingleNode("/root/clear"));

            // Transform = SetAttribute(type)
            AssertTransform(patch.SelectSingleNode("/root/clear"), "InsertAfter(/root/child[1])");

            AssertCanTransform(source, target);
        }

        [TestMethod]
        public void InsertAfterSimple()
        {
            var source = @"
                <root>
                    <child1 />
                    <child3 />
                </root>";

            var target = @"
                <root>
                    <child1 />
                    <clear all='true' />
                    <child3 />
                </root>";

            var patch = GetPatch(source, target);

            // Locator = none
            AssertNoLocator(patch.SelectSingleNode("/root/clear"));

            // Transform = SetAttribute(type)
            AssertTransform(patch.SelectSingleNode("/root/clear"), "InsertAfter(/root/child1)");

            AssertCanTransform(source, target);
        }

        [TestMethod]
        public void InsertEnd()
        {
            var source = @"
                <root>
                    <child1 />
                    <child3 />
                </root>";

            var target = @"
                <root>
                    <child1 />
                    <child3 />
                    <clear all='true' />
                </root>";

            var patch = GetPatch(source, target);

            // Locator = none
            AssertNoLocator(patch.SelectSingleNode("/root/clear"));

            // Transform = SetAttribute(type)
            AssertTransform(patch.SelectSingleNode("/root/clear"), "Insert");

            AssertCanTransform(source, target);
        }

        [TestMethod]
        public void MatchAndReplace()
        {
            var source = @"
                <root>
                    <child name='child1' type='elem1'>child1</child>
                    <child name='child2' type='elem2'>child2</child>
                </root>";

            var target = @"
                <root>
                    <child name='child1' type='elem1'>DIFFERENT</child>
                    <child name='child2' type='elem2'>child2</child>
                </root>";

            var patch = GetPatch(source, target);

            // Locator = Match(type)
            AssertLocator(patch.SelectSingleNode("/root/child"), "Match(name)");

            // Transform = SetAttribute(type)
            AssertTransform(patch.SelectSingleNode("/root/child"), "Replace");

            AssertCanTransform(source, target);
        }

        [TestMethod]
        public void ConditionAndReplace()
        {
            var source = @"
                <root>
                    <child name='child1' type='elem' />
                    <child name='child2' type='elem' />
                </root>";

            var target = @"
                <root>
                    <child name='DIFFERENT' type='DIFFERENT' />
                    <child name='child2' type='elem2' />
                </root>";

            var patch = GetPatch(source, target);

            // Locator = Match(type)
            AssertLocator(patch.SelectSingleNode("/root/child"), "Condition([(@name='child1')])");

            // Transform = SetAttribute(type)
            AssertTransform(patch.SelectSingleNode("/root/child"), "Replace");

            AssertCanTransform(source, target);
        }

        private void AssertTransform(XmlNode node, string expected)
        {
            Assert.IsNotNull(node);
            var value = node.SelectSingleNode("@*[local-name() = 'Transform']").Value;
            Assert.AreEqual(expected, value);
        }

        private void AssertValue(XmlNode node, string expected)
        {
            Assert.IsNotNull(node);
            Assert.AreEqual(expected, node.Value);
        }

        private void AssertLocator(XmlNode node, string expected)
        {
            Assert.IsNotNull(node);
            var value = node.SelectSingleNode("@*[local-name() = 'Locator']").Value;
            Assert.AreEqual(expected, value);
        }

        private void AssertNoLocator(XmlNode node)
        {
            Assert.IsNotNull(node);
            var attribute = node.SelectSingleNode("@*[local-name() = 'Locator']");
            Assert.IsNull(attribute);
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

        private XmlDocument GetPatch(string sourceXml, string targetXml)
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
            return writer.GetDiff(tree2);
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
