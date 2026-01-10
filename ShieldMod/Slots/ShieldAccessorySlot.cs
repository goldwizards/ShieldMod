using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using ShieldMod.Interfaces;

namespace ShieldMod.Slots
{
	public sealed class ShieldAccessorySlot : ModAccessorySlot
	{
		public override bool DrawVanitySlot => false;
		public override bool DrawDyeSlot => false;

		// === 슬롯 아이콘(52x52) ===
		public override string FunctionalTexture
			=> "ShieldMod/Assets/Slots/ShieldSlot_Icon";

		// (배경까지 쓰고 싶으면 사용)
		// public override string FunctionalBackgroundTexture
		//     => "ShieldMod/Assets/Slots/ShieldSlot_Back";

		/// <summary>
		/// 하드모드 이후에만 활성
		/// </summary>
		public override bool IsEnabled()
			=> Main.hardMode;

		/// <summary>
		/// A안:
		/// - 하드모드 후: 항상 표시
		/// - 하드모드 전: 비어있으면 숨김, 아이템 있으면 표시
		/// </summary>
		public override bool IsHidden()
		{
			if (Main.hardMode)
				return false;

			return IsEmpty;
		}

		/// <summary>
		/// 하드모드 전에도,
		/// 이미 장착된 아이템이 있으면 슬롯 표시
		/// </summary>
		public override bool IsVisibleWhenNotEnabled()
			=> !IsEmpty;

		/// <summary>
		/// 보호막 장신구만 허용
		/// 하드모드 전에는 새 장착 불가
		/// </summary>
		public override bool CanAcceptItem(Item checkItem, AccessorySlotType context)
		{
			if (!Main.hardMode)
				return false;

			return checkItem?.accessory == true
				&& checkItem.ModItem is IShieldAccessory;
		}

		public override void OnMouseHover(AccessorySlotType context)
		{
			if (!Main.hardMode)
			{
				Main.instance.MouseText("하드모드 진입 후 활성화되는 보호막 전용 슬롯");
				return;
			}

			Main.instance.MouseText("보호막 전용 슬롯");
		}

		public override void BackgroundDrawColor(AccessorySlotType context, ref Color color)
		{
			if (!Main.hardMode)
				color *= 0.85f; // 비활성 톤
		}
	}
}
