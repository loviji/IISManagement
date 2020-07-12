using System.Runtime.InteropServices;
using System.Text;

namespace IISMan.Infrastructure
{
    internal class DirectoryResolver
    {
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern uint GetWindowsDirectory(StringBuilder lpBuffer, uint uSize);

        public static string ResolveWindowsDirectory()
        {
            uint size = 0;
            size = GetWindowsDirectory(null, size);

            StringBuilder sb = new StringBuilder((int)size);
            GetWindowsDirectory(sb, size);

            return sb.ToString();
        }
    }
}
