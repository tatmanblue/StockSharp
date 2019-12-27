namespace StockSharp.Messages
{
	using System;

	/// <summary>
	/// The interface describing an message with <see cref="IsSubscribe"/> property.
	/// </summary>
	public interface ISubscriptionMessage : ITransactionIdMessage, IOriginalTransactionIdMessage
	{
		/// <summary>
		/// Start date, from which data needs to be retrieved.
		/// </summary>
		DateTimeOffset? From { get; set; }

		/// <summary>
		/// End date, until which data needs to be retrieved.
		/// </summary>
		DateTimeOffset? To { get; set; }

		/// <summary>
		/// The message is subscription.
		/// </summary>
		bool IsSubscribe { get; set; }
	}
}