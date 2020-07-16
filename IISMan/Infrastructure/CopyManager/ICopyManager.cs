using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IISMan.Infrastructure.CopyManager
{
    public interface ICopyManager
    {
        void CopyFolders(string sourcePath, string destinationPath);
    }
}
