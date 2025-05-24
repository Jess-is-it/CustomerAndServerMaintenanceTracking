using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedLibrary.Models;
using System.Windows.Forms;
using System.Diagnostics;

namespace CustomerAndServerMaintenanceTracking.CustomCells
{
    public class ActionDataGridViewMultiButtonCell : DataGridViewTextBoxCell
    {
        // Rectangles for hit testing (relative to cell's 0,0)
        private Rectangle startStopRect;
        private Rectangle deleteRect;

        private readonly Color DefaultButtonTextColor = SystemColors.ControlText;

        // Hover states
        private bool mouseOverStartStop = false;
        private bool mouseOverDelete = false;

        // Button Texts
        private const string StartText = "Start";
        private const string StopText = "Stop";
        private const string DeleteText = "Delete";

        // Button Colors (Customize as needed)
        private readonly Color StartStateColor = Color.FromArgb(40, 167, 69);  // Green
        private readonly Color StopStateColor = Color.FromArgb(220, 53, 69);   // Red
        private readonly Color DeleteButtonColor = Color.FromArgb(108, 117, 125); // Gray
        private readonly Color ButtonTextColor = Color.White;
        private readonly Font ButtonFont; // Will be initialized from cellStyle

        public ActionDataGridViewMultiButtonCell()
        {
            // It's good practice to initialize from a default style if possible,
            // or ensure ButtonFont is set before the first Paint call.
            // For simplicity, we'll grab it in Paint from cellStyle.
        }

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
            // Call base paint to handle background, borders, focus selection, etc.
            base.Paint(graphics, clipBounds, cellBounds, rowIndex, cellState, value,
                formattedValue, errorText, cellStyle, advancedBorderStyle,
                paintParts & ~(DataGridViewPaintParts.ContentForeground | DataGridViewPaintParts.ContentBackground)); // We paint our own content

            // Ensure ButtonFont is initialized
            Font buttonFont = cellStyle.Font;

            if (this.DataGridView == null || rowIndex < 0 || rowIndex >= this.DataGridView.RowCount) return;

            NetwatchConfigDisplay dataItem = this.DataGridView.Rows[rowIndex].DataBoundItem as NetwatchConfigDisplay;
            if (dataItem == null)
            {
                // Optionally draw a disabled state or clear if no data item
                using (SolidBrush backBrush = new SolidBrush(cellStyle.BackColor))
                {
                    graphics.FillRectangle(backBrush, cellBounds);
                }
                return;
            }

            // --- Button Layout (for 2 buttons) ---
            int totalPaddingAndGaps = 12; // 3px left_pad + 3px inter-button_gap + 3px right_pad + 3px extra for safety = 12px
            int availableWidthForButtons = cellBounds.Width - totalPaddingAndGaps;
            int buttonWidth = availableWidthForButtons / 2;
            int buttonHeight = cellBounds.Height - 6; // 3px top/bottom padding

            // Ensure buttonWidth is not negative if cell is too small
            if (buttonWidth < 10) buttonWidth = 10;
            if (buttonHeight < 5) buttonHeight = 5;

            int relativeXStart = 3; // Padding inside the cell from the left
            int relativeYStart = (cellBounds.Height - buttonHeight) / 2; // Center vertically

            // Define hit-test rectangles (relative to cell's 0,0)
            startStopRect = new Rectangle(relativeXStart, relativeYStart, buttonWidth, buttonHeight);
            deleteRect = new Rectangle(relativeXStart + buttonWidth + 3, relativeYStart, buttonWidth, buttonHeight); // 3px gap

            // --- Drawing ---
            // Determine Start/Stop button text and color
            string currentStartStopText = dataItem.IsEnabled ? StopText : StartText;
            Color currentStartStopButtonColor = dataItem.IsEnabled ? StopStateColor : StartStateColor;

            // Get button states for visual feedback (e.g., pushed look)
            ButtonState startStopVisualState = mouseOverStartStop && (Control.MouseButtons == MouseButtons.Left) ? ButtonState.Pushed : ButtonState.Normal;
            ButtonState deleteVisualState = mouseOverDelete && (Control.MouseButtons == MouseButtons.Left) ? ButtonState.Pushed : ButtonState.Normal;

            // 1. Draw Start/Stop Button
            ControlPaint.DrawButton(graphics, new Rectangle(cellBounds.Left + startStopRect.Left, cellBounds.Top + startStopRect.Top, startStopRect.Width, startStopRect.Height), startStopVisualState);
            using (SolidBrush brush = new SolidBrush(currentStartStopButtonColor)) // Custom fill color
            {
                graphics.FillRectangle(brush, cellBounds.Left + startStopRect.Left + 1, cellBounds.Top + startStopRect.Top + 1, startStopRect.Width - 2, startStopRect.Height - 2);
            }
            TextRenderer.DrawText(graphics, currentStartStopText, buttonFont,
                                  new Rectangle(cellBounds.Left + startStopRect.Left, cellBounds.Top + startStopRect.Top, startStopRect.Width, startStopRect.Height),
                                  ButtonTextColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.PreserveGraphicsClipping);
            if (mouseOverStartStop) // Optional: underline on hover
            {
                using (Pen hoverPen = new Pen(ButtonTextColor, 1))
                {
                    graphics.DrawRectangle(hoverPen, cellBounds.Left + startStopRect.Left, cellBounds.Top + startStopRect.Top, startStopRect.Width - 1, startStopRect.Height - 1);
                }
            }


            // 2. Draw Delete Button
            Rectangle actualDeleteDrawingRect = new Rectangle(cellBounds.Left + deleteRect.Left, cellBounds.Top + deleteRect.Top, deleteRect.Width, deleteRect.Height);
            ControlPaint.DrawButton(graphics, actualDeleteDrawingRect, deleteVisualState);
            TextRenderer.DrawText(graphics, DeleteText, buttonFont,
                                  actualDeleteDrawingRect,
                                  DefaultButtonTextColor, // Use system default text color
                                  TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.PreserveGraphicsClipping);
            // Hover/Focus state for Delete button is now primarily handled by 'deleteVisualState' in ControlPaint.DrawButton
            // If you want a stronger custom hover border for the default button:
            if (mouseOverDelete && deleteVisualState != ButtonState.Pushed)
            {
                ControlPaint.DrawBorder(graphics, actualDeleteDrawingRect, SystemColors.Highlight, ButtonBorderStyle.Solid); // Example hover border
            }
        }

        protected override void OnMouseMove(DataGridViewCellMouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (this.DataGridView == null || e.RowIndex < 0) return;

            Point mousePoint = new Point(e.X, e.Y); // e.X and e.Y are cell-relative
            bool needsInvalidate = false;

            bool oldMouseOverStartStop = mouseOverStartStop;
            mouseOverStartStop = startStopRect.Contains(mousePoint);
            if (oldMouseOverStartStop != mouseOverStartStop) needsInvalidate = true;

            bool oldMouseOverDelete = mouseOverDelete;
            mouseOverDelete = deleteRect.Contains(mousePoint);
            if (oldMouseOverDelete != mouseOverDelete) needsInvalidate = true;

            if (needsInvalidate)
            {
                this.DataGridView.InvalidateCell(e.ColumnIndex, e.RowIndex);
            }
        }

        protected override void OnMouseLeave(int rowIndex)
        {
            base.OnMouseLeave(rowIndex);
            if (this.DataGridView == null || rowIndex < 0) return;
            if (ResetMouseOverState())
            {
                this.DataGridView.InvalidateCell(this.ColumnIndex, rowIndex);
            }
        }

        protected override void OnMouseDown(DataGridViewCellMouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (this.DataGridView == null || e.RowIndex < 0) return;
            // Force repaint for pushed button state
            this.DataGridView.InvalidateCell(e.ColumnIndex, e.RowIndex);
        }

        protected override void OnMouseUp(DataGridViewCellMouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (this.DataGridView == null || e.RowIndex < 0) return;
            // Force repaint to revert pushed button state
            this.DataGridView.InvalidateCell(e.ColumnIndex, e.RowIndex);
            // The actual click action will be handled by CellClick in the UserControl.
        }


        private bool ResetMouseOverState()
        {
            bool changed = mouseOverStartStop || mouseOverDelete;
            mouseOverStartStop = false;
            mouseOverDelete = false;
            return changed;
        }

        // Enum for button types (simplified)
        public enum ActionButtonType
        {
            None,
            StartStop,
            Delete
        }

        // Method to determine which button is at a given point (relative to the cell)
        public ActionButtonType GetButtonAt(Point cellLocation)
        {

            if (startStopRect.Contains(cellLocation))
            {
                return ActionButtonType.StartStop;
            }
            if (deleteRect.Contains(cellLocation))
            {
                return ActionButtonType.Delete;
            }

            return ActionButtonType.None;
        }
    }

   

}
