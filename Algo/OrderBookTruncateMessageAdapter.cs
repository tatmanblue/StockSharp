﻿namespace StockSharp.Algo
{
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;

	using StockSharp.Messages;

	/// <summary>
	/// The messages adapter build order book from incremental updates <see cref="QuoteChangeStates.Increment"/>.
	/// </summary>
	public class OrderBookTruncateMessageAdapter : MessageAdapterWrapper
	{
		private readonly SynchronizedDictionary<long, int> _depths = new SynchronizedDictionary<long, int>();

		/// <summary>
		/// Initializes a new instance of the <see cref="OrderBookTruncateMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">Underlying adapter.</param>
		public OrderBookTruncateMessageAdapter(IMessageAdapter innerAdapter)
			: base(innerAdapter)
		{
		}

		/// <inheritdoc />
		protected override void OnSendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Reset:
					_depths.Clear();
					break;

				case MessageTypes.MarketData:
				{
					var mdMsg = (MarketDataMessage)message;

					if (mdMsg.DataType == MarketDataTypes.MarketDepth)
					{
						if (mdMsg.IsSubscribe)
						{
							if (mdMsg.MaxDepth != null)
							{
								var actualDepth = mdMsg.MaxDepth.Value;

								var supportedDepth = InnerAdapter.NearestSupportedDepth(actualDepth);

								if (supportedDepth != actualDepth)
								{
									mdMsg = (MarketDataMessage)mdMsg.Clone();
									mdMsg.MaxDepth = supportedDepth;

									_depths.Add(mdMsg.TransactionId, actualDepth);
								}
							}
						}
						else
						{
							_depths.Remove(mdMsg.OriginalTransactionId);
						}
					}

					break;
				}
			}

			base.OnSendInMessage(message);
		}

		/// <inheritdoc />
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			List<QuoteChangeMessage> clones = null;

			switch (message.Type)
			{
				case MessageTypes.QuoteChange:
				{
					var quoteMsg = (QuoteChangeMessage)message;

					foreach (var group in quoteMsg.GetSubscriptionIds().GroupBy(_depths.TryGetValue2))
					{
						if (group.Key == null)
							continue;

						if (clones == null)
							clones = new List<QuoteChangeMessage>();

						var maxDepth = group.Key.Value;

						var clone = (QuoteChangeMessage)quoteMsg.Clone();

						clone.SetSubscriptionIds(group.ToArray());

						if (clone.Bids.Length > maxDepth)
							clone.Bids = clone.Bids.Take(maxDepth).ToArray();

						if (clone.Asks.Length > maxDepth)
							clone.Asks = clone.Asks.Take(maxDepth).ToArray();

						clones.Add(clone);
					}

					if (clones != null)
					{
						var ids = quoteMsg.GetSubscriptionIds().Except(clones.SelectMany(c => c.GetSubscriptionIds())).ToArray();

						if (ids.Length > 0)
							quoteMsg.SetSubscriptionIds(ids);
						else
							message = null;
					}

					break;
				}
			}

			if (message != null)
				base.OnInnerAdapterNewOutMessage(message);

			if (clones != null)
			{
				foreach (var clone in clones)
					base.OnInnerAdapterNewOutMessage(clone);
			}
		}

		/// <summary>
		/// Create a copy of <see cref="OrderBookTruncateMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new OrderBookTruncateMessageAdapter((IMessageAdapter)InnerAdapter.Clone());
		}
	}
}