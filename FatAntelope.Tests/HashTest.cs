using System;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using FatAntelope;

namespace FatAntelope.Tests
{
    [TestClass]
    public class HashTest
    {
        [TestMethod]
        public void IsomorphicXml()
        {
            var doc1 = new XmlDocument();
            doc1.LoadXml("<root><child1 name1=\"child1\" type1=\"elem1\">child1</child1><child2 name2=\"child2\" type2=\"elem2\">child2</child2></root>");

            // reordered xml but same values
            var doc2 = new XmlDocument();
            doc2.LoadXml("<root><child2 type2=\"elem2\" name2=\"child2\">child2</child2><child1 type1=\"elem1\"  name1=\"child1\">child1</child1></root>");

            var tree1 = new XTree(doc1);
            var tree2 = new XTree(doc2);

            Assert.IsTrue(Enumerable.SequenceEqual(tree1.Root.Hash, tree2.Root.Hash));
        }

        [TestMethod]
        public void DifferentXmlAttribute()
        {
            var doc1 = new XmlDocument();
            doc1.LoadXml("<root><child1 name1=\"child1\" type1=\"elem1\">child1</child1><child2 name2=\"child2\" type2=\"elem2\">child2</child2></root>");

            var doc2 = new XmlDocument();
            doc2.LoadXml("<root><child1 name1=\"child3\" type1=\"elem1\">child1</child1><child2 name2=\"child2\" type2=\"elem2\">child2</child2></root>");

            var tree1 = new XTree(doc1);
            var tree2 = new XTree(doc2);

            Assert.IsFalse(Enumerable.SequenceEqual(tree1.Root.Hash, tree2.Root.Hash));
        }

        [TestMethod]
        public void DifferentXmlText()
        {
            var doc1 = new XmlDocument();
            doc1.LoadXml("<root><child1 name1=\"child1\" type1=\"elem1\">child1</child1><child2 name2=\"child2\" type2=\"elem2\">child2</child2></root>");

            var doc2 = new XmlDocument();
            doc2.LoadXml("<root><child1 name1=\"child1\" type1=\"elem1\">child1</child1><child2 name2=\"child2\" type2=\"elem2\">child3</child2></root>");

            var tree1 = new XTree(doc1);
            var tree2 = new XTree(doc2);

            Assert.IsFalse(Enumerable.SequenceEqual(tree1.Root.Hash, tree2.Root.Hash));
        }

        [TestMethod]
        public void DifferentXmlTag()
        {
            var doc1 = new XmlDocument();
            doc1.LoadXml("<root><child1 name1=\"child1\" type1=\"elem1\">child1</child1><child2 name2=\"child2\" type2=\"elem2\">child2</child2></root>");

            var doc2 = new XmlDocument();
            doc2.LoadXml("<root><child1 name1=\"child1\" type1=\"elem1\">child1</child1><child3 name2=\"child2\" type2=\"elem2\">child2</child3></root>");

            var tree1 = new XTree(doc1);
            var tree2 = new XTree(doc2);

            Assert.IsFalse(Enumerable.SequenceEqual(tree1.Root.Hash, tree2.Root.Hash));
        }
    }
}
