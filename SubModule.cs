﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
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
				CampaignGameStarter campaignStarter = (CampaignGameStarter)gameStarterObject;

				bool settingsLoaded = LoadSettings();

				if (settingsLoaded) {
					XDocument modSettings = ReadXml("Config");
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
				return false;
			}

			if (File.Exists(CustomFilePath)) {
				XmlDocument defaultItems = MBObjectManager.ToXmlDocument(XDocument.Load(DefaultFilePath));
				XmlDocument customItems = MBObjectManager.ToXmlDocument(XDocument.Load(CustomFilePath));

				bool shouldOverride = bool.Parse(customItems.GetElementsByTagName("Override")[0].InnerText);

				if (shouldOverride) {
					settings = ReadXml("CustomItems");
				} else {
					XmlDocument mergedItems = MBObjectManager.MergeTwoXmls(defaultItems, customItems);

					settings = MergeItemsInXDocument(MBObjectManager.ToXDocument(mergedItems));
				}
			} else {
				settings = ReadXml("DefaultItems");
			}

			if (Directory.Exists(ModsDir)) {
				string[] files = Directory.GetFiles(ModsDir);

				if (files.Length < 1) {
					return true;
				}

				foreach (string file in files) {
					if (!file.EndsWith(".xml")) {
						continue;
					}

					XmlDocument modXml = MBObjectManager.ToXmlDocument(XDocument.Load(file));
					XmlDocument mergedItems = MBObjectManager.MergeTwoXmls(modXml, MBObjectManager.ToXmlDocument(settings));

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
		private XDocument MergeItemsInXDocument(XDocument xDocument) {
			List<XName> duplicates = xDocument.ListDuplicates();

			if (duplicates.Count > 0) {
				// iterate over all representative factions
				// eg. if there are two Vlandias, it will run once (for Vlandia)
				// if there are two Vlandias and three Sturgias, it will run twice (for Vlandia and Sturgia)
				foreach (XName faction in duplicates) {
					IEnumerable<XElement> factionDuplicates = xDocument.Descendants(faction);
					XElement factionToAdd = factionDuplicates.First();

					// iterate over all actual duplicate elements (eg. both Vlandias)
					foreach (XElement duplicate in factionDuplicates) {
						// ignore first occurrence (we're moving items to it)
						if (duplicate.Equals(factionToAdd)) {
							continue;
						}

						// blanks will be removed later
						if (!duplicate.HasElements) {
							continue;
						}

						foreach (XElement item in duplicate.Elements()) {
							factionToAdd.AddFirst(item);
						}

						// finally, clear this duplicate
						duplicate.Elements().Remove();
					}
				}
			}

			xDocument.Descendants().Where(e => e.IsEmpty && !e.HasAttributes).Remove();

			return xDocument;
		}
	}
}