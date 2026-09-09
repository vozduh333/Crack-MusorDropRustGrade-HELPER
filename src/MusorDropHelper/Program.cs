using System;
using System.IO;
using System.Windows.Forms;
using MusorDropHelper.Core;
using MusorDropHelper.Protection;
using MusorDropHelper.Ui;

namespace MusorDropHelper;

internal static class Program
{
	private static void Log(string m)
	{
		try
		{
			File.AppendAllText(Path.Combine(Path.GetTempPath(), "mdh_crash.log"),
				DateTime.Now.ToString("o") + "  " + m + Environment.NewLine);
		}
		catch
		{
		}
	}

	[STAThread]
	private static void Main()
	{
		AppDomain.CurrentDomain.UnhandledException += (s, e) => Log("UNHANDLED: " + e.ExceptionObject);
		Application.ThreadException += (s, e) => Log("THREAD: " + e.Exception);
		Log("main start");
		try
		{
			AntiDebug.GuardOrExit();
			ApplicationConfiguration.Initialize();
			Application.SetHighDpiMode(HighDpiMode.SystemAware);
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(defaultValue: false);
			if (!ActivationGate.EnsureActivated())
			{
				return;
			}
			Analyzer analyzer = new Analyzer();
			MainForm main = new MainForm(analyzer);
			FeedClient feed = new FeedClient(main.EnqueueDrop, main.EnqueueStatus, main.EnqueueLog);
			main.Shown += delegate
			{
				feed.Start(main);
			};
			main.EnqueueLog(LicenseClient.StatusText());
			main.EnqueueLog(S.Get(S.Id.StartMsg));
			try
			{
				Application.Run(main);
			}
			finally
			{
				feed.Stop();
			}
		}
		catch (Exception ex)
		{
			Log("CATCH: " + ex);
			throw;
		}
	}
}
