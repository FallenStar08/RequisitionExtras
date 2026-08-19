using Terraria.ID;
using TerraStorage.Common;

namespace TerraStorageOverflow.Common.Utils.Items
{
    internal class ItemConditionsUtils
    {
        public static object GetItemCondition(int itemType)
        {
            return itemType switch
            {
                ItemID.BottomlessBucket => CraftingCondition.NearWater,
                ItemID.BottomlessLavaBucket => CraftingCondition.NearLava,
                ItemID.BottomlessHoneyBucket => CraftingCondition.NearHoney,
                ItemID.IceMachine => CraftingCondition.InSnow,
                ItemID.Tombstone
                or ItemID.GraveMarker
                or ItemID.CrossGraveMarker
                or ItemID.Headstone
                or ItemID.Gravestone
                or ItemID.Obelisk => CraftingCondition.InGraveyard,
                _ => CraftingCondition.None,
            };
        }

        public static bool IsNoneCondition(object conditionEnum)
        {
            if (conditionEnum == null)
                return true;

            string name = conditionEnum.ToString();
            return name == "None";
        }
    }
}
