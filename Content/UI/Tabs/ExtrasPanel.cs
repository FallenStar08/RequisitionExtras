using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader.UI;
using Terraria.UI;
using TerraStorageOverflow.Content.UI.Components;
using TerraStorageOverflow.Content.UI.Services;
using TerraStorageOverflow.Content.UI.Styles;
using static TerraStorageOverflow.Common.Utils.UIUtils;

namespace TerraStorageOverflow.Content.UI.Tabs
{
    public class ExtrasPanel(object terminalUIInstance) : UIElement
    {
        private UIText _headerText;
        private UITextPanel<string>[] _radioButtons = new UITextPanel<string>[3];
        private string[] _optionTooltips = new string[3];
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

            _optionTooltips = [
                EasyLoca.ModeKeepBestTooltip,
                EasyLoca.ModeKeepFirstTooltip,
                EasyLoca.ModeExactMatchesTooltip
            ];

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
        /// Tooltip display needs to happen on draw and not update cause otherwise it gets instantly cleared
        /// </summary>
        /// <param name="spriteBatch"></param>
        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);

            for (int i = 0; i < _radioButtons.Length; i++)
            {
                if (_radioButtons[i] != null && _radioButtons[i].IsMouseHovering)
                {
                    UICommon.TooltipMouseText(_optionTooltips[i]);
                    break;
                }
            }
            if (_actionButton != null)
            {
                _actionButton.BackgroundColor = _actionButton.IsMouseHovering ? ButtonStyle.BG_HOVER : ButtonStyle.BG_ACTIVE;
            }

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
            for (int i = 0; i < 3; i++)
            {
                bool isSelected = i == _selectedOption;
                _radioButtons[i].BackgroundColor = isSelected ? RadioStyle.BG_ACTIVE : RadioStyle.BG_INACTIVE;
                _radioButtons[i].BorderColor = isSelected ? RadioStyle.BORDER_SELECTED : RadioStyle.BORDER_UNSELECTED;
            }
        }
    }

    public class SellReportPanel : UIPanel
    {
        private UIText _titleText;
        private UIList _list;
        private UIScrollbar _scrollbar;
        private SummaryRow _summaryRow;

        public override void OnInitialize()
        {
            OverflowHidden = true;
            BackgroundColor = new Color(23, 28, 51) * 0.9f;
            BorderColor = new Color(50, 60, 100);

            _titleText = new UIText(EasyLoca.SellReportHeader, 0.85f, false);
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

            _summaryRow = new SummaryRow();
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
            string summaryText = EasyLoca.ReportSummary(report.TotalItemsSold, formattedCoins);
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

    }
}