
using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.IO.Compression;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;


namespace PythonShell {
    public class PythonShellConfig {
        public String DefaultPythonPath { get; set; } = "python3";
        public String DefaultPythonVersion { get; set; } = "3.9.13";
        // public String? DefaultWorkingDirectory { get; set; } = null;
        // public String? PythonRequireFile { get; set; } = null;
        // public String[]? PythonRequires { get; set; } = null;

        // internal String? AppDir { get; set; } = null;
        // internal String TempDir {
        //     get => Path.Join(AppDir, "temp");
        // }
        // internal String InstanceDir {
        //     get => Path.Join(AppDir, "instances");
        // }
        // internal String? DefaultPythonEnvPath { get; set; } = null;

        public PythonShellConfig() {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                if (this.DefaultPythonPath == "python" || this.DefaultPythonPath == "python2" || this.DefaultPythonPath == "python3") {
                    this.DefaultPythonPath = $"/usr/bin/{this.DefaultPythonPath}";
                }
                else if (!File.Exists(this.DefaultPythonPath)) {
                    this.DefaultPythonPath = "/usr/bin/python3";
                }
            }

            // if (this.DefaultWorkingDirectory != null) {
            //     this.DefaultWorkingDirectory = Path.GetFullPath(this.DefaultWorkingDirectory);
            // }
        }
    }

    public class PythonShell {
        public PythonShell(PythonShellConfig shellConfig) {}

        public void Clear() {
            Shell.ShellManager.Clear();
            foreach (var path in Directory.GetDirectories(Config.TempDir)) {
                Directory.Delete(path, true);
            }
            foreach (var path in Directory.GetFiles(Config.TempDir)) {
                File.Delete(path);
            }
        }

        public async Task Initialize() {
            Console.WriteLine("Initializing shell...");
            Config.DefaultPythonVersion = Config.CheckPythonVersion(Config.DefaultPythonVersion);

            if (!Directory.Exists(Config.AppDir)) Directory.CreateDirectory(Config.AppDir);
            if (!Directory.Exists(Config.InstanceDir)) Directory.CreateDirectory(Config.InstanceDir);
            if (!Directory.Exists(Config.TempDir)) Directory.CreateDirectory(Config.TempDir);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                Console.WriteLine("Check default python binary files...");
                String pythonDir = Path.Join(Config.AppDir, "python");
                if (!Directory.Exists(pythonDir)) {
                    String pythonZipFile = Path.Join(Config.TempDir, "python.zip");
                    await Util.DownloadFile($"https://www.python.org/ftp/python/{Config.DefaultPythonVersion}/python-{Config.DefaultPythonVersion}-embed-{Config.PythonRuntimeBit}.zip", pythonZipFile, "binary");
                    ZipFile.ExtractToDirectory(pythonZipFile, pythonDir);
                    File.Delete(pythonZipFile);

                    var pthFile = Path.Join(pythonDir, $"python{Config.DefaultPythonVersion.Replace($".{Config.DefaultPythonVersion.Split(".").Last()}", "").Replace(".", "")}._pth");
                    File.WriteAllText(
                        pthFile,
                        File.ReadAllText(pthFile).Replace("#import site", "import site")
                    );
                }
                Console.WriteLine("Python check finished.");

                Config.DefaultPythonPath = Path.Join(pythonDir, "python.exe");
            }

            if (File.Exists(Config.DefaultPythonPath)) {
                Console.WriteLine("Default settings for virtualenv...");
                var p = Util.RunSettingCmd(Config.DefaultPythonPath, new String[] { "-m", "pip", "install", "pip", "--upgrade" });
                if (p.ExitCode != 0) {
                    String pipInstallFile = Path.Join(Config.TempDir, "get-pip.py");
                    await Util.DownloadFile("https://bootstrap.pypa.io/pip/get-pip.py", pipInstallFile, "text");
                    Util.RunSettingCmd(Config.DefaultPythonPath, new String[] { pipInstallFile });
                    File.Delete(pipInstallFile);
                }

                Util.RunSettingCmd(Config.DefaultPythonPath, new String[] { "-m", "pip", "install", "virtualenv", "--upgrade" });
                Console.WriteLine("Virtualenv settings finished.");
            }

            String defaultEnvdir = Path.Join(Config.InstanceDir, "default");
            if (!Directory.Exists(defaultEnvdir)) {
                Console.WriteLine("Creating default env...");
                Shell.ShellManager.CreateInstance("default");
                Console.WriteLine("Default env created.");
            }

            Console.WriteLine("Shell initialized.");
        }

        public async Task RunFile(String pythonFile, Shell.ShellInstance? instance = null, String? workingDirectory = null, Listener.ShellListener? listener = null, Boolean echo = true) {
            instance = instance ?? Shell.ShellManager.GetInstance("default");
            await instance.RunFile(pythonFile, workingDirectory, listener, echo);
        }

        public async Task RunString(String pythonCode, Shell.ShellInstance? instance = null, String? workingDirectory = null, Listener.ShellListener? listener = null, Boolean echo = true) {
            instance = instance ?? Shell.ShellManager.GetInstance("default");
            await instance.RunString(pythonCode, workingDirectory, listener, echo);
        }
    }
}
