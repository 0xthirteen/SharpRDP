using System;
using System.IO.Compression;
using System.Reflection;
using System.Collections.Generic;
using System.IO;

namespace SharpRDP
{
    class Program
    {
        static void HowTo()
        {
            Console.WriteLine("SharpRDP");
            Console.WriteLine("");
            Console.WriteLine("  Regular RDP Connection");
            Console.WriteLine("    SharpRDP.exe computername=target command=\"C:\\Temp\\file.exe\" username=domain\\user password=password");
            Console.WriteLine("  Exec as child process of cmd or ps");
            Console.WriteLine("    SharpRDP.exe computername=target command=\"C:\\Temp\\file.exe\" username=domain\\user password=password exec=cmd");
            Console.WriteLine("  Use restricted admin mode");
            Console.WriteLine("    SharpRDP.exe computername=target command=\"C:\\Temp\\file.exe\"");
            Console.WriteLine("  Connect first host drives");
            Console.WriteLine("    SharpRDP.exe computername=target command=\"\\\\tsclient\\C\\Temp\\file.exe\" username=domain\\user password=password connectdrive=true");
            Console.WriteLine("  Take over existing RDP session");
            Console.WriteLine("    SharpRDP.exe computername=target command=\"C:\\Temp\\file.exe\" username=domain\\user password=password takeover=true");
            Console.WriteLine("  Network level authentication");
            Console.WriteLine("    SharpRDP.exe computername=target command=\"C:\\Temp\\file.exe\" username=domain\\user password=password nla=true");
            Console.WriteLine("  Execute command elevated through Run Dialog");
            Console.WriteLine("    SharpRDP.exe computername=target command=\"C:\\Temp\\file.exe\" username=domain\\user password=password elevated=winr");
            Console.WriteLine("  Execute command elevated through task manager");
            Console.WriteLine("    SharpRDP.exe computername=target command=\"C:\\Temp\\file.exe\" username=domain\\user password=password elevated=taskmgr");
            Console.WriteLine("  Paste command via clipboard (faster, handles all characters)");
            Console.WriteLine("    SharpRDP.exe computername=target command=\"C:\\Temp\\file.exe\" username=domain\\user password=password clipboard=true");
            Console.WriteLine("  Custom RDP port");
            Console.WriteLine("    SharpRDP.exe computername=target command=\"whoami\" username=domain\\user password=password port=3390");
            Console.WriteLine("  Delay multiplier for slow connections");
            Console.WriteLine("    SharpRDP.exe computername=target command=\"whoami\" username=domain\\user password=password delay=3");
            Console.WriteLine("  Connection timeout (seconds)");
            Console.WriteLine("    SharpRDP.exe computername=target command=\"whoami\" username=domain\\user password=password timeout=30");
            Console.WriteLine("  RDP Gateway");
            Console.WriteLine("    SharpRDP.exe computername=internal.target command=\"whoami\" username=domain\\user password=password gateway=gw.domain.com");
            Console.WriteLine("  Capture command output via drive redirect");
            Console.WriteLine("    SharpRDP.exe computername=target command=\"whoami\" username=domain\\user password=password connectdrive=true output=\\\\tsclient\\C\\temp\\out.txt");
            Console.WriteLine("  Bypass CredSSP fresh credential policy (enterprise AD environments)");
            Console.WriteLine("    SharpRDP.exe computername=target command=\"whoami\" username=domain\\user password=password legacyauth=true");
        }
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, argtwo) => {
                Assembly thisAssembly = Assembly.GetEntryAssembly();
                String resourceName = string.Format("SharpRDP.{0}.dll.bin",
                    new AssemblyName(argtwo.Name).Name);
                var assembly = Assembly.GetExecutingAssembly();
                using (var rs = assembly.GetManifestResourceStream(resourceName))
                {
                    if (rs == null) return null;
                    using (var zs = new DeflateStream(rs, CompressionMode.Decompress))
                    using (var ms = new MemoryStream())
                    {
                        zs.CopyTo(ms);
                        return Assembly.Load(ms.ToArray());
                    }
                }
            };

            var arguments = new Dictionary<string, string>();
            foreach (string argument in args)
            {
                int idx = argument.IndexOf('=');
                if (idx > 0)
                    arguments[argument.Substring(0, idx)] = argument.Substring(idx + 1);
            }

            string username = string.Empty;
            string domain = string.Empty;
            string password = string.Empty;
            string command = string.Empty;
            string execElevated = string.Empty;
            string execw = string.Empty;
            bool connectdrive = false;
            bool takeover = false;
            bool nla = false;
            bool clipboard = false;
            int delayMultiplier = 1;
            int timeout = 0;
            int port = 3389;
            string gateway = string.Empty;
            string outputfile = string.Empty;
            bool legacyauth = false;

            if (arguments.ContainsKey("username"))
            {
                if (!arguments.ContainsKey("password"))
                {
                    Console.WriteLine("[X] Error: A password is required");
                    return;
                }
                else
                {
                    if (arguments["username"].Contains("\\"))
                    {
                        string[] tmp = arguments["username"].Split('\\');
                        domain = tmp[0];
                        username = tmp[1];
                    }
                    else
                    {
                        domain = ".";
                        username = arguments["username"];
                    }
                    password = arguments["password"];
                }
            }

            if (arguments.ContainsKey("password") && !arguments.ContainsKey("username"))
            {
                Console.WriteLine("[X] Error: A username is required");
                return;
            }
            if ((arguments.ContainsKey("computername")) && (arguments.ContainsKey("command")))
            {
                command = arguments["command"];
                if (arguments.ContainsKey("exec"))
                {
                    string ex = arguments["exec"].ToLower();
                    if (ex == "cmd")
                    {
                        execw = "cmd";
                    }
                    else if (ex == "powershell" || ex == "ps")
                    {
                        execw = "powershell";
                    }
                }
                if (arguments.ContainsKey("elevated"))
                {
                    string elev = arguments["elevated"].ToLower();
                    if (elev == "true" || elev == "win+r" || elev == "winr")
                    {
                        execElevated = "winr";
                    }
                    else if (elev == "taskmgr" || elev == "taskmanager")
                    {
                        execElevated = "taskmgr";
                    }
                }
                if (arguments.ContainsKey("connectdrive"))
                {
                    if (arguments["connectdrive"].ToLower() == "true")
                    {
                        connectdrive = true;
                    }
                }
                if (arguments.ContainsKey("takeover"))
                {
                    if (arguments["takeover"].ToLower() == "true")
                    {
                        takeover = true;
                    }
                }
                if (arguments.ContainsKey("nla"))
                {
                    if (arguments["nla"].ToLower() == "true")
                    {
                        nla = true;
                    }
                }
                if (arguments.ContainsKey("clipboard"))
                {
                    if (arguments["clipboard"].ToLower() == "true")
                    {
                        clipboard = true;
                    }
                }
                if (arguments.ContainsKey("delay"))
                {
                    int.TryParse(arguments["delay"], out delayMultiplier);
                    if (delayMultiplier < 1) delayMultiplier = 1;
                }
                if (arguments.ContainsKey("timeout"))
                {
                    int.TryParse(arguments["timeout"], out timeout);
                }
                if (arguments.ContainsKey("port"))
                {
                    int.TryParse(arguments["port"], out port);
                    if (port <= 0) port = 3389;
                }
                if (arguments.ContainsKey("gateway"))
                {
                    gateway = arguments["gateway"];
                }
                if (arguments.ContainsKey("output"))
                {
                    outputfile = arguments["output"];
                }
                if (arguments.ContainsKey("legacyauth"))
                {
                    if (arguments["legacyauth"].ToLower() == "true")
                    {
                        legacyauth = true;
                    }
                }
                string[] computerNames = arguments["computername"].Split(',');
                foreach (string server in computerNames)
                {
                    string trimmed = server.Trim();
                    if (string.IsNullOrEmpty(trimmed)) continue;
                    Client rdpconn = new Client();
                    rdpconn.CreateRdpConnection(trimmed, username, domain, password, command, execw,
                        execElevated, connectdrive, takeover, nla, clipboard, delayMultiplier,
                        timeout, port, gateway, outputfile, legacyauth);
                }
            }
            else
            {
                HowTo();
                return;
            }

        }
    }
}
