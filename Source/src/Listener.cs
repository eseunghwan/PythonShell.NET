
using System;
using System.Diagnostics;


namespace PythonShell.Listener {
    public class ShellListener {
        public Action<String>? OnMessage { get; set; } = (message) => {};
        public Action<DataReceivedEventArgs>? OnError { get; set; } = (message) => {};
        public Action? OnComplete { get; set; } = () => {};
    }
}
