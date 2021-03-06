﻿using System;
using System.Collections.Generic;
using System.Linq;
using Cosmos.Logging.Configurations;
using Cosmos.Logging.Core;
using Cosmos.Logging.Core.Components;
using Cosmos.Logging.RunsOn.NancyFX;
using Cosmos.Logging.RunsOn.NancyFX.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nancy.Bootstrapper;
using Nancy.TinyIoc;

// ReSharper disable once CheckNamespace
namespace Cosmos.Logging {
    public static class TinyIoCNancyPipelineExtensions {
        public static IPipelines RegisterCosmosLogging(this IPipelines pipelines, TinyIoCContainer container, Action<ILogServiceCollection> config) {
            if (pipelines == null) throw new ArgumentNullException(nameof(pipelines));
            if (container == null) throw new ArgumentNullException(nameof(container));

            var serviceImpl = new NancyLogServiceCollection();

            config?.Invoke(serviceImpl);

            RegisterCoreComponents(serviceImpl);

            BuildSoloContainer(serviceImpl);

            UpdateStaticProvider();

            UpdateTinyIoCContainer(container);

            NancyPipelinesHook.RegisterLoggingHandlers(pipelines);

            return pipelines;
        }

        private static void RegisterCoreComponents(NancyLogServiceCollection serviceImpl) {
            serviceImpl.AddDependency(s => s.AddSingleton<ILoggingServiceProvider, NancyLoggingServiceProvider>());
            serviceImpl.BuildConfiguration();
            serviceImpl.ActiveSinkSettings();
            serviceImpl.ActiveOriginConfiguration();
            serviceImpl.AddDependency(s => s.AddSingleton(Options.Create((LoggingOptions) serviceImpl.ExposeLogSettings())));
            serviceImpl.AddDependency(s => s.AddSingleton(serviceImpl.ExposeLoggingConfiguration()));
        }

        private static void BuildSoloContainer(ILogServiceCollection serviceImpl) {
            NancyContainerSolo.ServiceProvider = serviceImpl.ExposeServices().BuildServiceProvider();
        }

        private static void UpdateStaticProvider() {
            StaticServiceResolver.SetResolver(NancyContainerSolo.ServiceProvider.GetRequiredService<ILoggingServiceProvider>());
        }

        private static void UpdateTinyIoCContainer(TinyIoCContainer container) {
            var provider = NancyContainerSolo.ServiceProvider;
            container.Register<IServiceProvider>(provider, NancyContainerSolo.Name);
            var registrations = AllComponentsRegistrations;
            foreach (var registration in registrations) {
                var serviceType = registration.Many ? typeof(IEnumerable<>).MakeGenericType(registration.ServiceType) : registration.ServiceType;
                if (registration.Lifetime == ServiceLifetime.Singleton) {
                    if (registration.Many) {
                        container.Register(serviceType, provider.GetServices(registration.ServiceType)).AsSingleton();
                    } else {
                        container.Register(serviceType, provider.GetService(registration.ServiceType)).AsSingleton();
                    }
                } else if (registration.Lifetime == ServiceLifetime.Scoped) {
                    InternalLogger.WriteLine("PreRequestSingleton do not supported by TinyIoC in NancyFX, but the Official's one does.");
                } else if (registration.Lifetime == ServiceLifetime.Transient) {
                    if (registration.Many) {
                        container.Register(serviceType, provider.GetServices(registration.ServiceType)).AsMultiInstance();
                    } else {
                        container.Register(serviceType, provider.GetService(registration.ServiceType)).AsMultiInstance();
                    }
                }
            }
        }

        private static IEnumerable<ComponentsRegistration> AllComponentsRegistrations
            => CoreComponentsTypes.Defaults.Concat(CoreComponentsTypes.Appends)
                .Where(r => r.CanUnidirectionalTransfer && r.Lifetime != ServiceLifetime.Scoped);
    }
}