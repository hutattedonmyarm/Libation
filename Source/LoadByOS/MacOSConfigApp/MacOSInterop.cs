﻿using LibationFileManager;
using System.Diagnostics;

namespace MacOSConfigApp
{
    internal class MacOSInterop : IInteropFunctions
	{
		private const string AppPath = "/Applications/Libation.app";
		public MacOSInterop() { }
        public MacOSInterop(params object[] values) { }

		public void SetFolderIcon(string image, string directory) => throw new PlatformNotSupportedException();
        public void DeleteFolderIcon(string directory) => throw new PlatformNotSupportedException();

		//I haven't figured out how to find the app bundle's directory from within
		//the running process, so don't update unless it's "installed" in /Applications
		public bool CanUpdate => Directory.Exists(AppPath);

		public void InstallUpdate(string updateBundle)
		{
			Serilog.Log.Information($"Extracting update bundle to {AppPath}");

			//tar wil overwrite existing without elevated privileges
			Process.Start("tar", $"-xzf \"{updateBundle}\" -C \"/Applications\"").WaitForExit();
			
			//For now, it seems like this step is unnecessary. We can overwrite and
			//run Libation without needing to re-add the exception. This is insurance.
			RunAsRoot(null, $"""
sudo spctl --master-disable
sudo spctl --add --label 'Libation' {AppPath}
open {AppPath}
sudo spctl --master-enable
""");
		}

		//Using osascript -e '[script]' works from the terminal, but I haven't figured
		//out the syntax for it to work from create_process, so write to stdin instead.
		public Process RunAsRoot(string _, string command)
		{
			const string osascript = "osascript";
			var fullCommand = $"do shell script \"{command}\" with administrator privileges";

			var psi = new ProcessStartInfo()
			{
				FileName = osascript,
				UseShellExecute = false,
				Arguments = "-",
				RedirectStandardError= true,
				RedirectStandardOutput= true,
				RedirectStandardInput= true,
			};

			Serilog.Log.Logger.Information($"running {osascript} as root: {{script}}", fullCommand);

			var proc = Process.Start(psi);
			proc.ErrorDataReceived += Proc_ErrorDataReceived;
			proc.OutputDataReceived += Proc_OutputDataReceived;
			proc.BeginErrorReadLine();
			proc.BeginOutputReadLine();
			proc.StandardInput.WriteLine(fullCommand);
			proc.StandardInput.Close();

			return proc;
		}

		private void Proc_OutputDataReceived(object sender, DataReceivedEventArgs e)
		{
			if (e.Data != null)
				Serilog.Log.Logger.Information("stderr: {data}", e.Data);
		}

		private void Proc_ErrorDataReceived(object sender, DataReceivedEventArgs e)
		{
			if (e.Data!= null)
				Serilog.Log.Logger.Information("stderr: {data}", e.Data);
		}
	}
}
