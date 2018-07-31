using Core;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Plugin1
{
    class DependencyInjectionModule : IDependencyRegister, IControllersContainer
    {
        public void Register(IServiceCollection services)
        {
            // Register dependecies
        }
    }
}
