using System;
using System.IO;

namespace IISMan.Infrastructure.CopyManager
{
    public class CopyManager : ICopyManager
    {
        private static readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();

        public void CopyFolders(string sourcePath, string destinationPath)
        {
            _log.Info("Started copying files from @{0} to @{1}", sourcePath, destinationPath);
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*",
    SearchOption.AllDirectories))
            {
                string fPath = dirPath.Replace(sourcePath, destinationPath);
                try
                {
                    if (Directory.Exists(fPath))
                    {
                        _log.Info("That path @{0} exists already.", fPath);
                        return;
                    }
                    Directory.CreateDirectory(fPath);
                }
                catch (Exception ex)
                {
                    _log.Error(ex.Message, "Error occured while creating directory @{0}", fPath);
                }
            }
            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(sourcePath, "*.*",
                SearchOption.AllDirectories))
            {
                string rPath = newPath.Replace(sourcePath, destinationPath);
                try
                {
                    File.Copy(newPath, rPath, true);
                }
                catch (Exception ex)
                {
                    _log.Error(ex.Message, "Error occured while creating file @{0}", rPath);
                }
            }
            _log.Info("Finished copying files from @{0} to @{1}", sourcePath, destinationPath);
        }
    }
}
