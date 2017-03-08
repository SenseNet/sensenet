using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Packaging.Steps
{
    public abstract class ManageIIS : Step
    {
        /// <summary>
        /// Name of the remote machine (optional).
        /// </summary>
        [Annotation("Name of the remote machine (optional).")]
        public string MachineName { get; set; }

        /// <summary>
        /// Name of the IIS site to manage.
        /// </summary>
        [DefaultProperty]
        [Annotation("Name of the IIS site to manage.")]
        public string Site { get; set; }

        protected void ExecuteCommand(string command)
        {
            var cmdName = @"psexec";
            var machineName = string.IsNullOrEmpty(MachineName) ? @"\\" + Environment.MachineName : MachineName;
            var args = string.Format(@"{0} %windir%\system32\inetsrv\appcmd.exe {1} /site.name:{2}", machineName, command, Site);

            var startInfo = new ProcessStartInfo(cmdName, args)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                ErrorDialog = false,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            var process = new Process()
            {
                StartInfo = startInfo
            };            

            process.Start();
            process.WaitForExit();
        }
    }

    /// <summary>
    /// Stops an IIS site on a local or a remote machine.
    /// </summary>
    [Annotation("Stops an IIS site on a local or a remote machine.")]
    public class StopSite : ManageIIS
    {
        public override void Execute(ExecutionContext context)
        {
            // psexec \\sndev04 %windir%\system32\inetsrv\appcmd.exe stop site /site.name:ecm.test.sn.hu
            ExecuteCommand("stop site");
        }
    }

    /// <summary>
    /// Starts an IIS site on a local or a remote machine.
    /// </summary>
    [Annotation("Starts an IIS site on a local or a remote machine.")]
    public class StartSite : ManageIIS
    {
        public override void Execute(ExecutionContext context)
        {
            ExecuteCommand("start site");
        }
    }
}
