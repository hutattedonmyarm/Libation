﻿using System.Drawing;
using System.Windows.Forms;
using Dinah.Core.WindowsDesktop.Forms;

namespace LibationWinForms.GridView
{
	public class EditTagsDataGridViewImageButtonColumn : DataGridViewButtonColumn
	{
		public EditTagsDataGridViewImageButtonColumn()
		{
			CellTemplate = new EditTagsDataGridViewImageButtonCell();
		}
	}

	internal class EditTagsDataGridViewImageButtonCell : DataGridViewImageButtonCell
	{
		private static Image ButtonImage { get; } = Properties.Resources.edit_25x25;
		private static Color HiddenForeColor { get; } = Color.LightGray;

		protected override void Paint(Graphics graphics, Rectangle clipBounds, Rectangle cellBounds, int rowIndex, DataGridViewElementStates elementState, object value, object formattedValue, string errorText, DataGridViewCellStyle cellStyle, DataGridViewAdvancedBorderStyle advancedBorderStyle, DataGridViewPaintParts paintParts)
		{
			if (rowIndex >= 0 && DataGridView.GetBoundItem<GridEntry>(rowIndex) is SeriesEntry)
			{
				base.Paint(graphics, clipBounds, cellBounds, rowIndex, elementState, null, null, null, cellStyle, advancedBorderStyle, DataGridViewPaintParts.Background | DataGridViewPaintParts.Border);
				return;
			}

			var tagsString = (string)value;

			var foreColor = tagsString?.Contains("hidden") == true ? HiddenForeColor : DataGridView.DefaultCellStyle.ForeColor;

			if (DataGridView.Rows[rowIndex].DefaultCellStyle.ForeColor != foreColor)
			{
				DataGridView.Rows[rowIndex].DefaultCellStyle.ForeColor = foreColor;
			}

			if (tagsString?.Length == 0)
			{
				base.Paint(graphics, clipBounds, cellBounds, rowIndex, elementState, null, null, null, cellStyle, advancedBorderStyle, paintParts);
				DrawButtonImage(graphics, ButtonImage, cellBounds);
			}
			else
			{
				base.Paint(graphics, clipBounds, cellBounds, rowIndex, elementState, value, formattedValue, errorText, cellStyle, advancedBorderStyle, paintParts);
			}
		}
	}
}
