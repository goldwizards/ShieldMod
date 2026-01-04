using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent.ItemDropRules;
using ShieldMod.Items.Accessories; // 아이템 네임스페이스

namespace ShieldMod.Globals
{
    public class MoonLordDrop : GlobalNPC
    {
        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            // 문로드 코어 사망 시 드랍 (100%)
            if (npc.type == NPCID.MoonLordCore)
            {
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<EmergencyAegis>(), 1)); // 1 = 100%
            }
        }
    }
}