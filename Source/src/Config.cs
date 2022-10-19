
using System;
using System.IO;
using System.Linq;


namespace PythonShell {
    internal static class Config {
        public static String AppDir = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".python_shell.net");
        public static String TempDir {
            get => Path.Join(Config.AppDir, "temp");
        }
        public static String InstanceDir {
            get => Path.Join(Config.AppDir, "instances");
        }

        public static String DefaultPythonVersion = "3.9.13";
        public static String DefaultPythonPath = "python3";
        public static String PythonRuntimeBit {
            get => System.Environment.Is64BitProcess ? "amd64" : "win32";
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
    }
}
