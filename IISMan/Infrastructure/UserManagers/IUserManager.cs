namespace IISMan.Infrastructure.UserManagers
{
    public interface IUserManager
    {
        void CreateLocalUser(string username, string description, bool isRunUnderIdentity);
        bool IsUserExists(string username);
    }
}