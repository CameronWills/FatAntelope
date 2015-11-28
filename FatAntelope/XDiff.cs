/**
  * Copyright (c) 2001 - 2005
  * 	Yuan Wang. All rights reserved.
  *
  * Redistribution and use in source and binary forms, with or without
  * modification, are permitted provided that the following conditions
  * are met:
  * 1. Redistributions of source code must retain the above copyright 
  * notice, this list of conditions and the following disclaimer.
  * 2. Redistributions in binary form must reproduce the above copyright
  * notice, this list of conditions and the following disclaimer in the 
  * documentation and/or other materials provided with the distribution.
  * 3. Redistributions in any form must be accompanied by information on
  * how to obtain complete source code for the X-Diff software and any
  * accompanying software that uses the X-Diff software.  The source code
  * must either be included in the distribution or be available for no
  * more than the cost of distribution plus a nominal fee, and must be
  * freely redistributable under reasonable conditions.  For an executable
  * file, complete source code means the source code for all modules it
  * contains.  It does not include source code for modules or files that
  * typically accompany the major components of the operating system on
  * which the executable file runs.
  *
  * THIS SOFTWARE IS PROVIDED BY YUAN WANG "AS IS" AND ANY EXPRESS OR IMPLIED
  * WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF
  * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE, OR NON-INFRINGEMENT,
  * ARE DISCLAIMED.  IN NO EVENT SHALL YUAN WANG BE LIABLE FOR ANY DIRECT,
  * INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
  * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
  * SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
  * HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
  * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING
  * IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
  * POSSIBILITY OF SUCH DAMAGE.
  *
  */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FatAntelope
{
    /// <summary>
    /// The XDiff algorithm for doing an unordered comparison of two xml documents and flaging the changed, inserted and deleted nodes.
    /// A C# port, with some modifications, of the original X-Diff algorithm by Yuan Wang described here: 
    /// http://pages.cs.wisc.edu/~yuanwang/xdiff.html
    /// The node matching logic uses the minimum-cost maximum flow algorithm when necessary, to find the minimum-cost bipartite mapping of the two trees. 
    /// This gives an optimal matching of nodes between the two trees.
    /// </summary>
    public class XDiff
    {
        private const int NoMatch = -1;
        private const int Delete = -1;
        private const int Insert = -1;
        private const int NoConnection = 1048576;
        private const int MaxCircuitLength = 2048;

        private static Dictionary<Tuple<XNode, XNode>, int> distanceLookup;

        /// <summary>
        /// Compare and match the two trees
        /// </summary>
        public static void Diff(XTree tree1, XTree tree2)
        {
            if (tree1.Root.HashEquals(tree2.Root.Hash))
            {
                SetMatching(tree1.Root, tree2.Root, MatchType.Match);
                return;
            }
            
            if (tree1.Root.Name != tree2.Root.Name)
            {
                SetMatching(tree1.Root, tree1.Root, MatchType.NoMatch);
                return;
            }

            distanceLookup = new Dictionary<Tuple<XNode, XNode>, int>();
            SetMatching(tree1.Root, tree2.Root, MatchType.Change);
            DiffElements(tree1.Root, tree2.Root);
        }

        /// <summary>
        /// Compare and match the two elements (and their children).
        /// </summary>
        private static void DiffElements(XNode node1, XNode node2)
        {
            // Attributes
            if (node1.Attributes.Length > 0)
            {
                if (node2.Attributes.Length > 0)
                    DiffAttributes(node1.Attributes, node2.Attributes);
                else
                    SetMatching(node1.Attributes, MatchType.NoMatch);
            }
            else if (node2.Attributes.Length > 0)
            {
                SetMatching(node2.Attributes, MatchType.NoMatch);
            }

            // Handle child elements and Text

            // First, if no children
            if (node1.Children.Length == 0)
                SetMatching(node2.Children, MatchType.NoMatch);

            else if (node2.Children.Length == 0)
                SetMatching(node1.Children, MatchType.NoMatch);

            // Next, if one child each
            else if (node2.Children.Length == 1 && node1.Children.Length == 1)
            {
                var child1 = node1.Children[0];
                var child2 = node2.Children[0];

                if (child1.HashEquals(child2.Hash))
                    return;

                var isElement1 = child1.IsElement();
                var isElement2 = child2.IsElement();

                if (isElement1 && isElement2)
                {
                    if (child1.Name == child2.Name)
                    {
                        SetMatching(child1, child2, MatchType.Change);
                        DiffElements(child1, child2);
                    }
                    else
                        SetMatching(child1, child2, MatchType.NoMatch);
                }
                else if (!isElement1 && !isElement2)
                    SetMatching(child1, child2, MatchType.Change);
                else
                    SetMatching(child1, child2, MatchType.NoMatch);
            }

            // Then, if many children
            else
            {
                // Match text nodes
                if (node1.Texts.Length > 0)
                {
                    if (node2.Texts.Length > 0)
                        DiffTexts(node1.Texts, node2.Texts);
                    else
                        SetMatching(node1.Texts, MatchType.NoMatch);
                }
                else if (node2.Texts.Length > 0)
                    SetMatching(node2.Texts, MatchType.NoMatch);

                // Match element nodes with equal hashes
                var matched = MatchEqual(node1.Elements, node2.Elements, MatchType.Match);
                if (matched == node1.Elements.Length && matched == node2.Elements.Length)
                    return;

                if (matched == node1.Elements.Length)
                    SetUnmatched(node2.Elements, MatchType.NoMatch);

                if (matched == node2.Elements.Length)
                    SetUnmatched(node1.Elements, MatchType.NoMatch);

                // 'Match' remaining unmatched child elements nodes.
                int remaining1 = node1.Elements.Length - matched;
                int remaining2 = node2.Elements.Length - matched;
                int matchCount1 = 0;
                int matchCount2 = 0;

                while ((matchCount1 < remaining1) && (matchCount2 < remaining2))
                {
                    var unmatched1 = new List<XNode>();
                    var unmatched2 = new List<XNode>();
                    string name = null;

                    // Find and group unmatched elements by their name
                    foreach (var child1 in node1.Elements)
                    {
                        if (child1.Matching == null && child1.Match != MatchType.NoMatch)
                        {
                            if (name == null)
                                name = child1.Name;

                            if (name == child1.Name)
                            {
                                unmatched1.Add(child1);
                                matchCount1++;
                            }
                        }
                    }

                    // Find unmatched nodes in the other subtree with the same element name
                    foreach (var child2 in node2.Elements)
                    {
                        if (child2.Matching == null && child2.Match != MatchType.NoMatch)
                        {
                            if (name == child2.Name)
                            {
                                unmatched2.Add(child2);
                                matchCount2++;
                            }
                        }
                    }

                    if (unmatched2.Count == 0)
                        SetMatching(unmatched1, MatchType.NoMatch);
                    else
                    {
                        if ((unmatched1.Count == 1) && (unmatched2.Count == 1))
                        {
                            SetMatching(unmatched1[0], unmatched2[0], MatchType.Change);
                            DiffElements(unmatched1[0], unmatched2[0]);
                        }

                        // Find minimal-cost matching between those unmatched
                        else if (unmatched1.Count >= unmatched2.Count)
                            MatchList(unmatched1.ToArray(), unmatched2.ToArray(), true);
                        else
                            MatchList(unmatched2.ToArray(), unmatched1.ToArray(), false);
                    }

                }

                // Finally mark any remaining child elements as unmatched
                if (matchCount1 < remaining1)
                    SetUnmatched(node1.Elements, MatchType.NoMatch);
                else if (matchCount2 < remaining2)
                    SetUnmatched(node2.Elements, MatchType.NoMatch);
            }

        }

        /// <summary>
        /// Diff and match two lists of attributes
        /// </summary>
        private static void DiffAttributes(XNode[] attributes1, XNode[] attributes2)
        {
            // If only one attribute in both nodes
            if ((attributes1.Length == 1) && (attributes2.Length == 1))
            {
                if (attributes1[0].HashEquals(attributes2[0].Hash))
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

                    if (attr2.HashEquals(attr1.Hash))
                    {
                        SetMatching(attr1, attr2, MatchType.Match);
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

            // If node2 has more attributes
            if (matched != attributes2.Length)
                SetUnmatched(attributes2, MatchType.NoMatch);
        }

        /// <summary>
        /// Diff and match two lists of text nodes
        /// </summary>
        private static void DiffTexts(XNode[] texts1, XNode[] texts2)
        {
            // First, try matching exactly equal text nodes
            var matched = MatchEqual(texts1, texts2, MatchType.Match);

            // Randomly match any remaining unmatched text1 nodes with any remaining unmatched text2 nodes 
            if (matched < texts1.Length && texts1.Length <= texts2.Length)
                matched += MatchAny(texts1, texts2, MatchType.Change);

            else if (matched < texts2.Length && texts2.Length <= texts1.Length)
                matched += MatchAny(texts2, texts1, MatchType.Change);


            // Finally, set any remaining text nodes as unmatched
            if (matched < texts1.Length)
                SetUnmatched(texts1, MatchType.NoMatch);

            else if (matched < texts2.Length)
                SetUnmatched(texts2, MatchType.NoMatch);
        }

        /// <summary>
        /// Set match for child nodes with equal hash values (equal sub-trees)
        /// </summary>
        private static int MatchEqual(XNode[] nodes1, XNode[] nodes2, MatchType match)
        {
            var matched = 0;
            foreach (var node1 in nodes1)
            {
                foreach (var node2 in nodes2)
                {
                    if (node2.Matching == null && node2.Match != MatchType.NoMatch && node1.HashEquals(node2.Hash))
                    {
                        SetMatching(node1, node2, MatchType.Match);
                        matched++;
                        break;
                    }
                }

                if (matched == nodes2.Length)
                    break;
            }

            return matched;
        }

        /// <summary>
        /// Randomly match nodes any nodes that are unmatched with other unmatched nodes
        /// </summary>
        private static int MatchAny(XNode[] nodes1, XNode[] nodes2, MatchType match)
        {
            var matched = 0;
            foreach (var node1 in nodes1)
            {
                if (node1.Matching == null && node1.Match != MatchType.NoMatch)
                {
                    foreach (var node2 in nodes2)
                    {
                        if (node2.Matching == null && node2.Match != MatchType.NoMatch)
                        {
                            SetMatching(node1, node2, match);
                            matched++;
                            break;
                        }
                    }
                }
            }

            return matched;
        }

        /// <summary>
        /// Set the match for the given node
        /// </summary>
        private static void SetMatching(XNode node1, MatchType match)
        {
            node1.Match = match;
        }

        /// <summary>
        /// Set the match for the given nodes to each other.
        /// </summary>
        private static void SetMatching(XNode node1, XNode node2, MatchType match)
        {
            node1.Match = match;
            node2.Match = match;
            node1.Matching = node2;
            node2.Matching = node1;
        }

        /// <summary>
        /// Set the match for the given nodes.
        /// </summary>
        private static void SetMatching(List<XNode> nodes, MatchType match)
        {
            for (var i = 0; i < nodes.Count; i++)
                nodes[i].Match = match;
        }

        /// <summary>
        /// Set the match for the given nodes.
        /// </summary>
        private static void SetMatching(XNode[] nodes, MatchType match)
        {
            for (var i = 0; i < nodes.Length; i++)
                nodes[i].Match = match;
        }

        /// <summary>
        /// Set the match for the given nodes if they do not have a matching node.
        /// </summary>
        /// <returns>The number of unmatched nodes that were updated</returns>
        private static int SetUnmatched(XNode[] nodes, MatchType match)
        {
            var count = 0;
            foreach (var node in nodes)
            {
                if (node.Matching == null && node.Match != MatchType.NoMatch)
                {
                    node.Match = match;
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Find minimal cost matching between two node lists; Record the matching info back to the trees.
        /// </summary>
        private static void MatchList(XNode[] nodes1, XNode[] nodes2, bool treeOrder)
        {
            var distance = new int[nodes1.Length + 1, nodes2.Length + 1];

            // Calculate insert cost.
            for (int i = 0; i < nodes2.Length; i++)
                distance[nodes1.Length, i] = nodes2[i].GetDescendantCount() + 1;

            for (int i = 0; i < nodes1.Length; i++)
            {
                // Calculate delete cost
                var deleteCost = nodes1[i].GetDescendantCount() + 1;
                distance[i, nodes2.Length] = deleteCost;

                for (int j = 0; j < nodes2.Length; j++)
                {
                    int dist = 0;
                    
                    dist = treeOrder
                        ? Distance(nodes1[i], nodes2[j], true, NoConnection)
                        : Distance(nodes2[j], nodes1[i], true, NoConnection);

                    if (dist < NoConnection)
                    {
                        var key = treeOrder
                            ? new Tuple<XNode, XNode>(nodes1[i], nodes2[j])
                            : new Tuple<XNode, XNode>(nodes2[j], nodes1[i]);

                        distanceLookup[key] = dist;
                    }

                    distance[i, j] = dist;
                }
            }

            // compute the minimal cost matching.
            var matching1 = new int[nodes1.Length];
            var matching2 = new int[nodes2.Length];
            FindMinimalMatching(distance, matching1, matching2);

            for (int i = 0; i < matching1.Length; i++)
            {
                if (matching1[i] == NoMatch)
                    SetMatching(nodes1[i], MatchType.NoMatch);
                else
                    SetMatching(nodes1[i], nodes2[matching1[i]], MatchType.Change);
            }

            for (int i = 0; i < matching2.Length; i++)
            {
                if (matching2[i] == NoMatch)
                    SetMatching(nodes2[i], MatchType.NoMatch);
                else
                    SetMatching(nodes2[i], nodes1[matching2[i]], MatchType.Change);
            }

            for (int i = 0; i < matching1.Length; i++)
            {
                if (matching1[i] != NoMatch)
                {
                    var node1 = nodes1[i];
                    var node2 = nodes2[matching1[i]];
                    if (node1.IsElement() && node2.IsElement())
                    {
                        if (treeOrder)
                            DiffElements(node1, node2);
                        else
                            DiffElements(node2, node1);
                    }
                }
            }
        }

        /// <summary>
        /// Calculate the editing distance between the two nodes (with caching)
        /// </summary>
        /// <returns>The minimal editing distance</returns>
        private static int Distance(XNode node1, XNode node2, bool toRecord, int threshold)
        {
            var isElement1 = node1.IsElement();
            var isElement2 = node2.IsElement();
            if (isElement1 && isElement2)
            {
                if (node1.Name != node2.Name)
                    return NoConnection;

                int dist = DistanceElements(node1, node2, threshold);
                if (toRecord && (dist < NoConnection))
                    distanceLookup[new Tuple<XNode, XNode>(node1, node2)] = dist;

                return dist;
            }

            if (!isElement1 && !isElement2)
                return 1;

            return NoConnection;
        }


        /// <summary>
        /// Calculate the editing distance between two elements, up to a maximum threshold.
        /// </summary>
        /// <returns>The minimal editing distance</returns>
        private static int DistanceElements(XNode node1, XNode node2, int threshold)
        {
            int dist = 0;

            // Distance of attributes.
            if (node1.Attributes.Length == 0)
                dist = node2.Attributes.Length * 2;
            else if (node2.Attributes.Length == 0)
                dist = node1.Attributes.Length * 2;
            else
                dist = DistanceAttributes(node1.Attributes, node2.Attributes);

            // Match second level nodes first.
            if (node1.Children.Length == 0)
            {
                foreach(var child2 in node2.Children)
                    dist += child2.GetDescendantCount() + 1;
            }
            else if (node2.Children.Length == 0)
            {
                foreach (var child1 in node1.Children)
                    dist += child1.GetDescendantCount() + 1;
            }
            else if (node1.Children.Length == 1 && node2.Children.Length == 1)
            {
                var child1 = node1.Children[0];
                var child2 = node2.Children[0];

                if (child1.HashEquals(child2.Hash))
                    return dist;

                var isElement1 = child1.IsElement();
                var isElement2 = child2.IsElement();

                if (isElement1 && isElement2)
                {
                    if (child1.Name == child2.Name)
                        dist += DistanceElements(child1, child1, threshold - dist);
                    else
                        dist += child1.GetDescendantCount() + child2.GetDescendantCount() + 2;
                }
                else if (!isElement1 && !isElement2)
                    dist++;
                else
                    dist += child1.GetDescendantCount() + child2.GetDescendantCount() + 2;
            }
            else
            {
                // Match text nodes.
                if (node1.Texts.Length == 0)
                    dist += node1.Texts.Length;
                else if (node2.Texts.Length == 0)
                    dist += node2.Texts.Length;
                else
                    dist += DistanceTexts(node1.Texts, node2.Texts);

                var elementCount1 = node1.Elements.Length;
                var elementCount2 = node2.Elements.Length;

                var matched1 = new bool[elementCount1];
                var matched2 = new bool[elementCount2];
                int matched = MatchFilter(node1.Elements, node2.Elements, matched1, matched2);

                if (elementCount1 == matched && elementCount2 == matched)
                    return dist;

                if (elementCount1 == matched)
                {
                    for (int i = 0; i < elementCount2; i++)
                    {
                        if (!matched2[i])
                            dist += node2.Elements[i].GetDescendantCount() + 1;
                    }
                    return dist;
                }

                if (elementCount2 == matched)
                {
                    for (int i = 0; i < elementCount1; i++)
                    {
                        if (!matched1[i])
                            dist += node1.Elements[i].GetDescendantCount() + 1;
                    }
                    return dist;
                }


                 // 'Match' remaining unmatched child elements nodes.
                int remaining1 = node1.Elements.Length - matched;
                int remaining2 = node2.Elements.Length - matched;
                int matchCount1 = 0;
                int matchCount2 = 0;

                while ((matchCount1 < remaining1) && (matchCount2 < remaining2))
                {
                    var unmatched1 = new List<XNode>();
                    var unmatched2 = new List<XNode>();
                    string name = null;

                    // Find and group unmatched elements by their name
                    foreach (var child1 in node1.Elements)
                    {
                        if (child1.Matching == null && child1.Match != MatchType.NoMatch)
                        {
                            if (name == null)
                                name = child1.Name;

                            if (name == child1.Name)
                            {
                                unmatched1.Add(child1);
                                matchCount1++;
                            }
                        }
                    }

                    // Find unmatched nodes in the other subtree with the same element name
                    foreach (var child2 in node2.Elements)
                    {
                        if (child2.Matching == null && child2.Match != MatchType.NoMatch)
                        {
                            if (name == child2.Name)
                            {
                                unmatched2.Add(child2);
                                matchCount2++;
                            }
                        }
                    }

                    if (unmatched2.Count == 0)
                    {
                        for (int i = 0; i < unmatched2.Count; i++)
                            dist += unmatched2[i].GetDescendantCount();
                    }
                    else
                    {
                        // To find minimal-cost matching between those unmatched elements
                        dist += (unmatched1.Count >= unmatched2.Count)
                            ? DistanceMatchList(unmatched1.ToArray(), unmatched2.ToArray(), true)
                            : DistanceMatchList(unmatched2.ToArray(), unmatched1.ToArray(), false);
                    }
                }

                if (matchCount1 < remaining1)
                {
                    for (int i = 0; i < elementCount1; i++)
                    {
                        if (!matched1[i])
                            dist += node1.Elements[i].GetDescendantCount();
                    }
                }
                else if (matchCount2 < remaining2)
                {
                    for (int i = 0; i < elementCount2; i++)
                    {
                        if (!matched2[i])
                            dist += node2.Elements[i].GetDescendantCount();
                    }
                }
            }

            if (dist < threshold)
                return dist;
            
            return NoConnection;
        }

        
        /// <summary>
        /// Calculate the editing distance between two lists of attributes
        /// </summary>
        private static int DistanceAttributes(XNode[] attributes1, XNode[] attributes2)
        {
            if (attributes1.Length == 1 && attributes2.Length == 1)
            {
                if (attributes1[0].HashEquals(attributes2[0].Hash))
                    return 0;

                return (attributes1[0].Name == attributes2[0].Name) ? 1 : 2;
            }

            var dist = 0;
            var matched = 0;
            var matching = new bool[attributes2.Length];
            for (int i = 0; i < attributes1.Length; i++)
            {
                var found = false;

                for (int j = 0; j < attributes2.Length; j++)
                {
                    if (matching[j])
                        continue;

                    else if (attributes1[i].HashEquals(attributes2[j].Hash))
                    {
                        matching[j] = true;
                        found = true;
                        matched++;
                        break;
                    }
                    else if (attributes1[i].Name == attributes2[j].Name)
                    {
                        matching[j] = true;
                        dist++;
                        found = true;
                        matched++;
                        break;
                    }
                }

                if (!found)
                    dist += 2;
            }

            dist += (attributes2.Length - matched) * 2;
            return dist;
        }

        /// <summary>
        /// Compute the editing distance between two groups of text nodes
        /// </summary>
        private static int DistanceTexts(XNode[] texts1, XNode[] texts2)
        {
            var matched = 0;
            var matching = new bool[texts2.Length];
            for (int i = 0; i < texts1.Length; i++)
            {
                for (int j = 0; j < texts2.Length; j++)
                {
                    if (!matching[j] && texts1[i].HashEquals(texts2[j].Hash))
                    {
                        matching[j] = true;
                        matched++;
                        break;
                    }
                }

                if (matched == texts2.Length)
                    break;
            }

            return texts1.Length >= texts2.Length
                ? texts1.Length - matched
                : texts2.Length - matched;
        }

        /// <summary>
        /// Compute the minimal editing distance between two lists of elements
        /// </summary>
        private static int DistanceMatchList(XNode[] nodes1, XNode[] nodes2, bool treeOrder)
	    {
            var distance = new int[nodes1.Length + 1, nodes2.Length + 1];
            var matching1 = new int[nodes1.Length];
            var matching2 = new int[nodes2.Length];

		    // Insert cost.
            for (int i = 0; i < nodes2.Length; i++)
                distance[nodes1.Length, i] = nodes2[i].GetDescendantCount() + 1;

            for (int i = 0; i < nodes1.Length; i++)
		    {
                // delete cost.
                int deleteCost = nodes1[i].GetDescendantCount() + 1;
                distance[i, nodes2.Length] = deleteCost;

                for (int j = 0; j < nodes2.Length; j++)
			    {
                    var dist = treeOrder 
                        ? Distance(nodes1[i], nodes2[j], true, NoConnection) 
                        : Distance(nodes2[j], nodes1[i], true, NoConnection);

				    if (dist < NoConnection)
				    {
					    var key = treeOrder
                            ? new Tuple<XNode, XNode>(nodes1[i], nodes2[j])
                            : new Tuple<XNode, XNode>(nodes2[j], nodes1[i]);
                        
						distanceLookup[key] = dist;
				    }
				    distance[i, j] = dist;
			    }
		    }

		    // compute the minimal cost matching.
            return FindMinimalMatching(distance, matching1, matching2);
	    }


        /// <summary>
        /// Filter out matched elements (equal hashes).
        /// </summary>
        /// <returns>The number of matched nodes</returns>
        private static int MatchFilter(XNode[] elements1, XNode[] elements2, bool[] matched1, bool[] matched2)
        {
            int matched = 0;
            for (int i = 0; i < elements2.Length; i++)
            {
                for (int j = 0; j < elements1.Length; j++)
                {
                    if (!matched1[j] && !matched2[i] && elements1[j].HashEquals(elements2[i].Hash))
                    {
                        matched1[j] = true;
                        matched2[i] = true;
                        matched++;
                        break;
                    }
                }
            }

            return matched;
        }

        /// <summary>
        /// Perform minimal-cost matching between two node lists
        /// </summary>
        private static int FindMinimalMatching(int[,] distance, int[] matching1, int[] matching2)
        {
            if (matching1.Length == 1)
            {
                // count2 == 1
                if (distance[0, 0] < NoConnection)
                {
                    matching1[0] = 0;
                    matching2[0] = 0;
                }
                else
                {
                    matching1[0] = Delete;
                    matching2[0] = Delete;
                }

                return distance[0, 0];
            }
            else if (matching2.Length == 1)
            {
                int dist = 0, mate = 0;
                int minDistance = NoConnection;

                matching2[0] = Delete;
                for (int i = 0; i < matching1.Length; i++)
                {
                    matching1[i] = Delete;
                    if (minDistance > distance[i, 0])
                    {
                        minDistance = distance[i, 0];
                        mate = i;
                    }

                    // Suppose we delete every node on list1.
                    dist += distance[i, 1];
                }

                if (minDistance < NoConnection)
                {
                    matching1[mate] = 0;
                    matching2[0] = mate;
                    dist += minDistance - distance[mate, 1];
                }
                else
                {
                    // Add the delete cost of the single node on list2.
                    dist += distance[matching1.Length, 0];
                }

                return dist;
            }
            else if (matching1.Length == 2 && matching2.Length == 2)
            {
                int dist1 = distance[0, 0] + distance[1, 1];
                int dist2 = distance[0, 1] + distance[1, 0];
                if (dist1 < dist2)
                {
                    if (distance[0, 0] < NoConnection)
                    {
                        matching1[0] = 0;
                        matching2[0] = 0;
                        dist1 = distance[0, 0];
                    }
                    else
                    {
                        matching1[0] = Delete;
                        matching2[0] = Delete;
                        dist1 = distance[0, 2] + distance[2, 0];
                    }

                    if (distance[1, 1] < NoConnection)
                    {
                        matching1[1] = 1;
                        matching2[1] = 1;
                        dist1 += distance[1, 1];
                    }
                    else
                    {
                        matching1[1] = Delete;
                        matching2[1] = Delete;
                        dist1 += distance[1, 2] + distance[2, 1];
                    }

                    return dist1;
                }
                else
                {
                    if (distance[0, 1] < NoConnection)
                    {
                        matching1[0] = 1;
                        matching2[1] = 0;
                        dist2 = distance[0, 1];
                    }
                    else
                    {
                        matching1[0] = Delete;
                        matching2[1] = Delete;
                        dist2 = distance[0, 2] + distance[2, 1];
                    }

                    if (distance[1, 0] < NoConnection)
                    {
                        matching1[1] = 0;
                        matching2[0] = 1;
                        dist2 += distance[1, 0];
                    }
                    else
                    {
                        matching1[1] = Delete;
                        matching2[0] = Delete;
                        dist2 += distance[1, 2] + distance[2, 0];
                    }

                    return dist2;
                }
            }

            return DoMinimalMatching(distance, matching1, matching2);
        }

        /// <summary>
        /// Perform minimal-cost matching algorithm between two node lists
        /// </summary>
        private static int DoMinimalMatching(int[,] distance, int[] matching1, int[] matching2)
        {
            // Initialize matching.  Initial guess will be pair-matching between two lists.
            //  Others will be insertion or deletion
            for (int i = 0; i < matching2.Length; i++)
                matching1[i] = i;
            for (int i = matching2.Length; i < matching1.Length; i++)
                matching1[i] = Delete;

            // Three artificial nodes: "start", "end" and "delete".
            int nodeCount = matching1.Length + matching2.Length + 3;

            while (true)
            {
                // Construct least cost matrix.
                var costMatrix = new int[nodeCount, nodeCount];
                ConstructCostMatrix(distance, matching1, matching2, nodeCount, costMatrix);

                // Initialize path matrix.
                var pathMatrix = new int[nodeCount, nodeCount];
                for (int i = 0; i < nodeCount; i++)
                    for (int j = 0; j < nodeCount; j++)
                        pathMatrix[i, j] = i;

                // Search negative cost circuit.
                var circuit = new int[MaxCircuitLength];
                int circuitLength = SearchNegativeCircuit(nodeCount, costMatrix, pathMatrix, circuit);
                if (circuitLength > 0)
                {
                    // Modify matching.
                    for (int i = 0, next = 0; i < circuitLength - 1; i++)
                    {
                        int n1 = circuit[next];
                        next = circuit[next + 1];
                        // Node in node list 1.
                        if ((n1 > 0) && (n1 <= matching1.Length))
                        {
                            int nid1 = n1 - 1;
                            int nid2 = circuit[next] - matching1.Length - 1;
                            if (nid2 == matching2.Length)
                                nid2 = Delete;

                            matching1[nid1] = nid2;
                        }
                    }
                }
                else // Stop.
                    break;
            }

            int dist = 0;

            // Suppose all insertion on list2
            for (int i = 0; i < matching2.Length; i++)
            {
                matching2[i] = Insert;
                dist += distance[matching1.Length, i];
            }

            // Update distance by looking at matching pairs.
            for (int i = 0; i < matching1.Length; i++)
            {
                int mate = matching1[i];
                if (mate == Delete)
                    dist += distance[i, matching2.Length];
                else
                {
                    matching2[mate] = i;
                    dist += distance[i, mate] -
                            distance[matching1.Length, mate];
                }
            }

            return dist;
        }

        /// <summary>
        /// Construct a least cost matrix (of the flow network) based on the distance matrix
        /// </summary>
        private static void ConstructCostMatrix(int[,] distance, int[] matching1, int[] matching2, int nodeCount, int[,] costMatrix)
        {
            // Initialize.
            for (int i = 0; i < nodeCount; i++)
            {
                for (int j = 0; j < nodeCount; j++)
                    costMatrix[i, j] = NoConnection;

                // self.
                costMatrix[i, i] = 0;
            }

            // Between start node and nodes in list 1.
            // Start -> node1 = Infinity; node1 -> Start = -0.
            for (int i = 0; i < matching1.Length; i++)
                costMatrix[i + 1, 0] = 0;

            // Between nodes in list2 and the end node.
            // Unless matched (later), node2 -> end = 0;
            // end -> node2 = Infinity.
            for (int i = 0; i < matching2.Length; i++)
                costMatrix[i + matching1.Length + 1, nodeCount - 1] = 0;

            int deleteCount = 0;

            // Between nodes in list1 and nodes in list2.
            // For matched, node1 -> node2 = Infinity;
            // node2 -> node1 = -1 * distance
            // For unmatched, node1 -> node2 = distance;
            // node2 -> node1 = Infinity
            for (int i = 0; i < matching1.Length; i++)
            {
                int node1 = i + 1;
                int node2;

                // According to cost matrix.
                for (int j = 0; j < matching2.Length; j++)
                {
                    node2 = j + matching1.Length + 1;
                    costMatrix[node1, node2] = distance[i, j];
                }

                // According to matching.
                if (matching1[i] == Delete)
                {
                    deleteCount++;

                    // node1 -> Delete = Infinity;
                    // Delete -> node1 = -1 * DELETE_COST
                    costMatrix[nodeCount - 2, node1] = -1 * distance[i, matching2.Length];
                }
                else
                {
                    node2 = matching1[i] + matching1.Length + 1;

                    // Between node1 and node2.
                    costMatrix[node1, node2] = NoConnection;
                    costMatrix[node2, node1] = distance[i, matching1[i]] * -1;

                    // Between node1 and delete.
                    costMatrix[node1, nodeCount - 2] = distance[i, matching2.Length];

                    // Between node2 and end.
                    costMatrix[node2, nodeCount - 1] = NoConnection;
                    costMatrix[nodeCount - 1, node2] = distance[matching1.Length, matching1[i]];
                }
            }

            // Between the "Delete" and the "End".
            // If delete all, delete -> end = Infinity; end -> delete = 0.
            if (deleteCount == matching1.Length)
                costMatrix[nodeCount - 1, nodeCount - 2] = 0;
            // if no delete, delete -> end = 0; end -> delete = Infinity.
            else if (deleteCount == 0)
                costMatrix[nodeCount - 2, nodeCount - 1] = 0;
            // else, both 0;
            else
            {
                costMatrix[nodeCount - 2, nodeCount - 1] = 0;
                costMatrix[nodeCount - 1, nodeCount - 2] = 0;
            }
        }

        /// <summary>
        /// Search for negative cost circuit in the least cost matrix.
        /// </summary>
        /// <returns>The length of the circuit</returns>
        private static int SearchNegativeCircuit(int nodeCount, int[,] costMatrix, int[,] pathMatrix, int[] circuit)
        {
            for (int k = 0; k < nodeCount; k++)
            {
                for (int i = 0; i < nodeCount; i++)
                {
                    if (i != k && costMatrix[i, k] != NoConnection)
                    {
                        for (int j = 0; j < nodeCount; j++)
                        {
                            if ((j != k && costMatrix[k, j] != NoConnection))
                            {
                                int less = costMatrix[i, k] + costMatrix[k, j];
                                if (less < costMatrix[i, j])
                                {
                                    costMatrix[i, j] = less;
                                    pathMatrix[i, j] = k;

                                    // Found!
                                    if ((i == j) && (less < 0))
                                    {
                                        // Locate the circuit.
                                        circuit[0] = i;
                                        circuit[1] = 2;
                                        circuit[2] = pathMatrix[i, i];
                                        circuit[3] = 4;
                                        circuit[4] = i;
                                        circuit[5] = -1;

                                        int circuitLength = 3;
                                        bool finish;

                                        do
                                        {
                                            finish = true;
                                            for (int cit = 0, n = 0; cit < circuitLength - 1; cit++)
                                            {
                                                int left = circuit[n];
                                                int next = circuit[n + 1];
                                                int right = (next == -1) ? -1 : circuit[next];

                                                int middle = pathMatrix[left, right];
                                                if (middle != left)
                                                {
                                                    circuit[circuitLength * 2] = middle;
                                                    circuit[circuitLength * 2 + 1] = next;
                                                    circuit[n + 1] = circuitLength * 2;
                                                    circuitLength++;

                                                    finish = false;
                                                    break;
                                                }
                                                n = next;
                                            }
                                        } while (!finish);

                                        return circuitLength;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return 0;
        }

    }
}
