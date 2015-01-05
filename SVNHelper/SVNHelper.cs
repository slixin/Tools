using SharpSvn;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SVNHelper
{
    public class SVNHelper
    {
        public string SVNPath { get; set; }

        private SvnClient _svnClient;
        private string _svnUsername;
        private string _svnPassword;

        public SVNHelper(string username, string password)
        {
            _svnUsername = username;
            _svnPassword = password;

            _svnClient = new SvnClient();
            _svnClient.Authentication.DefaultCredentials = new NetworkCredential(_svnUsername, _svnPassword);
        }

        ~SVNHelper()
        {
            if (_svnClient != null)
                _svnClient.Dispose();
        }

        public List<SVNCheckInInfo> GetSVNCheckInInfoByRevison(string svnUri, int firstRevision, int secondRevison)
        {
            if (string.IsNullOrEmpty(svnUri))
                throw new Exception("SVN Uri cannot be empty.");

            if (firstRevision == 0 || secondRevison == 0)
                throw new Exception("Revision cannot be 0.");

            if (secondRevison < firstRevision)
                throw new Exception("Second revision number has to be larger than first one.");

            List<SVNCheckInInfo> svnInfo = new List<SVNCheckInInfo>();

            Collection<SvnLogEventArgs> logItems;
            bool result = _svnClient.GetLog(new Uri(svnUri), new SvnLogArgs { Range = new SvnRevisionRange(firstRevision, secondRevison) }, out logItems);
            if (result)
            {
                foreach(SvnLogEventArgs log in logItems)
                {
                    SVNCheckInInfo svnciInfo = new SVNCheckInInfo();
                    svnciInfo.Author = log.Author;
                    svnciInfo.Revision = log.Revision;
                    svnciInfo.LogText = log.LogMessage;
                    svnciInfo.Defects = GetDefects(log.LogMessage);
                    svnciInfo.UserStories = GetUserstories(log.LogMessage);
                    svnciInfo.Files = new List<SVNCheckInFile>();
                    foreach (SvnChangeItem changeItem in log.ChangedPaths)
                    {
                        svnciInfo.Files.Add(new SVNCheckInFile() { File = changeItem.RepositoryPath.ToString(), Action = changeItem.Action.ToString() });
                    }
                    svnInfo.Add(svnciInfo);
                }
            }

            return svnInfo;
        }

        private List<int> GetDefects(string text)
        {
            List<int> defectIds = new List<int>();
            string defectRegex = @"Defect ID/URL:.*[\d]{3,5}\s|Defect ID/URL:.*\s[\d]{3,5}";
            string numRegex = @"[\d]{3,5}";

            Match dm = Regex.Match(text, defectRegex, RegexOptions.IgnoreCase|RegexOptions.Multiline);
            if (dm.Success)
            {
                string dstr = dm.Groups[0].Value;
                MatchCollection dNums = Regex.Matches(dstr, numRegex);
                foreach(Match dn in dNums)
                {
                    if(dn.Success)
                    {
                        int defectId = Convert.ToInt32(dn.Groups[0].Value);
                        if (!defectIds.Contains(defectId))
                            defectIds.Add(defectId);
                    }
                }
            }

            return defectIds;
        }

        private List<int> GetUserstories(string text)
        {
            List<int> usIds = new List<int>();
            string usRegex = @"Feature/User Story:.*[\d]{3,5}\s|Feature/User Story:.*\s[\d]{3,5}";
            string numRegex = @"[\d]{3,5}";

            Match usm = Regex.Match(text, usRegex, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            if (usm.Success)
            {
                string usstr = usm.Groups[0].Value;
                MatchCollection usNums = Regex.Matches(usstr, numRegex);
                foreach (Match usn in usNums)
                {
                    if (usn.Success)
                    {
                        int usId = Convert.ToInt32(usn.Groups[0].Value);
                        if (!usIds.Contains(usId))
                            usIds.Add(usId);
                    }
                }
            }

            return usIds;
        }
    }
}
