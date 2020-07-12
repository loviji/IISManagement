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
            if (!_userManager.IsUserExists(_config.UserName))
                _userManager.CreateLocalUser(_config.UserName, _config.UserName, false);
        }

        private void CreateIISDirectoryAndChangeDefaults()
        {
            if (!Directory.Exists(_iisDefaults.DefaultRoot))
            {
                Directory.CreateDirectory(_iisDefaults.ProsysRoot);
                _iisDefaults.DefaultRoot = _iisDefaults.ProsysRoot;
            }
        }

        private void CreateIISPool()
        {
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
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error occured while creating IIS pool. Exception: {0}", ex.Message);
            }
        }

        private void CreateWebSite()
        {
            if (HasWebsite())
            {
                return;
            }

            if (!Directory.Exists(_iisDefaults.DefaultRoot + "\\erppro"))
            {
                Directory.CreateDirectory(_iisDefaults.DefaultRoot + "\\erppro");
                _iisDefaults.DefaultRoot = _iisDefaults.ProsysRoot;
            }

            ServerManager iisManager = new ServerManager();
            iisManager.Sites.Add(DomainName, "http", "*:8057:", _iisDefaults.DefaultRoot + "\\erppro");
            iisManager.ApplicationDefaults.ApplicationPoolName = _config.AppPoolName;
            iisManager.CommitChanges();
        }

        private bool HasWebsite()
        {
            var serverManager = new ServerManager();
            SiteCollection availableSites = serverManager.Sites;

            return availableSites.Any(x => x.Name == DomainName);
        }
    }
}