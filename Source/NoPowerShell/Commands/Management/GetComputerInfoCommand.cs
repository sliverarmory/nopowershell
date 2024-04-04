﻿using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.Management
{
    public class GetComputerInfo : PSCommand
    {
        public GetComputerInfo(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            // Collect parameters for remote execution
            base.Execute();

            bool simple = _arguments.Get<BoolArgument>("Simple").Value;

            ResultRecord wmiOS = WmiHelper.ExecuteWmiQuery("Select * From Win32_OperatingSystem", computername, username, password)[0];
            ResultRecord wmiCS = WmiHelper.ExecuteWmiQuery("Select * From Win32_ComputerSystem", computername, username, password)[0];

            // OS Version
            string strOsVersion = string.Format("{0} Build {1}",
                wmiOS["Version"],
                /* wmiInfo["CSDVersion"],*/ // TODO
                wmiOS["BuildNumber"]);

            // Original Install Date
            string sOrigInstallDate = null;
            Match dateMatch = MatchDate(wmiOS, "InstallDate");
            if(dateMatch != null)
            {
                    sOrigInstallDate = string.Format("{0}-{1}-{2}, {3}:{4}:{5}",
                    dateMatch.Groups[3], dateMatch.Groups[2], dateMatch.Groups[1],
                    dateMatch.Groups[4], dateMatch.Groups[5], dateMatch.Groups[6]);
            }

            // System Boot Time
            string sSystemBootTime = null;
            dateMatch = MatchDate(wmiOS, "LastBootUpTime");
            if(dateMatch != null)
            {
                sSystemBootTime = string.Format("{0}-{1}-{2}, {3}:{4}:{5}",
                dateMatch.Groups[3], dateMatch.Groups[2], dateMatch.Groups[1],
                dateMatch.Groups[4], dateMatch.Groups[5], dateMatch.Groups[6]);
            } 

            // Processors
            CommandResult wmiCPUs = WmiHelper.ExecuteWmiQuery("Select * From Win32_Processor", computername, username, password);
            List<string> cpus = new List<string>(wmiCPUs.Count);
            foreach(ResultRecord cpu in wmiCPUs)
                cpus.Add(string.Format("{0} ~{1} Mhz", cpu["Description"], cpu["CurrentClockSpeed"]));

            // Bios
            string strBiosVersion = null;
            ResultRecord wmiBios = WmiHelper.ExecuteWmiQuery("Select * From Win32_BIOS", computername, username, password)[0];
            dateMatch = MatchDate(wmiBios, "ReleaseDate");
            if(dateMatch != null)
            {
                strBiosVersion = string.Format("{0} {1}, {2}-{3}-{4}",
                wmiBios["Manufacturer"], wmiBios["SMBIOSBIOSVersion"],
                dateMatch.Groups[3], dateMatch.Groups[2], dateMatch.Groups[1]);
            }

            // Hotfixes
            List<string> hotfixes = new List<string>() { "[unlisted]" };

            if (!simple)
            {
                CommandResult wmiHotfixes = WmiHelper.ExecuteWmiQuery("Select HotFixID From Win32_QuickFixEngineering", computername, username, password);
                hotfixes = new List<string>(wmiHotfixes.Count);
                foreach (ResultRecord hotfix in wmiHotfixes)
                    hotfixes.Add(hotfix["HotFixID"]);
            }

            // Time zone
            int timeZone = Convert.ToInt32(wmiOS["CurrentTimeZone"]) / 60;
            string sTimeZone = string.Format("UTC{0}{1}", timeZone > 0 ? "+" : "-", timeZone);

            // Pagefile
            string sPageFile = WmiHelper.ExecuteWmiQuery("Select Name From Win32_PageFileUsage", computername, username, password)[0]["Name"];

            // Summarize information
            _results.Add(
                new ResultRecord()
                {
                    { "Host Name", wmiOS["CSName"] },
                    { "OS Name", wmiOS["Caption"] },
                    { "OS Version", strOsVersion },
                    { "OS Manufacturer", wmiOS["Manufacturer"] },
                    { "OS Build Type", wmiOS["BuildType"] },
                    { "Registered Owner", wmiOS["RegisteredUser"] },
                    { "Registered Organization", wmiOS["Organization"] },
                    { "Product ID", wmiOS["SerialNumber"] },
                    { "Original Install Date", sOrigInstallDate },
                    { "System Boot Time", sSystemBootTime },
                    { "System Manufacturer", wmiCS["Manufacturer"] },
                    { "System Model", wmiCS["Model"] },
                    { "System Type", wmiCS["SystemType"] },
                    { "Processor(s)", string.Join(", ", cpus.ToArray()) },
                    { "BIOS Version", strBiosVersion },
                    { "Windows Directory", wmiOS["WindowsDirectory"] },
                    { "System Directory", wmiOS["SystemDirectory"] },
                    { "Boot Device", wmiOS["BootDevice"] },
                    { "System Locale", wmiOS["OSLanguage"] },
                    { "Input Locale", wmiOS["OSLanguage"] }, // TODO
                    { "Time Zone", sTimeZone}, // TODO
                    { "Total Physical Memory", wmiOS["TotalVisibleMemorySize"] },
                    { "Available Physical Memory", wmiOS["FreePhysicalMemory"] },
                    { "Virtual Memory: Max Size", wmiOS["TotalVirtualMemorySize"] },
                    { "Virtual Memory: Available", wmiOS["FreeVirtualMemory"] },
                    { "Virtual Memory: In Use", "[not implemented]" }, // TODO
                    { "Page File Location(s)", sPageFile },
                    { "Domain", wmiCS["Domain"] },
                    { "Logon Server", Environment.GetEnvironmentVariable("LOGONSERVER") }, // TODO: Win32_NTDomain
                    { "Hotfix(s)", string.Join(", ", hotfixes.ToArray()) },
                    { "Network Card(s)", "[not implemented]" }, // TODO
                    { "Hyper-V Requirements", "[not implemented]" } // TODO
                }
            );
            
            return _results;
        }

        private static Match MatchDate(ResultRecord result, string key)
        {
            if (!result.ContainsKey(key))
                return null;

            Regex dateRegex = new Regex("([0-9]{4})([01][0-9])([012][0-9])([0-9]{2})([0-9]{2})([0-9]{2})");
            MatchCollection allMatches = dateRegex.Matches(result[key]);
            if (allMatches.Count > 0)
                return allMatches[0];
            else
                return null;
        }

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "Get-ComputerInfo", "systeminfo" }; }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new BoolArgument("Simple")
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Shows details about the system such as hardware and Windows installation."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry
                    (
                        "Show information about the system",
                        new List<string>()
                        {
                            "Get-ComputerInfo",
                            "systeminfo"
                        }
                    ),
                    new ExampleEntry("Show information about the system not listing patches", "systeminfo -Simple"),
                    new ExampleEntry
                    (
                        "Show information about a remote machine using WMI",
                        new List<string>()
                        {
                            "Get-ComputerInfo -ComputerName MyServer -Username MyUser -Password MyPassword",
                            "Get-ComputerInfo -ComputerName MyServer"
                        }
                    ),
                };
            }
        }
    }
}
