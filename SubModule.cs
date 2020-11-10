using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using VsRoyalArmoryRewritten.Config;
using static VsRoyalArmoryRewritten.Helpers;

namespace VsRoyalArmoryRewritten {
	public class SubModule : MBSubModuleBase {
		public readonly string SettingsDir = BasePath.Name + "Modules/VsRoyalArmoryRewritten/bin/Win64_Shipping_Client/";
		public readonly string DefaultFilePath;
		public readonly string CustomFilePath;
		public Settings settings;

		public SubModule() {
			DefaultFilePath = SettingsDir + "DefaultItems.xml";
			CustomFilePath = SettingsDir + "CustomItems.xml";
		}


		/// <summary>
		/// Registers the mod when campaign starts.
		/// </summary>
		protected override void OnGameStart(Game game, IGameStarter gameStarterObject) {
			if (game.GameType is Campaign) {
				var campaignStarter = (CampaignGameStarter)gameStarterObject;

				var settingsLoadedOk = LoadSettings();

				if (settingsLoadedOk) {
					campaignStarter.AddBehavior(new ArmouryBehaviour(settings));
				}
			}
		}


		/// <summary>
		/// Loads settings from XML files into memory. Fails if there is no default file. 
		/// If both default and custom files are present, checks if custom has "override"
		/// element and either merges both lists or only uses custom.
		/// </summary>
		/// <returns><see langword="true"/> if successful, <see langword="false"/> if failed.</returns>
		private bool LoadSettings() {
			if (!File.Exists(DefaultFilePath)) {
				return false; // not even default file exists, failing
			}

			var serializer = new XmlSerializer(typeof(Settings));

			if (File.Exists(CustomFilePath)) {
				var defaultItems = MBObjectManager.ToXmlDocument(XDocument.Load(DefaultFilePath));
				var customItems = MBObjectManager.ToXmlDocument(XDocument.Load(CustomFilePath));

				var overrideValue = customItems.GetElementsByTagName("Override")[0].InnerText;

				// if <Override> tag is false, try to merge
				if (overrideValue == "false") {
					var mergedItems = MBObjectManager.MergeTwoXmls(defaultItems, customItems);
				 
				 	settings = ReadAndMergeItemList(MBObjectManager.ToXDocument(mergedItems), serializer);
				} else {
					settings = ReadItemList("CustomItems", serializer);
				}
			} else {
				settings = ReadItemList("DefaultItems", serializer);
			}

			return true;
		}

		/// <summary>
		/// Reads item list from provided <paramref name="filename"/>.
		/// </summary>
		/// <param name="filename">Name of XML file without '.xml'.</param>
		/// <returns>XML file as an object.</returns>
		private Settings ReadItemList(string filename, XmlSerializer serializer) {
			Settings result;

			using (var reader = new StreamReader(SettingsDir + filename + ".xml")) {
				result = serializer.Deserialize(reader) as Settings;
			}

			return result;
		}


		/// <summary>
		/// Reads item list from provided <see cref="XDocument"/>
		/// and merges them if there are multiple faction entries.
		/// </summary>
		private Settings ReadAndMergeItemList(XDocument xDoc, XmlSerializer serializer) {
			var duplicates = xDoc.ListDuplicates();

			// if there are multiple faction entries, merge them
			if (duplicates.Count > 0) {
				// iterate over all representative factions
				// eg. if there are two Vlandias, it will run once (for Vlandia)
				// if there are two Vlandias and three Sturgias, it will run twice (for Vlandia and Sturgia)
				foreach (var faction in duplicates) {
					var factionDuplicates = xDoc.Descendants(faction);
					var factionToAdd = factionDuplicates.First();

					// iterate over all actual duplicate elements (eg. both Vlandias from above example)
					foreach (var duplicate in factionDuplicates) {
						// ignore first occurrence (we're moving items to it)
						if (duplicate.Equals(factionToAdd)) {
							continue;
						}

						// blanks will be removed later
						if (!duplicate.HasElements) {
							continue;
						}

						foreach (var item in duplicate.Elements()) {
							factionToAdd.AddFirst(item);
						}

						// finally, clear this duplicate
						duplicate.Elements().Remove();
					}
				}
			}

			// remove blank factions
			xDoc.Descendants().Where(e => e.IsEmpty && e.Name != "Item").Remove();

			return serializer.Deserialize(xDoc.CreateReader()) as Settings;
		}
	}
}