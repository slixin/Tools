using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HyperVHelper
{
    class Program
    {
        static private ConnectionOptions connOptions;
        static private string host;
        static private string host_username;
        static private string host_password;
        static private string vmname;
        static private string vmip;
        static private string vmusername;
        static private string vmpassword;

        enum OperationType { NotSupport, Restore, Reboot, Reset };

        static void Main(string[] args)
        {
            try
            {
                host = GetArgument(args, "/host");
                host_username = GetArgument(args, "/username");
                host_password = GetArgument(args, "/password");
                vmname = GetArgument(args, "/vmname");
                vmip = GetArgument(args, "/vmip");
                vmusername = GetArgument(args, "/vmusername");
                vmpassword = GetArgument(args, "/vmpassword");
                bool vmIsLinux = string.IsNullOrEmpty(GetArgument(args, "/islinux")) ? false : Convert.ToBoolean(GetArgument(args, "/islinux"));

                connOptions = new ConnectionOptions();
                connOptions.Username = host_username;
                connOptions.Password = host_password;
                connOptions.EnablePrivileges = true;
                connOptions.Authentication = AuthenticationLevel.Default;
                connOptions.Timeout = new TimeSpan(0, 15, 0);

                switch (GetOperationType(args))
                {
                    case OperationType.Restore:
                        Utility.RequestStateChange(host, connOptions, vmname, "stop");
                        ApplyVirtualSystemSnapshot(vmname);
                        Utility.RequestStateChange(host, connOptions, vmname, "start");
                        WaitUntilActive(vmIsLinux);
                        break;
                    case OperationType.Reboot:
                        Utility.RequestStateChange(host, connOptions, vmname, "reboot");
                        WaitUntilActive(vmIsLinux);
                        break;
                    case OperationType.Reset:
                        Utility.RequestStateChange(host, connOptions, vmname, "reset");
                        WaitUntilActive(vmIsLinux);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static string GetArgument(string[] args, string name)
        {
            string value = null;

            foreach (string arg in args)
            {
                if (arg.StartsWith(string.Format("{0}=", name), StringComparison.InvariantCultureIgnoreCase))
                {
                    if (arg.Split(new char[] { '=' }).Length == 2)
                    {
                        value = arg.Split(new char[] { '=' })[1];
                        break;
                    }
                }
            }

            return value;
        }

        static OperationType GetOperationType(string[] args)
        {
            foreach (string arg in args)
            {
                if (arg.Equals("/restore", StringComparison.InvariantCultureIgnoreCase))
                    return OperationType.Restore;
                if (arg.Equals("/reboot", StringComparison.InvariantCultureIgnoreCase))
                    return OperationType.Reboot;
                if (arg.Equals("/reset", StringComparison.InvariantCultureIgnoreCase))
                    return OperationType.Reset;
            }
            return OperationType.NotSupport;
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
            bool isRestarting = false;

            while (!isActive && DateTime.Now.Subtract(rebootTime).TotalSeconds < RebootTimeoutSeconds)
            {
                try
                {
                    if (!PingHost(vmip))
                    {
                        Console.WriteLine("Detected restart start");
                        isRestarting = true;
                    }
                    else if (isRestarting)
                    {
                        Console.WriteLine("Restarted");
                        isActive = true;
                    }
                    else
                    {
                        Console.WriteLine("Detecting restarting");
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

            while (!isActive && DateTime.Now.Subtract(rebootTime).TotalSeconds < RebootTimeoutSeconds)
            {
                try
                {
                    string output = null;

                    string exe = string.Format(@"{0}\PsService.exe", Environment.CurrentDirectory);
                    string argument = string.Format("-accepteula \\\\{0} -u {1} -p {2}  query spooler", vmip, vmusername, vmpassword);

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

        #region HyperV related methods
        static ManagementObject GetLastVirtualSystemSnapshot(ManagementObject vm)
        {
            ManagementObjectCollection settings = vm.GetRelated(
                "Msvm_VirtualSystemsettingData",
                "Msvm_PreviousSettingData",
                null,
                null,
                "SettingData",
                "ManagedElement",
                false,
                null);

            ManagementObject virtualSystemsetting = null;
            foreach (ManagementObject setting in settings)
            {
                Console.WriteLine(setting.Path.Path);
                Console.WriteLine(setting["ElementName"]);
                virtualSystemsetting = setting;

            }

            return virtualSystemsetting;
        }

        static void ApplyVirtualSystemSnapshot(string vmName)
        {
            ManagementScope scope = new ManagementScope(string.Format(@"\\{0}\root\virtualization", host), connOptions);
            ManagementObject virtualSystemService = Utility.GetServiceObject(scope, "Msvm_VirtualSystemManagementService");
            ManagementBaseObject inParams = virtualSystemService.GetMethodParameters("ApplyVirtualSystemSnapshot");
            ManagementObject vm = Utility.GetTargetComputer(vmName, scope);
            ManagementObject vmSnapshot = GetLastVirtualSystemSnapshot(vm);

            inParams["SnapshotSettingData"] = vmSnapshot.Path.Path;
            inParams["ComputerSystem"] = vm.Path.Path;

            ManagementBaseObject outParams = virtualSystemService.InvokeMethod("ApplyVirtualSystemSnapshot", inParams, null);

            if ((UInt32)outParams["ReturnValue"] == ReturnCode.Started)
            {
                if (Utility.JobCompleted(outParams, scope))
                {
                    Console.WriteLine("Snapshot was applied successfully.");
                }
                else
                {
                    Console.WriteLine("Failed to apply snapshot.");
                }
            }
            else if ((UInt32)outParams["ReturnValue"] == ReturnCode.Completed)
            {
                Console.WriteLine("Snapshot was applied successfully.");
            }
            else
            {
                Console.WriteLine("Apply virtual system snapshot failed with error {0}", outParams["ReturnValue"]);
            }

            inParams.Dispose();
            outParams.Dispose();
            vmSnapshot.Dispose();
            vm.Dispose();
            virtualSystemService.Dispose();
        }
        
        static string GetIP(string vmname)
	    {
            string vmIP = null;
	  
	        ManagementScope scope = new ManagementScope(string.Format(@"\\{0}\root\virtualization",host), connOptions);
	        ManagementObject computer = Utility.GetTargetComputer(vmname, scope);
	        string ips = Utility.GetComputerKVP(computer["ElementName"].ToString(), scope, "NetworkAddressIPv4");
	        if (!string.IsNullOrEmpty(ips))
	        {
	            foreach (string ip in ips.Split(new char[] { ';' }).Where(o => !string.IsNullOrEmpty(o.Trim()) & !o.Equals("127.0.0.1")))
	            {
	                vmIP = ip;
	                break;
	            }
	        }
	  
	    return vmIP;
	    }
        #endregion
    }
}
