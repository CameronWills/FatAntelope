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
            AssertTransform(patch.SelectSingleNode("/root/clear"), "InsertBefore(/root/child[(@type='elem1')])");

            AssertCanTransform(source, target);
        }

        [TestMethod]
        public void InsertFollowedByParentChange()
        {
            // Insert a child element into a parent element that has a change affecting its unique trait.

            var source = @"
                <rootNode>
                  <parentNode nameNode='elem1' />
                  <parentNode nameNode='elem3'>
                    <childNode nameNode='child1' />
                    <childNode nameNode='child3' />
                  </parentNode>
                </rootNode>";

            var target = @"
                <rootNode>
                  <parentNode nameNode='elem1' />
                  <parentNode nameNode='elem2' />
                  <parentNode nameNode='DIFFERENT'>
                    <childNode nameNode='child1' />
                    <childNode nameNode='child2' />
                    <childNode nameNode='child3' />
                  </parentNode>
                </rootNode>";

            var patch = GetPatch(source, target);

            AssertCanTransform(source, target);
        }

        [TestMethod]
        public void InsertAndRemove()
        {
            // Insert a child element into a parent element that has a change affecting its unique trait.

            var source = @"
                <rootNode>
                  <type1Node />
                  <type1Node />
                  <type1Node />
                  <type1Node />
                </rootNode>";

            var target = @"
                <rootNode>
                  <type1Node />
                  <type1Node />
                  <type2Node />
                  <type1Node />
                  <type2Node />
                  <type2Node />
                </rootNode>";

            var patch = GetPatch(source, target);

            AssertCanTransform(source, target);
        }

        [TestMethod]
        public void InsertChildWithParentChange()
        {
            // Insert a child element into a parent element that has a change affecting its unique trait.

            var source = @"
                <rootNode>
                    <parentNode nameNode='parent1'>
                        <childNode nameNode='child1' />
                        <childNode nameNode='child3' />
                    </parentNode>
                    <parentNode nameNode='parent2'>
                        <childNode nameNode='child1' />
                        <childNode nameNode='child2' />
                    </parentNode>
                </rootNode>";

            var target = @"
                <rootNode>
                    <parentNode nameNode='DIFFERENT'>
                        <childNode nameNode='child1' />
                        <childNode nameNode='child2' />
                        <childNode nameNode='child3' />
                    </parentNode>
                    <parentNode nameNode='parent2'>
                        <childNode nameNode='child1' />
                        <childNode nameNode='child2' />
                    </parentNode>
                </rootNode>";

            var patch = GetPatch(source, target);

            AssertLocator(patch.SelectSingleNode("/rootNode/parentNode[1]"), "Condition(1)");
            AssertTransform(patch.SelectSingleNode("/rootNode/parentNode[1]"), "SetAttributes(nameNode)");
            AssertTransform(patch.SelectSingleNode("/rootNode/parentNode[1]/childNode[1]"), "InsertBefore(/rootNode/parentNode[(@nameNode='DIFFERENT')]/childNode[(@nameNode='child3')])");

            AssertCanTransform(source, target);
        }

        [TestMethod]
        public void InsertChildWithParentChangeDuplicate()
        {
            var source = @"
                <root>
                    <parent name='parent1'>
                        <child name='child1' />
                        <child name='child3' />
                    </parent>
                    <parent name='parent2'>
                        <child name='child1' />
                        <child name='child2' />
                    </parent>
                </root>";

            var target = @"
                <root>
                    <parent name='parent2'>
                        <child name='child1' />
                        <child name='child2' />
                        <child name='child3' />
                    </parent>
                    <parent name='parent2'>
                        <child name='child1' />
                        <child name='child2' />
                    </parent>
                </root>";

            var patch = GetPatch(source, target);

            AssertLocator(patch.SelectSingleNode("/root/parent[1]"), "Condition(1)");
            AssertTransform(patch.SelectSingleNode("/root/parent[1]"), "SetAttributes(name)");
            AssertTransform(patch.SelectSingleNode("/root/parent[1]/child[1]"), "InsertBefore(/root/parent[1]/child[(@name='child3')])");

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
            AssertValue(patch.SelectSingleNode("/root/child[2]/@name"), "child1");

            // Transform = SetAttributes
            AssertTransform(patch.SelectSingleNode("/root/child[1]"), "SetAttributes(value)");

            // Transform = InsertBefore
            AssertTransform(patch.SelectSingleNode("/root/child[2]"), "InsertBefore(/root/child[(@name='child2')])");

            AssertCanTransform(source, target);
        }

        [TestMethod]
        public void InsertAfterChange()
        {
            var source = @"
                <root>
                    <child name='same' value='123abc' />
                </root>";

            var target = @"
                <root>
                    <child name='same' value='elem1' />
                    <child name='same' value='456xyz' />
                </root>";

            var patch = GetPatch(source, target);

            // Values
            AssertValue(patch.SelectSingleNode("/root/child[1]/@value"), "elem1");

            // Transform = SetAttributes
            AssertTransform(patch.SelectSingleNode("/root/child[1]"), "SetAttributes(value)");

            // No Locator
            AssertNoLocator(patch.SelectSingleNode("/root/child[1]"));

            // Transform = Insert
            AssertTransform(patch.SelectSingleNode("/root/child[2]"), "Insert");

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
            AssertTransform(patch.SelectSingleNode("/root/clear"), "InsertBefore(/root/child[(@name='child2')])");

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
        public void InsertDuplicateAfter()
        {
            var source = @"
                <root>
                    <child />
                </root>";

            var target = @"
                <root>
                    <child />
                    <clear all='true' />
                    <child />
                </root>";

            var patch = GetPatch(source, target);

            AssertTransform(patch.SelectSingleNode("/root/clear"), "InsertAfter(/root/child[1])");
            AssertTransform(patch.SelectSingleNode("/root/child"), "Insert");

            AssertCanTransform(source, target);
        }

        [TestMethod]
        public void InsertMultipleBefore()
        {
            var source = @"
                <root>
                    <child1 />
                </root>";

            var target = @"
                <root>
                    <child3 />
                    <child2 />
                    <child1 />
                </root>";

            var patch = GetPatch(source, target);

            AssertTransform(patch.SelectSingleNode("/root/child2"), "InsertBefore(/root/child1)");
            AssertTransform(patch.SelectSingleNode("/root/child3"), "InsertBefore(/root/child2)");

            AssertCanTransform(source, target);
        }

        [TestMethod]
        public void InsertAfterSimple()
        {
            var source = @"
                <configSettings>
                    <appSettings />
                    <webServer />
                </configSettings>";

            var target = @"
                <configSettings>
                    <appSettings />
                    <connectionStrings all='true' />
                    <webServer />
                </configSettings>";

            var patch = GetPatch(source, target);

            // Locator = none
            AssertNoLocator(patch.SelectSingleNode("/configSettings/connectionStrings"));

            // Transform = SetAttribute(type)
            AssertTransform(patch.SelectSingleNode("/configSettings/connectionStrings"), "InsertBefore(/configSettings/webServer)");

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
        public void RemoveWithNoLocator()
        {
            var source = @"
                <root>
                    <child1 />
                    <child3 />
                </root>";

            var target = @"
                <root>
                    <child1 />
                </root>";

            var patch = GetPatch(source, target);

            // Locator = none
            AssertNoLocator(patch.SelectSingleNode("/root/child3"));

            // Transform = SetAttribute(type)
            AssertTransform(patch.SelectSingleNode("/root/child3"), "Remove");

            AssertCanTransform(source, target);
        }

        [TestMethod]
        public void RemoveWithMatchLocator()
        {
            var source = @"
                <root>
                    <child name='child1' />
                    <child name='child2' />
                </root>";

            var target = @"
                <root>
                    <child name='child1' />
                </root>";

            var patch = GetPatch(source, target);

            // Locator = Match(name
            AssertLocator(patch.SelectSingleNode("/root/child[(@name='child2')]"), "Match(name)");

            Assert.AreEqual(patch.SelectSingleNode("/root").ChildNodes.Count, 1);

            // Transform = SetAttribute(type)
            AssertTransform(patch.SelectSingleNode("/root/child[(@name='child2')]"), "Remove");

            AssertCanTransform(source, target);
        }

        [TestMethod]
        public void RemoveAndChangeAttributes()
        {
            var source = @"
                <root>
                    <child name='child1' test='true' optional='value' />
                    <child name='child2' test='true' optional='value' />
                </root>";

            var target = @"
                <root>
                    <child name='child1' test='false' optional='value' />
                    <child name='child2' test='false' />
                </root>";

            var patch = GetPatch(source, target);

            
            // Two transform nodes for the same child
            Assert.AreEqual(patch.SelectSingleNode("/root").ChildNodes.Count, 3);
            Assert.AreEqual(patch.SelectNodes("/root/child[(@name='child2')]").Count, 2);

            // First child has single attribute changed (test='false')
            AssertLocator(patch.SelectSingleNode("/root/child[(@name='child1')]"), "Match(name)");
            AssertTransform(patch.SelectSingleNode("/root/child[(@name='child1')]"), "SetAttributes(test)");
            AssertValue(patch.SelectSingleNode("/root/child[(@name='child1')]/@test"), "false");

            // Second child has both attribute changed (test='false') and attribute removed (optional='value')
            AssertLocator(patch.SelectSingleNode("/root/child[2]"), "Match(name)");
            AssertValue(patch.SelectSingleNode("/root/child[2]/@name"), "child2");
            AssertTransform(patch.SelectSingleNode("/root/child[2]"), "RemoveAttributes(optional)");

            AssertLocator(patch.SelectSingleNode("/root/child[3]"), "Match(name)");
            AssertValue(patch.SelectSingleNode("/root/child[3]/@name"), "child2");
            AssertTransform(patch.SelectSingleNode("/root/child[3]"), "SetAttributes(test)");

            AssertCanTransform(source, target);
        }

        [TestMethod]
        public void RemoveMultipleAttributes()
        {
            var source = @"
                <root>
                    <child name='child1' test='true' optional='value' />
                    <child name='child2' test='true' optional='value' />
                </root>";

            var target = @"
                <root>
                    <child name='child1' test='false' optional='value' />
                    <child name='child2' />
                </root>";

            var patch = GetPatch(source, target);


            // Two transform nodes for the same child
            Assert.AreEqual(patch.SelectSingleNode("/root").ChildNodes.Count, 2);

            // First child has single attribute changed (test='false')
            AssertLocator(patch.SelectSingleNode("/root/child[(@name='child1')]"), "Match(name)");
            AssertTransform(patch.SelectSingleNode("/root/child[(@name='child1')]"), "SetAttributes(test)");
            AssertValue(patch.SelectSingleNode("/root/child[(@name='child1')]/@test"), "false");

            // Second child has two attributes removed (test and optional)
            AssertLocator(patch.SelectSingleNode("/root/child[2]"), "Match(name)");
            AssertValue(patch.SelectSingleNode("/root/child[2]/@name"), "child2");
            AssertTransform(patch.SelectSingleNode("/root/child[2]"), "RemoveAttributes(test,optional)");

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

        #region Private helpers

        private void AssertTransform(XmlNode node, string expected)
        {
            AssertAttribute(node, expected, "Transform");
        }

        private void AssertNoTransform(XmlNode node)
        {
            AssertNoAttribute(node, "Transform");
        }

        private void AssertLocator(XmlNode node, string expected)
        {
            AssertAttribute(node, expected, "Locator");
        }

        private void AssertNoLocator(XmlNode node)
        {
            AssertNoAttribute(node, "Locator");
        }

        private void AssertValue(XmlNode node, string expected)
        {
            Assert.IsNotNull(node);
            Assert.AreEqual(expected, node.Value);
        }

        private void AssertAttribute(XmlNode node, string expected, string attributeName)
        {
            Assert.IsNotNull(node);
            var value = node.SelectSingleNode(string.Format("@*[local-name() = '{0}']", attributeName)).Value;
            Assert.AreEqual(expected, value);
        }

        private void AssertNoAttribute(XmlNode node, string attributeName)
        {
            Assert.IsNotNull(node);
            var attribute = node.SelectSingleNode(string.Format("@*[local-name() = '{0}']", attributeName));
            Assert.IsNull(attribute, string.Format("Attribute not expected: {0}", attributeName));
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

        #endregion
    }
}
