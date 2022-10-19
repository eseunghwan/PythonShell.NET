
using System;


namespace PythonShell.Tests {
    internal class Program {
        static async System.Threading.Tasks.Task Main(String[] args) {
            var shell = new PythonShell(new PythonShellConfig {});
            await shell.Initialize();

            var instance = Shell.ShellManager.GetInstance("default");
            // instance.InstallRequires(new String[] { "PySide6" });
            instance.InstallRequiresFromFile("Scripts/requirements.txt");
            await instance.RunFile("Scripts/test.py", workingDirectory: "Scripts");

            Console.WriteLine("finished");
        }
    }
}
