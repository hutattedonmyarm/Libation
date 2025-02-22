﻿using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using System;
using System.Threading.Tasks;

namespace LibationAvalonia.Dialogs.Login
{
	public abstract class AvaloniaLoginBase
	{

		/// <returns>True if ShowDialog's DialogResult == OK</returns>
		protected static async Task<bool> ShowDialog(DialogWindow dialog)
		{
			if (Application.Current.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
				return false;

			var result = await dialog.ShowDialog<DialogResult>(desktop.MainWindow);
			Serilog.Log.Logger.Debug("{@DebugInfo}", new { DialogResult = result });
			return result == DialogResult.OK;
		}
	}
}
