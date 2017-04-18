﻿using System;
using System.Linq;
using NServiceBus;
using NServiceBus.Logging;
using Payments.Data.Context;

namespace Payments.Config
{
	[EndpointName("HMS.Payments")]
	public class EndpointConfig : IConfigureThisEndpoint, AsA_Server
	{
		private static readonly ILog Log = LogManager.GetLogger<EndpointConfig>();

		public EndpointConfig()
		{
			LogManager.Use<DefaultFactory>();

			if (Environment.UserInteractive)
				Console.Title = "HMS.Payments";

			InitializeDatbase();
		}

		public void Customize(EndpointConfiguration endpointConfiguration)
		{
			Log.Info("Customize...");

			var container = ContainerSetup.Create();

			endpointConfiguration.UseSerialization<JsonSerializer>();
			endpointConfiguration.Recoverability().Delayed(c => c.NumberOfRetries(0));
			endpointConfiguration.UseContainer<AutofacBuilder>(c => c.ExistingLifetimeScope(container));
			endpointConfiguration.UseTransport<MsmqTransport>().ConnectionString("deadLetter=false;journal=false");
			endpointConfiguration.UsePersistence<InMemoryPersistence>();

			endpointConfiguration.SendFailedMessagesTo("error");
			endpointConfiguration.AuditProcessedMessagesTo("audit");

			var conventions = endpointConfiguration.Conventions();
			conventions.DefiningCommandsAs(
				t =>
					t.Namespace != null && t.Namespace.StartsWith("Payments") && t.Namespace.EndsWith("Commands") &&
					t.Name.EndsWith("Command"));

			conventions.DefiningEventsAs(
				t =>
					t.Namespace != null && t.Namespace.StartsWith("Payments") && t.Namespace.EndsWith("Events") &&
					t.Name.EndsWith("Event"));

			endpointConfiguration.EnableInstallers();
		}

		private void InitializeDatbase()
		{
			Log.Info("Initializing database");

			var context = new PaymentsContext();
			var count = context.PaymentMethods.Count();

			Log.InfoFormat($"Database initialized with {count} payments");
		}
	}
}