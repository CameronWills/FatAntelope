using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConfigDiff.Utility
{
    /// <summary>
    /// Compares byte arrays. Arrays must have equal length.
    /// </summary>
    class ByteArrayComparer : IComparer<byte[]>
    {
        public int Compare(byte[] x, byte[] y)
        {
            if (x.Length != y.Length)
                throw new Exception("Arrays must have equal length for comparison.");

            for (int i = 0; i < x.Length; i++)
            {
                if (x[i] > y[i])
                    return 1;
                else if (x[i] < y[i])
                    return -1;
            }

            return 0;
        }
    }
}
