using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;

namespace IISMan.Infrastructure.UserManagers
{
    // TODO should be refactored also.
    public class UserManager : IUserManager
    {
        private readonly string groupSchemaClass = "group";
        private readonly string administratorGroupName = "Administrators";
        private readonly string administratorGroupNameRussian = "Администраторы";

        public void CreateLocalUser(string username, string description, bool isRunUnderIdentity)
        {
            char[] aPWchars = { 'P', 'a', 's', 's', 'w', 'o', 'r', 'd' };
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
            oNewUser.Invoke("SetPassword",
                new object[] { Marshal.PtrToStringUni(pString) });
            // Add a description
            oNewUser.Invoke("Put", new object[] { "Description", description });
            // Save changes
            oNewUser.CommitChanges();
            // Cleanup and free Password pointer
            Marshal.ZeroFreeGlobalAllocUnicode(pString);
            // Get Group

            DirectoryEntry oGroup = null;
            if (IsGroupExist(administratorGroupName))
                oGroup = oComputer.Children.Find(administratorGroupName, groupSchemaClass);
            else
            {
                if (IsGroupExist(administratorGroupNameRussian))
                {
                    oGroup = oComputer.Children.Find(administratorGroupNameRussian, groupSchemaClass);
                }
            }

            // And add the recently created user
            oGroup.Invoke("Add", new object[] { oNewUser.Path.ToString() });
        }

        public bool IsUserExists(string username)
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

        private bool IsGroupExist(string groupName)
        {
            DirectoryEntry oComputer = new DirectoryEntry("WinNT://" + Environment.MachineName + ",computer");
            return oComputer.Children.Cast<DirectoryEntry>().Any(d => d.SchemaClassName.Equals("Group") && d.Name.Equals(groupName));
        }
    }
}
