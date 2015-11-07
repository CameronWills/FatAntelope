using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FatAntelope.Utility
{
    public static class ArrayExtensions
    {
        public static void ToCsvFile<T>(this T[,] array, string filename)
        {
            var enumerator = array.Cast<T>().Select((s, i) => 
                (i + 1) % array.GetLength(0) == 0 
                    ? string.Concat(s, Environment.NewLine) 
                    : string.Concat(s, ","));

            var result = string.Join(string.Empty, enumerator.ToArray<string>());
            File.WriteAllText(filename, result);
        }
    }
}
