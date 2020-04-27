namespace StockSharp.Community
{
	using StockSharp.Community.Messages;
	using StockSharp.Messages;

	/// <summary>
	/// The interface describing a client for access to the registration service.
	/// </summary>
	public interface IProfileClient
	{
		/// <summary>
		/// To start the registration.
		/// </summary>
		/// <param name="profile">The profile information.</param>
		void CreateProfile(UserInfoMessage profile);

		/// <summary>
		/// To send an e-mail message.
		/// </summary>
		void SendEmail();

		/// <summary>
		/// To confirm the e-mail address.
		/// </summary>
		/// <param name="email">E-mail address.</param>
		/// <param name="emailCode">The e-mail confirmation code.</param>
		void ValidateEmail(string email, string emailCode);

		/// <summary>
		/// To send SMS.
		/// </summary>
		/// <param name="email">E-mail address.</param>
		/// <param name="phone">Phone.</param>
		void SendSms(string email, string phone);

		/// <summary>
		/// To confirm the phone number.
		/// </summary>
		/// <param name="email">E-mail address.</param>
		/// <param name="smsCode">SMS verification code.</param>
		void ValidatePhone(string email, string smsCode);

		/// <summary>
		/// To update profile information.
		/// </summary>
		/// <param name="profile">The profile information.</param>
		void UpdateProfile(UserInfoMessage profile);

		/// <summary>
		/// To get profile information.
		/// </summary>
		/// <returns>The profile information.</returns>
		UserInfoMessage GetProfile();

		/// <summary>
		/// To get user information.
		/// </summary>
		/// <param name="userId">User ID.</param>
		/// <returns>The user information.</returns>
		UserInfoMessage GetUserProfile(long userId);

		/// <summary>
		/// To get all available products.
		/// </summary>
		/// <returns>All available products.</returns>
		ProductInfoMessage[] GetProducts();
	}
}