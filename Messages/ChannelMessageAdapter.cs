#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: ChannelMessageAdapter.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Messages
{
	using System;

	using Ecng.Common;

	/// <summary>
	/// Message adapter, forward messages through a transport channel <see cref="IMessageChannel"/>.
	/// </summary>
	public class ChannelMessageAdapter : MessageAdapterWrapper
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ChannelMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">Underlying adapter.</param>
		/// <param name="inputChannel">Incoming messages channel.</param>
		/// <param name="outputChannel">Outgoing message channel.</param>
		public ChannelMessageAdapter(IMessageAdapter innerAdapter, IMessageChannel inputChannel, IMessageChannel outputChannel)
			: base(innerAdapter)
		{
			InputChannel = inputChannel ?? throw new ArgumentNullException(nameof(inputChannel));
			OutputChannel = outputChannel ?? throw new ArgumentNullException(nameof(outputChannel));

			InputChannel.NewOutMessage += InputChannelOnNewOutMessage;
			OutputChannel.NewOutMessage += OutputChannelOnNewOutMessage;
		}

		/// <summary>
		/// Adapter.
		/// </summary>
		public IMessageChannel InputChannel { get; }

		/// <summary>
		/// Adapter.
		/// </summary>
		public IMessageChannel OutputChannel { get; }

		/// <summary>
		/// Control the lifetime of the incoming messages channel.
		/// </summary>
		public bool OwnInputChannel { get; set; } = true;

		/// <summary>
		/// Control the lifetime of the outgoing messages channel.
		/// </summary>
		public bool OwnOutputChannel { get; set; } = true;

		private void OutputChannelOnNewOutMessage(Message message)
		{
			RaiseNewOutMessage(message);
		}

		/// <inheritdoc />
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			if (!OutputChannel.IsOpened)
				OutputChannel.Open();

			OutputChannel.SendInMessage(message);
		}

		private void InputChannelOnNewOutMessage(Message message)
		{
			InnerAdapter.SendInMessage(message);
		}

		/// <inheritdoc />
		public override void Dispose()
		{
			InputChannel.NewOutMessage -= InputChannelOnNewOutMessage;
			OutputChannel.NewOutMessage -= OutputChannelOnNewOutMessage;

			if (OwnInputChannel)
				InputChannel.Dispose();

			if (OwnOutputChannel)
				OutputChannel.Dispose();

			base.Dispose();
		}

		/// <inheritdoc />
		protected override bool OnSendInMessage(Message message)
		{
			if (!InputChannel.IsOpened)
				InputChannel.Open();

			return InputChannel.SendInMessage(message);
		}

		/// <summary>
		/// Send outgoing message.
		/// </summary>
		/// <param name="message">Message.</param>
		public void SendOutMessage(Message message)
		{
			if (!OutputChannel.IsOpened)
				OutputChannel.Open();

			OutputChannel.SendInMessage(message);
		}

		/// <summary>
		/// Create a copy of <see cref="ChannelMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new ChannelMessageAdapter(InnerAdapter.TypedClone(), InputChannel.Clone(), OutputChannel.Clone());
		}
	}
}