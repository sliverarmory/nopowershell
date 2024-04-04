﻿using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System.Collections.Generic;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.NetTCPIP
{
    public class GetNetRouteCommand : PSCommand
    {
        public GetNetRouteCommand(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            // Collect the (optional) ComputerName, Username and Password parameters
            base.Execute();

            _results = WmiHelper.ExecuteWmiQuery("Select Caption, Description, Destination, Mask, NextHop From Win32_IP4RouteTable", computername, username, password);

            return _results;
        }

        public static new CaseInsensitiveList Aliases
        {
            get {
                return new CaseInsensitiveList()
                {
                    "Get-NetRoute",
                    "route" // Not official
                };
            }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Gets the IP route information from the IP routing table."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry
                    (
                        "Show the IP routing table",
                        new List<string>()
                        {
                            "Get-NetRoute",
                            "route"
                        }
                    ),
                    new ExampleEntry
                    (
                        "Show the IP routing table on a remote machine using WMI",
                        new List<string>()
                        {
                            "Get-NetRoute -ComputerName MyServer -Username MyUser -Password MyPassword",
                            "route -ComputerName MyServer"
                        }
                    )
                };
            }
        }
    }
}
