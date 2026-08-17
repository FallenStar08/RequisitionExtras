using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent.UI.Chat;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;
using TerraStorageOverflow.Common.Utils;

namespace TerraStorageOverflow.Content.UI.Tabs
{
    public class ExtrasPanel(object terminalUIInstance) : UIElement
    {
        private UIText _headerText;
        private UITextPanel<string>[] _radioButtons = new UITextPanel<string>[3];
        private int _selectedOption = 0;

        private UITextPanel<string> _actionButton;
        private SellReportPanel _reportPanel;
        private object _terminalUIInstance = terminalUIInstance;

        public override void OnInitialize()
        {
            Width.Set(0f, 1f);
            Height.Set(0f, 1f);
            Top.Set(40f, 0f);

            _headerText = new UIText(EasyLoca.SelectModeHeader, 0.9f);
            _headerText.Left.Set(20f, 0f);
            _headerText.Top.Set(10f, 0f);
            Append(_headerText);

            string[] optionLabels = {
                EasyLoca.ModeKeepBest,
                EasyLoca.ModeKeepFirst,
                EasyLoca.ModeExactMatches
            };

            float startY = 40f;
            float spacingY = 36f;

            for (int i = 0; i < 3; i++)
            {
                int index = i;
                var radio = new UITextPanel<string>(optionLabels[i], 0.8f);
                radio.Left.Set(20f, 0f);
                radio.Top.Set(startY + (i * spacingY), 0f);
                radio.Width.Set(270f, 0f);
                radio.Height.Set(30f, 0f);

                radio.OnLeftClick += (evt, el) => SelectRadio(index);
                _radioButtons[i] = radio;
                Append(radio);
            }

            UpdateRadioVisuals();

            _actionButton = new UITextPanel<string>(EasyLoca.SellActionButton, 0.9f);
            _actionButton.Left.Set(20f, 0f);
            _actionButton.Top.Set(startY + (3 * spacingY) + 15f, 0f);
            _actionButton.Width.Set(270f, 0f);
            _actionButton.Height.Set(35f, 0f);

            _actionButton.OnLeftClick += (evt, el) =>
            {
                SellReportData report = DuplicateSellService.ExecuteSell(_terminalUIInstance, (SellMode)_selectedOption);
                if (report != null)
                {
                    _reportPanel.Populate(report);
                }
            };
            Append(_actionButton);

            _reportPanel = new SellReportPanel();
            _reportPanel.Left.Set(310f, 0f);
            _reportPanel.Top.Set(10f, 0f);
            _reportPanel.Width.Set(300f, 0f);
            _reportPanel.Height.Set(220f, 0f);
            Append(_reportPanel);
        }


        /// <summary>
        /// wrapper method to reset the report panel, clearing any previous report data and resetting the summary text.
        /// </summary>
        public void ResetReport()
        {
            _reportPanel?.ClearReport();
        }

        private void SelectRadio(int index)
        {
            _selectedOption = index;
            UpdateRadioVisuals();
        }

        private void UpdateRadioVisuals()
        {
            Color activeColor = new Color(63, 82, 151) * 0.85f;
            Color inactiveColor = new Color(33, 43, 79) * 0.85f;

            for (int i = 0; i < 3; i++)
            {
                bool isSelected = i == _selectedOption;
                _radioButtons[i].BackgroundColor = isSelected ? activeColor : inactiveColor;
                _radioButtons[i].BorderColor = isSelected ? Color.Gold : Color.Black;
            }
        }
    }

    public class SellReportPanel : UIPanel
    {
        private UIText _titleText;
        private UIList _list;
        private UIScrollbar _scrollbar;
        private UISummaryRow _summaryRow;

        public override void OnInitialize()
        {
            BackgroundColor = new Color(23, 28, 51) * 0.9f;
            BorderColor = new Color(50, 60, 100);

            _titleText = new UIText("Sell Report :", 0.85f, false);
            _titleText.Left.Set(5f, 0f);
            _titleText.Top.Set(2f, 0f);
            Append(_titleText);

            _list = [];
            _list.Left.Set(0f, 0f);
            _list.Top.Set(26f, 0f);
            _list.Width.Set(-20f, 1f);
            _list.Height.Set(-60f, 1f);
            _list.ListPadding = 4f;

            // Fixes sorting issue: prevents UIList from re-sorting inserted elements (we sort them by value beforehand)
            _list.ManualSortMethod = items => { };
            Append(_list);

            _scrollbar = new UIScrollbar();
            _scrollbar.SetView(100f, 1000f);
            _scrollbar.Top.Set(26f, 0f);
            _scrollbar.Height.Set(-60f, 1f);
            _scrollbar.Left.Set(-15f, 1f);
            _list.SetScrollbar(_scrollbar);
            Append(_scrollbar);

            _summaryRow = new UISummaryRow();
            _summaryRow.Left.Set(5f, 0f);
            _summaryRow.Top.Set(-25f, 1f);
            _summaryRow.Width.Set(0f, 1f);
            _summaryRow.Height.Set(20f, 0f);
            Append(_summaryRow);
        }

        /// <summary>
        /// Populates the report panel with the provided SellReportData, creating a row for each entry and updating the summary text.
        /// </summary>
        /// <param name="report"></param>
        public void Populate(SellReportData report)
        {
            _list.Clear();

            if (report.TotalItemsSold == 0)
            {
                _summaryRow.SetText(EasyLoca.ReportNoDuplicates, Color.Yellow);
                return;
            }

            var sortedEntries = report.Entries.OrderByDescending(e => e.TotalValue).ToList();

            foreach (var entry in sortedEntries)
            {
                _list.Add(new SellReportRow(entry));
            }

            string formattedCoins = FormatCoinString(report.TotalEarnedCopper);
            string summaryText = string.Format(EasyLoca.ReportSummary, report.TotalItemsSold, formattedCoins);
            _summaryRow.SetText(summaryText, Color.Gold);
        }

        /// <summary>
        /// Resets the report panel to its initial state, clearing any entries and resetting the summary text.
        /// </summary>
        public void ClearReport()
        {
            _list?.Clear();
            _summaryRow?.SetText(EasyLoca.ReportNoItems, Color.LightGray);
        }

        /// <summary>
        /// Formats a copper value into a string with coin IDs for platinum, gold, silver, and copper.
        /// </summary>
        /// <param name="copper"></param>
        /// <returns></returns>
        public static string FormatCoinString(long copper)
        {
            List<string> parts = [];
            long plat = copper / 1000000; copper %= 1000000;
            long gold = copper / 10000; copper %= 10000;
            long silver = copper / 100; long cop = copper % 100;

            if (plat > 0) parts.Add($"{plat}[i:{ItemID.PlatinumCoin}]");
            if (gold > 0) parts.Add($"{gold}[i:{ItemID.GoldCoin}]");
            if (silver > 0) parts.Add($"{silver}[i:{ItemID.SilverCoin}]");
            if (cop > 0 || parts.Count == 0) parts.Add($"{cop}[i:{ItemID.CopperCoin}]");

            return string.Join(" ", parts);
        }
    }

    /// <summary>
    /// cute lil summary row at the bottom of the report panel
    /// </summary>
    public class UISummaryRow : UIElement
    {
        private string _text = EasyLoca.ReportNoItems;
        private Color _color = Color.LightGray;

        public void SetText(string text, Color color)
        {
            _text = text;
            _color = color;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);
            Vector2 pos = GetDimensions().Position();
            UIUtils.DrawTextWithTags(spriteBatch, _text, pos, _color, 0.75f);
        }
    }

    /// <summary>
    /// my cute report row for each item sold, with item icon and text :3
    /// </summary>
    public class SellReportRow : UIElement
    {
        private SellReportEntry _entry;

        public SellReportRow(SellReportEntry entry)
        {
            _entry = entry;
            Width.Set(0f, 1f);
            Height.Set(16f, 0f);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);
            Vector2 pos = GetDimensions().Position();

            string itemTag = ItemTagHandler.GenerateTag(_entry.ItemSample);
            string lineText = $"{itemTag} x{_entry.Count} - {SellReportPanel.FormatCoinString(_entry.TotalValue)}";

            UIUtils.DrawTextWithTags(spriteBatch, lineText, pos + new Vector2(10f, 0f), Color.White, 0.75f);
        }
    }
}