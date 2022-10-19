
using System;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Diagnostics;
using System.Runtime.InteropServices;


namespace PythonShell.Utils {
    internal class InstanceMap {
        public String Dir { get; set; } = String.Empty;
        public String Python { get; set; } = String.Empty;
    }

    internal class Util {
        private static String GetShellInstanceDir(ShellConfig config, String? instanceName = null) {
            instanceName = instanceName ?? DateTime.Now.ToString("yyyy.MM.dd.HH.mm.ss");
            if (instanceName.ToLower() == "default") {
                return Path.Join(config.InstanceDir, "default");
            }
            else {
                return Path.Join(config.InstanceDir, instanceName);
            }
        }

        private static async Task DownloadFile(String url, String destFile, String fileType = "text") {
            var client = new HttpClient();
            var resp = await client.GetAsync(url);

            if (fileType.ToLower() == "binary") {
                File.WriteAllBytes(destFile, await resp.Content.ReadAsByteArrayAsync());
            }
            else if (fileType.ToLower() == "text") {
                File.WriteAllText(destFile, await resp.Content.ReadAsStringAsync());
            }
        }

        private static Process RunSettingCmd(String pythonToRun, String[] arguments) {
            var process = new Process {
                StartInfo = new ProcessStartInfo {
                    FileName = pythonToRun,
                    Arguments = String.Join(" ", arguments),
                    UseShellExecute = false,
                    RedirectStandardOutput = true, RedirectStandardError = true
                }
            }!;

            process.Start();
            process.WaitForExit();

            return process;
        }

        public static async Task InitializeApp(ShellConfig config, Boolean createDefaultEnv) {
            Console.WriteLine("Initializing shell...");
            config.DefaultPythonVersion = Util.CheckPythonVersion(config.DefaultPythonVersion);

            Console.WriteLine("Check shell app directory...");
            var userHomeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            config.AppDir = Path.Join(userHomeDir, ".python_shell.net");
            if (!Directory.Exists(config.AppDir)) {
                Directory.CreateDirectory(config.AppDir);
            }

            if (!Directory.Exists(config.TempDir)) {
                Directory.CreateDirectory(config.TempDir);
            }

            if (!Directory.Exists(config.InstanceDir)) {
                Directory.CreateDirectory(config.InstanceDir);
            }
            Console.WriteLine("Shell app directory check finished.");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                Console.WriteLine("Check default python binary files...");
                String pythonDir = Path.Join(config.AppDir, "python");
                if (!Directory.Exists(pythonDir)) {
                    String pythonZipFile = Path.Join(config.TempDir, "python.zip");
                    await Util.DownloadFile($"https://www.python.org/ftp/python/{config.DefaultPythonVersion}/python-{config.DefaultPythonVersion}-embed-amd64.zip", pythonZipFile, "binary");
                    ZipFile.ExtractToDirectory(pythonZipFile, pythonDir);
                    File.Delete(pythonZipFile);

                    var pthFile = Util.GetPythonPthFile(config, pythonDir);
                    File.WriteAllText(
                        pthFile,
                        File.ReadAllText(pthFile).Replace("#import site", "import site")
                    );
                }
                Console.WriteLine("Python check finished.");

                config.DefaultPythonPath = Path.Join(pythonDir, "python.exe");
            }

            if (File.Exists(config.DefaultPythonPath)) {
                Console.WriteLine("Default settings for virtualenv...");
                var p = Util.RunSettingCmd(config.DefaultPythonPath, new String[] { "-m", "pip", "install", "pip", "--upgrade" });
                if (p.ExitCode != 0) {
                    String pipInstallFile = Path.Join(config.TempDir, "get-pip.py");
                    await Util.DownloadFile("https://bootstrap.pypa.io/pip/get-pip.py", pipInstallFile, "text");
                    Util.RunSettingCmd(config.DefaultPythonPath, new String[] { pipInstallFile });
                    File.Delete(pipInstallFile);
                }

                Util.RunSettingCmd(config.DefaultPythonPath, new String[] { "-m", "pip", "install", "virtualenv", "--upgrade" });
                Console.WriteLine("Virtualenv settings finished.");
            }

            String defaultEnvdir = Util.GetShellInstanceDir(config, "default");
            if (createDefaultEnv) {
                Console.WriteLine("Creating default env...");
                if (Directory.Exists(defaultEnvdir)) {
                    config.DefaultPythonEnvPath = Util.GetVirtualEnv(config, "default");
                }
                else {
                    config.DefaultPythonEnvPath = Util.CreateShellInstance(config, "default").Python;
                }

                // Util.InstallRequiresToEnv(config, config.DefaultPythonEnvPath);
                Console.WriteLine("Default env created.");
            }
            // else if (File.Exists(config.DefaultPythonPath)) {
            //     Util.InstallRequiresToEnv(config, config.DefaultPythonPath);
            // }

            Console.WriteLine("Shell initialized.");
        }

        public static String CheckPythonVersion(String rawPythonVersion) {
            String realPythonVersion = "3.9.13";

            var versions = rawPythonVersion.Split(".");
            if (versions.Length == 3) {
                if (rawPythonVersion.EndsWith(".")) {
                    if (versions.Last() != "") {
                        realPythonVersion = String.Join(".", versions.SkipLast(1));
                    }
                }
                else {
                    realPythonVersion = rawPythonVersion;
                }
            }
            else if (versions.Length == 2) {
                if (rawPythonVersion.EndsWith(".")) {
                    if (versions.Last() != "") {
                        realPythonVersion = String.Join(".", versions) + ".0";
                    }
                }
                else {
                    realPythonVersion = $"{rawPythonVersion}.0";
                }
            }

            return realPythonVersion;
        }

        public static String GetPythonPthFile(ShellConfig config, String pythonDiir) {
            return Path.Join(pythonDiir, $"python{config.DefaultPythonVersion.Replace($".{config.DefaultPythonVersion.Split(".").Last()}", "").Replace(".", "")}._pth");
        }

        public static void ClearShellInstance(ShellConfig config, String instanceName) {
            var instanceDir = Util.GetShellInstanceDir(config, instanceName);
            foreach (var path in Directory.GetDirectories(Path.Join(instanceDir, "temp"))) {
                Directory.Delete(path, true);
            }

            foreach (var path in Directory.GetFiles(Path.Join(instanceDir, "temp"))) {
                File.Delete(path);
            }
        }

        public static InstanceMap CreateShellInstance(ShellConfig config, String? instanceName = null, Boolean echo = true) {
            instanceName = instanceName ?? DateTime.Now.ToString("yyyy.MM.dd.HH.mm.ss");
            if (echo) Console.WriteLine($"Creating shell instance [{instanceName}]...");
            String instanceDir = Util.GetShellInstanceDir(config, instanceName);
            String envPython;

            if (!Directory.Exists(instanceDir)) {
                Directory.CreateDirectory(instanceDir);
                Directory.CreateDirectory(Path.Join(instanceDir, "temp"));
                envPython = Util.CreateVirtualEnv(config, instanceName, echo: echo);
            }
            else {
                Util.ClearShellInstance(config, instanceName);
                envPython = Util.GetVirtualEnv(config, instanceName);
            }
            if (echo) Console.WriteLine("Shell instance created.");

            return new InstanceMap {
                Dir = instanceDir, Python = envPython
            };
        }

        public static String CreateVirtualEnv(ShellConfig config, String instanceName, String[]? pythonRequires = null, Boolean echo = true) {
            if (echo) Console.WriteLine("Creating virtualenv...");
            pythonRequires = pythonRequires ?? config.PythonRequires;

            String envDir = Path.Join(Util.GetShellInstanceDir(config, instanceName), "env");
            if (instanceName.ToLower() == "default") {
                config.DefaultPythonEnvPath = Util.GetVirtualEnv(config, "default");
            }

            Util.RunSettingCmd(config.DefaultPythonPath, new String[] { "-m", "virtualenv", envDir });
            String envPython = Util.GetVirtualEnv(config, instanceName);
            Util.RunSettingCmd(envPython, new String[] { "-m", "pip", "install", "pip", "--upgrade" });
            Util.InstallRequiresToEnv(config, envPython, pythonRequires: pythonRequires);
            if (echo) Console.WriteLine("Virtualenv created.");

            return envPython;
        }

        public static void DeleteShellInstance(ShellConfig config, String instanceName) {
            Directory.Delete(Util.GetShellInstanceDir(config, instanceName), true);
        }

        public static InstanceMap GetShellInstance(ShellConfig config, String instanceName, Boolean echo = true) {
            String instanceDir = Util.GetShellInstanceDir(config, instanceName);
            if (Directory.Exists(instanceDir)) {
                return new InstanceMap {
                    Dir = instanceDir,
                    Python = Util.GetVirtualEnv(config, instanceName)
                };
            }
            else {
                return Util.CreateShellInstance(config, instanceName, echo: echo);
            }
        }

        public static String GetVirtualEnv(ShellConfig config, String instanceName) {
            String envDir = Util.GetShellInstanceDir(config, instanceName);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                return Path.Join(envDir, "env", "Scripts", "python");
            }
            else {
                return Path.Join(envDir, "env", "bin", "python");
            }
        }

        public static void InstallRequiresToEnv(ShellConfig config, String envPython, String[]? pythonRequires = null, Boolean echo = true) {
            pythonRequires = pythonRequires ?? config.PythonRequires;

            if (echo) Console.WriteLine("Installing requirements...");
            if (pythonRequires == null && config.PythonRequireFile != null) {
                Util.RunSettingCmd(envPython, new String[] { "-m", "pip", "install", "-r", config.PythonRequireFile });
            }
            else if (pythonRequires != null) {
                String tempPythonRequireFile = Path.Join(config.TempDir, DateTime.Now.ToString("yyyy.MM.dd.HH.mm.ss"));
                File.WriteAllText(tempPythonRequireFile, String.Join("\n", pythonRequires));
                Util.RunSettingCmd(envPython, new String[] { "-m", "pip", "install", "-r", tempPythonRequireFile });
                File.Delete(tempPythonRequireFile);
            }
            if (echo) Console.WriteLine("Requirements installed.");
        }

        public static void RemoveShellInstance(ShellConfig config, String instanceName) {
            String instanceDir = Util.GetShellInstanceDir(config, instanceName);
            if (Directory.Exists(instanceDir)) {
                Directory.Delete(instanceDir, true);
            }
        }
    }
}
