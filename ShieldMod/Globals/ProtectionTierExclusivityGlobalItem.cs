using Terraria;
using Terraria.ModLoader;
using ShieldMod.Interfaces;

namespace ShieldMod.Globals
{
	/// <summary>
	/// 보호막 강화 1~4단계 장신구는 서로 동시에 장착되지 않도록(전체 슬롯 통합) 강제합니다.
	/// - 일반 장신구 칸 + 모드 전용 슬롯(보호막 슬롯) 포함
	/// </summary>
	public sealed class ProtectionTierExclusivityGlobalItem : GlobalItem
	{
		private static bool IsProtectionTier(Item item)
			=> item?.ModItem is IProtectionTierAccessory;

		/// <summary>
		/// 서로 호환 불가인 액세서리 조합을 정의합니다.
		/// false를 반환하면 스왑(교체) 동작으로 처리되어 "둘을 동시에" 착용할 수 없게 됩니다.
		/// </summary>
		public override bool CanAccessoryBeEquippedWith(Item equippedItem, Item incomingItem, Player player)
		{
			// 1~4단계끼리는 서로(자기 자신 포함) 중복 착용 불가
			if (IsProtectionTier(equippedItem) && IsProtectionTier(incomingItem))
				return false;

			return true;
		}
	}
}
