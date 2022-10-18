
using System;


namespace PythonShell.Tests {
    internal class Program {
        static async System.Threading.Tasks.Task Main(String[] args) {
            var shell = new Shell {
                Config = new ShellConfig {
                    DefaultWorkingDirectory = "Scripts",
                    PythonRequires = new String[] { "PySide6" }
                },
                // CreateDefaultEnv = true
            };
            await shell.Initialize();

            await shell.RunFile("Scripts/test.py", useInstance: true, instanceName: "testInstance1", listener: new Listener.ShellListener {
                OnComplete = () => shell.Clear()
            });
        }
    }
}
