using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TerraStorage.Content.Items;
using TerraStorageOverflow.Common.Systems;
using TerraStorageOverflow.Common.Utils;

namespace TerraStorageOverflow.Common.ModPlayers
{
    public class TerraStorageOverflowPlayer : ModPlayer
    {
        private bool _isHandlingPickup;

        private uint _lastFullMessageUpdateCount;
        private const uint FULL_NETWORK_MESSAGE_COOLDOWN = 5400;

        public bool HasActiveStorage => RemoteCache.HasActiveStorage;
        public RemoteCache RemoteCache { get; private set; }

        public override void Initialize()
        {
            RemoteCache = new RemoteCache();
        }

        public override void OnEnterWorld()
        {
            RemoteCache.MarkDirty();
        }

        public override void PostUpdate()
        {
            if (Player.whoAmI != Main.myPlayer)
                return;

            RemoteCache.Update(Player);
        }

        public override bool OnPickup(Item item)
        {
            if (item.IsAir || InventoryUtils.IsInstantPickup(item) || _isHandlingPickup)
                return true;

            RemoteCache.Update(Player);
            if (!HasActiveStorage)
                return true;

            //Mark dirty if we grab a new remote?
            if (item.ModItem is RemoteTerminal)
            {
                RemoteCache.MarkDirty();
            }
            _isHandlingPickup = true;
            try
            {
                Item leftover = Player.GetItem(
                    Player.whoAmI,
                    item,
                    GetItemSettings.PickupItemFromWorld
                );

                if (leftover.stack > 0)
                {
                    bool fullyStored = RemoteCache.DepositIntoAllNetworks(leftover);

                    if (
                        !fullyStored
                        && Main.GameUpdateCount - _lastFullMessageUpdateCount
                            > FULL_NETWORK_MESSAGE_COOLDOWN
                    )
                    {
                        Loggers.Log("All connected networks are full!", Color.OrangeRed);
                        _lastFullMessageUpdateCount = Main.GameUpdateCount;
                    }
                    else
                    {
                        SoundEngine.PlaySound(SoundID.Grab);
                    }
                }

                if (leftover.stack <= 0)
                {
                    leftover.TurnToAir();
                    return false;
                }

                return true;
            }
            finally
            {
                _isHandlingPickup = false;
            }
        }
    }
}
