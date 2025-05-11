using System;
using System.Drawing;
using System.Windows.Forms;

namespace CustomerAndServerMaintenanceTracking.CustomCells
{
    public class TagsDataGridViewMultiButtonCell : DataGridViewTextBoxCell
    {
        private Rectangle startRect;
        private Rectangle stopRect;
        private Rectangle viewRect;

        public event EventHandler<MultiButtonClickEventArgs> ButtonClicked;
        private const string StartText = "Start";
        private const string StopText = "Stop";
        private const string ViewText = "View";

        protected override void Paint(
            Graphics graphics,
            Rectangle clipBounds,
            Rectangle cellBounds,
            int rowIndex,
            DataGridViewElementStates cellState,
            object value,
            object formattedValue,
            string errorText,
            DataGridViewCellStyle cellStyle,
            DataGridViewAdvancedBorderStyle advancedBorderStyle,
            DataGridViewPaintParts paintParts)
        {
            base.Paint(graphics, clipBounds, cellBounds, rowIndex, cellState, value,
                formattedValue, errorText, cellStyle, advancedBorderStyle, paintParts);

            int buttonWidth = (cellBounds.Width - 8) / 3;
            int buttonHeight = cellBounds.Height - 6;
            int xStart = cellBounds.X + 2;
            int yStart = cellBounds.Y + 3;

            startRect = new Rectangle(xStart, yStart, buttonWidth, buttonHeight);
            stopRect = new Rectangle(xStart + buttonWidth + 2, yStart, buttonWidth, buttonHeight);
            viewRect = new Rectangle(xStart + (buttonWidth * 2) + 4, yStart, buttonWidth, buttonHeight);

            ControlPaint.DrawButton(graphics, startRect, ButtonState.Normal);
            TextRenderer.DrawText(graphics, StartText, this.DataGridView.Font,
                startRect, SystemColors.ControlText,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            ControlPaint.DrawButton(graphics, stopRect, ButtonState.Normal);
            TextRenderer.DrawText(graphics, StopText, this.DataGridView.Font,
                stopRect, SystemColors.ControlText,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            ControlPaint.DrawButton(graphics, viewRect, ButtonState.Normal);
            TextRenderer.DrawText(graphics, ViewText, this.DataGridView.Font,
                viewRect, SystemColors.ControlText,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        protected override void OnMouseUp(DataGridViewCellMouseEventArgs e)
        {
            base.OnMouseUp(e);
            Point pt = new Point(e.X, e.Y);

            if (startRect.Contains(pt))
            {
                ButtonClicked?.Invoke(this, new MultiButtonClickEventArgs(MultiButtonType.Start));
            }
            else if (stopRect.Contains(pt))
            {
                ButtonClicked?.Invoke(this, new MultiButtonClickEventArgs(MultiButtonType.Stop));
            }
            else if (viewRect.Contains(pt))
            {
                ButtonClicked?.Invoke(this, new MultiButtonClickEventArgs(MultiButtonType.View));
            }
        }
    }

    public enum MultiButtonType
    {
        Start,
        Stop,
        View
    }

    public class MultiButtonClickEventArgs : EventArgs
    {
        public MultiButtonType ButtonType { get; private set; }
        public MultiButtonClickEventArgs(MultiButtonType type)
        {
            ButtonType = type;
        }
    }
}
