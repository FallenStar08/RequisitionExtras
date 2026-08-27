using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.ModLoader.UI;
using Terraria.UI;
using TerraStorage.Common;
using TerraStorage.Content.Tiles;
using TerraStorage.Content.UI;
using TerraStorageOverflow.Common.Utils;
using TerraStorageOverflow.Common.Utils.Reflection;
using TerraStorageOverflow.Content.UI.Services;
using TerraStorageOverflow.Content.UI.Styles;

namespace TerraStorageOverflow.Common.Hooks
{
    internal class CraftingCoreUIHookSystem : ModSystem
    {
        private bool _injected = false;

        private CraftingCoreUISystem System => ModContent.GetInstance<CraftingCoreUISystem>();
        private object UIState =>
            System != null ? Reflect.GetValue<object>(System, "_uiState") : null;

        public override void UpdateUI(GameTime gameTime)
        {
            if (_injected)
                return;

            object state = UIState;
            if (state is not UIState uiState)
                return;

            object parentObj = Reflect.GetValue<object>(uiState, "_panel") ?? uiState;
            if (parentObj is UIElement parentElement)
            {
                CraftingCoreAddonButton button = new();
                parentElement.Append(button);
                _injected = true;
            }
        }
    }

    public class CraftingCoreAddonButton(
        string text = "",
        float textScale = 0.65f,
        bool large = false
    ) : UITextPanel<string>(text, textScale, large)
    {
        public override void OnInitialize()
        {
            SetText(EasyLoca.PopulateCraftingCoreButtonText);
            BackgroundColor = ButtonStyle.BG_ACTIVE;
            Left.Set(5f, 0f);
            Top.Set(5f, 0f);
            Width.Set(60f, 0f);
            Height.Set(24f, 0f);

            OnLeftClick += (evt, listeningElement) =>
            {
                CraftingCoreUISystem craftingSystem =
                    ModContent.GetInstance<CraftingCoreUISystem>();

                CraftingCoreEntity entity = craftingSystem?.OpenEntity;

                if (entity == null)
                {
                    Loggers.Warn("CraftingCoreEntity is not open or null.", Color.Yellow);
                    return;
                }

                List<DiskData> disks = StorageNetworkHelper.GetConnectedDisks(entity);
                if (disks != null && disks.Count > 0)
                {
                    CraftingCoreStationInserter.AutoInsertMissingStations(entity, disks);
                }
            };
        }

        protected override void DrawSelf(SpriteBatch sprite)
        {
            base.DrawSelf(sprite);

            if (IsMouseHovering)
            {
                Terraria.Main.LocalPlayer.mouseInterface = true;
                UICommon.TooltipMouseText(EasyLoca.PopulateCraftingCoreButtonTooltip);
                BackgroundColor = ButtonStyle.BG_HOVER;
            }
            else
            {
                BackgroundColor = ButtonStyle.BG_ACTIVE;
            }
        }
    }
}
