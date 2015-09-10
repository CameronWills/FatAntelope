using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConfigDiff
{
    public class XTree
    {
        Dictionary<string, List<XNode>> nodes = new Dictionary<string, List<XNode>>();

        public void Add(XNode node, string path)
        {
            if (nodes.ContainsKey(path))
            {
                nodes[path].Add(node);
            }
            else
            {
                var list = nodes[path] = new List<XNode>();
                list.Add(node);
            }
        }
    }
}
