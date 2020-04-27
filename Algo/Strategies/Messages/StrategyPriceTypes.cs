namespace StockSharp.Community
{
	using System.ComponentModel.DataAnnotations;
	using System.Runtime.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Strategy price types.
	/// </summary>
	[DataContract]
	public enum StrategyPriceTypes
	{
		/// <summary>
		/// Lifetime.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.LifetimeKey)]
		Lifetime,

		/// <summary>
		/// Per month.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.PerMonthKey)]
		PerMonth,

		/// <summary>
		/// Annual.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.AnnualKey)]
		Annual
	}
}