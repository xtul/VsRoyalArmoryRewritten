using System;
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
		/// Fills the <paramref name="armoury"/> with items of given <paramref name="culture"/>, if found.
		/// </summary>
		/// <param name="armoury"></param>
		/// <param name="culture"></param>
		private void PopulateItemList(ItemRoster armoury, string culture) {
			//	<Vlandia> -- cultureElement
			//		<Item /> -- cultureItem
			//		...
			//	</Vlandia>
			var cultureElement = _settings.Descendants(culture.ToProper()).FirstOrDefault();
			if (cultureElement is null) return;

			var cultureItems = cultureElement.Descendants("Item");
			if (cultureItems.Count() < 1) return;

			foreach (var item in cultureItems) {
				try {
					var itemId = item.Attribute("name").Value;
					var rng = MBRandom.RandomInt(item.Attribute("minCount").ToInt(), item.Attribute("maxCount").ToInt());

					armoury.AddToCounts(MBObjectManager.Instance.GetObject<ItemObject>(itemId), rng);
				} catch { }
			}

			// also add <Any> tag that will add items to any armoury in game
			// don't add if "Any" culture was provided to prevent infinite loop
			if (culture == "Any") return;
			PopulateItemList(armoury, "Any");
		}

		public override void SyncData(IDataStore dataStore) {
		}
	}
}
