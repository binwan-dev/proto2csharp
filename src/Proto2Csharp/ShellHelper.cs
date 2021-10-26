using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Proto2Csharp
{
    public class ShellHelper
    {

        public ShellHelper()
        {
        }

        public (int ExitCode, string Out) QuickRun(string exeFile, string args, int milliseconds = 0)
        {
            // var process = new Process();
            // process.StartInfo = new ProcessStartInfo(exeFile, args);
            // process.StartInfo.RedirectStandardOutput = true;
            // process.StartInfo.RedirectStandardError = true;
            // process.Start();
            // if (milliseconds > 0)
            // {
            //     process.WaitForExit(milliseconds);
            //     process.Kill();
            // }
            // else
            // {
            //     process.WaitForExit();
            // }
            // if (process.ExitCode != 0)
            // {
            //     return (process.ExitCode, process.StandardError.ReadToEnd());
            // }
            // else
            // {
            //     return (0, process.StandardOutput.ReadToEnd());
            // }

            return Run(exeFile, args, milliseconds: milliseconds);
        }

        public Task<(int ExitCode, string Out)> LongRun(string exeFile, string args, Action<string> outputCallback, int milliseconds = 0)
        {
            return Task.Factory.StartNew(() => Run(exeFile, args, outputCallback));
        }

        public (int ExitCode, string Out) Run(string exeFile, string args, Action<string> outCallback = null, int milliseconds = 0)
        {
            var errStr = new StringBuilder();
            var outStr = new StringBuilder();
            var outEvent = new ManualResetEvent(false);
            var errEvent = new ManualResetEvent(false);

            using (var process = new Process())
            {
                process.StartInfo = new ProcessStartInfo(exeFile, args);
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.EnableRaisingEvents = true;
                process.OutputDataReceived += (o, e) =>
                {
                    if (e.Data != null)
                        if (outCallback != null)
                            outCallback(e.Data);
                        else
                            outStr.Append(e.Data);
                    else
                    {
                        outEvent.Set();
                    }
                };
                process.ErrorDataReceived += (o, e) =>
                {
                    if (e.Data != null)
                        errStr.Append(e.Data);
                    else
                        errEvent.Set();
                };
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                if (milliseconds > 0)
                {
                    outEvent.WaitOne(milliseconds);
                    errEvent.WaitOne(milliseconds);
                }
                else
                {
                    outEvent.WaitOne();
                    errEvent.WaitOne();
                }
                process.CancelErrorRead();
                process.CancelOutputRead();
                if (!process.HasExited)
                {
                    process.Kill();
                }
                if (process.ExitCode != 0)
                {
                    return (process.ExitCode, errStr.ToString());
                }
                else
                {
                    return (0, outStr.ToString());
                }
            }
        }
    }
}
