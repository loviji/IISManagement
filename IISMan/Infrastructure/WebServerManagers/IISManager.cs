using IISMan.Infrastructure.UserManagers;
using Microsoft.Web.Administration;
using System;
using System.IO;
using System.Linq;

namespace IISMan.Infrastructure.WebServerManagers
{
    internal struct IISState
    {
        public string DefaultRoot { get; set; }
        public string ProsysRoot { get; set; }
    }

    public class IISManager : IWebServerManager
    {
        private const string DomainName = "ERPPRO";

        private readonly WebServerConfig _config;
        private readonly IUserManager _userManager;

        private IISState _iisState;

        private bool HasWebsite
        {
            get
            {
                var serverManager = new ServerManager();
                SiteCollection availableSites = serverManager.Sites;

                return availableSites.Any(x => x.Name == DomainName);
            }
        }

        public IISManager(WebServerConfig webServerConfig, IUserManager userManager)
        {
            _config = webServerConfig ?? throw new ArgumentNullException(nameof(webServerConfig));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));

            InitIISProperties();
        }

        private void InitIISProperties()
        {
            var systemDrive = Path.GetPathRoot(DirectoryResolver.ResolveWindowsDirectory());
            _iisState = new IISState
            {
                ProsysRoot = $@"{systemDrive}\PROSYSinetpub\wwwroot",
                DefaultRoot = $@"{systemDrive}inetpub\wwwroot"
            };
        }

        public void CreateUserSite()//(string userName, string userPwd, bool isIdentity, string siteName, string appPoolName)
        {
            if (!_userManager.IsUserExists(_config.UserName))
                _userManager.CreateLocalUser(_config.UserName, _config.UserName, false);

            if (!Directory.Exists(_iisState.DefaultRoot))
            {
                Directory.CreateDirectory(_iisState.ProsysRoot);
                _iisState.DefaultRoot = _iisState.ProsysRoot;
            }

            CreateIISPool();
            CreateWebSite();
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
            if (!HasWebsite)
            {
                if (!Directory.Exists(_iisState.DefaultRoot + "\\erppro"))
                {
                    Directory.CreateDirectory(_iisState.DefaultRoot + "\\erppro");
                    _iisState.DefaultRoot = _iisState.ProsysRoot;
                }

                ServerManager iisManager = new ServerManager();
                iisManager.Sites.Add(DomainName, "http", "*:8057:", _iisState.DefaultRoot + "\\erppro");
                iisManager.ApplicationDefaults.ApplicationPoolName = _config.AppPoolName;
                iisManager.CommitChanges();

                Console.WriteLine("Site created");
            }
            else
            {
                Console.WriteLine("Name Exists already");
            }
        }
    }
}