using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace FatAntelope.Writers
{
    public abstract class BaseDiffWriter
    {
        public abstract void WriteDiff(XTree tree, string file);
    }
}
