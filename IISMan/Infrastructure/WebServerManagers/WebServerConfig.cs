using System;
using System.Collections.Generic;

namespace IISMan.Infrastructure.WebServerManagers
{
    public class WebServerConfig
    {
        public string SetupGuid { get; }

        public string UserName { get; }

        public string UserPassword { get; }

        public string SiteName { get; }

        public string[] AppPoolNames { get; }

        public string[] AppNames { get; }

        public bool IsIdentity { get; }

        public int ApplicationPortNumber { get; }

        public WebServerConfig(
            string setupGuid,
            string userName,
            string userPassword,
            string siteName,
            int applicationPortNumber,
            string[] appPoolNames,
            string[] applicationNames,
            bool isIdentity)
        {
            SetupGuid = setupGuid ?? throw new ArgumentException(nameof(setupGuid));
            UserName = userName ?? throw new ArgumentNullException(nameof(userName));
            UserPassword = userPassword ?? throw new ArgumentNullException(nameof(userPassword));
            SiteName = siteName ?? throw new ArgumentNullException(nameof(siteName));
            ApplicationPortNumber = applicationPortNumber;
            AppPoolNames = appPoolNames ?? throw new ArgumentNullException(nameof(appPoolNames));
            AppNames = applicationNames ?? throw new ArgumentNullException(nameof(applicationNames));
            IsIdentity = isIdentity;
        }
    }
}