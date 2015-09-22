using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConfigDiff
{
    public class XDiff
    {
        public static void SetMatching(XNode node1, XNode node2, MatchType match)
        {
            node1.Match = match;
            node1.Matching = node2;
            node2.Matching = node1;
        }

        public static void SetMatching(IEnumerable<XNode> nodes, MatchType match)
        {
            foreach (var node in nodes)
                node.Match = match;
        }

        public static void Diff(XTree tree1, XTree tree2)
        {
		    if (tree1.Root.Hash == tree2.Root.Hash)
			    Console.WriteLine("No Difference!");
		    else
            {
                if (tree1.Root.Name != tree2.Root.Name)
		        {
			        Console.WriteLine("The root tag name has changed");
				    tree1.Root.Match = MatchType.NoMatch;
                    tree2.Root.Match = MatchType.NoMatch;
			    }
			    else
			    {
				    SetMatching(tree1.Root, tree2.Root, MatchType.Change);
				    Diff(tree1.Root, tree2.Root, false);
			    }

                // Write the diff here
			    //writeDiff(input1, output);
            }
		}
	    
        /// <summary>
        /// 
        /// </summary>
        /// <param name="node1"></param>
        /// <param name="node2"></param>
        /// <param name="withDistance"></param>
        public static void Diff(XNode node1, XNode node2, bool withDistance)
        {
            if (node1.Attributes.Count > 0)
            {
                if (node2.Attributes.Count > 0)
                    diffAttributes(node1.Attributes, node2.Attributes);
                else
                    SetMatching(node1.Attributes, MatchType.NoMatch);
            }
            else if (node2.Attributes.Count > 0)
            {
                SetMatching(node2.Attributes, MatchType.NoMatch);
            }   
        }

        /// <summary>
        /// Diff and match two lists of attributes
        /// </summary>
        private static void diffAttributes(List<XNode> attributes1, List<XNode> attributes2)
        {
            // CAM: if only one attribute in both nodes
            if ((attributes1.Count == 1) && (attributes2.Count == 1))
            {
                if (attributes1[0].Hash == attributes2[0].Hash)
                    return;

                if (attributes1[0].Name == attributes2[0].Name)
                { 
                    SetMatching(attributes1[0], attributes2[0], MatchType.Change);
                    return;
                }
                
                SetMatching(attributes1[0], attributes2[0], MatchType.NoMatch);
                return;
            }

            // Try and match every attribute in node1 with each attribute in node2
            var matched = 0;
            foreach (var attr1 in attributes1)
            {
                var found = false;
                foreach (var attr2 in attributes2)
                {
                    if (attr2.Matching != null)
                        continue;

                    if(attr2.Hash == attr1.Hash)
                    {
                        attr2.Matching = attr1;
                        matched++;
                        found = true;
                        break;
                    }

                    if (attr2.Name == attr1.Name)
                    {
                        SetMatching(attr1, attr2, MatchType.Change);
                        matched++;
                        found = true;
                        break;
                    }
                }

                if (!found)
                    attr1.Match = MatchType.NoMatch;
            }

            if (matched != attributes2.Count)
            {
                foreach(var attr2 in attributes2)
                {
                    if (attr2.Matching == null)
                        attr2.Match = MatchType.NoMatch;
                }
            }
        }
    }
}
