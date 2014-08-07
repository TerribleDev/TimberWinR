using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace TimberWinR.ServiceHost
{
    [RunInstaller(true)]
    public partial class Installer : System.Configuration.Install.Installer
    {
        public Installer()
        {
            InitializeComponent();
        }

        public override void Install(IDictionary stateSaver)
        {
            base.Install(stateSaver);

            string keyPath = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\TimberWinR";
            string keyName = "ImagePath";

            string currentValue = Registry.GetValue(keyPath, keyName, "").ToString();
            if (!string.IsNullOrEmpty(currentValue))
            {
                string configFile = Context.Parameters["configfile"];
                if (!string.IsNullOrEmpty(configFile) && !currentValue.Contains("-configFile "))
                {
                    currentValue += string.Format(" -configFile \"{0}\"", configFile.Replace("\\\\", "\\"));                    
                    Registry.SetValue(keyPath, keyName, currentValue);
                }

                currentValue = Registry.GetValue(keyPath, keyName, "").ToString();

                string logDir = Context.Parameters["logdir"];
                if (!string.IsNullOrEmpty(logDir) && !currentValue.Contains("-logDir "))
                {
                    currentValue += string.Format(" -logDir \"{0}\"", logDir.Replace("\\\\", "\\"));                   
                    Registry.SetValue(keyPath, keyName, currentValue);
                }
            }
        }     
    }
}
