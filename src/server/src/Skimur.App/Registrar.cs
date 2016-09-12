﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Skimur.App.Services;
using Skimur.App.Services.Impl;

namespace Skimur.App
{
    public class Registrar : IRegistrar
    {
        public int Order => 0;

        public void Register(IServiceCollection services)
        {
            services.AddSingleton<IMembershipService, MembershipService>();
            services.AddSingleton<IPasswordManager, PasswordManager>();
        }
    }
}