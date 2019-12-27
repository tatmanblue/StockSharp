namespace StockSharp.Algo.Server
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Net;
	using System.Security;

	using Ecng.Security;

	using StockSharp.Localization;
	using StockSharp.Messages;

	/// <summary>
	/// The interface describing the connection access check module.
	/// </summary>
	public interface IRemoteAuthorization : IAuthorization
	{
		/// <summary>
		/// All available users.
		/// </summary>
		IEnumerable<PermissionCredentials> AllRemoteUsers { get; }

		/// <summary>
		/// Save user.
		/// </summary>
		/// <param name="login">Login.</param>
		/// <param name="password">Password.</param>
		/// <param name="possibleAddresses">Possible addresses.</param>
		/// <param name="permissions">Permissions.</param>
		void SaveRemoteUser(string login, SecureString password, IEnumerable<IPAddress> possibleAddresses, UserPermissions permissions);

		/// <summary>
		/// Delete user by login.
		/// </summary>
		/// <param name="login">Login.</param>
		/// <returns>Returns <see langword="true"/>, if user was deleted, otherwise return <see langword="false"/>.</returns>
		bool DeleteRemoteUser(string login);

		/// <summary>
		/// Get permission for request.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="requiredPermissions">Required permissions.</param>
		/// <param name="securityId">Security ID.</param>
		/// <param name="dataType">Market data type.</param>
		/// <param name="arg">The parameter associated with the <paramref name="dataType" /> type. For example, <see cref="CandleMessage.Arg"/>.</param>
		/// <param name="date">Date.</param>
		/// <returns>Possible permissions.</returns>
		bool HasPermissions(Guid sessionId, UserPermissions requiredPermissions, string securityId, string dataType, object arg, DateTime? date);
	}

	/// <summary>
	/// The connection access check module which provides access to all connections.
	/// </summary>
	public class AnonymousRemoteAuthorization : AnonymousAuthorization, IRemoteAuthorization
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AnonymousRemoteAuthorization"/>.
		/// </summary>
		public AnonymousRemoteAuthorization()
		{
		}

		/// <inheritdoc />
		public virtual bool HasPermissions(Guid sessionId, UserPermissions requiredPermissions, string securityId, string dataType, object arg, DateTime? date)
		{
			switch (requiredPermissions)
			{
				case UserPermissions.Save:
				case UserPermissions.Delete:
				case UserPermissions.EditSecurities:
				case UserPermissions.EditExchanges:
				case UserPermissions.EditBoards:
				case UserPermissions.DeleteSecurities:
				case UserPermissions.DeleteExchanges:
				case UserPermissions.DeleteBoards:
				case UserPermissions.GetUsers:
				case UserPermissions.EditUsers:
				case UserPermissions.ServerManage:
				case UserPermissions.Trading:
				case UserPermissions.Withdraw:
					return false;
				case UserPermissions.Load:
				case UserPermissions.SecurityLookup:
				case UserPermissions.ExchangeLookup:
				case UserPermissions.ExchangeBoardLookup:
					return true;
				default:
					throw new ArgumentOutOfRangeException(nameof(requiredPermissions), requiredPermissions, LocalizedStrings.Str1219);
			}
		}

		/// <inheritdoc />
		public virtual IEnumerable<PermissionCredentials> AllRemoteUsers => Enumerable.Empty<PermissionCredentials>();

		/// <inheritdoc />
		public virtual void SaveRemoteUser(string login, SecureString password, IEnumerable<IPAddress> possibleAddresses, UserPermissions permissions)
		{
			SaveUser(login, password, possibleAddresses);
		}

		/// <inheritdoc />
		public virtual bool DeleteRemoteUser(string login)
		{
			return DeleteUser(login);
		}
	}
}