﻿using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System;
using System.Collections.Generic;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.ActiveDirectory
{
    public class GetADComputerCommand : PSCommand
    {
        public GetADComputerCommand(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            // Obtain cmdlet parameters
            string server = _arguments.Get<StringArgument>("Server").Value;
            string searchBase = _arguments.Get<StringArgument>("SearchBase").Value;
            string identity = _arguments.Get<StringArgument>("Identity").Value;
            string ldapFilter = _arguments.Get<StringArgument>("LDAPFilter").Value;
            string filter = _arguments.Get<StringArgument>("Filter").Value;
            string properties = _arguments.Get<StringArgument>("Properties").Value;

            // Determine filters
            bool filledIdentity = !string.IsNullOrEmpty(identity);
            bool filledLdapFilter = !string.IsNullOrEmpty(ldapFilter);
            bool filledFilter = !string.IsNullOrEmpty(filter);

            // Input checks
            if (filledIdentity && filledLdapFilter)
                throw new NoPowerShellException("Specify either Identity or LDAPFilter, not both");
            if (!filledIdentity && !filledLdapFilter && !filledFilter)
                throw new NoPowerShellException("Specify either Identity, Filter or LDAPFilter");

            // Build filter
            string filterBase = "(&(objectCategory=computer){0})";
            string queryFilter = string.Empty;

            // -Identity DC01
            if (filledIdentity)
                queryFilter = string.Format(filterBase, string.Format("(cn={0})", identity));

            // -LDAPFilter "(msDFSR-ComputerReferenceBL=*)"
            else if (filledLdapFilter)
            {
                queryFilter = string.Format(filterBase, ldapFilter);
            }

            // -Filter *
            else if (filledFilter)
            {
                // TODO: allow more types of filters
                if (filter != "*")
                    throw new NoPowerShellException("Currently only * filter is supported");

                queryFilter = string.Format(filterBase, string.Empty);
            }

            // Query
            _results = LDAPHelper.QueryLDAP(searchBase, queryFilter, new List<string>(properties.Split(',')), server, username, password);

            return _results;
        }

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "Get-ADComputer" }; }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new StringArgument("Server", true),
                    new StringArgument("SearchBase", true),
                    new StringArgument("Identity"),
                    new StringArgument("Filter", true),
                    new StringArgument("LDAPFilter", true),
                    new StringArgument("Properties", "DistinguishedName,DNSHostName,Name,ObjectClass,ObjectGUID,SamAccountName,ObjectSID,UserPrincipalName", true)
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Gets one or more Active Directory computers."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry("List all properties of the DC01 domain computer", "Get-ADComputer -Identity DC01 -Properties *"),
                    new ExampleEntry("List all Domain Controllers", "Get-ADComputer -LDAPFilter \"(msDFSR-ComputerReferenceBL=*)\""),
                    new ExampleEntry("List all computers in domain", "Get-ADComputer -Filter *"),
                    new ExampleEntry("List domain controllers", "Get-ADComputer -searchBase \"OU=Domain Controllers,DC=bitsadmin,DC=local\" -Filter *"),
                    new ExampleEntry("List specific attributes of the DC01 domain computer", "Get-ADComputer DC01 -Properties Name,operatingSystem")
                };
            }
        }
    }
}
