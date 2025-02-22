﻿using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using LibationFileManager;
using LibationAvalonia.Views;
using System;
using Avalonia.Platform;
using LibationAvalonia.Dialogs;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using ApplicationServices;

namespace LibationAvalonia
{
	public class App : Application
	{
		public static IBrush ProcessQueueBookFailedBrush { get; private set; }
		public static IBrush ProcessQueueBookCompletedBrush { get; private set; }
		public static IBrush ProcessQueueBookCancelledBrush { get; private set; }
		public static IBrush ProcessQueueBookDefaultBrush { get; private set; }
		public static IBrush SeriesEntryGridBackgroundBrush { get; private set; }

		public static IAssetLoader AssetLoader { get; private set; }

		public static readonly Uri AssetUriBase = new("avares://Libation/Assets/");
		public static Stream OpenAsset(string assetRelativePath)
			=> AssetLoader.Open(new Uri(AssetUriBase, assetRelativePath));

		public override void Initialize()
		{
			AvaloniaXamlLoader.Load(this);
			AssetLoader = AvaloniaLocator.Current.GetService<IAssetLoader>();
		}

		public static Task<List<DataLayer.LibraryBook>> LibraryTask;
		public static bool SetupRequired;

		public override void OnFrameworkInitializationCompleted()
		{
			LoadStyles();

			if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
			{
				if (SetupRequired)
				{
					var config = Configuration.Instance;

					var defaultLibationFilesDir = Configuration.UserProfile;

					// check for existing settings in default location
					var defaultSettingsFile = Path.Combine(defaultLibationFilesDir, "Settings.json");
					if (Configuration.SettingsFileIsValid(defaultSettingsFile))
						Configuration.SetLibationFiles(defaultLibationFilesDir);

					if (config.LibationSettingsAreValid)
					{
						LibraryTask = Task.Run(() => DbContexts.GetLibrary_Flat_NoTracking(includeParents: true));
						ShowMainWindow(desktop);
					}
					else
					{
						var setupDialog = new SetupDialog { Config = config };
						setupDialog.Closing += Setup_Closing;
						desktop.MainWindow = setupDialog;
					}
				}
				else
					ShowMainWindow(desktop);
			}

			base.OnFrameworkInitializationCompleted();
		}

		private async void Setup_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			var setupDialog = sender as SetupDialog;
			var desktop = ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;

			try
			{
				// all returns should be preceded by either:
				// - if config.LibationSettingsAreValid
				// - error message, Exit()
				if (setupDialog.IsNewUser)
				{
					Configuration.SetLibationFiles(Configuration.UserProfile);
					ShowSettingsWindow(desktop, setupDialog.Config, OnSettingsCompleted);
				}
				else if (setupDialog.IsReturningUser)
				{
					ShowLibationFilesDialog(desktop, setupDialog.Config, OnLibationFilesCompleted);
				}
				else
				{
					await CancelInstallation();
					return;
				}

			}
			catch (Exception ex)
			{
				var title = "Fatal error, pre-logging";
				var body = "An unrecoverable error occurred. Since this error happened before logging could be initialized, this error can not be written to the log file.";
				try
				{
					await MessageBox.ShowAdminAlert(null, body, title, ex);
				}
				catch
				{
					await MessageBox.Show($"{body}\r\n\r\n{ex.Message}\r\n\r\n{ex.StackTrace}", title, MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
				return;
			}
		}

		private async Task RunMigrationsAsync(Configuration config)
		{
			// most migrations go in here
			AppScaffolding.LibationScaffolding.RunPostConfigMigrations(config);

			await MessageBox.VerboseLoggingWarning_ShowIfTrue();

			// logging is init'd here
			AppScaffolding.LibationScaffolding.RunPostMigrationScaffolding(config);
		}

		private void ShowSettingsWindow(IClassicDesktopStyleApplicationLifetime desktop, Configuration config, Action<IClassicDesktopStyleApplicationLifetime, SettingsDialog, Configuration> OnClose)
		{
			config.Books ??= Path.Combine(Configuration.UserProfile, "Books");

			var settingsDialog = new SettingsDialog();
			desktop.MainWindow = settingsDialog;
			settingsDialog.RestoreSizeAndLocation(Configuration.Instance);
			settingsDialog.Show();

			void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
			{
				settingsDialog.Closing -= WindowClosing;
				e.Cancel = true;
				OnClose?.Invoke(desktop, settingsDialog, config);
			}
			settingsDialog.Closing += WindowClosing;
		}

		private async void OnSettingsCompleted(IClassicDesktopStyleApplicationLifetime desktop, SettingsDialog settingsDialog, Configuration config)
		{
			if (config.LibationSettingsAreValid)
			{
				await RunMigrationsAsync(config);
				LibraryTask = Task.Run(() => DbContexts.GetLibrary_Flat_NoTracking(includeParents: true));
				AudibleUtilities.AudibleApiStorage.EnsureAccountsSettingsFileExists();
				ShowMainWindow(desktop);
			}
			else
				await CancelInstallation();

			settingsDialog.Close();
		}


		private void ShowLibationFilesDialog(IClassicDesktopStyleApplicationLifetime desktop, Configuration config, Action<IClassicDesktopStyleApplicationLifetime, LibationFilesDialog, Configuration> OnClose)
		{
			var libationFilesDialog = new LibationFilesDialog();
			desktop.MainWindow = libationFilesDialog;
			libationFilesDialog.Show();

			void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
			{
				libationFilesDialog.Closing -= WindowClosing;
				e.Cancel = true;
				OnClose?.Invoke(desktop, libationFilesDialog, config);
			}
			libationFilesDialog.Closing += WindowClosing;
		}

		private async void OnLibationFilesCompleted(IClassicDesktopStyleApplicationLifetime desktop, LibationFilesDialog libationFilesDialog, Configuration config)
		{
			Configuration.SetLibationFiles(libationFilesDialog.SelectedDirectory);
			if (config.LibationSettingsAreValid)
			{
				await RunMigrationsAsync(config);

				LibraryTask = Task.Run(() => DbContexts.GetLibrary_Flat_NoTracking(includeParents: true));
				AudibleUtilities.AudibleApiStorage.EnsureAccountsSettingsFileExists();
				ShowMainWindow(desktop);
			}
			else
			{
				// path did not result in valid settings
				var continueResult = await MessageBox.Show(
					$"No valid settings were found at this location.\r\nWould you like to create a new install settings in this folder?\r\n\r\n{libationFilesDialog.SelectedDirectory}",
					"New install?",
					MessageBoxButtons.YesNo,
					MessageBoxIcon.Question);

				if (continueResult == DialogResult.Yes)
					ShowSettingsWindow(desktop, config, OnSettingsCompleted);
				else
					await CancelInstallation();

			}
			libationFilesDialog.Close();
		}

		static async Task CancelInstallation()
		{
			await MessageBox.Show("Initial set up cancelled.", "Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			Environment.Exit(0);
		}

		private static void ShowMainWindow(IClassicDesktopStyleApplicationLifetime desktop)
		{
			var mainWindow = new MainWindow();
			desktop.MainWindow = mainWindow;
			mainWindow.RestoreSizeAndLocation(Configuration.Instance);
			mainWindow.OnLoad();
			mainWindow.OnLibraryLoaded(LibraryTask.GetAwaiter().GetResult());
			mainWindow.Show();
		}

		private static void LoadStyles()
		{
			ProcessQueueBookFailedBrush = AvaloniaUtils.GetBrushFromResources("ProcessQueueBookFailedBrush");
			ProcessQueueBookCompletedBrush = AvaloniaUtils.GetBrushFromResources("ProcessQueueBookCompletedBrush");
			ProcessQueueBookCancelledBrush = AvaloniaUtils.GetBrushFromResources("ProcessQueueBookCancelledBrush");
			ProcessQueueBookDefaultBrush = AvaloniaUtils.GetBrushFromResources("ProcessQueueBookDefaultBrush");
			SeriesEntryGridBackgroundBrush = AvaloniaUtils.GetBrushFromResources("SeriesEntryGridBackgroundBrush");
		}
	}
}
