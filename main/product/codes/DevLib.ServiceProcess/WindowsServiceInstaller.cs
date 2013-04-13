﻿//-----------------------------------------------------------------------
// <copyright file="WindowsServiceInstaller.cs" company="YuGuan Corporation">
//     Copyright (c) YuGuan Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace DevLib.ServiceProcess
{
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.Configuration.Install;
    using System.ServiceProcess;

    /// <summary>
    /// Class WindowsServiceInstaller.
    /// </summary>
    [RunInstaller(true)]
    public class WindowsServiceInstaller : Installer
    {
        /// <summary>
        /// Field _serviceProcessInstaller.
        /// </summary>
        private ServiceProcessInstaller _serviceProcessInstaller;

        /// <summary>
        /// Field _serviceInstaller.
        /// </summary>
        private ServiceInstaller _serviceInstaller;

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsServiceInstaller" /> class.
        /// </summary>
        public WindowsServiceInstaller()
        {
            this.InstallerSetupInfo = new WindowsServiceSetup();
            this.InitInstaller();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsServiceInstaller" /> class.
        /// </summary>
        /// <param name="windowsServiceSetup">Instance of WindowsServiceSetup.</param>
        public WindowsServiceInstaller(WindowsServiceSetup windowsServiceSetup)
        {
            this.InstallerSetupInfo = windowsServiceSetup;
            this.InitInstaller();
        }

        /// <summary>
        /// Gets or sets instance of WindowsServiceSetup.
        /// </summary>
        public WindowsServiceSetup InstallerSetupInfo
        {
            get;
            set;
        }

        /// <summary>
        /// Install a windows service at runtime.
        /// </summary>
        /// <param name="windowsServiceSetup">Instance of WindowsServiceSetup.</param>
        public static void RuntimeInstall(WindowsServiceSetup windowsServiceSetup)
        {
            TransactedInstaller transactedInstaller = null;
            try
            {
                transactedInstaller = new TransactedInstaller();
                transactedInstaller.Installers.Add(new WindowsServiceInstaller(windowsServiceSetup));
                transactedInstaller.Context = new InstallContext(null, new[] { string.Format("/assemblypath=\"{0}\"", windowsServiceSetup.ServiceAssemblyPath) });
                transactedInstaller.Install(new Hashtable());
            }
            catch (Exception e)
            {
                ExceptionHandler.Log(e);
            }
            finally
            {
                if (transactedInstaller != null)
                {
                    transactedInstaller.Dispose();
                    transactedInstaller = null;
                }
            }
        }

        /// <summary>
        /// Uninstall a windows service at runtime.
        /// </summary>
        /// <param name="windowsServiceSetup">Instance of WindowsServiceSetup.</param>
        public static void RuntimeUninstall(WindowsServiceSetup windowsServiceSetup)
        {
            TransactedInstaller transactedInstaller = null;
            try
            {
                transactedInstaller = new TransactedInstaller();
                transactedInstaller.Installers.Add(new WindowsServiceInstaller(windowsServiceSetup));
                transactedInstaller.Context = new InstallContext(null, new[] { string.Format("/assemblypath=\"{0}\"", windowsServiceSetup.ServiceAssemblyPath) });
                transactedInstaller.Uninstall(null);
            }
            catch (Exception e)
            {
                ExceptionHandler.Log(e);
            }
            finally
            {
                if (transactedInstaller != null)
                {
                    transactedInstaller.Dispose();
                    transactedInstaller = null;
                }
            }
        }

        /// <summary>
        /// When overridden in a derived class, performs the installation.
        /// </summary>
        /// <param name="stateSaver">An <see cref="T:System.Collections.IDictionary" /> used to save information needed to perform a commit, rollback, or uninstall operation.</param>
        public override void Install(IDictionary stateSaver)
        {
            this.InitInstaller();
            base.Install(stateSaver);
        }

        /// <summary>
        /// When overridden in a derived class, removes an installation.
        /// </summary>
        /// <param name="savedState">An <see cref="T:System.Collections.IDictionary" /> that contains the state of the computer after the installation was complete.</param>
        public override void Uninstall(IDictionary savedState)
        {
            try
            {
                this.InitInstaller();
                base.Uninstall(savedState);
            }
            catch (Exception e)
            {
                ExceptionHandler.Log(e);
            }
        }

        /// <summary>
        /// When overridden in a derived class, restores the pre-installation state of the computer.
        /// </summary>
        /// <param name="savedState">An <see cref="T:System.Collections.IDictionary" /> that contains the pre-installation state of the computer.</param>
        public override void Rollback(IDictionary savedState)
        {
            this.InitInstaller();
            base.Rollback(savedState);
        }

        /// <summary>
        /// Method InitInstaller.
        /// </summary>
        private void InitInstaller()
        {
            this.Installers.Clear();

            if (this._serviceProcessInstaller == null)
            {
                this._serviceProcessInstaller = new ServiceProcessInstaller();
            }

            this._serviceProcessInstaller.Account = this.InstallerSetupInfo.Account;
            this._serviceProcessInstaller.Password = this.InstallerSetupInfo.Username;
            this._serviceProcessInstaller.Username = this.InstallerSetupInfo.Password;

            if (this._serviceInstaller == null)
            {
                this._serviceInstaller = new ServiceInstaller();
            }

            this._serviceInstaller.ServiceName = this.InstallerSetupInfo.ServiceName;
            this._serviceInstaller.DisplayName = this.InstallerSetupInfo.DisplayName;
            this._serviceInstaller.Description = this.InstallerSetupInfo.Description;
            this._serviceInstaller.StartType = this.InstallerSetupInfo.StartType;
            this._serviceInstaller.ServicesDependedOn = this.InstallerSetupInfo.ServicesDependedOn;

            this.Installers.AddRange(new Installer[] { this._serviceProcessInstaller, this._serviceInstaller });
        }
    }
}