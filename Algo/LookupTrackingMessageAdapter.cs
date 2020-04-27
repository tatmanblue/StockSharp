namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Collections;

	using StockSharp.Localization;
	using StockSharp.Logging;
	using StockSharp.Messages;

	/// <summary>
	/// Message adapter that tracks multiple lookups requests and put them into single queue.
	/// </summary>
	public class LookupTrackingMessageAdapter : MessageAdapterWrapper
	{
		private class LookupInfo
		{
			private readonly TimeSpan _initLeft;
			private TimeSpan _left;

			public LookupInfo(ISubscriptionMessage subscription, TimeSpan left)
			{
				Subscription = subscription ?? throw new ArgumentNullException(nameof(subscription));
				_initLeft = left;
				_left = left;
			}

			public ISubscriptionMessage Subscription { get; }

			public bool ProcessTime(TimeSpan diff)
			{
				var left = _left - diff;

				if (left <= TimeSpan.Zero)
					return true;

				_left = left;
				return false;
			}

			public void IncreaseTimeOut()
			{
				_left = _initLeft;
			}
		}

		private readonly CachedSynchronizedDictionary<long, LookupInfo> _lookups = new CachedSynchronizedDictionary<long, LookupInfo>();
		private readonly Dictionary<MessageTypes, Dictionary<long, ITransactionIdMessage>> _queue = new Dictionary<MessageTypes, Dictionary<long, ITransactionIdMessage>>();
		private DateTimeOffset _prevTime;

		/// <summary>
		/// Initializes a new instance of the <see cref="LookupTrackingMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">Inner message adapter.</param>
		public LookupTrackingMessageAdapter(IMessageAdapter innerAdapter)
			: base(innerAdapter)
		{
		}

		private TimeSpan _timeOut = TimeSpan.FromSeconds(10);

		/// <summary>
		/// Securities and portfolios lookup timeout.
		/// </summary>
		/// <remarks>
		/// By default is 10 seconds.
		/// </remarks>
		public TimeSpan TimeOut
		{
			get => _timeOut;
			set
			{
				if (value < TimeSpan.Zero)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.IntervalMustBePositive);

				_timeOut = value;
			}
		}

		/// <inheritdoc />
		protected override bool OnSendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Reset:
				{
					lock (_lookups.SyncRoot)
					{
						_prevTime = default;
						_lookups.Clear();
						_queue.Clear();
					}

					break;
				}

				default:
					if (message.Type.IsLookup() && !ProcessLookupMessage((ISubscriptionMessage)message))
						return true;

					break;
			}

			return base.OnSendInMessage(message);
		}

		private bool ProcessLookupMessage(ISubscriptionMessage message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			if (message is OrderStatusMessage orderMsg && orderMsg.HasOrderId())
				return true;

			var transId = message.TransactionId;

			var isEnqueue = false;
			var isStarted = false;

			try
			{
				lock (_lookups.SyncRoot)
				{
					var queue = _queue.SafeAdd(message.Type);

					// not prev queued lookup
					if (queue.TryAdd(transId, message.TypedClone()))
					{
						if (queue.Count > 1)
						{
							isEnqueue = true;
							return false;
						}
					}

					if (message.IsSubscribe && !this.IsResultMessageSupported(message.Type) && TimeOut > TimeSpan.Zero)
					{
						_lookups.Add(transId, new LookupInfo(message.TypedClone(), TimeOut));
						isStarted = true;
					}
				}
			
				return true;
			}
			finally
			{
				if (isEnqueue)
					this.AddInfoLog("Lookup queued {0}.", message);

				if (isStarted)
					this.AddInfoLog("Lookup timeout {0} started for {1}.", TimeOut, transId);
			}
		}

		/// <inheritdoc />
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			base.OnInnerAdapterNewOutMessage(message);

			Message TryInitNextLookup(MessageTypes type, long id)
			{
				if (!_queue.TryGetValue(type, out var queue))
					return null;

				if (!queue.Remove(id))
					return null;

				if (queue.Count == 0)
				{
					_queue.Remove(type);
					return null;
				}
				
				return (Message)queue.First().Value;
			}

			Message nextLookup = null;

			if (message is IOriginalTransactionIdMessage originIdMsg)
			{
				if (originIdMsg is SubscriptionFinishedMessage ||
				    originIdMsg is SubscriptionOnlineMessage ||
				    originIdMsg is SubscriptionResponseMessage resp && !resp.IsOk())
				{
					var id = originIdMsg.OriginalTransactionId;

					lock (_lookups.SyncRoot)
					{
						if (_lookups.TryGetValue(id, out var info))
						{
							_lookups.Remove(originIdMsg.OriginalTransactionId);
							this.AddInfoLog("Lookup finished {0}.", id);

							nextLookup = TryInitNextLookup(info.Subscription.Type, info.Subscription.TransactionId);
						}
						else
						{
							foreach (var type in _queue.Keys.ToArray())
							{
								nextLookup = TryInitNextLookup(type, id);

								if (nextLookup != null)
									break;
							}
						}
					}
				}
				else if (message is ISubscriptionIdMessage subscrMsg)
				{
					lock (_lookups.SyncRoot)
					{
						foreach (var id in subscrMsg.GetSubscriptionIds())
						{
							if (_lookups.TryGetValue(id, out var info))
								info.IncreaseTimeOut();	
						}
					}
				}
			}

			if (nextLookup != null)
			{
				nextLookup.LoopBack(this);
				base.OnInnerAdapterNewOutMessage(nextLookup);
			}

			List<Message> nextLookups = null;

			if (_prevTime != DateTimeOffset.MinValue)
			{
				var diff = message.LocalTime - _prevTime;

				foreach (var pair in _lookups.CachedPairs)
				{
					var info = pair.Value;

					if (!info.ProcessTime(diff))
						continue;

					var transId = info.Subscription.TransactionId;
					_lookups.Remove(transId);
					this.AddInfoLog("Lookup timeout {0}.", transId);

					base.OnInnerAdapterNewOutMessage(info.Subscription.CreateResult());

					if (nextLookups == null)
						nextLookups = new List<Message>();

					lock (_lookups.SyncRoot)
					{
						var next = TryInitNextLookup(info.Subscription.Type, info.Subscription.TransactionId);

						if (next != null)
							nextLookups.Add(next);
					}
				}
			}

			if (nextLookups != null)
			{
				foreach (var lookup in nextLookups)
				{
					lookup.LoopBack(this);
					base.OnInnerAdapterNewOutMessage(lookup);	
				}
			}

			_prevTime = message.LocalTime;
		}

		/// <summary>
		/// Create a copy of <see cref="LookupTrackingMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new LookupTrackingMessageAdapter(InnerAdapter.TypedClone()) { TimeOut = TimeOut };
		}
	}
}