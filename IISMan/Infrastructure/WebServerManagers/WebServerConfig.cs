using System;

namespace IISMan.Infrastructure.WebServerManagers
{
    public class WebServerConfig
    {
        public string UserName { get; }

        public string UserPassword { get; }

        public string SiteName { get; }

        public string AppPoolName { get; }

        public bool IsIdentity { get; }

        public WebServerConfig(string userName,
            string userPassword,
            string siteName,
            string appPoolName,
            bool isIdentity)
        {
            UserName = userName ?? throw new ArgumentNullException(nameof(userName));
            UserPassword = userPassword ?? throw new ArgumentNullException(nameof(userPassword));
            SiteName = siteName ?? throw new ArgumentNullException(nameof(siteName));
            AppPoolName = appPoolName ?? throw new ArgumentNullException(nameof(appPoolName));
            IsIdentity = isIdentity;
        }
    }
}