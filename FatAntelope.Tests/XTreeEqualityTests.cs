using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Xml;

namespace FatAntelope.Tests
{
    [TestClass]
    public class XTreeEqualityTests
    {
        [TestMethod]
        public void SingleElement()
        {
            var doc1 = new XmlDocument();
            doc1.LoadXml("<root></root>");
            var tree1 = new XTree(doc1);

            Assert.AreEqual(tree1.Root.Children.Length, 0);
        }
        
        [TestMethod]
        public void XmlCommentsAreIgnored()
        {
            var doc1 = new XmlDocument();
            doc1.LoadXml(@"
                <root>
                    <child1 name1='child1' type1='elem1'>child1</child1>
                    <child2 name2='child2' type2='elem2'>child2</child2>
                </root>"
            );

            // reordered xml but same values
            var doc2 = new XmlDocument();
            doc2.LoadXml(@"
                <root>
                    <!-- Comments should be ignored -->
                    <child1 type1='elem1' name1='child1'>child1</child1>                    
                    <child2 type2='elem2' name2='child2'>child2</child2>
                </root>"
            );

            var tree1 = new XTree(doc1);
            var tree2 = new XTree(doc2);

            Assert.IsTrue(Enumerable.SequenceEqual(tree1.Root.Hash, tree2.Root.Hash));
        }
        
        [TestMethod]
        public void IsomorphicXmlAreEqual()
        {
            var doc1 = new XmlDocument();
            doc1.LoadXml(@"
                <root>
                    <child1 name1='child1' type1='elem1'>child1</child1>
                    <child2 name2='child2' type2='elem2'>child2</child2>
                </root>"
            );

            // reordered xml but same values
            var doc2 = new XmlDocument();
            doc2.LoadXml(@"
                <root>
                    <child2 type2='elem2' name2='child2'>child2</child2>
                    <child1 type1='elem1' name1='child1'>child1</child1>
                </root>"
            );

            var tree1 = new XTree(doc1);
            var tree2 = new XTree(doc2);

            Assert.IsTrue(Enumerable.SequenceEqual(tree1.Root.Hash, tree2.Root.Hash));
        }


        [TestMethod]
        public void ElementsAreCaseSensitive()
        {
            var doc1 = new XmlDocument();
            doc1.LoadXml(@"
                <root>
                    <child1 name1='child1' type1='elem1'>child1</child1>
                    <child2 name2='child2' type2='elem2'>child2</child2>
                </root>"
            );

            // reordered xml but same values
            var doc2 = new XmlDocument();
            doc2.LoadXml(@"
                <root>
                    <child1 type1='elem1' name1='child1'>child1</child1>
                    <CHILD2 type2='elem2' name2='child2'>child2</CHILD2>
                </root>"
            );

            var tree1 = new XTree(doc1);
            var tree2 = new XTree(doc2);

            Assert.IsFalse(Enumerable.SequenceEqual(tree1.Root.Hash, tree2.Root.Hash));
        }

        [TestMethod]
        public void AttributesAreCaseSensitive()
        {
            var doc1 = new XmlDocument();
            doc1.LoadXml(@"
                <root>
                    <child1 name1='child1' type1='elem1'>child1</child1>
                    <child2 name2='child2' type2='elem2'>child2</child2>
                </root>"
            );

            // reordered xml but same values
            var doc2 = new XmlDocument();
            doc2.LoadXml(@"
                <root>
                    <child1 type1='elem1' name1='child1'>child1</child1>
                    <child2 TYPE2='elem2' name2='child2'>child2</child2>
                </root>"
            );

            var tree1 = new XTree(doc1);
            var tree2 = new XTree(doc2);

            Assert.IsFalse(Enumerable.SequenceEqual(tree1.Root.Hash, tree2.Root.Hash));
        }

        [TestMethod]
        public void DifferentXmlAttributesAreDifferent()
        {
            var doc1 = new XmlDocument();
            doc1.LoadXml(@"
                <root>
                    <child1 name1='child1' type1='elem1'>child1</child1>
                    <child2 name2='child2' type2='elem2'>child2</child2>
                </root>"
            );

            var doc2 = new XmlDocument();
            doc2.LoadXml(@"
                <root>
                    <child1 name1='DIFFERENT' type1='elem1'>child1</child1>
                    <child2 name2='child2' type2='elem2'>child2</child2>
                </root>"
            );

            var tree1 = new XTree(doc1);
            var tree2 = new XTree(doc2);

            Assert.IsFalse(Enumerable.SequenceEqual(tree1.Root.Hash, tree2.Root.Hash));
        }

        [TestMethod]
        public void DifferentXmlTextsAreDifferent()
        {
            var doc1 = new XmlDocument();
            doc1.LoadXml(@"
                <root>
                    <child1 name1='child1' type1='elem1'>child1</child1>
                    <child2 name2='child2' type2='elem2'>child2</child2>
                </root>"
            );

            var doc2 = new XmlDocument();
            doc2.LoadXml(@"
                <root>
                    <child1 name1='child1' type1='elem1'>child1</child1>
                    <child2 name2='child2' type2='elem2'>DIFFERENT</child2>
                </root>"
            );

            var tree1 = new XTree(doc1);
            var tree2 = new XTree(doc2);

            Assert.IsFalse(Enumerable.SequenceEqual(tree1.Root.Hash, tree2.Root.Hash));
        }

        [TestMethod]
        public void DifferentXmlTagsAreDifferent()
        {
            var doc1 = new XmlDocument();
            doc1.LoadXml(@"
                <root>
                    <child1 name1='child1' type1='elem1'>child1</child1>
                    <child2 name2='child2' type2='elem2'>child2</child2>
                </root>"
            );

            var doc2 = new XmlDocument();
            doc2.LoadXml(@"
                <root>
                    <child1 name1='child1' type1='elem1'>child1</child1>
                    <DIFFERENT name2='child2' type2='elem2'>child2</DIFFERENT>
                </root>"
            );

            var tree1 = new XTree(doc1);
            var tree2 = new XTree(doc2);

            Assert.IsFalse(Enumerable.SequenceEqual(tree1.Root.Hash, tree2.Root.Hash));
        }
    }
}
