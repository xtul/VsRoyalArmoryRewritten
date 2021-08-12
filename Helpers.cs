using System.Collections.Generic;
using System.Linq;
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
            List<XName> duplicates = xDoc.Root.Elements().GroupBy(x => x.Name).Where(g => g.Count() > 1).Select(y => y.Key).ToList();

            duplicates.Remove("Override");

            return duplicates;
        }

        public static int ToInt(this XAttribute attr) {
            return int.Parse(attr.Value);
        }
    }
}
