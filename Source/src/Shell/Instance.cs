
using System;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;


namespace PythonShell.Shell {
    public class ShellInstance {
        public String? Name { get; set; }
        public String? PythonPath { get; set; }
        public String? TempDir { get; set; }
        public String? DefaultWorkingDirectory { get; set; }

        public void InstallRequires(String[]? pythonRequires, Boolean echo = true) {
            if (pythonRequires != null) {
                String tempPythonRequireFile = Path.Join(this.TempDir, "requirements.txt");
                File.WriteAllText(tempPythonRequireFile, String.Join("\n", pythonRequires));
                this.InstallRequiresFromFile(tempPythonRequireFile, echo: echo);
                File.Delete(tempPythonRequireFile);
            }
        }

        public void InstallRequiresFromFile(String pythonRequireFile, Boolean echo = true) {
            if (!File.Exists(pythonRequireFile)) {
                throw new FileNotFoundException("File does not exist or is not path!");
            }

            if (echo) Console.WriteLine("Installing requirements...");
            Util.RunSettingCmd(this.PythonPath!, new String[] { "-m", "pip", "install", "-r", pythonRequireFile }, (s, e) => { throw new Exceptions.InstallRequireFailedException(e.Data!); });
            if (echo) Console.WriteLine("Requirements installed.");
        }

        public async Task RunFile(String pythonFile, String? workingDirectory = null, Listener.ShellListener? listener = null, Boolean echo = true) {
            if (!File.Exists(pythonFile)) {
                throw new FileNotFoundException("File does not exist or is not path!");
            }

            Listener.ShellListener newListener = listener ?? new Listener.ShellListener();
            var process = new Process {
                StartInfo = new ProcessStartInfo {
                    FileName = this.PythonPath!,
                    ArgumentList = { "-u", Path.GetFullPath(pythonFile) },
                    WorkingDirectory = this.DefaultWorkingDirectory ?? workingDirectory!,
                    UseShellExecute = false
                }
            }!;
            process.OutputDataReceived += (s, e) => {
                if (echo) Console.WriteLine(e.Data);
                newListener.OnMessage!(e.Data!);
            };
            process.ErrorDataReceived += (s, e) => {
                newListener.OnError!(e);
                throw new Exceptions.RunFailedException(e.Data!);
            };
            process.Exited += (s, e) => {
                newListener.OnComplete!();
            };
            process.Start();
            await process.WaitForExitAsync();
        }

        public async Task RunString(String pythonCode, String? workingDirectory, Listener.ShellListener? listener, Boolean echo = true) {
            Listener.ShellListener newListener = listener ?? new Listener.ShellListener();
            
            String tempPythonFile = Path.Join(this.TempDir, $"{DateTime.Now.ToString("yyyy.MM.dd.HH.mm.ss")}.py");
            File.WriteAllText(tempPythonFile, pythonCode);
            Listener.ShellListener listenerToSend = new Listener.ShellListener {
                OnMessage = (message) => newListener.OnMessage!(message),
                OnError = (e) => {
                    File.Delete(tempPythonFile);
                    newListener.OnError!(e);
                },
                OnComplete = () => {
                    File.Delete(tempPythonFile);
                    newListener.OnComplete!();
                }
            };

            await this.RunFile(tempPythonFile, workingDirectory, listenerToSend, echo);
        }

        public void Remove() {
            Directory.Delete(Path.Join(Config.InstanceDir, this.Name), true);
        }
    }
}
