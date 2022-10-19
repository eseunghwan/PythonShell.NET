
using System;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;


namespace PythonShell {
    public class ShellConfig {
        public String DefaultPythonPath { get; set; } = "python3";
        public String DefaultPythonVersion { get; set; } = "3.9.13";
        public String? DefaultWorkingDirectory { get; set; } = null;
        public String? PythonRequireFile { get; set; } = null;
        public String[]? PythonRequires { get; set; } = null;

        internal String? AppDir { get; set; } = null;
        internal String TempDir {
            get => Path.Join(AppDir, "temp");
        }
        internal String InstanceDir {
            get => Path.Join(AppDir, "instances");
        }
        internal String? DefaultPythonEnvPath { get; set; } = null;

        public ShellConfig() {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                if (this.DefaultPythonPath == "python" || this.DefaultPythonPath == "python2" || this.DefaultPythonPath == "python3") {
                    this.DefaultPythonPath = $"/usr/bin/{this.DefaultPythonPath}";
                }
                else if (!File.Exists(this.DefaultPythonPath)) {
                    this.DefaultPythonPath = "/usr/bin/python3";
                }
            }

            if (this.DefaultWorkingDirectory != null) {
                this.DefaultWorkingDirectory = Path.GetFullPath(this.DefaultWorkingDirectory);
            }
        }
    }

    public class Shell {
        private List<Int32> RunningProccese { get; set; } = new List<Int32>();

        public Boolean CreateDefaultEnv { get; set; } = false;
        public ShellConfig Config { get; set; } = new ShellConfig();
        public Boolean Resolved {
            get => this.RunningProccese.Count == 0;
        }

        public async Task Initialize() {
            await Utils.Util.InitializeApp(this.Config, this.CreateDefaultEnv);
        }

        public void Clear() {
            foreach (var path in Directory.GetDirectories(this.Config.InstanceDir)) {
                if (Path.GetFileName(path).ToLower() != "default") {
                    Console.WriteLine(path);
                    // Directory.Delete(path, true);
                }
            }
        }

        public async Task<Process?> RunFile(String pythonFile, String? workingDirectory = null, Boolean useInstance = false, String? instanceName = null, Boolean echo = true, Listener.ShellListener? listener = null) {
            String pythonToRun;
            String instanceDir = "";
            if (useInstance) {
                var instanceMap = instanceName == null ? Utils.Util.CreateShellInstance(this.Config, instanceName, echo: echo) : Utils.Util.GetShellInstance(this.Config, instanceName, echo: echo);
                pythonToRun = instanceMap.Python;
                instanceDir = instanceMap.Dir;
            }
            else {
                pythonToRun = this.Config.DefaultPythonEnvPath ?? this.Config.DefaultPythonPath;
            }

            var process = new Process {
                StartInfo = new ProcessStartInfo {
                    FileName = pythonToRun,
                    ArgumentList = { "-u", Path.GetFullPath(pythonFile) },
                    WorkingDirectory = this.Config.DefaultWorkingDirectory ?? workingDirectory!,
                    UseShellExecute = false
                }
            }!;
            Int32 processId = Int32.MaxValue;

            process.OutputDataReceived += (s, e) => {
                if (echo) {
                    Console.WriteLine(e.Data);
                }

                listener!.OnMessage!(e.Data!);
            };
            process.ErrorDataReceived += (s, e) => {
                this.RunningProccese.Remove(processId);
                if (useInstance) {
                    Directory.Delete(instanceDir, true);
                }

                listener!.OnError!(e);
            };
            process.Exited += (s, e) => {
                this.RunningProccese.Remove(processId);
                if (useInstance) {
                    Directory.Delete(instanceDir, true);
                }

                listener!.OnComplete!();
            };
            process.Start();
            processId = process.Id;
            this.RunningProccese.Add(processId);
            await process.WaitForExitAsync();

            return process;
        }

        public async Task<Process?> RunString(String pythonCode, String? workingDirectory = null, Boolean useInstance = false, String? instanceName = null, Boolean echo = true, Listener.ShellListener? listener = null) {
            String tempPythonFileName = DateTime.Now.ToString("yyyy.MM.dd.HH.mm.ss");
            String tempPythonFile;
            if (useInstance) {
                var instanceMap = instanceName == null ? Utils.Util.CreateShellInstance(this.Config, instanceName) : Utils.Util.GetShellInstance(this.Config, instanceName);
                tempPythonFile = Path.Join(instanceMap.Dir, "temp", tempPythonFileName);
            }
            else {
                tempPythonFile = Path.Join(this.Config.TempDir, tempPythonFileName);
            }
            File.WriteAllText(tempPythonFile, pythonCode, System.Text.Encoding.UTF8);

            var newListener = new Listener.ShellListener {
                OnMessage = listener!.OnMessage,
                OnError = (e) => {
                    listener!.OnError!(e);
                    if (File.Exists(tempPythonFile)) {
                        File.Delete(tempPythonFile);
                    }
                },
                OnComplete = () => {
                    listener!.OnComplete!();
                    if (File.Exists(tempPythonFile)) {
                        File.Delete(tempPythonFile);
                    }
                }
            };

            return await this.RunFile(
                tempPythonFile, workingDirectory, useInstance, instanceName, echo, newListener
            );
        }
    }
}
