using System.IO;
using System.Linq;
using System.Xml.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace VsRoyalArmoryRewritten {
	public class SubModule : MBSubModuleBase {
		private readonly string SettingsDir = BasePath.Name + "Modules/VsRoyalArmoryRewritten/bin/Win64_Shipping_Client/";
		private readonly string ModsDir;
		private readonly string DefaultFilePath;
		private readonly string CustomFilePath;
		private XDocument settings;

		public SubModule() {
			DefaultFilePath = SettingsDir + "DefaultItems.xml";
			CustomFilePath = SettingsDir + "CustomItems.xml";
			ModsDir = SettingsDir + "Mods/";
		}

		/// <summary>
		/// Registers the mod when campaign starts.
		/// </summary>
		protected override void OnGameStart(Game game, IGameStarter gameStarterObject) {
			if (game.GameType is Campaign) {
				var campaignStarter = (CampaignGameStarter)gameStarterObject;

				var settingsLoadedOk = LoadSettings();

				if (settingsLoadedOk) {
					var modSettings = ReadXml("Config");

					// if everything went alright, add behaviour with a fully processed XML
					campaignStarter.AddBehavior(new ArmouryBehaviour(settings, modSettings));
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

			if (File.Exists(CustomFilePath)) {
				var defaultItems = MBObjectManager.ToXmlDocument(XDocument.Load(DefaultFilePath));
				var customItems = MBObjectManager.ToXmlDocument(XDocument.Load(CustomFilePath));

				var overrideValue = customItems.GetElementsByTagName("Override")[0].InnerText;

				// if <Override> tag is false, try to merge
				if (overrideValue == "false") {
					var mergedItems = MBObjectManager.MergeTwoXmls(defaultItems, customItems);
				 
				 	settings = MergeItemsInXDocument(MBObjectManager.ToXDocument(mergedItems));
				} else {
					settings = ReadXml("CustomItems");
				}
			} else {
				settings = ReadXml("DefaultItems");
			}

			// if "Mods" directory was found...
			if (Directory.Exists(ModsDir)) {
				// get all files in this dir
				var files = Directory.GetFiles(ModsDir);

				if (files.Length < 1) {
					return true;
				}

				foreach (var file in files) {
					// if it isn't an xml, don't process it
					if (!file.EndsWith(".xml")) {
						continue;
					}

					// do the same as with default + custom merge
					var modXml = MBObjectManager.ToXmlDocument(XDocument.Load(file));
					var mergedItems = MBObjectManager.MergeTwoXmls(modXml, MBObjectManager.ToXmlDocument(settings));

					settings = MergeItemsInXDocument(MBObjectManager.ToXDocument(mergedItems));
				}
			}

			return true;
		}

		/// <summary>
		/// Reads XML from provided <paramref name="filename"/>.
		/// </summary>
		/// <param name="filename">Name of XML file without '.xml'.</param>
		/// <returns>XML file as a XDocument.</returns>
		private XDocument ReadXml(string filename) {
			return XDocument.Load(SettingsDir + filename + ".xml");
		}

		/// <summary>
		/// Reads item list from provided <see cref="XDocument"/>
		/// and merges them if there are multiple faction entries.
		/// </summary>
		private XDocument MergeItemsInXDocument(XDocument xDoc) {
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
			xDoc.Descendants().Where(e => e.IsEmpty && !e.HasAttributes).Remove();

			return xDoc;
		}
	}
}