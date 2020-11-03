using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace VsRoyalArmoryRewritten {
	public static class Helpers {
        public readonly static string[] Cultures = {
            "battania",
            "aserai",
            "sturgia",
            "vlandia",
            "empire",
            "khuzait"
        };

        /// <summary>
        /// Makes first character of string uppercase.
        /// </summary>
        public static string ToProper(this string str) {
            var strSplit = str.ToCharArray();
            strSplit[0] = char.ToUpper(strSplit[0]);
            return new string(strSplit);
		}

        public class ElementComparer : EqualityComparer<XElement> {
            public override int GetHashCode(XElement xe) {
                return xe.Name.GetHashCode() ^ xe.Value.GetHashCode();
            }

            public override bool Equals(XElement xe1, XElement xe2) {
                var result = xe1.Name.Equals(xe2.Name);
                if (result) {
                    result = xe1.FirstAttribute.Value.Equals(xe2.FirstAttribute.Value);
                }
                return result;
            }
        }
    }
}
