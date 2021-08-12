using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace VsRoyalArmoryRewritten {
	public class ArmouryBehaviour : CampaignBehaviorBase {
		private readonly XDocument _settings;
		private readonly XDocument _modSettings;

		public ArmouryBehaviour(XDocument settings, XDocument modSettings) {
			_settings = settings;
			_modSettings = modSettings;
		}

		public override void RegisterEvents() {
			CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(OnCampaignStarted));
		}

		private void OnCampaignStarted(CampaignGameStarter campaignGameStarter) {
			int index = 0;
			try {
				index = int.Parse(_modSettings.Descendants("IndexInMenu").FirstOrDefault().Value);
			} catch { }

			campaignGameStarter.AddGameMenuOption("town_keep", "armoury", "Access the Armoury", OnCondition, OnConsequence, false, index);
		}

		private bool OnCondition(MenuCallbackArgs args) {
			args.optionLeaveType = GameMenuOption.LeaveType.Trade;

			return true;
		}

		private void OnConsequence(MenuCallbackArgs args) {
			ItemRoster armoury = new ItemRoster();

			string townCulture = "vlandia";

			try {
				townCulture = Settlement.CurrentSettlement.OwnerClan.Kingdom.Culture.StringId;
			} catch { }

			PopulateItemList(armoury, townCulture);
			
			InventoryManager.OpenScreenAsTrade(armoury, Settlement.CurrentSettlement.Town);
		}


		/// <summary>
		/// Fills the <paramref name="armoury"/> with items of given <paramref name="cultureName"/>, if found.
		/// </summary>
		private void PopulateItemList(ItemRoster armoury, string cultureName) {
			XElement cultureElement = _settings.Descendants(cultureName.ToProper()).FirstOrDefault();
			if (cultureElement is null) return;

			IEnumerable<XElement> cultureItems = cultureElement.Descendants("Item");
			if (cultureItems.Count() < 1) return;

			foreach (XElement item in cultureItems) {
				try {
					int rng = MBRandom.RandomInt(item.Attribute("minCount").ToInt(), item.Attribute("maxCount").ToInt());
					string itemId = item.Attribute("name").Value;
					ItemObject itemToAdd = MBObjectManager.Instance.GetObject<ItemObject>(itemId);

					armoury.AddToCounts(itemToAdd, rng);
				} catch { }
			}

			if (cultureName == "Any") return;
			PopulateItemList(armoury, "Any");
		}

		public override void SyncData(IDataStore dataStore) { }
	}
}
