﻿using FileManager;
using System;
using System.Threading.Tasks;

namespace AaxDecrypter
{
	public class UnencryptedAudiobookDownloader : AudiobookDownloadBase
	{
		protected override long InputFilePosition => InputFileStream.WritePosition;

		public UnencryptedAudiobookDownloader(string outFileName, string cacheDirectory, IDownloadOptions dlLic)
			: base(outFileName, cacheDirectory, dlLic)
		{
			AsyncSteps.Name = "Download Unencrypted Audiobook";
			AsyncSteps["Step 1: Download Audiobook"] = Step_DownloadAndDecryptAudiobookAsync;
			AsyncSteps["Step 2: Download Clips and Bookmarks"] = Step_DownloadClipsBookmarksAsync;
			AsyncSteps["Step 3: Create Cue"] = Step_CreateCueAsync;
		}

		public override Task CancelAsync()
		{
			IsCanceled = true;
			FinalizeDownload();
			return Task.CompletedTask;
		}

		protected override async Task<bool> Step_DownloadAndDecryptAudiobookAsync()
		{
			// MUST put InputFileStream.Length first, because it starts background downloader.
			while (InputFileStream.Length > InputFilePosition && !InputFileStream.IsCancelled)
				await Task.Delay(200);

			if (IsCanceled)
				return false;
			else
			{
				FinalizeDownload();
				FileUtility.SaferMove(InputFileStream.SaveFilePath, OutputFileName);
				OnFileCreated(OutputFileName);
				return true;
			}
		}
	}
}
