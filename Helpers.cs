using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace VsRoyalArmoryRewritten {
	public static class Helpers {
        /// <summary>
        /// Makes first character of string uppercase.
        /// </summary>
        public static string ToProper(this string value) {
            char[] valueChars = value.ToCharArray();
            valueChars[0] = char.ToUpper(valueChars[0]);
            return new string(valueChars);
		}

        public static List<XName> ListDuplicates(this XDocument xDoc) {
            // find duplicates and return a representative of them
            var duplicates = xDoc.Root.Elements().GroupBy(x => x.Name)
                                .Where(g => g.Count() > 1)
                                .Select(y => y.Key)
                                .ToList();

            // ignore Override in the result, it doesn't matter at this point
            duplicates.Remove("Override");

            return duplicates;
        }

        public static int ToInt(this XAttribute attr) {
            return int.Parse(attr.Value);
        }
    }
}
