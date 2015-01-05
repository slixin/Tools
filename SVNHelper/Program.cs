using HPAGMRestAPIWrapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SVNHelper
{
    class Program
    {
        static Dictionary<string, string> Arguments;
        static string svnUri;
        static string svnUsername;
        static string svnPassword;
        static string svnAction;
        static int revision1;
        static int revision2;

        static string agmServer;
        static string agmUsername;
        static string agmPassword;
        static string agmDomain;
        static string agmProject;

        static void Main(string[] args)
        {
            GetArguments(args);
            svnUsername = !Arguments.ContainsKey("-svnu") ? string.Empty : Arguments["-svnu"];
            svnPassword = !Arguments.ContainsKey("-svnpw") ? string.Empty : Arguments["-svnpw"];
            svnUri = !Arguments.ContainsKey("-svnuri") ? string.Empty : Arguments["-svnuri"];
            svnAction = !Arguments.ContainsKey("-svnaction") ? string.Empty : Arguments["-svnaction"];
            revision1 = !Arguments.ContainsKey("-svnr1") ? -1 : Convert.ToInt32(Arguments["-svnr1"]);
            revision2 = !Arguments.ContainsKey("-svnr2") ? -1 : Convert.ToInt32(Arguments["-svnr2"]);

            agmServer = !Arguments.ContainsKey("-agms") ? string.Empty : Arguments["-agms"];
            agmUsername = !Arguments.ContainsKey("-agmu") ? string.Empty : Arguments["-agmu"];
            agmPassword = !Arguments.ContainsKey("-agmpw") ? string.Empty : Arguments["-agmpw"];
            agmDomain = !Arguments.ContainsKey("-agmd") ? string.Empty : Arguments["-agmd"];
            agmProject = !Arguments.ContainsKey("-agmp") ? string.Empty : Arguments["-agmp"];

            switch(svnAction.ToLower())
            {
                case "br":
                    GetInfoBetweenRevision();
                    break;
            }
        }

        static void GetInfoBetweenRevision()
        {
            SVNHelper svnHelper = new SVNHelper(svnUsername, svnPassword);
            var infos = svnHelper.GetSVNCheckInInfoByRevison(svnUri, revision1, revision2);
           
            Console.WriteLine("{0}", GetDefectsOutput(infos));
            Console.WriteLine("{0}", GetUSsOutput(infos));
        }



        static private Dictionary<int, string> GetUserStoriesFromAGM(List<int> ids)
        {
            Dictionary<int, string> uscollection = new Dictionary<int, string>();
            List<AGMField> agmFields = new List<AGMField>();
            string idstr = null;
            foreach (int id in ids.Distinct().OrderBy(o => o))
            {
                if (!string.IsNullOrEmpty(idstr))
                    idstr += " or ";

                idstr += string.Format("{0}", id);
            }
            agmFields.Add(new AGMField() { Name = "id", Value = idstr });


            using (AGMConnection conn = new AGMConnection(agmServer, agmUsername, agmPassword, agmDomain, agmProject))
            {
                AGMRequirements uss = new AGMRequirements(conn);
                try
                {
                    List<AGMRequirement> usList = uss.GetCollection(agmFields, "id", "name");
                    if (usList.Count > 0)
                    {
                        foreach (AGMRequirement us in usList)
                        {
                            uscollection.Add(us.Id.Value, us.GetField("name").Value);
                        }
                    }
                }
                catch { }

            }


            return uscollection;
        }

        static string GetUSsOutput(List<SVNCheckInInfo> infos)
        {
            StringBuilder usSB = new StringBuilder();
            List<int> USs = new List<int>();
            usSB.AppendLine("User Stories:");

            foreach (SVNCheckInInfo info in infos)
            {
                USs.AddRange(info.UserStories);
            }

            Dictionary<int, string> us = GetUserStoriesFromAGM(USs);

            foreach (KeyValuePair<int, string> kv in us)
            {
                usSB.AppendLine(string.Format("{0} - {1}", kv.Key, kv.Value));
            }

            return usSB.ToString();
        }

        static private Dictionary<int, string> GetDefectsFromAGM(List<int> ids)
        {
            Dictionary<int, string> defcollection = new Dictionary<int, string>();
            List<AGMField> agmFields = new List<AGMField>();
            string idstr = null;
            foreach (int id in ids.Distinct().OrderBy(o => o))
            {
                if (!string.IsNullOrEmpty(idstr))
                    idstr += " or ";

                idstr += string.Format("{0}", id);
            }
            agmFields.Add(new AGMField() { Name = "id", Value = idstr });

            using (AGMConnection conn = new AGMConnection(agmServer, agmUsername, agmPassword, agmDomain, agmProject))
            {
                AGMDefects defects = new AGMDefects(conn);
                try
                {
                    List<AGMDefect> defs = defects.GetCollection(agmFields, "id", "name");
                    if (defs.Count > 0)
                    {
                        foreach (AGMDefect d in defs)
                        {
                            defcollection.Add(d.Id.Value, d.GetField("name").Value);
                        }
                    }
                }
                catch { }

            }


            return defcollection;
        }

        static string GetDefectsOutput(List<SVNCheckInInfo> infos)
        {
            StringBuilder defectSB = new StringBuilder();
            List<int> defects = new List<int>();
            defectSB.AppendLine("Defects:");            

            foreach(SVNCheckInInfo info in infos)
            {
                defects.AddRange(info.Defects);
            }

            Dictionary<int, string> def = GetDefectsFromAGM(defects);

            foreach(KeyValuePair<int, string> kv in def)
            {
                defectSB.AppendLine(string.Format("{0} - {1}", kv.Key, kv.Value));
            }

            return defectSB.ToString();
        }

        static string GetDetails(List<SVNCheckInInfo> infos)
        {
            StringBuilder commentSB = new StringBuilder();
            commentSB.AppendLine("Details:");
            foreach (SVNCheckInInfo info in infos)
            {
                commentSB.AppendLine("=========================");
                commentSB.AppendLine(string.Format("DEV: {0}", info.Author));
                commentSB.AppendLine(string.Format("{0}", info.LogText));
                commentSB.AppendLine("=========================");
            }

            return commentSB.ToString();
        }

        static void GetArguments(string[] args)
        {
            Arguments = new Dictionary<string, string>();
            foreach (string s in args)
            {
                Arguments.Add(s.Split(new char[] { '=' })[0], s.Split(new char[] { '=' })[1]);
            }
        }

        static void Warning(string msg, int exitcode)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("===============================================================================");
            sb.AppendLine("Welcome to use SVNHelper, this tool is to assist Get information from SVN & AGM.");
            sb.AppendLine("Format: SVNHelper.exe -svnu=<username> -svnpw=<password> -svnuri=<uri> -svnaction=<svn action> [-svnr1=<revision 1>] [-svnr2=<revision 2>] -agms=<agm server> -agmu=<agm username> -agmpw=<agm password> -agmd=<agm domain> -agmp=<agm project>");
            sb.AppendLine("SVN Action:");
            sb.AppendLine("     br - Between Revision, get check in information between 2 revision.");
            sb.AppendLine("Example: SVNHelper.exe -svnu=xin.li21@hp.com -svnpw=password -svnuri=https://svn.isr.hp.com/rg0202/tsg-bto-apps-tcs/trunk/app -agms=https://agilemanager-int.saas.hp.com -agmu=xin.li21@hp.com -agmpw=password -agmd=t758142732_hp_com -agmp=TruClient");
            sb.AppendLine("===============================================================================");
            sb.AppendLine();
            if (!string.IsNullOrEmpty(msg))
            {
                sb.AppendLine("Error:");
                sb.AppendLine(msg);
            }
            Console.WriteLine(sb.ToString());
            Environment.Exit(exitcode);
        }
    }
}
