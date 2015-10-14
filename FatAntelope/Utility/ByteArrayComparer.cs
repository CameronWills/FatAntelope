using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FatAntelope.Utility
{
    /// <summary>
    /// Compares byte arrays. Arrays must have equal length.
    /// </summary>
    class ByteArrayComparer : IComparer<byte[]>
    {
        public static readonly ByteArrayComparer Instance = new ByteArrayComparer();
        
        public int Compare(byte[] x, byte[] y)
        {
            if (x.Length != y.Length)
                throw new Exception("Arrays must have equal length for comparison.");

            for (int i = 0; i < x.Length; i++)
            {
                if (x[i] > y[i])
                    return 1;
                if (x[i] < y[i])
                    return -1;
            }

            return 0;
        }
    }
}
