﻿namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Runtime.Serialization;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Logging;
	using StockSharp.Messages;

	/// <summary>
	/// Message adapter that splits large market data requests on smaller.
	/// </summary>
	public class PartialDownloadMessageAdapter : MessageAdapterWrapper
	{
		/// <summary>
		/// Message for iterate action.
		/// </summary>
		[DataContract]
		[Serializable]
		private class PartialDownloadMessage : Message, IOriginalTransactionIdMessage
		{
			/// <summary>
			/// Initializes a new instance of the <see cref="PartialDownloadMessage"/>.
			/// </summary>
			public PartialDownloadMessage()
				: base(ExtendedMessageTypes.PartialDownload)
			{
			}

			[DataMember]
			public long OriginalTransactionId { get; set; }

			/// <inheritdoc />
			public override string ToString()
			{
				return base.ToString() + $",OrigTrId={OriginalTransactionId}";
			}

			/// <summary>
			/// Create a copy of <see cref="PartialDownloadMessage"/>.
			/// </summary>
			/// <returns>Copy.</returns>
			public override Message Clone()
			{
				return CopyTo(new PartialDownloadMessage());
			}

			/// <summary>
			/// Copy the message into the <paramref name="destination" />.
			/// </summary>
			/// <param name="destination">The object, to which copied information.</param>
			/// <returns>The object, to which copied information.</returns>
			private PartialDownloadMessage CopyTo(PartialDownloadMessage destination)
			{
				destination.OriginalTransactionId = OriginalTransactionId;

				this.CopyExtensionInfo(destination);

				return destination;
			}
		}

		private class DownloadInfo
		{
			public MarketDataMessage Origin { get; }

			public long CurrTransId { get; private set; }
			public bool LastIteration => Origin.To != null && _nextFrom >= Origin.To.Value;

			public bool ReplyReceived { get; set; }
			public long? UnsubscribingId { get; set; }

			private readonly PartialDownloadMessageAdapter _adapter;
			private readonly TimeSpan _iterationInterval;
			private readonly TimeSpan _step;

			private DateTimeOffset _currFrom;
			private bool _firstIteration;
			private DateTimeOffset _nextFrom;
			private readonly DateTimeOffset _to;

			private bool IsStepMax => _step == TimeSpan.MaxValue;

			public DownloadInfo(PartialDownloadMessageAdapter adapter, MarketDataMessage origin, TimeSpan step, TimeSpan iterationInterval)
			{
				if (step <= TimeSpan.Zero)
					throw new ArgumentOutOfRangeException(nameof(step));

				if (iterationInterval < TimeSpan.Zero)
					throw new ArgumentOutOfRangeException(nameof(iterationInterval));

				_adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
				Origin = origin ?? throw new ArgumentNullException(nameof(origin));
				_step = step;
				_iterationInterval = iterationInterval;

				_to = origin.To ?? DateTimeOffset.Now;
				_currFrom = origin.From ?? _to - (IsStepMax ? TimeSpan.FromDays(1) : step);

				_firstIteration = true;
			}

			public void TryUpdateNextFrom(DateTimeOffset last)
			{
				if (_nextFrom < last)
					_nextFrom = last;
			}

			public MarketDataMessage InitNext()
			{
				if (LastIteration)
					throw new InvalidOperationException("LastIteration == true");

				var mdMsg = Origin.TypedClone();

				if (_firstIteration)
				{
					_firstIteration = false;

					_nextFrom = IsStepMax ? _to : _currFrom + _step;

					if (_nextFrom > _to)
						_nextFrom = _to;

					mdMsg.TransactionId = _adapter.TransactionIdGenerator.GetNextId();
					mdMsg.From = _currFrom;
					mdMsg.To = _nextFrom;

					CurrTransId = mdMsg.TransactionId;
				}
				else
				{
					_iterationInterval.Sleep();

					if (Origin.To == null && _nextFrom >= _to)
					{
						// on-line
						mdMsg.From = null;
					}
					else
					{
						_currFrom = _nextFrom;
						_nextFrom += _step;

						if (_nextFrom > _to)
							_nextFrom = _to;

						mdMsg.TransactionId = _adapter.TransactionIdGenerator.GetNextId();
						mdMsg.From = _currFrom;
						mdMsg.To = _nextFrom;

						CurrTransId = mdMsg.TransactionId;
					}
				}

				return mdMsg;
			}
		}

		private readonly SyncObject _syncObject = new SyncObject();
		private readonly Dictionary<long, DownloadInfo> _original = new Dictionary<long, DownloadInfo>();
		private readonly Dictionary<long, DownloadInfo> _partialRequests = new Dictionary<long, DownloadInfo>();
		private readonly Dictionary<long, bool> _liveRequests = new Dictionary<long, bool>();

		/// <summary>
		/// Initializes a new instance of the <see cref="PartialDownloadMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">Underlying adapter.</param>
		public PartialDownloadMessageAdapter(IMessageAdapter innerAdapter)
			: base(innerAdapter)
		{
		}

		/// <inheritdoc />
		protected override bool OnSendInMessage(Message message)
		{
			Message outMsg = null;

			switch (message.Type)
			{
				case MessageTypes.Reset:
				case MessageTypes.Disconnect:
				{
					lock (_syncObject)
					{
						_partialRequests.Clear();
						_original.Clear();
						_liveRequests.Clear();
					}

					break;
				}

				case MessageTypes.OrderStatus:
				case MessageTypes.PortfolioLookup:
				{
					var subscriptionMsg = (ISubscriptionMessage)message;

					if (subscriptionMsg.IsSubscribe)
					{
						if (subscriptionMsg is OrderStatusMessage statusMsg && statusMsg.HasOrderId())
							break;

						var from = subscriptionMsg.From;
						var to = subscriptionMsg.To;

						if (from != null || to != null)
						{
							var step = InnerAdapter.GetHistoryStepSize(DataType.Transactions, out _);

							// adapter do not provide historical request
							if (step == TimeSpan.Zero)
							{
								if (to != null)
								{
									// finishing current history request

									outMsg = subscriptionMsg.CreateResult();
									message = null;
								}
								else
								{
									// or sending further only live subscription
									subscriptionMsg.From = null;
									subscriptionMsg.To = null;

									lock (_syncObject)
										_liveRequests.Add(subscriptionMsg.TransactionId, false);
								}
							}
						}
						else
						{
							lock (_syncObject)
								_liveRequests.Add(subscriptionMsg.TransactionId, false);
						}
					}

					break;
				}

				case MessageTypes.MarketData:
				{
					var mdMsg = (MarketDataMessage)message;
					var transId = mdMsg.TransactionId;

					if (mdMsg.IsSubscribe)
					{
						var from = mdMsg.From;
						var to = mdMsg.To;

						if (from != null || to != null)
						{
							var step = InnerAdapter.GetHistoryStepSize(mdMsg.ToDataType(), out var iterationInterval);

							// adapter do not provide historical request
							if (step == TimeSpan.Zero)
							{
								if (to != null)
								{
									// finishing current history request
									outMsg = new SubscriptionFinishedMessage { OriginalTransactionId = transId };
									message = null;
									break;
								}
								else
								{
									// or sending further only live subscription
									mdMsg.From = null;
									mdMsg.To = null;

									lock (_syncObject)
										_liveRequests.Add(transId, false);

									break;
								}
							}

							var info = new DownloadInfo(this, mdMsg.TypedClone(), step, iterationInterval);

							message = info.InitNext();

							lock (_syncObject)
							{
								_original.Add(info.Origin.TransactionId, info);
								_partialRequests.Add(info.CurrTransId, info);
							}

							this.AddInfoLog("Downloading {0}/{1}: {2}-{3}", mdMsg.SecurityId, mdMsg.DataType, mdMsg.From, mdMsg.To);
						}
						else
						{
							lock (_syncObject)
								_liveRequests.Add(transId, false);
						}
					}
					else
					{
						lock (_syncObject)
						{
							if (!_original.TryGetValue(mdMsg.OriginalTransactionId, out var info))
								break;

							info.UnsubscribingId = transId;
							message = null;
						}
					}

					break;
				}

				case ExtendedMessageTypes.PartialDownload:
				{
					var partialMsg = (PartialDownloadMessage)message;

					MarketDataMessage mdMsg;

					lock (_syncObject)
					{
						if (!_original.TryGetValue(partialMsg.OriginalTransactionId, out var info))
							return false;

						if (info.UnsubscribingId != null)
						{
							outMsg = new SubscriptionResponseMessage { OriginalTransactionId = info.UnsubscribingId.Value };
							message = null;
							break;
						}

						mdMsg = info.InitNext();

						if (mdMsg.To == null)
						{
							_liveRequests.Add(mdMsg.TransactionId, true);

							_original.Remove(partialMsg.OriginalTransactionId);
							_partialRequests.RemoveWhere(p => p.Value == info);
						}
						else
							_partialRequests.Add(info.CurrTransId, info);

						message = mdMsg;
					}

					this.AddInfoLog("Downloading {0}/{1}: {2}-{3}", mdMsg.SecurityId, mdMsg.DataType, mdMsg.From, mdMsg.To);

					break;
				}
			}

			var result = true;
			
			if (message != null)
				result = base.OnSendInMessage(message);

			if (outMsg != null)
				RaiseNewOutMessage(outMsg);

			return result;
		}

		/// <inheritdoc />
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			Message extra = null;

			switch (message.Type)
			{
				case MessageTypes.SubscriptionResponse:
				{
					var responseMsg = (SubscriptionResponseMessage)message;
					var originId = responseMsg.OriginalTransactionId;

					lock (_syncObject)
					{
						if (_liveRequests.TryGetAndRemove(originId, out var isPartial))
						{
							if (responseMsg.IsOk())
							{
								if (!this.IsOutMessageSupported(MessageTypes.SubscriptionOnline))
								{
									if (isPartial)
									{
										// reply was sent previously for the first partial request,
										// now sending "online" message
										message = new SubscriptionOnlineMessage
										{
											OriginalTransactionId = originId
										};
									}
									else
									{
										extra = new SubscriptionOnlineMessage
										{
											OriginalTransactionId = originId
										};
									}
								}
							}

							break;
						}

						if (!_partialRequests.TryGetValue(originId, out var info))
							break;

						var requestId = info.Origin.TransactionId;

						if (!responseMsg.IsOk())
						{
							_original.Remove(requestId);
							_partialRequests.RemoveWhere(p => p.Value == info);

							if (info.ReplyReceived)
							{
								// unexpected subscription stop
								responseMsg.OriginalTransactionId = requestId;
								break;
							}
						}
						
						if (info.ReplyReceived)
							return;

						info.ReplyReceived = true;
						
						responseMsg.OriginalTransactionId = requestId;
					}

					break;
				}

				case MessageTypes.SubscriptionFinished:
				{
					var finishMsg = (SubscriptionFinishedMessage)message;

					lock (_syncObject)
					{
						if (_partialRequests.TryGetAndRemove(finishMsg.OriginalTransactionId, out var info))
						{
							var origin = info.Origin;

							if (info.LastIteration)
							{
								_original.Remove(origin.TransactionId);
								_partialRequests.RemoveWhere(p => p.Value == info);

								finishMsg.OriginalTransactionId = origin.TransactionId;
							}
							else
							{
								message = new PartialDownloadMessage { OriginalTransactionId = origin.TransactionId }.LoopBack(this);
							}
						}
						else
							break;
					}

					this.AddInfoLog("Partial {0} finished.", finishMsg.OriginalTransactionId);

					break;
				}

				case MessageTypes.Execution:
				{
					TryUpdateSubscriptionResult((ExecutionMessage)message);
					break;
				}

				case MessageTypes.Level1Change:
				{
					TryUpdateSubscriptionResult((Level1ChangeMessage)message);
					break;
				}

				case MessageTypes.QuoteChange:
				{
					TryUpdateSubscriptionResult((QuoteChangeMessage)message);
					break;
				}

				case MessageTypes.PositionChange:
				{
					TryUpdateSubscriptionResult((PositionChangeMessage)message);
					break;
				}

				default:
				{
					if (message is CandleMessage candleMsg)
						TryUpdateSubscriptionResult(candleMsg);

					break;
				}
			}

			base.OnInnerAdapterNewOutMessage(message);

			if (extra != null)
				base.OnInnerAdapterNewOutMessage(extra);
		}

		private void TryUpdateSubscriptionResult<TMessage>(TMessage message)
			where TMessage : ISubscriptionIdMessage, IServerTimeMessage
		{
			var originId = message.OriginalTransactionId;

			if (originId == 0)
				return;

			lock (_syncObject)
			{
				if (!_partialRequests.TryGetValue(originId, out var info))
					return;

				info.TryUpdateNextFrom(message.ServerTime);

				message.OriginalTransactionId = info.Origin.TransactionId;
				message.SetSubscriptionIds(subscriptionId: info.Origin.TransactionId);
			}
		}

		/// <summary>
		/// Create a copy of <see cref="PartialDownloadMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new PartialDownloadMessageAdapter(InnerAdapter.TypedClone());
		}
	}
}