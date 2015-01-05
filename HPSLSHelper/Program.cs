using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HPSLSHelper
{
    class Program
    {
        #region private members
        static string url;
        static string username;
        static string password;
        static string cmd;
        static string vmName;
        static string snapshotName;
        static string vmUsername;
        static string vmPassword;
        static SLS sls;
        static bool isLinux = false;
        #endregion

        #region command text
        const string CMD_ROLLBACK = "rollback";
        const string CMD_RESUME = "resume";
        const string CMD_REVERTSNAPSHOT = "revertss";
        const string CMD_DELETESNAPSHOT = "deletess";
        const string CMD_CREATESNAPSHOT = "createss";
        const string CMD_SHUTDOWN = "shutdown";
        const string CMD_POWEROFF = "poweroff";
        const string CMD_REBOOT = "reboot";
        const string CMD_SUSPEND = "suspend";
        #endregion


        static void Main(string[] args)
        {
            url = ParseArgument(args, "/url:");
            username = ParseArgument(args, "/u:");
            password = ParseArgument(args, "/p:");
            cmd = ParseArgument(args, "/c:");
            vmName = ParseArgument(args, "/vm:");
            snapshotName = ParseArgument(args, "/ss:");
            vmUsername = ParseArgument(args, "/vmu:");
            vmPassword = ParseArgument(args, "/vmp:");

            if (!string.IsNullOrEmpty(ParseArgument(args, "/linux:")))
                isLinux = Convert.ToBoolean(ParseArgument(args, "/linux:"));

            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(cmd) || string.IsNullOrEmpty(vmName))
            {
                Warning("URL, Email, Password, Command, Virtual Machine name are mandatory", -1);
            }

            if (cmd.Equals(CMD_CREATESNAPSHOT, StringComparison.InvariantCultureIgnoreCase) && string.IsNullOrEmpty(snapshotName) ||
                cmd.Equals(CMD_DELETESNAPSHOT, StringComparison.InvariantCultureIgnoreCase) && string.IsNullOrEmpty(snapshotName) ||
                cmd.Equals(CMD_REVERTSNAPSHOT, StringComparison.InvariantCultureIgnoreCase) && string.IsNullOrEmpty(snapshotName))
            {
                Warning("The snapshot name cannot be empty.", -1);
            }

            if (cmd.Equals(CMD_REBOOT, StringComparison.InvariantCultureIgnoreCase) && (string.IsNullOrEmpty(vmUsername) || string.IsNullOrEmpty(vmPassword)) ||
                cmd.Equals(CMD_RESUME, StringComparison.InvariantCultureIgnoreCase) && (string.IsNullOrEmpty(vmUsername) || string.IsNullOrEmpty(vmPassword)) ||
                cmd.Equals(CMD_ROLLBACK, StringComparison.InvariantCultureIgnoreCase) && (string.IsNullOrEmpty(vmUsername) || string.IsNullOrEmpty(vmPassword)))
            {
                Warning("When command is resume, reboot, rollback, have to specify Administrator username and password of the virtual machine.", -1);
            }

            if (Login())
            {
                ExecuteCommand();
            }
            else
            {
                Warning("Login fail.", -2);
            }
        }

        static bool Login()
        {
            bool result = false;
            sls = new SLS(url, username, password);
            result = sls.Login();

            return result;
        }

        static void ExecuteCommand()
        {
            switch(cmd)
            {
                case CMD_ROLLBACK:
                    if (sls.RevertSnapshot(vmName, snapshotName))
                    {
                        if (sls.Resume(vmName))
                        {
                            if (!WaitUntilActive(isLinux))
                                Warning("Virtual machine is not started.", -100);                            
                        }
                        else
                        {
                            Warning("Resume virtual machine fail.", -20);
                        }
                    }                        
                    else
                    {
                        Warning("Revert virtual machine to snapshot fail.", -10);
                    }
                    break;
                case CMD_RESUME:
                    if (!sls.Resume(vmName))
                        Warning("Resume virtual machine fail.", -20);
                    else
                        if (!WaitUntilActive(isLinux))
                            Warning("Virtual machine is not started.", -100);
                    break;
                case CMD_REVERTSNAPSHOT:
                    if (!sls.RevertSnapshot(vmName, snapshotName))
                        Warning("Revert virtual machine to snapshot fail.", -10);
                    break;
                case CMD_DELETESNAPSHOT:
                    if (!sls.DeleteSnapshot(vmName, snapshotName))
                        Warning("Delete snapshot on virtual machine fail.", -40);
                    break;
                case CMD_CREATESNAPSHOT:
                    if (!sls.CreateSnapshot(vmName, snapshotName))
                        Warning("Create snapshot on virtual machine fail.", -50);
                    break;
                case CMD_POWEROFF:
                    if (!sls.PowerOff(vmName))
                        Warning("Power off virtual machine fail.", -60);
                    break;
                case CMD_SHUTDOWN:
                    if (!sls.Shutdown(vmName))
                        Warning("Shut down virtual machine fail.", -30);
                    break;
                case CMD_REBOOT:
                    if (!sls.Reboot(vmName))
                        Warning("Reboot virtual machine fail.", -70);
                    else
                        if (!WaitUntilActive(isLinux))
                            Warning("Virtual machine is not started.", -100);
                    break;
                case CMD_SUSPEND:
                    if (!sls.Suspend(vmName))
                        Warning("Suspend virtual machine fail.", -80);
                    break;
                default:
                    Warning(string.Format("The command: {0} is not supported", cmd), -5);
                    break;
            }
        }

        static void Warning(string msg, int exitcode)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("===============================================================================");
            sb.AppendLine("Welcome to use HP SLS Helper, this tool is to assist controlling the SLS Virtual machine from command line.");
            sb.AppendLine("Format: HPSLSHelper.exe /u:<email address> /p:<password> /url:<sls web site url> /c:<command> /vm:<virtual machine display name> [/ss:<snapshot name>] [/vmu:<virtual machine username>] [/vmp:<virtual machine password>] [/linux:<true|false>]");
            sb.AppendLine("Supported Command:");
            sb.AppendLine("     rollback - revert the virtual machine and start it up.");
            sb.AppendLine("     resume - resume the virtual machine.");
            sb.AppendLine("     revertss - revert the virtual machine to specified snapshot.");
            sb.AppendLine("     deletess - delete the specified snapshot on virtual machine.");
            sb.AppendLine("     createss - create a snapshot on virtual machine.");
            sb.AppendLine("     shutdown - shutdown the virtual machine.");
            sb.AppendLine("     poweroff - Hard turn off the virtual machine.");
            sb.AppendLine("     reboot - Reboot the virtual machine.");
            sb.AppendLine("     suspend - Suspend the virtual machine.");
            sb.AppendLine("Example: HPSLSHelper.exe /u:tom@hp.com /p:a1#1s /url:http://shslsportal.chn.hp.com /c:rollback /vm:SH_WIN7_32BIT_60G_EN_HPSWSH_xli20_SGDLITVM0224 /ss:CLEAN");
            sb.AppendLine("===============================================================================");
            sb.AppendLine();
            if (!string.IsNullOrEmpty(msg))
            {
                sb.AppendLine("Error:");
                sb.AppendLine(msg);
            }
            Console.WriteLine(sb.ToString());
            Environment.Exit(exitcode);
        }

        static string ParseArgument(string[] args, string key)
        {
            string value = string.Empty;

            if (args.Where(o=>o.StartsWith(key, StringComparison.InvariantCultureIgnoreCase)).Count() > 0)
            {
                string arg = args.Where(o => o.StartsWith(key, StringComparison.InvariantCultureIgnoreCase)).Single() as string;
                value = arg.Replace(key, string.Empty);
            }

            return value;
        }

        static string GetIP(string host)
        {
            string ip = null;
            IPAddress[] addresslist = Dns.GetHostAddresses(host);

            foreach(IPAddress ipaddr in addresslist.Where(o=>o.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork))
            {                
                ip = ipaddr.ToString();
                break;
            }

            if (string.IsNullOrEmpty(ip))
                throw new Exception("Cannot get the IP");

            return ip;
        }

        static bool WaitUntilActive(bool isLinux)
        {
            bool result = false;

            if (isLinux)
            {
                result = WaitLinuxMachineUntilActive();
            }
            else
            {
                result = WaitWindowsMachineUntilActive();
            }

            return result;
        }

        static bool PingHost(string nameOrAddress)
        {
            bool pingable = false;
            Ping pinger = new Ping();

            try
            {
                PingReply reply = pinger.Send(nameOrAddress);

                pingable = reply.Status == IPStatus.Success;
            }
            catch (PingException)
            {
                // Discard PingExceptions and return false;
            }

            return pingable;
        }

        static bool WaitLinuxMachineUntilActive()
        {
            bool isActive = false;
            DateTime rebootTime = DateTime.Now;
            int RebootTimeoutSeconds = 600;
            bool isRestarting =  false;

            string remoteVMFullName = string.Format(@"{0}.hpswlabs.adapps.hp.com", vmName.Substring(vmName.LastIndexOf("_") + 1));

            while (!isActive && DateTime.Now.Subtract(rebootTime).TotalSeconds < RebootTimeoutSeconds)
            {
                try
                {
                    if (!PingHost(remoteVMFullName))
                    {
                        isRestarting = true;
                    }
                    else if (isRestarting)
                    {
                        isActive = true;
                    }
                        
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            return isActive;    
        }

        static bool WaitWindowsMachineUntilActive()
        {
            bool isActive = false;
            DateTime rebootTime = DateTime.Now;
            int RebootTimeoutSeconds = 600;
            int timeout = 300 * 1000;

            string remoteVMFullName = string.Format(@"{0}.hpswlabs.adapps.hp.com", vmName.Substring(vmName.LastIndexOf("_") + 1));

            while (!isActive && DateTime.Now.Subtract(rebootTime).TotalSeconds < RebootTimeoutSeconds)
            {
                try
                {
                    string output = null;

                    string exe = string.Format(@"{0}\PsService.exe", Environment.CurrentDirectory);
                    string argument = string.Format("-accepteula \\\\{0} -u {1} -p {2}  query spooler", GetIP(remoteVMFullName), vmUsername, vmPassword);

                    using (Process process = new Process())
                    {
                        StringBuilder consoleoutput = new StringBuilder();
                        StringBuilder consoleerror = new StringBuilder();

                        process.StartInfo.FileName = exe;
                        process.StartInfo.Arguments = argument;
                        process.StartInfo.CreateNoWindow = true;
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.RedirectStandardError = true;
                        process.StartInfo.RedirectStandardOutput = true;

                        using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
                        using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
                        {
                            process.OutputDataReceived += (sender, e) =>
                            {
                                if (e.Data == null)
                                {
                                    if (outputWaitHandle != null)
                                        if (!outputWaitHandle.SafeWaitHandle.IsClosed)
                                            outputWaitHandle.Set();
                                }
                                else
                                {
                                    consoleoutput.AppendLine(e.Data);
                                }
                            };
                            process.ErrorDataReceived += (sender, e) =>
                            {
                                if (e.Data == null)
                                {
                                    if (errorWaitHandle != null)
                                        if (!errorWaitHandle.SafeWaitHandle.IsClosed)
                                            errorWaitHandle.Set();
                                }
                                else
                                {
                                    consoleerror.AppendLine(e.Data);
                                }
                            };

                            process.Start();

                            process.BeginOutputReadLine();
                            process.BeginErrorReadLine();

                            if (process.WaitForExit(timeout) &&
                                outputWaitHandle.WaitOne(timeout) &&
                                errorWaitHandle.WaitOne(timeout))
                            {
                                output = string.Format("{0} {1}", consoleoutput.ToString(), consoleerror.ToString());
                            }
                            else
                            {
                                output = string.Format("{0} {1}", consoleoutput.ToString(), consoleerror.ToString());
                            }
                        }
                    }
                    if (!string.IsNullOrEmpty(output))
                    {
                        if (output.IndexOf("SERVICE_NAME: Spooler") >= 0)
                            isActive = true;
                    }
                }
                catch { }

                System.Threading.Thread.Sleep(1000);
            }

            return isActive;                   
        }
    }
}
