namespace ShieldMod.Interfaces
{
	/// <summary>
	/// "보호막 장신구"임을 표시하는 마커 인터페이스입니다.
	/// 보호막 전용 슬롯에서 이 인터페이스를 기준으로 장착 가능 여부를 판정합니다.
	/// </summary>
	public interface IShieldAccessory
	{
	}

	/// <summary>
	/// 보호막 강화 1~4단계(서로 중복 장착 불가) 그룹 마커입니다.
	/// </summary>
	public interface IProtectionTierAccessory : IShieldAccessory
	{
	}
}
