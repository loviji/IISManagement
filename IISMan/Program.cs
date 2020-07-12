using IISMan.Infrastructure.UserManagers;
using IISMan.Infrastructure.WebServerManagers;
using System;

/// <summary>
/// 1. Create localuser for for pool with password   
/// 2. Create created user in step one to IIS_IUSRS group
/// 3. Then create applicationPool with unique Name. 
///    IF exist continue
/// 4. Create web-site with specified applicationpool in step 3 
/// 5. Add web-applications from list 
/// little not the must be a flag, possible then pool shot be run under ApplicationPoolIdentity Identity 
/// </summary>
namespace IISMan
{
    public class Program
    {
        private static readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();
        private static readonly WebServerConfig _webServerConfig =
            new WebServerConfig(
              userName: "app_usr",
              userPassword: "12#$qwER",
              siteName: "ERPPRO",
              appPoolName: "DMS_App_Pool",
              isIdentity: false);

        public static void Main(string[] args)
        {
            _log.Info("Started");

            try
            {
                var userManager = new UserManager();
                var iisManagement = new IISManager(_webServerConfig, userManager);
                iisManagement.CreateUserSite();
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }
            _log.Info("Finished");

            Console.ReadKey();
        }
    }
}
