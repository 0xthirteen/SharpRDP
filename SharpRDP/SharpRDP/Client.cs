using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using AxMSTSCLib;
using MSTSCLib;

namespace SharpRDP
{
    public class Client
    {
        private Dictionary<string, Code> keycode;
        private IMsRdpClientNonScriptable keydata;
        private int LogonErrorCode { get; set; }
        private int DisconnectCode { get; set; }
        private string cmd;
        private string execwith;
        private string target;
        private string runtype;
        private bool isdrive;
        private bool takeover;
        private bool networkauth;
        private bool useclipboard;
        private int delaymult;
        private int connectTimeout;
        private int rdpport;
        private string gateway;
        private string outputfile;
        private Form mainForm;
        private bool connected;

        private enum LogonErrors : uint
        {
            ARBITRATION_CODE_BUMP_OPTIONS = 0xFFFFFFFB,
            ARBITRATION_CODE_CONTINUE_LOGON = 0xFFFFFFFE,
            ARBITRATION_CODE_CONTINUE_TERMINATE = 0xFFFFFFFD,
            ARBITRATION_CODE_NOPERM_DIALOG = 0xFFFFFFFA,
            ARBITRATION_CODE_REFUSED_DIALOG = 0xFFFFFFF9,
            ARBITRATION_CODE_RECONN_OPTIONS = 0xFFFFFFFC,
            ERROR_CODE_ACCESS_DENIED = 0xFFFFFFFF,
            LOGON_FAILED_BAD_PASSWORD = 0x0,
            LOGON_FAILED_OTHER = 0x2,
            LOGON_FAILED_UPDATE_PASSWORD = 0x1,
            LOGON_WARNING = 0x3,
            STATUS_ACCOUNT_RESTRICTION = 0xC000006E,
            STATUS_LOGON_FAILURE = 0xC000006D,
            STATUS_PASSWORD_MUST_CHANGE = 0xC0000224
        }
        private enum DisconnectReasons : uint
        {
            disconnectReasonAtClientWinsockFDCLOSE = 0x904,
            disconnectReasonByServer = 0x3,
            disconnectReasonClientDecompressionError = 0xC08,
            disconnectReasonConnectionTimedOut = 0x108,
            disconnectReasonDecryptionError = 0xC06,
            disconnectReasonDNSLookupFailed = 0x104,
            disconnectReasonDNSLookupFailed2 = 0x508,
            disconnectReasonEncryptionError = 0xB06,
            disconnectReasonGetHostByNameFailed = 0x604,
            disconnectReasonHostNotFound = 0x208,
            disconnectReasonInternalError = 0x408,
            disconnectReasonInternalSecurityError = 0x906,
            disconnectReasonInternalSecurityError2 = 0xA06,
            disconnectReasonInvalidEncryption = 0x506,
            disconnectReasonInvalidIP = 0x804,
            disconnectReasonInvalidServerSecurityInfo = 0x606,
            disconnectReasonInvalidSecurityData = 0x406,
            disconnectReasonInvalidIPAddr = 0x308,
            disconnectReasonLicensingFailed = 0x808,
            disconnectReasonLicensingTimeout = 0x908,
            disconnectReasonLocalNotError = 0x1,
            disconnectReasonNoInfo = 0x0,
            disconnectReasonOutOfMemory = 0x106,
            disconnectReasonOutOfMemory2 = 0x206,
            disconnectReasonOutOfMemory3 = 0x306,
            disconnectReasonRemoteByUser = 0x2,
            disconnectReasonServerCertificateUnpackErr = 0x706,
            disconnectReasonSocketConnectFailed = 0x204,
            disconnectReasonSocketRecvFailed = 0x404,
            disconnectReasonTimeoutOccurred = 0x704,
            disconnectReasonTimerError = 0x608,
            disconnectReasonWinsockSendFailed = 0x304,
            SSL_ERR_ACCOUNT_DISABLED = 0xB07,
            SSL_ERR_ACCOUNT_EXPIRED = 0xE07,
            SSL_ERR_ACCOUNT_LOCKED_OUT = 0xD07,
            SSL_ERR_ACCOUNT_RESTRICTION = 0xC07,
            SSL_ERR_CERT_EXPIRED = 0x1B07,
            SSL_ERR_DELEGATION_POLICY = 0x1607,
            SSL_ERR_FRESH_CRED_REQUIRED_BY_SERVER = 0x2107,
            SSL_ERR_LOGON_FAILURE = 0x807,
            SSL_ERR_NO_AUTHENTICATING_AUTHORITY = 0x1807,
            SSL_ERR_NO_SUCH_USER = 0xA07,
            SSL_ERR_PASSWORD_EXPIRED = 0xF07,
            SSL_ERR_PASSWORD_MUST_CHANGE = 0x1207,
            SSL_ERR_POLICY_NTLM_ONLY = 0x1707,
            SSL_ERR_SMARTCARD_CARD_BLOCKED = 0x2207,
            SSL_ERR_SMARTCARD_WRONG_PIN = 0x1C07
        }

        private void Delay(int baseMs)
        {
            Thread.Sleep(baseMs * delaymult);
        }

        public void CreateRdpConnection(string server, string user, string domain, string password,
            string command, string execw, string runelevated, bool condrive, bool tover, bool nla,
            bool clipboard = false, int delayMultiplier = 1, int timeout = 0, int port = 3389,
            string gw = "", string outfile = "")
        {
            keycode = new Dictionary<string, Code>();
            KeyCodes();
            runtype = runelevated;
            isdrive = condrive;
            cmd = command;
            target = server;
            execwith = execw;
            takeover = tover;
            networkauth = nla;
            useclipboard = clipboard;
            delaymult = delayMultiplier < 1 ? 1 : delayMultiplier;
            connectTimeout = timeout > 0 ? timeout * 1000 : 0;
            rdpport = port;
            gateway = gw;
            outputfile = outfile;
            connected = false;

            void ProcessTaskThread()
            {
                mainForm = new Form();
                mainForm.Opacity = 0;
                mainForm.Visible = false;
                mainForm.WindowState = FormWindowState.Minimized;
                mainForm.ShowInTaskbar = false;
                mainForm.FormBorderStyle = FormBorderStyle.None;
                mainForm.Width = Screen.PrimaryScreen.WorkingArea.Width;
                mainForm.Height = Screen.PrimaryScreen.WorkingArea.Height;
                mainForm.Load += (sender, args) =>
                {
                    var rdpConnection = new AxMsRdpClient9NotSafeForScripting();
                    mainForm.Controls.Add(rdpConnection);
                    var ocx = rdpConnection.GetOcx();
                    var rdpC = ocx as IMsRdpClientNonScriptable5;
                    var rdpc2 = ocx as IMsRdpExtendedSettings;
                    rdpC.AllowPromptingForCredentials = false;
                    rdpC.AllowCredentialSaving = false;
                    rdpConnection.Server = server;
                    rdpConnection.Domain = domain;
                    rdpConnection.UserName = user;
                    rdpConnection.AdvancedSettings9.allowBackgroundInput = 1;
                    rdpConnection.AdvancedSettings9.BitmapPersistence = 0;
                    if (rdpport != 3389)
                    {
                        rdpConnection.AdvancedSettings9.RDPPort = rdpport;
                    }
                    if (condrive)
                    {
                        rdpConnection.AdvancedSettings5.RedirectDrives = true;
                    }
                    if (password != string.Empty || user != string.Empty)
                    {
                        rdpConnection.UserName = user;
                        rdpConnection.AdvancedSettings9.ClearTextPassword = password;
                    }
                    else
                    {
                        rdpc2.set_Property("RestrictedLogon", true);
                        rdpc2.set_Property("DisableCredentialsDelegation", true);
                    }
                    rdpConnection.AdvancedSettings9.EnableCredSspSupport = true;
                    if (networkauth)
                    {
                        rdpC.NegotiateSecurityLayer = true;
                    }
                    // RDP Gateway
                    if (!string.IsNullOrEmpty(gateway))
                    {
                        rdpConnection.TransportSettings2.GatewayHostname = gateway;
                        rdpConnection.TransportSettings2.GatewayUsageMethod = 1; // always use gateway
                        rdpConnection.TransportSettings2.GatewayProfileUsageMethod = 1;
                        rdpConnection.TransportSettings2.GatewayCredsSource = 0; // user password
                        Console.WriteLine("[+] Using RDP Gateway     :  {0}", gateway);
                    }
                    rdpConnection.OnDisconnected += RdpConnectionOnOnDisconnected;
                    rdpConnection.OnLoginComplete += RdpConnectionOnOnLoginComplete;
                    rdpConnection.OnLogonError += RdpConnectionOnOnLogonError;
                    rdpConnection.Connect();
                    rdpConnection.Enabled = false;
                    rdpConnection.Dock = DockStyle.Fill;
                    Application.Run(mainForm);
                };
                mainForm.Show();
            }

            var rdpClientThread = new Thread(ProcessTaskThread) { IsBackground = true };
            rdpClientThread.SetApartmentState(ApartmentState.STA);
            rdpClientThread.Start();

            if (connectTimeout > 0)
            {
                if (!rdpClientThread.Join(connectTimeout))
                {
                    if (!connected)
                    {
                        Console.WriteLine("[X] Connection timed out after {0} seconds", timeout);
                        CloseForm();
                        rdpClientThread.Join(5000);
                    }
                    else
                    {
                        rdpClientThread.Join();
                    }
                }
            }
            else
            {
                rdpClientThread.Join();
            }
        }

        private void CloseForm()
        {
            if (mainForm != null && mainForm.IsHandleCreated)
            {
                mainForm.BeginInvoke((MethodInvoker)delegate { mainForm.Close(); });
            }
        }

        private void RdpConnectionOnOnLogonError(object sender, IMsTscAxEvents_OnLogonErrorEvent e)
        {
            LogonErrorCode = e.lError;
            var errorstatus = Enum.GetName(typeof(LogonErrors), (uint)LogonErrorCode);
            Console.WriteLine("[-] Logon Error           :  {0} - {1}", LogonErrorCode, errorstatus ?? "Unknown");
            Delay(1000);

            if (LogonErrorCode == -5 && takeover)
            {
                var rdpSession = (AxMsRdpClient9NotSafeForScripting)sender;
                Delay(1000);
                keydata = (IMsRdpClientNonScriptable)rdpSession.GetOcx();
                Console.WriteLine("[+] Another user is logged on, asking to take over session");
                SendElement("Tab");
                Delay(500);
                SendElement("Enter+down");
                Delay(500);
                SendElement("Enter+up");
                Delay(500);
                Console.WriteLine("[+] Waiting for session takeover (30 seconds)...");
                Thread.Sleep(31000);

                Console.WriteLine("[+] Session takeover complete, executing command");
                Delay(2000);
                connected = true;
                ExecuteCommand(rdpSession);
            }
            else if (LogonErrorCode != -2)
            {
                Console.WriteLine("[-] Login failed, exiting");
                CloseForm();
            }
        }

        private void RdpConnectionOnOnLoginComplete(object sender, EventArgs e)
        {
            var rdpSession = (AxMsRdpClient9NotSafeForScripting)sender;
            connected = true;
            Console.WriteLine("[+] Connected to          :  {0}", target);
            Delay(1000);
            keydata = (IMsRdpClientNonScriptable)rdpSession.GetOcx();

            if (LogonErrorCode == -2)
            {
                Console.WriteLine("[+] User not currently logged in, creating new session");
                Thread.Sleep(10000);
            }

            ExecuteCommand(rdpSession);
        }

        private void ExecuteCommand(AxMsRdpClient9NotSafeForScripting rdpSession)
        {
            string privinfo = "non-elevated";
            if (runtype != string.Empty)
            {
                privinfo = "elevated";
            }
            Console.WriteLine("[+] Execution priv type   :  {0}", privinfo);
            Delay(1000);

            // Build the actual command — wrap with output redirect if outputfile specified
            string execCmd = cmd;
            if (!string.IsNullOrEmpty(outputfile))
            {
                // Redirect output through tsclient drive share
                execCmd = string.Format("cmd.exe /c \"{0}\" > {1} 2>&1", cmd, outputfile);
                Console.WriteLine("[+] Output redirected to  :  {0}", outputfile);
            }

            SendElement("Win+R+down");
            Delay(500);
            SendElement("Win+R+up");
            Delay(1000);

            if (execwith == "cmd")
            {
                RunConsole("cmd.exe", execCmd);
            }
            else if (execwith == "powershell" || execwith == "ps")
            {
                RunConsole("powershell.exe", execCmd);
            }
            else
            {
                RunRun(execCmd);
            }

            // If output file specified and drive is connected, try to read it back
            if (!string.IsNullOrEmpty(outputfile) && isdrive)
            {
                Delay(2000);
                try
                {
                    // outputfile is a remote path like \\tsclient\C\temp\out.txt
                    // or a local path on the remote machine — we can only read tsclient paths locally
                    if (outputfile.StartsWith("\\\\tsclient\\", StringComparison.OrdinalIgnoreCase))
                    {
                        // Convert \\tsclient\C\path to local C:\path
                        string localPath = outputfile.Substring(11); // strip \\tsclient\
                        if (localPath.Length > 1 && localPath[1] == '\\')
                        {
                            localPath = localPath[0] + ":" + localPath.Substring(1);
                        }
                        if (File.Exists(localPath))
                        {
                            string output = File.ReadAllText(localPath);
                            Console.WriteLine("[+] Command output:");
                            Console.WriteLine(output);
                            File.Delete(localPath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[!] Could not read output: {0}", ex.Message);
                }
            }

            Delay(1000);
            Console.WriteLine("[+] Disconnecting from    :  {0}", target);
            rdpSession.Disconnect();
        }

        private void RdpConnectionOnOnDisconnected(object sender, IMsTscAxEvents_OnDisconnectedEvent e)
        {
            DisconnectCode = e.discReason;
            var dire = Enum.GetName(typeof(DisconnectReasons), (uint)DisconnectCode);
            Console.WriteLine("[+] Connection closed     :  {0}", target);
            if (e.discReason != (int)DisconnectReasons.disconnectReasonLocalNotError)
            {
                Console.WriteLine("[-] Disconnection Reason  :  {0} - {1}", DisconnectCode, dire ?? "Unknown");
            }
            CloseForm();
        }

        private void RunRun(string execCmd)
        {
            if (runtype == "taskmgr")
            {
                Console.WriteLine("[+] Running task manager");
                Delay(500);
                SendText("taskmgr");
                Delay(1000);

                Delay(500);
                SendElement("Enter+down");
                Delay(500);
                SendElement("Enter+up");

                SendElement("Alt+F");
                Delay(1000);

                SendElement("Enter+down");
                Delay(500);
                SendElement("Enter+up");
                Delay(500);
            }

            Console.WriteLine("[+] Executing {0}", execCmd);
            SendClipboard(execCmd);
            Delay(1000);

            if (runtype == "taskmgr")
            {
                SendElement("Tab");
                Delay(500);
                SendElement("Space");
                Delay(500);
            }

            if (runtype == "winr")
            {
                SendElement("Ctrl+Shift+Enter+down");
                Delay(500);
                SendElement("Ctrl+Shift+Enter+up");
                Delay(500);
            }
            else
            {
                SendElement("Enter+down");
                Delay(500);
                SendElement("Enter+up");
                Delay(250);
            }

            if (isdrive)
            {
                SendElement("Left");
                Delay(500);
                SendElement("Enter+down");
                Delay(500);
                SendElement("Enter+up");
            }

            if (runtype == "winr")
            {
                Delay(1000);
                SendElement("Left");
                Delay(500);
                SendElement("Enter+down");
                Delay(500);
                SendElement("Enter+up");
            }
            if (runtype == "taskmgr")
            {
                Delay(250);
                SendElement("Alt+F4");
            }
        }

        private void RunConsole(string consoletype, string execCmd)
        {
            if (runtype == "taskmgr")
            {
                Console.WriteLine("[+] Executing task manager");
                Delay(500);
                SendText("taskmgr");
                Delay(1000);

                Delay(500);
                SendElement("Enter+down");
                Delay(500);
                SendElement("Enter+up");

                SendElement("Alt+F");
                Delay(1000);

                SendElement("Enter+down");
                Delay(500);
                SendElement("Enter+up");
                Delay(500);
            }

            Console.WriteLine("[+] Executing {0} from {1}", execCmd, consoletype);
            SendText(consoletype);
            Delay(1000);

            if (runtype == "taskmgr")
            {
                SendElement("Tab");
                Delay(500);
                SendElement("Space");
                Delay(250);
            }

            if (runtype == "winr")
            {
                SendElement("Ctrl+Shift+Enter+down");
                Delay(500);
                SendElement("Ctrl+Shift+Enter+up");
                Delay(500);
            }
            else
            {
                SendElement("Enter+down");
                Delay(500);
                SendElement("Enter+up");
                Delay(250);
            }

            Delay(500);
            SendClipboard(execCmd);

            Delay(1000);

            SendElement("Enter+down");
            Delay(500);
            SendElement("Enter+up");

            Delay(500);
            SendText("exit");

            SendElement("Enter+down");
            Delay(500);
            SendElement("Enter+up");

            if (runtype == "taskmgr")
            {
                Delay(250);
                SendElement("Alt+F4");
                Delay(250);
            }
        }

        public void SendClipboard(string text)
        {
            if (!useclipboard)
            {
                SendText(text);
                return;
            }
            try
            {
                string savedClip = null;
                try { if (Clipboard.ContainsText()) savedClip = Clipboard.GetText(); } catch { }

                Clipboard.SetText(text);
                Delay(300);
                SendElement("Ctrl+V");
                Delay(300);

                // Restore previous clipboard
                try
                {
                    if (savedClip != null)
                        Clipboard.SetText(savedClip);
                    else
                        Clipboard.Clear();
                }
                catch { }
            }
            catch
            {
                Console.WriteLine("[!] Clipboard paste failed, falling back to keystrokes");
                SendText(text);
            }
        }

        public void SendText(string text)
        {
            foreach (var t in text)
            {
                var symbol = t.ToString();
                if (keycode.TryGetValue(symbol, out Code code))
                {
                    keydata.SendKeys(code.length, ref code.bools[0], ref code.ints[0]);
                }
                else
                {
                    Console.WriteLine("[!] Unmapped character: '{0}' (0x{1:X2}) - skipped", symbol, (int)t);
                }
                Thread.Sleep(10 * delaymult);
            }
        }

        private void SendElement(string curchars)
        {
            var current = keycode[curchars];
            keydata.SendKeys(current.length, ref current.bools[0], ref current.ints[0]);
            Thread.Sleep(10 * delaymult);
        }

        private void KeyCodes()
        {
            keycode["Esc"] = new Code(new[] { false, true }, new[] { 0x01 });
            keycode["Enter+down"] = new Code(new[] { false }, new[] { 0x1c });
            keycode["Enter+up"] = new Code(new[] { true }, new[] { 0x1c });
            keycode["Win"] = new Code(new[] { false, true }, new[] { 0x15b });
            keycode["Down"] = new Code(new[] { false, true }, new[] { 0x150 });
            keycode["Right"] = new Code(new[] { false, true }, new[] { 0x14d });
            keycode["Left"] = new Code(new[] { false, true }, new[] { 0x14b });
            keycode["Alt"] = new Code(new[] { false, true }, new[] { 0x38 });
            keycode["Shift"] = new Code(new[] { false, true }, new[] { 0x2a });
            keycode["Space"] = new Code(new[] { false, true }, new[] { 0x39 });
            keycode["Tab"] = new Code(new[] { false, true }, new[] { 0x0f });

            keycode["Calc"] = new Code(new[] { false, true }, new[] { 0x121, 0x121 });
            keycode["Paste"] = new Code(new[] { false, true }, new[] { 0x10a, 0x10a });

            // Numbers
            keycode["1"] = new Code(new[] { false, true }, new[] { 0x02 });
            keycode["2"] = new Code(new[] { false, true }, new[] { 0x03 });
            keycode["3"] = new Code(new[] { false, true }, new[] { 0x04 });
            keycode["4"] = new Code(new[] { false, true }, new[] { 0x05 });
            keycode["5"] = new Code(new[] { false, true }, new[] { 0x06 });
            keycode["6"] = new Code(new[] { false, true }, new[] { 0x07 });
            keycode["7"] = new Code(new[] { false, true }, new[] { 0x08 });
            keycode["8"] = new Code(new[] { false, true }, new[] { 0x09 });
            keycode["9"] = new Code(new[] { false, true }, new[] { 0x0a });
            keycode["0"] = new Code(new[] { false, true }, new[] { 0x0b });

            // Lowercase letters
            keycode["a"] = new Code(new[] { false, true }, new[] { 0x1e });
            keycode["b"] = new Code(new[] { false, true }, new[] { 0x30 });
            keycode["c"] = new Code(new[] { false, true }, new[] { 0x2e });
            keycode["d"] = new Code(new[] { false, true }, new[] { 0x20 });
            keycode["e"] = new Code(new[] { false, true }, new[] { 0x12 });
            keycode["f"] = new Code(new[] { false, true }, new[] { 0x21 });
            keycode["g"] = new Code(new[] { false, true }, new[] { 0x22 });
            keycode["h"] = new Code(new[] { false, true }, new[] { 0x23 });
            keycode["i"] = new Code(new[] { false, true }, new[] { 0x17 });
            keycode["j"] = new Code(new[] { false, true }, new[] { 0x24 });
            keycode["k"] = new Code(new[] { false, true }, new[] { 0x25 });
            keycode["l"] = new Code(new[] { false, true }, new[] { 0x26 });
            keycode["m"] = new Code(new[] { false, true }, new[] { 0x32 });
            keycode["n"] = new Code(new[] { false, true }, new[] { 0x31 });
            keycode["o"] = new Code(new[] { false, true }, new[] { 0x18 });
            keycode["p"] = new Code(new[] { false, true }, new[] { 0x19 });
            keycode["q"] = new Code(new[] { false, true }, new[] { 0x10 });
            keycode["r"] = new Code(new[] { false, true }, new[] { 0x13 });
            keycode["s"] = new Code(new[] { false, true }, new[] { 0x1f });
            keycode["t"] = new Code(new[] { false, true }, new[] { 0x14 });
            keycode["u"] = new Code(new[] { false, true }, new[] { 0x16 });
            keycode["v"] = new Code(new[] { false, true }, new[] { 0x2f });
            keycode["w"] = new Code(new[] { false, true }, new[] { 0x11 });
            keycode["x"] = new Code(new[] { false, true }, new[] { 0x2d });
            keycode["y"] = new Code(new[] { false, true }, new[] { 0x15 });
            keycode["z"] = new Code(new[] { false, true }, new[] { 0x2c });
            keycode[" "] = new Code(new[] { false, true }, new[] { 0x39 });

            // Uppercase letters (Shift + key)
            keycode["A"] = new Code(new[] { false, false, true, true }, new[] { 0x2a, 0x1e });
            keycode["B"] = new Code(new[] { false, false, true, true }, new[] { 0x2a, 0x30 });
            keycode["C"] = new Code(new[] { false, false, true, true }, new[] { 0x2a, 0x2e });
            keycode["D"] = new Code(new[] { false, false, true, true }, new[] { 0x2a, 0x20 });
            keycode["E"] = new Code(new[] { false, false, true, true }, new[] { 0x2a, 0x12 });
            keycode["F"] = new Code(new[] { false, false, true, true }, new[] { 0x2a, 0x21 });
            keycode["G"] = new Code(new[] { false, false, true, true }, new[] { 0x2a, 0x22 });
            keycode["H"] = new Code(new[] { false, false, true, true }, new[] { 0x2a, 0x23 });
            keycode["I"] = new Code(new[] { false, false, true, true }, new[] { 0x2a, 0x17 });
            keycode["J"] = new Code(new[] { false, false, true, true }, new[] { 0x2a, 0x24 });
            keycode["K"] = new Code(new[] { false, false, true, true }, new[] { 0x2a, 0x25 });
            keycode["L"] = new Code(new[] { false, false, true, true }, new[] { 0x2a, 0x26 });
            keycode["M"] = new Code(new[] { false, false, true, true }, new[] { 0x2a, 0x32 });
            keycode["N"] = new Code(new[] { false, false, true, true }, new[] { 0x2a, 0x31 });
            keycode["O"] = new Code(new[] { false, false, true, true }, new[] { 0x2a, 0x18 });
            keycode["P"] = new Code(new[] { false, false, true, true }, new[] { 0x2a, 0x19 });
            keycode["Q"] = new Code(new[] { false, false, true, true }, new[] { 0x2a, 0x10 });
            keycode["R"] = new Code(new[] { false, false, true, true }, new[] { 0x2a, 0x13 });
            keycode["S"] = new Code(new[] { false, false, true, true }, new[] { 0x2a, 0x1f });
            keycode["T"] = new Code(new[] { false, false, true, true }, new[] { 0x2a, 0x14 });
            keycode["U"] = new Code(new[] { false, false, true, true }, new[] { 0x2a, 0x16 });
            keycode["V"] = new Code(new[] { false, false, true, true }, new[] { 0x2a, 0x2f });
            keycode["W"] = new Code(new[] { false, false, true, true }, new[] { 0x2a, 0x11 });
            keycode["X"] = new Code(new[] { false, false, true, true }, new[] { 0x2a, 0x2d });
            keycode["Y"] = new Code(new[] { false, false, true, true }, new[] { 0x2a, 0x15 });
            keycode["Z"] = new Code(new[] { false, false, true, true }, new[] { 0x2a, 0x2c });

            // Unshifted special characters
            keycode["-"] = new Code(new[] { false, true }, new[] { 0x0c });
            keycode["="] = new Code(new[] { false, true }, new[] { 0x0d });
            keycode[","] = new Code(new[] { false, true }, new[] { 0x33 });
            keycode["."] = new Code(new[] { false, true }, new[] { 0x34 });
            keycode["/"] = new Code(new[] { false, true }, new[] { 0x35 });
            keycode["["] = new Code(new[] { false, true }, new[] { 0x1a });
            keycode["]"] = new Code(new[] { false, true }, new[] { 0x1b });
            keycode["\\"] = new Code(new[] { false, true }, new[] { 0x2b });
            keycode[";"] = new Code(new[] { false, true }, new[] { 0x27 });
            keycode["'"] = new Code(new[] { false, true }, new[] { 0x28 });
            keycode["`"] = new Code(new[] { false, true }, new[] { 0x29 });

            // Shifted special characters
            keycode["\""] = new Code(new[] { false, false, true, true }, new[] { 0x2a, 0x28 });
            keycode[":"] = new Code(new[] { false, false, true, true }, new[] { 0x2a, 0x27 });
            keycode["|"] = new Code(new[] { false, false, true, true }, new[] { 0x2a, 0x2b });
            keycode["&"] = new Code(new[] { false, false, true, true }, new[] { 0x2a, 0x08 });
            keycode["%"] = new Code(new[] { false, false, true, true }, new[] { 0x2a, 0x06 });
            keycode["("] = new Code(new[] { false, false, true, true }, new[] { 0x2a, 0x0a });
            keycode[")"] = new Code(new[] { false, false, true, true }, new[] { 0x2a, 0x0b });
            keycode["!"] = new Code(new[] { false, false, true, true }, new[] { 0x2a, 0x02 });
            keycode["@"] = new Code(new[] { false, false, true, true }, new[] { 0x2a, 0x03 });
            keycode["#"] = new Code(new[] { false, false, true, true }, new[] { 0x2a, 0x04 });
            keycode["$"] = new Code(new[] { false, false, true, true }, new[] { 0x2a, 0x05 });
            keycode["^"] = new Code(new[] { false, false, true, true }, new[] { 0x2a, 0x07 });
            keycode["*"] = new Code(new[] { false, false, true, true }, new[] { 0x2a, 0x09 });
            keycode["_"] = new Code(new[] { false, false, true, true }, new[] { 0x2a, 0x0c });
            keycode["+"] = new Code(new[] { false, false, true, true }, new[] { 0x2a, 0x0d });
            keycode["~"] = new Code(new[] { false, false, true, true }, new[] { 0x2a, 0x29 });
            keycode["{"] = new Code(new[] { false, false, true, true }, new[] { 0x2a, 0x1a });
            keycode["}"] = new Code(new[] { false, false, true, true }, new[] { 0x2a, 0x1b });
            keycode["<"] = new Code(new[] { false, false, true, true }, new[] { 0x2a, 0x33 });
            keycode[">"] = new Code(new[] { false, false, true, true }, new[] { 0x2a, 0x34 });
            keycode["?"] = new Code(new[] { false, false, true, true }, new[] { 0x2a, 0x35 });

            // Key combinations
            keycode["Win+R+down"] = new Code(new[] { false, false }, new[] { 0x15b, 0x13 });
            keycode["Win+R+up"] = new Code(new[] { true, true }, new[] { 0x15b, 0x13 });
            keycode["Win+D"] = new Code(new[] { false, false, true, true }, new[] { 0x15b, 0x20 });
            keycode["Alt+Shift"] = new Code(new[] { false, false, true, true }, new[] { 0x38, 0x2a });
            keycode["Alt+Space"] = new Code(new[] { false, false, true, true }, new[] { 0x38, 0x39 });
            keycode["Ctrl+Shift"] = new Code(new[] { false, false, true, true }, new[] { 0x1d, 0x2a });
            keycode["Alt+F4"] = new Code(new[] { false, false, true, true }, new[] { 0x38, 0x3e });
            keycode["Ctrl+V"] = new Code(new[] { false, false, true, true }, new[] { 0x1d, 0x2f });
            keycode["Alt+F"] = new Code(new[] { false, false, true, true }, new[] { 0x38, 0x21 });

            // Ctrl+Shift+Enter for elevated execution
            keycode["Ctrl+Shift+Enter+down"] = new Code(new[] { false, false, false }, new[] { 0x1d, 0x2a, 0x1c });
            keycode["Ctrl+Shift+Enter+up"] = new Code(new[] { true, true, true }, new[] { 0x1d, 0x2a, 0x1c });
        }
    }
}
