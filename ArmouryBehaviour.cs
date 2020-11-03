﻿using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using VsRoyalArmoryRewritten.Config;

namespace VsRoyalArmoryRewritten {
	public class ArmouryBehaviour : CampaignBehaviorBase {
		private readonly Settings _settings;
		

		public ArmouryBehaviour(Settings settings) {
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

			string townCulture = Settlement.CurrentSettlement.OwnerClan.Kingdom.Culture.StringId;
			if (Helpers.Cultures.Contains(townCulture)) {
				return true;
			}
			return false;
		}

		private void OnConsequence(MenuCallbackArgs args) {
			ItemRoster armoury = new ItemRoster();

			PopulateItemList(Settlement.CurrentSettlement.Culture.StringId, armoury);
			
			InventoryManager.OpenScreenAsTrade(armoury, Settlement.CurrentSettlement.Town);
		}

		private void PopulateItemList(string culture, ItemRoster armoury) {
			var cultureObj = _settings.GetFactionFromString(culture);

			if (cultureObj is null || cultureObj.Items.Count < 1) {
				return;
			}

			foreach (var item in cultureObj.Items) {
				try {
					armoury.AddToCounts(MBObjectManager.Instance.GetObject<ItemObject>(item.Name), MBRandom.RandomInt(item.MinCount, item.MaxCount));
				} catch { }
			}
		}

		public override void SyncData(IDataStore dataStore) {
		}
	}
}
