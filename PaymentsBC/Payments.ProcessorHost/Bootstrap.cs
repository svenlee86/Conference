﻿using System;
using System.Reflection;
using Conference.Common;
using ECommon.Components;
using ECommon.Configurations;
using ECommon.Logging;
using ENode.Configurations;
using ENode.Infrastructure;
using ECommonConfiguration = ECommon.Configurations.Configuration;

namespace Payments.ProcessorHost
{
    public class Bootstrap
    {
        private static ILogger _logger;
        private static ECommonConfiguration _ecommonConfiguration;
        private static ENodeConfiguration _enodeConfiguration;

        public static void Initialize()
        {
            InitializeECommon();
            try
            {
                InitializeENode();
            }
            catch (Exception ex)
            {
                _logger.Error("Initialize ENode failed.", ex);
                throw;
            }
        }
        public static void Start()
        {
            try
            {
                _enodeConfiguration.StartEQueue();
            }
            catch (Exception ex)
            {
                _logger.Error("EQueue start failed.", ex);
                throw;
            }
        }
        public static void Stop()
        {
            try
            {
                _enodeConfiguration.ShutdownEQueue();
            }
            catch (Exception ex)
            {
                _logger.Error("EQueue stop failed.", ex);
                throw;
            }
        }

        private static void InitializeECommon()
        {
            _ecommonConfiguration = ECommonConfiguration
                .Create()
                .UseAutofac()
                .RegisterCommonComponents()
                .UseLog4Net()
                .UseJsonNet()
                .RegisterUnhandledExceptionHandler();
            _logger = ObjectContainer.Resolve<ILoggerFactory>().Create(typeof(Bootstrap).FullName);
            _logger.Info("ECommon initialized.");
        }
        private static void InitializeENode()
        {
            ConfigSettings.Initialize();

            var assemblies = new[]
            {
                Assembly.Load("Conference.Common"),
                Assembly.Load("Payments.Commands"),
                Assembly.Load("Payments.Domain"),
                Assembly.Load("Payments.Messages"),
                Assembly.Load("Payments.CommandHandlers"),
                Assembly.Load("Payments.MessagePublishers"),
                Assembly.Load("Payments.ReadModel"),
                Assembly.GetExecutingAssembly()
            };
            var setting = new ConfigurationSetting(ConfigSettings.ConferenceENodeConnectionString);

            _enodeConfiguration = _ecommonConfiguration
                .CreateENode(setting)
                .RegisterENodeComponents()
                .RegisterBusinessComponents(assemblies)
                .UseSqlServerLockService()
                .UseSqlServerCommandStore()
                .UseSqlServerEventStore()
                .UseEQueue()
                .InitializeBusinessAssemblies(assemblies);
            _logger.Info("ENode initialized.");
        }
    }
}
