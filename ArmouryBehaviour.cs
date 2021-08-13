using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using TaleWorlds.Localization;

namespace VsRoyalArmoryRewritten {
	public class ArmouryBehaviour : CampaignBehaviorBase {
		private readonly XDocument _settings;
		private readonly XDocument _modSettings;
		private bool _entryFeeEnabled;
		private string _armouryText = string.Empty;
		private int _entryCost = -1;
		private PaymentMethod _paymentMethod = PaymentMethod.None;

		public ArmouryBehaviour(XDocument settings, XDocument modSettings) {
			_settings = settings;
			_modSettings = modSettings;
		}

		public override void RegisterEvents() {
			CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(OnCampaignStarted));
		}

		private void OnCampaignStarted(CampaignGameStarter campaignGameStarter) {
			_entryFeeEnabled = bool.Parse(_modSettings.Descendants("EntryFeeEnabled").FirstOrDefault().Value);

			if (int.TryParse(_modSettings.Descendants("IndexInMenu").FirstOrDefault().Value, out int index)) {
				campaignGameStarter.AddGameMenuOption("town_keep", "armoury", "Enter the Armoury", OnCondition, OnConsequence, false, index);
			}
		}

		private bool OnCondition(MenuCallbackArgs args) {
			CalculateEntryCost(Settlement.CurrentSettlement);
			args.Tooltip = new TextObject(_armouryText);
			if (_paymentMethod == PaymentMethod.Disabled) {
				args.IsEnabled = false;
			}

			args.optionLeaveType = GameMenuOption.LeaveType.Trade;
			return true;
		}

		private void OnConsequence(MenuCallbackArgs args) {
			if (_paymentMethod == PaymentMethod.Disabled) return;

			switch (_paymentMethod) {
				case PaymentMethod.Gold:
					int gold = Hero.MainHero.Gold;
					if (gold < _entryCost) {
						return;
					}
					Hero.MainHero.ChangeHeroGold(_entryCost * -1);
					break;
				case PaymentMethod.Influence:
					float influence = Hero.MainHero.Clan.Influence;
					if (influence < _entryCost) {
						return;
					}
					Hero.MainHero.Clan.Influence -= _entryCost;
					break;
			}

			ItemRoster armoury = new ItemRoster();
			string townCulture = "vlandia";

			try {
				townCulture = Settlement.CurrentSettlement.OwnerClan.Kingdom.Culture.StringId;
			} catch { }

			PopulateItemList(armoury, townCulture);
			PopulateItemList(armoury, "Any");

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
		}

		private void CalculateEntryCost(Settlement settlement) {
			if (!_entryFeeEnabled) {
				_paymentMethod = PaymentMethod.None;
				_armouryText = "No entry fee.";
				return;
			}

			Kingdom playerKingdom = Clan.PlayerClan.Kingdom;
			Kingdom settlementKingdom = settlement.OwnerClan.Kingdom;

			if (playerKingdom.IsAtWarWith(settlementKingdom)) {
				_entryCost = -1;
				_paymentMethod = PaymentMethod.Disabled;
				_armouryText = "You wouldn't be able to sneak in.";
				return;
			}

			int clanTierInverse = (Clan.PlayerClan.Tier * -1) + 7;
			float charmModifier = (Hero.MainHero.GetSkillValue(DefaultSkills.Charm) * 0.002f * -1) + 1;
			string armoury = "Entry fee is ";

			if (settlement.OwnerClan.Kingdom == null || Clan.PlayerClan.Kingdom == null || playerKingdom.Id != settlementKingdom.Id) {
				float formula = clanTierInverse * 30294 * charmModifier;
				_entryCost = (int)formula;
				_paymentMethod = PaymentMethod.Gold;
				_armouryText = armoury + $"{_entryCost} gold.";
				return;
			}
			
			bool playerIsKing = Hero.MainHero.IsFactionLeader;
			bool playerIsMerc = Clan.PlayerClan.IsUnderMercenaryService;
			
			if (playerIsKing) {
				_paymentMethod = PaymentMethod.None;
				_armouryText = "The entry is free.";
			} else if (playerIsMerc) {
				float formula = clanTierInverse * 10561 * charmModifier;
				_entryCost = (int)formula;
				_paymentMethod = PaymentMethod.Gold;
				_armouryText = armoury + $"{_entryCost} gold.";
			} else {				
				float formula = clanTierInverse * 20 * (charmModifier * 1.5f);
				_entryCost = (int)formula;
				_paymentMethod = PaymentMethod.Influence;
				_armouryText = armoury + $"{_entryCost} influence.";
			}
		}

		public override void SyncData(IDataStore dataStore) { }
	}
}
