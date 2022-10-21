
using System;


namespace PythonShell.Exceptions {
    public class InstallRequireFailedException: Exception {
        public InstallRequireFailedException(): base() {}
        public InstallRequireFailedException(String message): base(message) {}
        public InstallRequireFailedException(String message, Exception innerException): base(message, innerException) {}
    }

    public class RunFailedException: Exception {
        public RunFailedException(): base() {}
        public RunFailedException(String message): base(message) {}
        public RunFailedException(String message, Exception innerException): base(message, innerException) {}
    }
}
