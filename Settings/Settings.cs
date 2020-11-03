using System.Collections.Generic;
using System.Xml.Serialization;

namespace VsRoyalArmoryRewritten.Config {

	[XmlRoot(ElementName = "Settings")]
	public class Settings {
		public bool Override { get; set; }
		public Vlandia Vlandia { get; set; }
		public Sturgia Sturgia { get; set; }
		public Aserai Aserai { get; set; }
		public Battania Battania { get; set; }
		public Khuzait Khuzait { get; set; }
		public Empire Empire { get; set; }

		public Faction GetFactionFromString(string str) {
			try {
				switch (str.ToLower()) {
					case "vlandia":
						return Vlandia;
					case "sturgia":
						return Sturgia;
					case "aserai":
						return Aserai;
					case "battania":
						return Battania;
					case "khuzait":
						return Khuzait;
					case "empire":
						return Empire;
					default:
						return null;
				}
			} catch { }
			return null;
 		}
	}


	[XmlRoot(ElementName = "Item")]
	public class Item {
		[XmlAttribute(AttributeName = "name")]
		public string Name { get; set; }
		[XmlAttribute(AttributeName = "minCount")]
		public int MinCount { get; set; }
		[XmlAttribute(AttributeName = "maxCount")]
		public int MaxCount { get; set; }
	}

	public abstract class Faction {
		[XmlElement(ElementName = "Item")]
		public List<Item> Items { get; set; }
	}

	[XmlRoot(ElementName = "Sturgia")]
	public class Sturgia : Faction { }
	[XmlRoot(ElementName = "Vlandia")]
	public class Vlandia : Faction { }
	[XmlRoot(ElementName = "Aserai")]
	public class Aserai : Faction { }
	[XmlRoot(ElementName = "Battania")]
	public class Battania : Faction { }
	[XmlRoot(ElementName = "Khuzait")]
	public class Khuzait : Faction { }
	[XmlRoot(ElementName = "Empire")]
	public class Empire : Faction { }
	
}
