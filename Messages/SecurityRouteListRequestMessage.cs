namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Security routes list request message.
	/// </summary>
	[Serializable]
	[DataContract]
	public class SecurityRouteListRequestMessage : Message, ITransactionIdMessage
	{
		/// <summary>
		/// Initialize <see cref="SecurityRouteListRequestMessage"/>.
		/// </summary>
		public SecurityRouteListRequestMessage()
			: base(MessageTypes.SecurityRouteListRequest)
		{
		}

		/// <inheritdoc />
		[DataMember]
		public long TransactionId { get; set; }

		/// <summary>
		/// Create a copy of <see cref="SecurityRouteListRequestMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new SecurityRouteListRequestMessage
			{
				TransactionId = TransactionId,
			};

			CopyTo(clone);

			return clone;
		}

		/// <inheritdoc />
		public override string ToString() => base.ToString() + $",TrId={TransactionId}";
	}
}