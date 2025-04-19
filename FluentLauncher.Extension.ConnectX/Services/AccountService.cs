﻿using System;
using System.Collections;
using System.Linq;

namespace FluentLauncher.Extension.ConnectX.Services;

internal class AccountService
{
    public Type ReflectionType { get; init; }

    private readonly object _wrappedService; 

    public AccountService(IServiceProvider serviceProvider)
    {
        ReflectionType = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => (a.FullName?.Contains("Natsurainko.FluentLauncher")).GetValueOrDefault())
            ?.GetType("Natsurainko.FluentLauncher.Services.Accounts.AccountService")
            ?? throw new NotImplementedException();

        _wrappedService = serviceProvider.GetService(ReflectionType)
            ?? throw new NotImplementedException();
    }

    public bool HasMicrosoftAccount()
    {
        IEnumerable accounts = (ReflectionType.GetProperty("Accounts")?.GetValue(_wrappedService) as IEnumerable) 
            ?? throw new Exception();

        foreach (var account in accounts)
        {
            if (account.GetType().Name.Contains("MicrosoftAccount"))
                return true;
        }

        return false;
    }
}
