using System;
using System.Collections.Generic;
using System.Text;

namespace SMTPHelper
{
    class Program
    {
        static string smtpserver;
        static int smtpport;
        static string sender;
        static string to;
        static string cc;
        static string bcc;
        static string smtp_username;
        static string smtp_password;
        static string attachments;
        static string subject;
        static string bodyText;
        static string bodyHTMLTemplate;
        static int priority;
        static SMTPEx smtp;

        static Dictionary<string, string> Arguments;

        static void Main(string[] args)
        {

            GetArguments(args);

            if(Arguments.Count == 0)
            {
                Warning("No arguments", -1);
            }

            smtpserver = !Arguments.ContainsKey("-s") ? string.Empty : Arguments["-s"];
            smtpport = !Arguments.ContainsKey("-p") ? 25 : Convert.ToInt32(Arguments["-p"]);
            smtp_username = !Arguments.ContainsKey("-usr") ? string.Empty : Arguments["-usr"];
            smtp_password = !Arguments.ContainsKey("-pwd") ? string.Empty : Arguments["-pwd"];
            sender = !Arguments.ContainsKey("-f") ? string.Empty : Arguments["-f"];
            to = !Arguments.ContainsKey("-t") ? string.Empty : Arguments["-t"];
            cc = !Arguments.ContainsKey("-cc") ? string.Empty : Arguments["-cc"];
            bcc = !Arguments.ContainsKey("-bcc") ? string.Empty : Arguments["-bcc"];
            attachments = !Arguments.ContainsKey("-a") ? string.Empty : Arguments["-a"];
            subject = !Arguments.ContainsKey("-sub") ? string.Empty : Arguments["-sub"];
            bodyText = !Arguments.ContainsKey("-b") ? string.Empty : Arguments["-b"];
            bodyHTMLTemplate = !Arguments.ContainsKey("-template") ? string.Empty : Arguments["-template"];
            priority = !Arguments.ContainsKey("-pri") ? 0 : Convert.ToInt32(Arguments["-pri"]);


            if (string.IsNullOrEmpty(smtpserver))
            {
                Warning("SMTP Server cannot be empty", -1);
            }

            if (string.IsNullOrEmpty(sender) || string.IsNullOrEmpty(to))
            {
                Warning("Sender and To cannot be empty", -1);
            }

            if (string.IsNullOrEmpty(subject))
            {
                Warning("Subject cannot be empty", -1);
            }

            if (string.IsNullOrEmpty(bodyText) && string.IsNullOrEmpty(bodyHTMLTemplate))
            {
                Warning("Plain Body Text or Body HTML Template has to be existed at least one.", -1);
            }

            smtp = new SMTPEx();
            smtp.SMTPServer = smtpserver;
            smtp.SMTPPort = smtpport;
            smtp.SMTPUsername = smtp_username;
            smtp.SMTPPassword = smtp_password;
            smtp.Sender = sender;
            smtp.To = to;
            smtp.CcTo = cc;
            smtp.BccTo = bcc;
            smtp.Subject = subject;
            smtp.BodyText = bodyText;
            if (string.IsNullOrEmpty(bodyText))
                smtp.BodyHTML = smtp.BuildHTMLBody(bodyHTMLTemplate, Arguments);
            smtp.Priority = priority;
            smtp.Send();
        }

        static void GetArguments(string[] args)
        {
            Arguments = new Dictionary<string, string>();
            foreach(string s in args)
            {
                Arguments.Add(s.Split(new string[] { "==" }, StringSplitOptions.RemoveEmptyEntries)[0], s.Split(new string[] { "==" }, StringSplitOptions.RemoveEmptyEntries)[1]);
            }
        }

        static void Warning(string msg, int exitcode)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("===============================================================================");
            sb.AppendLine("Welcome to use SMTPHelper, this tool is to assist sending mail via SMTP server.");
            sb.AppendLine("Format: SMTPHelper.exe -s==<server> [-p==<port>] [-usr==<smtp username>] [-pwd==<smtp password>] -f==<sender> -t==<to addresses> [-cc==<cc addresses>] [-bcc==<bcc addresses>] [-a==<attachments>] -sub==<Subject> [-b==<body plain text>] [-template==<body template file>] [-pri==<Priority>] [<variable name in body template>=<body template replace variables>]");
            sb.AppendLine("Detail Information:");
            sb.AppendLine("     -to, -cc, -bcc - support multiple addresses, split by ',', for example: -to==tom@hp.com,jerry@hp.com .");
            sb.AppendLine("     -a - support multiple attachements, split by ',', for example: -a==c:\\mailattach1.zip,c:\\mailattach2.zip .");
            sb.AppendLine("     -p - optional, default is 25");
            sb.AppendLine("     -usr, -pwd, -pri - optional values");
            sb.AppendLine("     -b, -template - at least one with value. -template is the file path.");
            sb.AppendLine("     <variable>, the format for example: -Username==Steve -DropNumber==12.0.1010.0 -UpdateDate==2013-12-1.");
            sb.AppendLine("Example: SMTPHelper.exe -s==smtp.hp.com -p==25 -f==tom@hp.com -t==jerry@hp.com,jack@hp.com -cc==jason@hp.com -a==c:\result.txt -sub=='Release Notification' -template==c:\\emailTemplate.html Release=12.01");
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
