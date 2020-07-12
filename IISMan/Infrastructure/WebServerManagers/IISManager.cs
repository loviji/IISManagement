using IISMan.Infrastructure.UserManagers;
using Microsoft.Web.Administration;
using System;
using System.IO;
using System.Linq;

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

        private const string DomainName = "ERPPRO";

        private readonly WebServerConfig _config;
        private readonly IUserManager _userManager;

        private IISDefaults _iisDefaults;

        public IISManager(WebServerConfig webServerConfig, IUserManager userManager)
        {
            _config = webServerConfig ?? throw new ArgumentNullException(nameof(webServerConfig));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));

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

        private void CreateIISPool()
        {
            _log.Info("Creating IIS pool:{0}", _config.AppPoolName);
            try
            {
                using (ServerManager serverManager = new ServerManager())
                {
                    ApplicationPool newPool = serverManager.ApplicationPools.Add(_config.AppPoolName);
                    newPool.ManagedRuntimeVersion = "v4.0";
                    newPool.AutoStart = false;
                    newPool.ProcessModel.IdentityType = ProcessModelIdentityType.SpecificUser;
                    newPool.ProcessModel.UserName = _config.UserName;
                    newPool.ProcessModel.Password = _config.UserPassword;
                    serverManager.CommitChanges();
                }

                _log.Info("IIS pool:{0} created", _config.AppPoolName);
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message, "Error occured while creating IIS pool:{0}", _config.AppPoolName);
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
                _log.Info("Attaching site to Application Pool: {0}", _config.AppPoolName);
                ServerManager iisManager = new ServerManager();
                iisManager.Sites.Add(DomainName, "http", "*:8057:", _iisDefaults.DefaultRoot + "\\erppro");
                iisManager.ApplicationDefaults.ApplicationPoolName = _config.AppPoolName;
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

            return availableSites.Any(x => x.Name == DomainName);
        }
    }
}