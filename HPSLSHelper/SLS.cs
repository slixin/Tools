using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HPSLSHelper
{
    public class SLS
    {
        private string CMDURL_CREATESNAPSHOT = @"/cgi-bin/snap_create.pl";
        private string CMDURL_REVERTSNAPSHOT = @"/cgi-bin/cmd_vm2.pl?cmd=revertSnapshot";
        private string CMDURL_DELETESNAPSHOT = @"/cgi-bin/cmd_vm2.pl?cmd=deleteSnapshot";
        private string CMDURL_LOGIN = @"/cgi-bin/home.pl";

        private HttpHelper http;
        private string _url;
        private string _email;
        private string _password;

        #region Properties
        #endregion

        public SLS(string URL, string email, string password)
        {
            _url = URL;
            _email = email;
            _password = password;
        }

        public bool Login()
        {
            bool result = false;
            http = new HttpHelper();
            http.Method = "POST";
            http.PostData = string.Format("email={0}&password={1}&submit=Login", _email, _password);
            int respcode = http.Request(string.Format("{0}{1}",_url,CMDURL_LOGIN));
            if (respcode == 200)
            {
                if (http.Response.IndexOf("Wrong Login/Password!") < 0)
                    result = true;
            }

            return result;
        }

        public bool RevertSnapshot(string vmName, string snapshotName)
        {
            string fullURL = string.Format("{0}{1}&target={2}&snapshot={3}", _url, CMDURL_REVERTSNAPSHOT, vmName, snapshotName);

            bool result = false;
            http.Method = "GET";
            int respcode = http.Request(fullURL);
            if (respcode == 200)
                result = true;

            return result;
        }

        public bool DeleteSnapshot(string vmName, string snapshotName)
        {
            string fullURL = string.Format("{0}{1}&target={2}&snapshot={3}", _url, CMDURL_DELETESNAPSHOT, vmName, snapshotName);

            bool result = false;
            http.Method = "GET";
            int respcode = http.Request(fullURL);
            if (respcode == 200)
                result = true;

            return result;
        }

        public bool CreateSnapshot(string vmName, string snapshotName)
        {
            string postData = string.Format("target={0}&snapshot={1}&description=&create=Create", vmName, snapshotName);
            bool result = false;
            http.Method = "POST";
            http.PostData = postData;
            int respcode = http.Request(string.Format("{0}{1}", _url, CMDURL_CREATESNAPSHOT));
            if (respcode == 200)
                result = true;

            return result;
        }

        public bool Resume(string vmName)
        {
            return ExecuteGETCMD(vmName, "resume");
        }

        public bool Shutdown(string vmName)
        {
            return ExecuteGETCMD(vmName, "stop");
        }

        public bool PowerOff(string vmName)
        {
            return ExecuteGETCMD(vmName, "stophard");
        }

        public bool Reboot(string vmName)
        {
            return ExecuteGETCMD(vmName, "reset");
        }

        public bool Suspend(string vmName)
        {
            return ExecuteGETCMD(vmName, "suspend");
        }

        private bool ExecuteGETCMD(string vmName, string cmd)
        {
            string fullURL = string.Format("{0}/cgi-bin/cmd_vm2.pl?cmd={1}&target={2}", _url, cmd, vmName);

            bool result = false;
            http.Method = "GET";
            int respcode = http.Request(fullURL);
            if (respcode == 200)
                result = true;

            return result;
        }
    }
}
