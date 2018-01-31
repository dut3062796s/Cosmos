﻿using System;
using Cosmos.Logging.Collectors;
using Cosmos.Logging.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Cosmos.Logging.Sinks.SampleLogSink {
    public static class SampleLogSinkExtensions {
        public static ILogServiceCollection AddSampleLog(this ILogServiceCollection services, Action<SampleOptions> settingAct = null,
            Action<IConfiguration, SampleLogConfiguration> config = null) {
            var options = new SampleOptions();
            settingAct?.Invoke(options);
            return services.AddSampleLog(Options.Create(options), config);
        }

        public static ILogServiceCollection AddSampleLog(this ILogServiceCollection services, IOptions<SampleOptions> settings,
            Action<IConfiguration, SampleLogConfiguration> config = null) {
            services.AddSinkSettings<SampleOptions, SampleLogConfiguration>(settings.Value, (conf, sink) => config?.Invoke(conf, sink));
            services.AddDependency(s => {
                s.TryAdd(ServiceDescriptor.Scoped<ILogPayloadClient, SampleLogPayloadClient>());
                s.TryAdd(ServiceDescriptor.Singleton<ILogPayloadClientProvider, SampleLogPayloadClientProvider>());
                s.TryAdd(ServiceDescriptor.Singleton(settings));
            });
            return services;
        }
    }
}