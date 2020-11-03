using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Serialization;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using VsRoyalArmoryRewritten.Config;

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

			XDocument defaultItems, customItems;

			var serializer = new XmlSerializer(typeof(Settings));

			if (File.Exists(CustomFilePath)) {
				defaultItems = XDocument.Load(SettingsDir + "DefaultItems.xml");
				customItems = XDocument.Load(SettingsDir + "CustomItems.xml");
				bool shouldMerge = false;

				foreach (var element in customItems.Descendants("Override")) {
					if (element.Value == bool.FalseString) {
						// if Override == false, merge settings
						shouldMerge = true;
					}
				}

				if (shouldMerge) {
					var mergedItems = defaultItems.Descendants("Settings")
												.Union(customItems.Descendants("Settings"))
												.First().Document;

					settings = ReadItemList(mergedItems, serializer);
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
		/// Reads item list from <see cref="XDocument"/>.
		/// </summary>
		/// <param name="xdoc">XDocument to read from.</param>
		/// <returns><see cref="XDocument"/> as an object.</returns>
		private Settings ReadItemList(XDocument xdoc, XmlSerializer serializer) {
			return serializer.Deserialize(xdoc.CreateReader()) as Settings;
		}
	}
}