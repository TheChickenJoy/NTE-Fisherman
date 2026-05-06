using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Windows;

namespace NTE_Fishing_Bot;

public class App : Application
{
	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.3.0")]
	public void InitializeComponent()
	{
		Startup += (s, e) => new MainWindow().Show();
	}

	[STAThread]
	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.3.0")]
	public static void Main()
	{
		App app = new App();
		app.InitializeComponent();
		app.Run();
	}
}
