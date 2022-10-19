<h1 align="center">
    <br />
    PythonShell.NET
</h1>
<h3 align="center">
    Python Environment Manager and Executor for .NET
    <br />
    <br />
</h3>
<br />

Available for:
- net5.0, net6.0


Supported Platforms:
- Windows 10+ (x86, amd64, arm64)
- Linux Distos (amd64, arm64)
- OSX 11+ (amd64, arm64)

<br />
<hr>
<br />
<br />

# Install
nuget package is not available not

<br /><br />

# Usage
- basic usage
```c#
using PythonShell.Listener;
using PythonShell.Shell;

var shell = new PythonShell.PythonShell(
    new PythonShell.PythonShellConfig()
);
await shell.Initialize();

shell.RunString("{pythonCode}");
```
- use instance
```c#
using PythonShell.Listener;
using PythonShell.Shell;

new PythonShell.PythonShell(
    new PythonShell.PythonShellConfig()
).Initialize();

var instance = ShellManager.GetInstance("default");
instance.RunString("{pythonCode}");
```

<br />

- onMessage, onError, onComplete
```c#
// setups like above ...
shell.RunString(
    "{pythonCode}",
    listener: ShellListener {
        OnMessage = (message) {
            // if `echo` is `true`, log to console automatically
            Console.WriteLine("message!");
        },
        OnError =  (e) {
            Console.WriteLine(e.Data);
        },
        OnComplete = () {
            Console.WriteLine("complete!");
        }
    }
);
```

<br />

for further informations, refers to [PythonShell.cs](https://github.com/eseunghwan/PythonShell.NET/blob/master/Source/src/PythonShell.cs)
