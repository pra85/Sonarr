﻿using System;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Diagnostics;
using NLog;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Instrumentation;
using NzbDrone.Core.Instrumentation;
using NzbDrone.Core.Lifecycle;
using NzbDrone.Core.Messaging.Events;
using Sonarr.Http.ErrorManagement;
using Sonarr.Http.Extensions.Pipelines;
using TinyIoC;

namespace Sonarr.Http
{
    public class SonarrBootstrapper : TinyIoCNancyBootstrapper
    {
        private readonly TinyIoCContainer _tinyIoCContainer;
        private static readonly Logger Logger = NzbDroneLogger.GetLogger(typeof(SonarrBootstrapper));

        public SonarrBootstrapper(TinyIoCContainer tinyIoCContainer)
        {
            _tinyIoCContainer = tinyIoCContainer;
        }

        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            Logger.Info("Starting NzbDrone API");

            if (RuntimeInfoBase.IsProduction)
            {
                DiagnosticsHook.Disable(pipelines);
            }

            RegisterPipelines(pipelines);

            container.Resolve<DatabaseTarget>().Register();
            container.Resolve<IEventAggregator>().PublishEvent(new ApplicationStartedEvent());

            ApplicationPipelines.OnError.AddItemToEndOfPipeline((Func<NancyContext, Exception, Response>) container.Resolve<SonarrErrorPipeline>().HandleException);
        }

        private void RegisterPipelines(IPipelines pipelines)
        {
            var pipelineRegistrars = _tinyIoCContainer.ResolveAll<IRegisterNancyPipeline>();

            foreach (var registerNancyPipeline in pipelineRegistrars)
            {
                registerNancyPipeline.Register(pipelines);
            }
        }

        protected override TinyIoCContainer GetApplicationContainer()
        {
            return _tinyIoCContainer;
        }

        protected override DiagnosticsConfiguration DiagnosticsConfiguration
        {
            get { return new DiagnosticsConfiguration { Password = @"password" }; }
        }

        protected override byte[] FavIcon
        {
            get
            {
                return null;
            }
        }
    }
}