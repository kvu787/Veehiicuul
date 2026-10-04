Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ('VeehiicuulWorkflow.ConsoleProcess' -as [type]) { return }

# C# callbacks can drain both pipes while PowerShell waits, without requiring a runspace.
Add-Type -TypeDefinition @'
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace VeehiicuulWorkflow
{
    public sealed class ConsoleProcess : IDisposable
    {
        private readonly object outputLock = new object();
        private readonly StreamWriter output;
        private Exception outputError;
        public Process Process { get; private set; }

        public ConsoleProcess(string filePath, string arguments, string logPath, bool createNoWindow)
        {
            output = new StreamWriter(logPath, false, new UTF8Encoding(false));
            output.AutoFlush = true;
            Process = new Process();
            Process.StartInfo.FileName = filePath;
            Process.StartInfo.Arguments = arguments;
            Process.StartInfo.WorkingDirectory = Path.GetDirectoryName(filePath);
            Process.StartInfo.UseShellExecute = false;
            Process.StartInfo.CreateNoWindow = createNoWindow;
            Process.StartInfo.RedirectStandardOutput = true;
            Process.StartInfo.RedirectStandardError = true;
            Process.OutputDataReceived += RecordOutput;
            Process.ErrorDataReceived += RecordOutput;
            try
            {
                if (!Process.Start()) { throw new InvalidOperationException("Could not start " + filePath); }
                Process.BeginOutputReadLine();
                Process.BeginErrorReadLine();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private void RecordOutput(object sender, DataReceivedEventArgs arguments)
        {
            if (arguments.Data == null) { return; }
            lock (outputLock)
            {
                try { output.WriteLine(arguments.Data); }
                catch (Exception exception) { outputError = exception; }
            }
        }

        public void FlushOutput()
        {
            // The parameterless overload also waits for the final pipe callbacks.
            Process.WaitForExit();
            lock (outputLock)
            {
                if (outputError != null) { throw new IOException("Console output could not be recorded.", outputError); }
                output.Flush();
            }
        }

        public void Dispose()
        {
            Process.Dispose();
            lock (outputLock) { output.Dispose(); }
        }
    }

    public static class PresentMonConsole
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint GetConsoleProcessList([Out] uint[] processes, uint count);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCtrlHandler(IntPtr handler, bool add);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GenerateConsoleCtrlEvent(uint controlEvent, uint processGroup);

        public static void Prepare()
        {
            uint[] processes = new uint[1];
            // CTRL_C_EVENT broadcasts to the entire console. Require a private helper console.
            if (GetConsoleProcessList(processes, 1) != 1 || processes[0] != (uint)Process.GetCurrentProcess().Id)
            {
                throw new InvalidOperationException("PresentMon capture requires its own console.");
            }
            // The child must not inherit an attribute that ignores Ctrl+C.
            if (!SetConsoleCtrlHandler(IntPtr.Zero, false)) { throw new Win32Exception(); }
        }

        public static void SendControlC(int presentMonProcessId)
        {
            uint[] processes = new uint[2];
            uint count = GetConsoleProcessList(processes, 2);
            uint helperProcessId = (uint)Process.GetCurrentProcess().Id;
            if (count == 0) { throw new Win32Exception(); }
            if (count > 2) { throw new InvalidOperationException("Unexpected processes share the PresentMon console."); }
            for (int index = 0; index < count; ++index)
            {
                if (processes[index] != helperProcessId && processes[index] != (uint)presentMonProcessId)
                {
                    throw new InvalidOperationException("Unexpected process shares the PresentMon console.");
                }
            }
            // Set this after starting the child so only the helper ignores the broadcast.
            if (!SetConsoleCtrlHandler(IntPtr.Zero, true)) { throw new Win32Exception(); }
            if (!GenerateConsoleCtrlEvent(0, 0)) { throw new Win32Exception(); }
        }
    }
}
'@
