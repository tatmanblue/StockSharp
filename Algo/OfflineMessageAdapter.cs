﻿namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Localization;
	using StockSharp.Messages;

	/// <summary>
	/// The messages adapter keeping message until connection will be done.
	/// </summary>
	public class OfflineMessageAdapter : MessageAdapterWrapper
	{
		private bool _connected;
		private readonly SyncObject _syncObject = new SyncObject();
		private readonly List<Message> _pendingMessages = new List<Message>();
		private readonly PairSet<long, PortfolioMessage> _pfSubscriptions = new PairSet<long, PortfolioMessage>();
		private readonly PairSet<long, MarketDataMessage> _mdSubscriptions = new PairSet<long, MarketDataMessage>();
		private readonly PairSet<long, OrderRegisterMessage> _pendingRegistration = new PairSet<long, OrderRegisterMessage>();

		/// <summary>
		/// Initializes a new instance of the <see cref="OfflineMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">Underlying adapter.</param>
		public OfflineMessageAdapter(IMessageAdapter innerAdapter)
			: base(innerAdapter)
		{
		}

		private int _maxMessageCount = 10000;

		/// <summary>
		/// Max message queue count. The default value is 10000.
		/// </summary>
		/// <remarks>
		/// Value set to -1 corresponds to the size without limitations.
		/// </remarks>
		public int MaxMessageCount
		{
			get => _maxMessageCount;
			set
			{
				if (value < -1)
					throw new ArgumentOutOfRangeException();

				_maxMessageCount = value;
			}
		}

		/// <inheritdoc />
		protected override bool SendInBackFurther => false;

		/// <inheritdoc />
		protected override void OnSendInMessage(Message message)
		{
			void ProcessOrderReplaceMessage(OrderReplaceMessage replaceMsg)
			{
				if (!_pendingRegistration.TryGetAndRemove(replaceMsg.OriginalTransactionId, out var originOrderMsg))
					_pendingMessages.Add(replaceMsg);
				else
				{
					_pendingMessages.Remove(originOrderMsg);

					RaiseNewOutMessage(new ExecutionMessage
					{
						ExecutionType = ExecutionTypes.Transaction,
						HasOrderInfo = true,
						OriginalTransactionId = replaceMsg.OriginalTransactionId,
						ServerTime = DateTimeOffset.Now,
						OrderState = OrderStates.Done,
						OrderType = originOrderMsg.OrderType,
					});

					var orderMsg = new OrderRegisterMessage();

					replaceMsg.CopyTo(orderMsg);

					_pendingRegistration.Add(replaceMsg.TransactionId, orderMsg);
					StoreMessage(orderMsg);
				}
			}

			switch (message.Type)
			{
				case MessageTypes.Reset:
				{
					lock (_syncObject)
					{
						_connected = false;

						_pendingMessages.Clear();
						_mdSubscriptions.Clear();
						_pfSubscriptions.Clear();
					}

					break;
				}
				case MessageTypes.Connect:
				case MessageTypes.Disconnect:
				case ExtendedMessageTypes.Reconnect:
					break;
				case MessageTypes.Time:
				{
					lock (_syncObject)
					{
						if (!_connected)
						{
							var timeMsg = (TimeMessage)message;

							if (timeMsg.OfflineMode == MessageOfflineModes.Force)
								break;

							return;
						}
					}

					break;
				}
				case MessageTypes.OrderRegister:
				{
					lock (_syncObject)
					{
						if (!_connected)
						{
							var orderMsg = (OrderRegisterMessage)message.Clone();

							_pendingRegistration.Add(orderMsg.TransactionId, orderMsg);
							StoreMessage(orderMsg);

							return;
						}
					}

					break;
				}
				case MessageTypes.OrderCancel:
				{
					lock (_syncObject)
					{
						if (!_connected)
						{
							var cancelMsg = (OrderCancelMessage)message.Clone();

							if (!_pendingRegistration.TryGetAndRemove(cancelMsg.OriginalTransactionId, out var originOrderMsg))
								_pendingMessages.Add(cancelMsg);
							else
							{
								_pendingMessages.Remove(originOrderMsg);

								RaiseNewOutMessage(new ExecutionMessage
								{
									ExecutionType = ExecutionTypes.Transaction,
									HasOrderInfo = true,
									OriginalTransactionId = cancelMsg.TransactionId,
									ServerTime = DateTimeOffset.Now,
									OrderState = OrderStates.Done,
									OrderType = originOrderMsg.OrderType,
								});
							}

							return;
						}
					}

					break;
				}
				case MessageTypes.OrderReplace:
				{
					lock (_syncObject)
					{
						if (!_connected)
						{
							ProcessOrderReplaceMessage((OrderReplaceMessage)message.Clone());
							return;
						}
					}

					break;
				}
				case MessageTypes.OrderPairReplace:
				{
					lock (_syncObject)
					{
						if (!_connected)
						{
							var pairMsg = (OrderPairReplaceMessage)message.Clone();

							ProcessOrderReplaceMessage(pairMsg.Message1);
							ProcessOrderReplaceMessage(pairMsg.Message2);
							return;
						}
					}

					break;
				}
				case MessageTypes.Portfolio:
				{
					lock (_syncObject)
					{
						if (!_connected)
						{
							var pfMsg = (PortfolioMessage)message;
							ProcessSubscriptionMessage(pfMsg, pfMsg.IsSubscribe, pfMsg.TransactionId, pfMsg.OriginalTransactionId, _pfSubscriptions);
							return;
						}
					}

					break;
				}
				case MessageTypes.MarketData:
				{
					lock (_syncObject)
					{
						if (!_connected)
						{
							var mdMsg = (MarketDataMessage)message;
							ProcessSubscriptionMessage(mdMsg, mdMsg.IsSubscribe, mdMsg.TransactionId, mdMsg.OriginalTransactionId, _mdSubscriptions);
							return;
						}
					}

					break;
				}
				default:
				{
					switch (message.OfflineMode)
					{
						case MessageOfflineModes.None:
							lock (_syncObject)
							{
								if (!_connected)
								{
									StoreMessage(message.Clone());
									return;
								}
							}

							break;
						case MessageOfflineModes.Force:
							break;
						case MessageOfflineModes.Cancel:
						{
							switch (message.Type)
							{
								case MessageTypes.SecurityLookup:
									var secLookup = (SecurityLookupMessage)message;
									RaiseNewOutMessage(new SecurityLookupResultMessage { OriginalTransactionId = secLookup.TransactionId });
									break;

								case MessageTypes.PortfolioLookup:
									var pfLookup = (PortfolioLookupMessage)message;

									if (pfLookup.IsSubscribe)
										RaiseNewOutMessage(new PortfolioLookupResultMessage { OriginalTransactionId = pfLookup.TransactionId });
									
									break;
							}

							return;
						}
						default:
							throw new ArgumentOutOfRangeException();
					}

					break;
				}
			}

			base.OnSendInMessage(message);
		}

		private void ProcessSubscriptionMessage<TMessage>(TMessage message, bool isSubscribe, long transactionId, long originalTransactionId, PairSet<long, TMessage> subscriptions)
			where TMessage : Message
		{
			if (isSubscribe)
			{
				var clone = (TMessage)message.Clone();

				if (transactionId != 0)
					subscriptions.Add(transactionId, clone);

				StoreMessage(clone);
			}
			else
			{
				if (originalTransactionId != 0)
				{
					var originMsg = subscriptions.TryGetValue(originalTransactionId);

					if (originMsg != null)
					{
						subscriptions.Remove(originalTransactionId);
						_pendingMessages.Remove(originMsg);
						return;
					}
				}
								
				StoreMessage(message.Clone());
			}
		}

		private void StoreMessage(Message message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			if (_maxMessageCount > 0 && _pendingMessages.Count == _maxMessageCount)
				throw new InvalidOperationException(LocalizedStrings.MaxMessageCountExceed);

			_pendingMessages.Add(message);
		}

		/// <inheritdoc />
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			ConnectMessage connectMessage = null;

			switch (message.Type)
			{
				case MessageTypes.Connect:
				{
					connectMessage = (ConnectMessage)message;
					break;
				}

				case MessageTypes.Disconnect:
				{
					lock (_syncObject)
						_connected = false;

					break;
				}

				case ExtendedMessageTypes.ReconnectingStarted:
				{
					lock (_syncObject)
						_connected = false;

					return;
				}

				case ExtendedMessageTypes.ReconnectingFinished:
				{
					break;
				}
			}

			base.OnInnerAdapterNewOutMessage(message);

			Message[] msgs = null;

			if ((connectMessage != null && connectMessage.Error == null) || message.Type == ExtendedMessageTypes.ReconnectingFinished)
			{
				lock (_syncObject)
				{
					_connected = true;

					msgs = _pendingMessages.CopyAndClear();

					foreach (var msg in msgs)
					{
						if (msg is MarketDataMessage mdMsg)
							_mdSubscriptions.RemoveByValue(mdMsg);
						else if (msg is PortfolioMessage pfMsg)
							_pfSubscriptions.RemoveByValue(pfMsg);
						else if (msg is OrderRegisterMessage orderMsg)
							_pendingRegistration.RemoveByValue(orderMsg);
					}
				}
			}

			if (msgs != null)
			{
				foreach (var msg in msgs)
				{
					msg.LoopBack(this);

					base.OnInnerAdapterNewOutMessage(msg);
				}
			}
		}

		/// <summary>
		/// Create a copy of <see cref="OfflineMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new OfflineMessageAdapter((IMessageAdapter)InnerAdapter.Clone());
		}
	}
}