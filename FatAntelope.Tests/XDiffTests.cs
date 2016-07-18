using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Xml;

namespace FatAntelope.Tests
{
    [TestClass]
    public class XDiffTest
    {
        [TestMethod]
        public void AttributeMarkedChanged()
        {
            var doc1 = new XmlDocument();
            doc1.LoadXml(@"
                <root>
                    <child1 name1='child1' type1='elem1'>child1</child1>
                    <child2 name1='child2' type1='elem2'>child2</child2>
                </root>"
            );

            // reordered xml but same values
            var doc2 = new XmlDocument();
            doc2.LoadXml(@"
                <root>
                    <child1 type1='elem1' name1='DIFFERENT'>child1</child1>
                    <child2 name1='child2' type1='elem2'>child2</child2>
                </root>"
            );

            var tree1 = new XTree(doc1);
            var tree2 = new XTree(doc2);
            XDiff.Diff(tree1, tree2);

            Assert.AreEqual(tree1.Root.Match, MatchType.Change);
            Assert.AreEqual(tree2.Root.Match, MatchType.Change);

            Assert.AreEqual(tree1.Root.Children.Length, 2);
            Assert.AreEqual(tree2.Root.Children.Length, 2);

            Assert.AreEqual(tree1.Root.Elements.Length, 2);
            Assert.AreEqual(tree2.Root.Elements.Length, 2);

            Assert.AreEqual(tree1.Root.Elements[0].Match, MatchType.Change);
            Assert.AreEqual(tree2.Root.Elements[0].Match, MatchType.Change);

            Assert.AreEqual(tree1.Root.Elements[1].Match, MatchType.Match);
            Assert.AreEqual(tree2.Root.Elements[1].Match, MatchType.Match);

            Assert.AreEqual(tree1.Root.Elements[0].Attributes.Length, 2);
            Assert.AreEqual(tree2.Root.Elements[0].Attributes.Length, 2);

            Assert.AreEqual(tree1.Root.Elements[0].Texts.Length, 1);
            Assert.AreEqual(tree2.Root.Elements[0].Texts.Length, 1);

            Assert.AreEqual(tree1.Root.Elements[0].Texts[0].Match, MatchType.Match);
            Assert.AreEqual(tree2.Root.Elements[0].Texts[0].Match, MatchType.Match);

            Assert.AreEqual(tree1.Root.Elements[0].Attributes[0].Match, MatchType.Change);
            Assert.AreEqual(tree2.Root.Elements[0].Attributes[0].Match, MatchType.Match);

            Assert.AreEqual(tree1.Root.Elements[0].Attributes[1].Match, MatchType.Match);
            Assert.AreEqual(tree2.Root.Elements[0].Attributes[1].Match, MatchType.Change);

            Assert.AreEqual(tree1.Root.Elements[0].Attributes[0].Matching, tree2.Root.Elements[0].Attributes[1]);
        }
    }
}
