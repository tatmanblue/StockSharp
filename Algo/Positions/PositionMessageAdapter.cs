#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Positions.Algo
File: PositionMessageAdapter.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Positions
{
	using System;

	using Ecng.Common;

	using StockSharp.Messages;

	/// <summary>
	/// The message adapter, automatically calculating position.
	/// </summary>
	public class PositionMessageAdapter : MessageAdapterWrapper
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PositionMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">The adapter, to which messages will be directed.</param>
		public PositionMessageAdapter(IMessageAdapter innerAdapter)
			: base(innerAdapter)
		{
		}

		private IPositionManager _positionManager = new PositionManager(true);

		/// <summary>
		/// The position manager.
		/// </summary>
		public IPositionManager PositionManager
		{
			get => _positionManager;
			set => _positionManager = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <inheritdoc />
		protected override bool OnSendInMessage(Message message)
		{
			PositionManager.ProcessMessage(message);
			return base.OnSendInMessage(message);
		}

		/// <inheritdoc />
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			var position = PositionManager.ProcessMessage(message);

			if (position != null)
				((ExecutionMessage)message).Position = position;

			base.OnInnerAdapterNewOutMessage(message);
		}

		/// <summary>
		/// Create a copy of <see cref="PositionMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new PositionMessageAdapter(InnerAdapter.TypedClone());
		}
	}
}