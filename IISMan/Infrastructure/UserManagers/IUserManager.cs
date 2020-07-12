namespace IISMan.Infrastructure.UserManagers
{
    public interface IUserManager
    {
        void CreateLocalUser(string username, string userPwd, string description, bool isRunUnderIdentity);
        bool IsUserExists(string username);
    }
}