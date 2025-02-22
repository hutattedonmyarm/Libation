﻿using DataLayer;
using Dinah.Core;
using LibationFileManager;
using LibationWinForms.ProcessQueue;
using System;
using System.Linq;
using System.Windows.Forms;

namespace LibationWinForms
{
	public partial class Form1
	{

		int WidthChange = 0;
		private void Configure_ProcessQueue()
		{
			processBookQueue1.popoutBtn.Click += ProcessBookQueue1_PopOut;
			splitContainer1.Panel2MinSize = 350;
			var coppalseState = Configuration.Instance.GetNonString(defaultValue: false, nameof(splitContainer1.Panel2Collapsed));
			WidthChange = splitContainer1.Panel2.Width + splitContainer1.SplitterWidth;
			int width = this.Width;
			SetQueueCollapseState(coppalseState);
			this.Width = width;
		}

		private void ProductsDisplay_LiberateClicked(object sender, LibraryBook libraryBook)
		{
			try
			{
				if (libraryBook.Book.UserDefinedItem.BookStatus is LiberatedStatus.NotLiberated or LiberatedStatus.PartialDownload)
				{
					Serilog.Log.Logger.Information("Begin single book backup of {libraryBook}", libraryBook);
					SetQueueCollapseState(false);
					processBookQueue1.AddDownloadDecrypt(libraryBook);
				}
				else if (libraryBook.Book.UserDefinedItem.PdfStatus is LiberatedStatus.NotLiberated)
				{
					Serilog.Log.Logger.Information("Begin single pdf backup of {libraryBook}", libraryBook);
					SetQueueCollapseState(false);
					processBookQueue1.AddDownloadPdf(libraryBook);
				}
				else if (libraryBook.Book.Audio_Exists())
				{
					// liberated: open explorer to file
					var filePath = AudibleFileStorage.Audio.GetPath(libraryBook.Book.AudibleProductId);
					if (!Go.To.File(filePath?.ShortPathName))
					{
						var suffix = string.IsNullOrWhiteSpace(filePath) ? "" : $":\r\n{filePath}";
						MessageBox.Show($"File not found" + suffix);
					}
				}
			}
			catch (Exception ex)
			{
				Serilog.Log.Logger.Error(ex, "An error occurred while handling the stop light button click for {libraryBook}", libraryBook);
			}
		}

		private void SetQueueCollapseState(bool collapsed)
		{
			if (collapsed && !splitContainer1.Panel2Collapsed)
			{
				WidthChange = splitContainer1.Panel2.Width + splitContainer1.SplitterWidth;
				splitContainer1.Panel2.Controls.Remove(processBookQueue1);
				splitContainer1.Panel2Collapsed = true;
				Width -= WidthChange;
			}
			else if (!collapsed && splitContainer1.Panel2Collapsed)
			{
				Width += WidthChange;
				splitContainer1.Panel2.Controls.Add(processBookQueue1);
				splitContainer1.Panel2Collapsed = false;
				processBookQueue1.popoutBtn.Visible = true;
			}
			toggleQueueHideBtn.Text = splitContainer1.Panel2Collapsed ? "❰❰❰" : "❱❱❱";
		}

		private void ToggleQueueHideBtn_Click(object sender, EventArgs e)
		{
			SetQueueCollapseState(!splitContainer1.Panel2Collapsed);
			Configuration.Instance.SetNonString(splitContainer1.Panel2Collapsed, nameof(splitContainer1.Panel2Collapsed));
		}

		private void ProcessBookQueue1_PopOut(object sender, EventArgs e)
		{
			ProcessBookForm dockForm = new();
			dockForm.WidthChange = splitContainer1.Panel2.Width + splitContainer1.SplitterWidth;
			dockForm.RestoreSizeAndLocation(Configuration.Instance);
			dockForm.FormClosing += DockForm_FormClosing;
			splitContainer1.Panel2.Controls.Remove(processBookQueue1);
			splitContainer1.Panel2Collapsed = true;
			processBookQueue1.popoutBtn.Visible = false;
			dockForm.PassControl(processBookQueue1);
			dockForm.Show();
			this.Width -= dockForm.WidthChange;
			toggleQueueHideBtn.Visible = false;
			int deltax = filterBtn.Margin.Right + toggleQueueHideBtn.Width + toggleQueueHideBtn.Margin.Left;
			filterBtn.Location= new System.Drawing.Point(filterBtn.Location.X + deltax, filterBtn.Location.Y);
			filterSearchTb.Location = new System.Drawing.Point(filterSearchTb.Location.X + deltax, filterSearchTb.Location.Y);
		}

		private void DockForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (sender is ProcessBookForm dockForm)
			{
				this.Width += dockForm.WidthChange;
				splitContainer1.Panel2.Controls.Add(dockForm.RegainControl());
				splitContainer1.Panel2Collapsed = false;
				processBookQueue1.popoutBtn.Visible = true;
				dockForm.SaveSizeAndLocation(Configuration.Instance);
				this.Focus();
				toggleQueueHideBtn.Visible = true;
				int deltax = filterBtn.Margin.Right + toggleQueueHideBtn.Width + toggleQueueHideBtn.Margin.Left;
				filterBtn.Location = new System.Drawing.Point(filterBtn.Location.X - deltax, filterBtn.Location.Y);
				filterSearchTb.Location = new System.Drawing.Point(filterSearchTb.Location.X - deltax, filterSearchTb.Location.Y);
			}
		}
	}
}
