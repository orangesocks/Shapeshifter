﻿using System;
using System.Linq;
using System.Reflection;
using System.Windows.Threading;
using Autofac;
using Autofac.Builder;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Helpers;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Services.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Dependencies.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Environment;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Environment.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading;
using AutofacModule = Autofac.Module;

namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Dependencies
{
    public class DefaultWiringModule : AutofacModule
    {
        private readonly Action<ContainerBuilder> callback;

        public DefaultWiringModule(Action<ContainerBuilder> callback = null)
        {
            this.callback = callback;
        }

        protected override void Load(ContainerBuilder builder)
        {
            RegisterAssemblyTypes(builder, typeof (DefaultWiringModule).Assembly);

            RegisterMainThread(builder);

            var environmentInformation = RegisterEnvironmentInformation(builder);
            if (environmentInformation.IsInDesignTime)
            {
                DesignTimeContainerHelper.RegisterFakes(builder);
            }

            callback?.Invoke(builder);

            base.Load(builder);
        }

        private static void RegisterMainThread(ContainerBuilder builder)
        {
            builder.RegisterInstance(new UserInterfaceThread(Dispatcher.CurrentDispatcher)).AsImplementedInterfaces();
        }

        private static EnvironmentInformation RegisterEnvironmentInformation(ContainerBuilder builder)
        {
            var environmentInformation = new EnvironmentInformation();
            builder
                .RegisterInstance(environmentInformation)
                .As<IEnvironmentInformation>()
                .SingleInstance();
            return environmentInformation;
        }

        private static void RegisterAssemblyTypes(ContainerBuilder builder, Assembly assembly)
        {
            var types = assembly.GetTypes();
            foreach (var type in types)
            {
                if (!type.IsClass || type.IsAbstract)
                {
                    continue;
                }

                var interfaces = type.GetInterfaces();
                if(interfaces.Contains(typeof(IDesignerService)))
                {
                    continue;
                }

                IRegistrationBuilder<object, ReflectionActivatorData, object> registration;
                if (type.IsGenericType)
                {
                    registration = builder.RegisterGeneric(type);

                    var genericInterfaces = interfaces
                        .Where(x => x.IsGenericType);
                    foreach (var genericInterface in genericInterfaces)
                    {
                        registration.As(genericInterface);
                    }
                }
                else
                {
                    var standardRegistration = builder.RegisterType(type);
                    standardRegistration.AsSelf();
                    standardRegistration.AsImplementedInterfaces();

                    registration = standardRegistration;
                }

                registration.FindConstructorsWith(new PublicConstructorFinder());
                
                if (interfaces.Contains(typeof (ISingleInstance)))
                {
                    registration.SingleInstance();
                }
            }
        }
    }
}