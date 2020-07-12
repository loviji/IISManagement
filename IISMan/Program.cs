using Microsoft.Web.Administration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.DirectoryServices;
using System.Threading.Tasks;
using System.DirectoryServices.AccountManagement;

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
    class Program
    {
        static void Main(string[] args)
        {

            string userName = "app_usr", userPwd = "12#$qwER", siteName = "ERPPRO", appPoolName = "DMS_App_Pool";
            bool isIdentity = false;

            WebServerManagement iisMan = new WebServerManagement();
            iisMan.IsIdentity = isIdentity;
            iisMan.UserName = userName;
            iisMan.UserPwd = userPwd;
            iisMan.SiteName = siteName;
            iisMan.AppPoolName = appPoolName;

            iisMan.createUserSite();// (userName, userPwd, isIdentity, siteName, appPoolName);


            Console.ReadKey();
        }







    }

    public class WebServerManagement :IWebserverManagement
    {
        public string UserName { get; set; }

        public string UserPwd { get; set; }

        public string SiteName { get; set; }

        public string AppPoolName { get; set; }

        public bool IsIdentity { get; set; }

        public WebServerManagement()
        {

        }


        public WebServerManagement(string userName, string userPwd, string siteName, string appPoolName, bool isIdentity)
        {

            this.UserName = userName;
            this.UserPwd = userPwd;
            this.SiteName = siteName;
            this.AppPoolName = appPoolName;
            this.IsIdentity = isIdentity;

        }


        public void createUserSite()//(string userName, string userPwd, bool isIdentity, string siteName, string appPoolName)
        {
            string systemDrive = System.IO.Path.GetPathRoot(WindowsDirectory());
            string iisdefaultroot = String.Format(@"{0}inetpub\wwwroot", systemDrive);
            string iisPROSYSRoot = String.Format(@"{0}\PROSYSinetpub\wwwroot", systemDrive);
            string domainName = "ERPPRO";

            UserManagement userManagement = new UserManagement();
            if (!userManagement.userExists(UserName))
                userManagement.createLocalUser(UserName, UserPwd, UserName, false);

            if (!Directory.Exists(iisdefaultroot))
            {
                Directory.CreateDirectory(iisPROSYSRoot);
                iisdefaultroot = iisPROSYSRoot;
            }


            createIISPool(AppPoolName, UserName, UserPwd);




            if (IsWebsiteExists(domainName) == false)
            {
                if (!Directory.Exists(iisdefaultroot + "\\erppro"))
                {
                    Directory.CreateDirectory(iisdefaultroot + "\\erppro");
                    iisdefaultroot = iisPROSYSRoot;
                }
                ServerManager iisManager = new ServerManager();
                iisManager.Sites.Add(domainName, "http", "*:8057:", iisdefaultroot + "\\erppro");


                iisManager.ApplicationDefaults.ApplicationPoolName = AppPoolName;

                iisManager.CommitChanges();
                Console.WriteLine("Site created");
            }
            else
            {
                Console.WriteLine("Name Exists already");
            }

        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern uint GetWindowsDirectory(StringBuilder lpBuffer, uint uSize);

        public string WindowsDirectory()
        {
            uint size = 0;
            size = GetWindowsDirectory(null, size);

            StringBuilder sb = new StringBuilder((int)size);
            GetWindowsDirectory(sb, size);

            return sb.ToString();
        }
        public bool IsWebsiteExists(string strWebsitename)
        {
            ServerManager serverMgr = new ServerManager();
            Boolean flagset = false;
            SiteCollection sitecollection = serverMgr.Sites;
            flagset = sitecollection.Any(x => x.Name == strWebsitename);
            return flagset;
        }

        private void createIISPool(string poolName, string userName, string pwd)
        {
            try
            {
                using (ServerManager serverManager = new ServerManager())
                {
                    ApplicationPool newPool = serverManager.ApplicationPools.Add(poolName);
                    newPool.ManagedRuntimeVersion = "v4.0";
                    newPool.AutoStart = false;
                    newPool.ProcessModel.IdentityType = ProcessModelIdentityType.SpecificUser;
                    newPool.ProcessModel.UserName = userName;
                    newPool.ProcessModel.Password = pwd;
                    serverManager.CommitChanges();
                }
            }
            catch (Exception ex)
            {

            }
        }
    }
    public class UserManagement
    {
        private readonly string groupSchemaClass = "group";
        private readonly string administratorGroupName = "Administrators";
        private readonly string administratorGroupNameRussian = "Администраторы";

        public bool userExists(string username)
        {
            bool userExists = false;
            using (PrincipalContext pc = new PrincipalContext(ContextType.Machine))
            {
                UserPrincipal up = UserPrincipal.FindByIdentity(
                    pc,
                    IdentityType.SamAccountName,
                    username);

                userExists = (up != null);
            }
            return userExists;

        }

        public bool groupExists(string groupName)
        {
            DirectoryEntry oComputer = new DirectoryEntry("WinNT://" + Environment.MachineName + ",computer");
            return oComputer.Children.Cast<DirectoryEntry>().Any(d => d.SchemaClassName.Equals("Group") && d.Name.Equals(groupName));
        }
        public void createLocalUser(string username, string userPwd, string description, bool isRunUnderIdentity)
        {

            char[] aPWchars = userPwd.ToCharArray();
            System.Security.SecureString oPW = new System.Security.SecureString();
            foreach (char cChr in aPWchars)
            {
                oPW.AppendChar(cChr);
            }
            // Get Computerobject via ADSI
            DirectoryEntry oComputer = new DirectoryEntry("WinNT://" + Environment.MachineName + ",computer");
            // New User
            DirectoryEntry oNewUser = null;
            PrincipalContext ctx = new PrincipalContext(ContextType.Machine);
            UserPrincipal up = UserPrincipal.FindByIdentity(
        ctx,
        IdentityType.SamAccountName,
        "UserName");

            oNewUser = oComputer.Children.Add(username, "user");
            // define Pointer to a string
            IntPtr pString = IntPtr.Zero;
            // Pointer to password
            pString = Marshal.SecureStringToGlobalAllocUnicode(oPW);
            // Set password
            oNewUser.Invoke("SetPassword", new object[] { Marshal.PtrToStringUni(pString)
});
            // Add a description
            oNewUser.Invoke("Put", new object[] { "Description", description });
            // Save changes
            oNewUser.CommitChanges();
            // Cleanup and free Password pointer
            Marshal.ZeroFreeGlobalAllocUnicode(pString);
            // Get Group

            DirectoryEntry oGroup = null;
            if (groupExists(administratorGroupName))
                oGroup = oComputer.Children.Find(administratorGroupName, groupSchemaClass);
            else
            {
                if (groupExists(administratorGroupNameRussian))
                {
                    oGroup = oComputer.Children.Find(administratorGroupNameRussian, groupSchemaClass);
                }
            }


            // And add the recently created user
            oGroup.Invoke("Add", new object[] { oNewUser.Path.ToString() });

        }
    }

}
