using IISMan.Infrastructure.UserManagers;
using Microsoft.Web.Administration;
using System;
using System.IO;
using System.Linq;
using IISMan.Infrastructure.CopyManager;

namespace IISMan.Infrastructure.WebServerManagers
{
    internal struct IISDefaults
    {
        public string DefaultRoot { get; set; }
        public string ProsysRoot { get; set; }
    }

    public class IISManager : IWebServerManager
    {
        private static readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();



        private readonly WebServerConfig _config;
        private readonly IUserManager _userManager;
        private readonly ICopyManager _copyManager;

        private IISDefaults _iisDefaults;

        public IISManager(WebServerConfig webServerConfig, IUserManager userManager, ICopyManager copyManager)
        {
            _config = webServerConfig ?? throw new ArgumentNullException(nameof(webServerConfig));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _copyManager = copyManager ?? throw new ArgumentNullException(nameof(copyManager));
            InitIISState();
        }

        private void InitIISState()
        {
            var systemDrive = Path.GetPathRoot(DirectoryResolver.ResolveWindowsDirectory());
            _iisDefaults = new IISDefaults
            {
                ProsysRoot = $@"{systemDrive}\PROSYSinetpub\wwwroot",
                DefaultRoot = $@"{systemDrive}inetpub\wwwroot"
            };
        }

        public void CreateUserSite()
        {
            CreateUserIfNotExists();
            CreateIISDirectoryAndChangeDefaults();
            CreateIISPool();
            CreateWebSite();
            CreateApplications();
            CopyApplicationFiles();
        }

        private void CopyApplicationFiles()
        {
            foreach (var _configAppPoolName in _config.AppPoolNames)
            {
                //string from = @"C:\ApplicationBackup\20200710-TQNKZ\DMSPRO";
                //string to = @"C:\inetpub\wwwroot\ERPPRO\DMS";

                string fromK = @"C:\ApplicationBackup\" + _config.SetupGuid + "\\" + _config.AppNames[0];
                string webapp = _configAppPoolName.Substring(0, _configAppPoolName.IndexOf("_"));
                string toK = _iisDefaults.DefaultRoot + "\\"+_config.SiteName+"\\"+webapp;
                _copyManager.CopyFolders(fromK, toK);
            }
        }

        private void CreateApplications()
        {
            foreach (var _configAppPoolName in _config.AppPoolNames)
            {
                _log.Info("Creating Web-site application:{0}", _configAppPoolName);
                try
                {
                    string webapp = _configAppPoolName.Substring(0, _configAppPoolName.IndexOf("_"));
                    CreateIISAppDirectory(webapp);
                    using (ServerManager serverManager = new ServerManager())
                    {
                        string siteName = _config.SiteName;
                        Site site = serverManager.Sites[siteName];
                        ApplicationPool appPool = serverManager.ApplicationPools[_configAppPoolName];
                        site.Stop();
                        site.ApplicationDefaults.ApplicationPoolName = appPool.Name;

                        site.Applications.Add("/" + webapp, _iisDefaults.DefaultRoot + "\\" + siteName + "\\" + webapp);
                        site.Applications.Where(s => s.Path == "/" + webapp).FirstOrDefault().ApplicationPoolName = appPool.Name;
                        //serverManager.Sites[siteName].Applications.Add("/HRM", _iisDefaults.DefaultRoot+"\\ERPPRO"+"\\HRM");
                        //serverManager.Sites[siteName].Applications.Add("/DMS", _iisDefaults.DefaultRoot+"\\ERPPRO"+"\\DMS");
                        site.Start();
                        serverManager.CommitChanges();
                    }

                    _log.Info("IIS Web-site application:{0} created", _configAppPoolName);
                }
                catch (Exception ex)
                {
                    _log.Error(ex.Message, "Error occured while creating IIS pool:{0}", _configAppPoolName);
                }
            }

        }

        private void CreateUserIfNotExists()
        {
            _log.Info("Creating user:{0} with password: ******", _config.UserName);

            if (!_userManager.IsUserExists(_config.UserName))
            {
                _userManager.CreateLocalUser(_config.UserName, _config.UserPassword, _config.UserName, false);
                _log.Info("User:{0} created: ******", _config.UserName);
            }
            else
            {
                _log.Info("User:{0} already exist. Not created.", _config.UserName);
            }
        }

        private void CreateIISDirectoryAndChangeDefaults()
        {
            if (!Directory.Exists(_iisDefaults.DefaultRoot))
            {
                _log.Info("Creating directory:{0}", _iisDefaults.ProsysRoot);
                Directory.CreateDirectory(_iisDefaults.ProsysRoot);
                _log.Info("Directory:{0} created", _iisDefaults.ProsysRoot);

                _iisDefaults.DefaultRoot = _iisDefaults.ProsysRoot;
            }
        }

        private void CreateIISAppDirectory(string appName)
        {
            string path = _iisDefaults.DefaultRoot + "\\" + _config.SiteName + "\\" + appName;
            if (!Directory.Exists(path))
            {
                _log.Info("Create web application:{0}", appName);
                Directory.CreateDirectory(path);
                _log.Info("Directory:{0} created", _iisDefaults.ProsysRoot);
            }
        }

        private void CreateIISPool()
        {
            foreach (var _configAppPoolName in _config.AppPoolNames)
            {
                _log.Info("Creating IIS pool:{0}", _configAppPoolName);
                try
                {
                    using (ServerManager serverManager = new ServerManager())
                    {
                        ApplicationPool newPool = serverManager.ApplicationPools.Add(_configAppPoolName);
                        newPool.ManagedRuntimeVersion = "v4.0";
                        newPool.AutoStart = false;
                        newPool.ProcessModel.IdentityType = ProcessModelIdentityType.SpecificUser;
                        newPool.ProcessModel.UserName = _config.UserName;
                        newPool.ProcessModel.Password = _config.UserPassword;
                        serverManager.CommitChanges();
                    }

                    _log.Info("IIS pool:{0} created", _configAppPoolName);
                }
                catch (Exception ex)
                {
                    _log.Error(ex.Message, "Error occured while creating IIS pool:{0}", _configAppPoolName);
                }
            }
        }

        private void CreateWebSite()
        {
            _log.Info("Creating Web Site");

            if (HasWebsite())
            {
                _log.Info("Web Site already exists");
                return;
            }

            if (!Directory.Exists(_iisDefaults.DefaultRoot + "\\erppro"))
            {
                _log.Info("Created a new directory: {0}", _iisDefaults.DefaultRoot + "\\erppro");
                Directory.CreateDirectory(_iisDefaults.DefaultRoot + "\\erppro");
                _iisDefaults.DefaultRoot = _iisDefaults.ProsysRoot;
            }

            try
            {
                _log.Info("Attaching site to Application Pool: {0}", _config.AppPoolNames[0]);
                ServerManager iisManager = new ServerManager();
                iisManager.Sites.Add(_config.SiteName, "http", String.Format("*:{0}:", _config.ApplicationPortNumber.ToString()), _iisDefaults.DefaultRoot + "\\erppro");
                iisManager.ApplicationDefaults.ApplicationPoolName = _config.AppPoolNames[0];
                iisManager.CommitChanges();
                _log.Info("Site successfuly attached to Application Pool.");
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message, "Error occured while creating Web Site");
            }
        }

        private bool HasWebsite()
        {
            var serverManager = new ServerManager();
            SiteCollection availableSites = serverManager.Sites;

            return availableSites.Any(x => x.Name == _config.SiteName);
        }
    }
}