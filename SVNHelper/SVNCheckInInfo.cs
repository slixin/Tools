using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SVNHelper
{
    public class SVNCheckInFile
    {
        public string Action { get; set; }
        public string File { get; set; }
    }
    public class SVNCheckInInfo
    {
        public string LogText { get; set; }
        public string Author { get; set; }
        public long Revision { get; set; }
        public List<int> Defects { get; set; }
        public List<int> UserStories { get; set; }
        public List<SVNCheckInFile> Files { get; set; }
        

    }
}
