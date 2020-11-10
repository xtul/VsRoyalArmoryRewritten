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
		

		public ArmouryBehaviour(XDocument settings) {
			_settings = settings;
		}

		public override void RegisterEvents() {
			CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(OnCampaignStarted));
		}

		private void OnCampaignStarted(CampaignGameStarter campaignGameStarter) {
			campaignGameStarter.AddGameMenuOption("town_keep", "armoury", "Access the Armoury", OnCondition, OnConsequence, false, 99);
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

			PopulateItemList(townCulture, armoury);
			
			InventoryManager.OpenScreenAsTrade(armoury, Settlement.CurrentSettlement.Town);
		}

		private void PopulateItemList(string culture, ItemRoster armoury) {
			//	<Vlandia> -- cultureElement
			//		<Item /> -- cultureItem
			//		...
			//	</Vlandia>
			var cultureElement = _settings.Descendants(culture.ToProper()).FirstOrDefault();
			if (cultureElement is null) return;

			var cultureItems = cultureElement.Descendants("item".ToProper());
			if (cultureItems.Count() < 1) return;

			foreach (var item in cultureItems) {
				try {
					var itemId = item.Attribute("name").ToItemId();
					var rng = MBRandom.RandomInt(item.Attribute("minCount").ToInt(), item.Attribute("maxCount").ToInt());

					armoury.AddToCounts(MBObjectManager.Instance.GetObject<ItemObject>(itemId), rng);
				} catch { }
			}
		}

		public override void SyncData(IDataStore dataStore) {
		}
	}
}
