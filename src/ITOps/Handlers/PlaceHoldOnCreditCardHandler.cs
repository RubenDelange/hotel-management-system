﻿using ITOps.Messages.Commands;
using ITOps.Messages.Events;
using NServiceBus;
using NServiceBus.Logging;
using Rates.Integration;
using System;
using System.Threading.Tasks;

namespace ITOps.Handlers
{
	public class PlaceHoldOnCreditCardHandler : IHandleMessages<PlaceHoldOnCreditCardCommand>
	{
		private static readonly ILog Log = LogManager.GetLogger<PlaceHoldOnCreditCardHandler>();
		public IProvideReservationRateTotal ReservationRateTotalProvider { get; set; }

		public async Task Handle(PlaceHoldOnCreditCardCommand message, IMessageHandlerContext context)
		{
			Log.Info($"Handle PlaceHoldOnCreditCardCommand for reservation {message.PurchaseUuid}");

			var reservationRatesTotal = ReservationRateTotalProvider.GetRateTotal(message.PurchaseUuid);
			// would use the Finance service here to get the credit card info using the PurchaseUuid

			var amountToHold = CalculateHoldAmount(reservationRatesTotal);

			Log.Info($"Credit card hold placed for purchase '{message.PurchaseUuid}' holding {amountToHold:C}");
			
			await PlaceHold(message.PaymentMethodId, reservationRatesTotal);
			await context.Publish<CreditCardHoldPlacedEvent>(e => e.PurchaseUuid = message.PurchaseUuid);
		}


		private static int CalculateHoldAmount(decimal reservationRatesTotal)
		{
			const double holdPercent = 10;
			return (int) Math.Round((double)reservationRatesTotal / 100 * holdPercent, 2);
		}


		private async Task PlaceHold(string paymentMethodId, decimal amount)
		{
			if (amount == 72.00m)  // Book a four night stay in the 1 King Bed Suite to trigger this
			{
				Log.Error("Simulating the external payment gateway system being down.");
				throw new Exception("The payment gateway did not respond.");
			}

			Log.Info("Simulating calling a slow (15 seconds) external payment gateway API to place a hold.");
			await Task.Delay(15000);

			Log.Info("Call to payment gateway completed.");
		}
	}
}