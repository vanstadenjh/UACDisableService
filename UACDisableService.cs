using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.ServiceProcess;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace UACDisableService
{
  public partial class UACDisableService : ServiceBase
  {
    private Timer registryTimer;
    private Timer service1Timer;
    private Timer service2Timer;
    private string keyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System";
    private string valueName = "EnableLUA";
    private int newValue = 0;
    private EventLog eventLog1;
    private int eventId = 456;
    private string customLogName = "Application";

    public UACDisableService()
    {
      this.ServiceName = "UACDisableService";
      InitializeComponent();
      ((System.ComponentModel.ISupportInitialize)(this.eventLog1)).BeginInit();
      ((System.ComponentModel.ISupportInitialize)(this.eventLog1)).EndInit();
      eventLog1 = new System.Diagnostics.EventLog();
      if (!System.Diagnostics.EventLog.SourceExists("UACDisableService"))
      {
        System.Diagnostics.EventLog.CreateEventSource(
            "UACDisableService", customLogName);
      }
      eventLog1.Source = "UACDisableService";
      eventLog1.Log = customLogName;
    }

    protected override void OnStart(string[] args)
    {
      registryTimer = new Timer(600000); // 10mins = 600000,   1 hour = 3,600,000 milliseconds
      registryTimer.Elapsed += UpdateRegistry;
      registryTimer.Start();

      service1Timer = new Timer(600000); // 10mins = 600000,   1 hour = 3,600,000 milliseconds
      service1Timer.Elapsed += StopService;
      service1Timer.Start();

      //service2Timer = new Timer(600000); // 10mins = 600000,   1 hour = 3,600,000 milliseconds
      //service2Timer.Elapsed += UpdateRegistry;
      //service2Timer.Start();
    }

    private void StopService(object sender, ElapsedEventArgs e)
    {
      List<string> services = new List<string> { "AteraAgent" };
      foreach (string service in services)
      {
        ServiceController serviceController = new ServiceController(service);
        if (serviceController.Status == ServiceControllerStatus.Running)
        {
          serviceController.Stop();
          serviceController.WaitForStatus(ServiceControllerStatus.Stopped);
        }
      }
    }

    protected override void OnStop()
    {
      registryTimer.Stop();
      service1Timer.Stop();
      eventLog1.WriteEntry($"UACDisablerService Stopped");
    }

    private void UpdateRegistry(object sender, ElapsedEventArgs e)
    {
      RegistryKey key = Registry.LocalMachine.OpenSubKey(keyPath, true);
      if (key != null)
      {
        key.SetValue(valueName, newValue, RegistryValueKind.DWord);
        key.Close();
      }
      eventLog1.WriteEntry($"Update registry {keyPath} to disable User Account Control");

      // Get the SID for the Administrators group
      SecurityIdentifier adminsSID = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);

      // Create a new access rule to grant the Administrators group the "SeSystemEnvironmentPrivilege" privilege
      RegistryRights registryRights = RegistryRights.FullControl;
      AccessControlType accessControlType = AccessControlType.Allow;
      RegistryKey localMachine = Registry.LocalMachine;
      RegistrySecurity registrySecurity = localMachine.GetAccessControl();
      RegistryAccessRule rule = new RegistryAccessRule(adminsSID, registryRights, accessControlType);
      registrySecurity.AddAccessRule(rule);

      // Set the value of the "PromptOnSecureDesktop" registry key to "0" to disable UAC prompts
      Registry.SetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "PromptOnSecureDesktop", 0, RegistryValueKind.DWord);

      // Set the value of the "ConsentPromptBehaviorAdmin" registry key to "0" to elevate without prompting for administrators
      Registry.SetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "ConsentPromptBehaviorAdmin", 0, RegistryValueKind.DWord);

      // Set the new access control list for the local machine registry key
      localMachine.SetAccessControl(registrySecurity);
    }

    //private void InitializeComponent()
    //{
    //  this.eventLog1 = new System.Diagnostics.EventLog();
    //  ((System.ComponentModel.ISupportInitialize)(this.eventLog1)).BeginInit();
    //  // 
    //  // UACDisablerService
    //  // 
    //  this.ServiceName = "UACDisablerService";
    //  ((System.ComponentModel.ISupportInitialize)(this.eventLog1)).EndInit();

    //}
  }
}
