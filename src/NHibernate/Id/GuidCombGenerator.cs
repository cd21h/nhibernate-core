using System;
using System.Diagnostics;
using NHibernate.Engine;

namespace NHibernate.Id
{
	/// <summary>
	/// An <see cref="IIdentifierGenerator" /> that generates <see cref="System.Guid"/> values 
	/// using a strategy suggested Jimmy Nilsson's 
	/// <a href="http://www.informit.com/articles/article.asp?p=25862">article</a>
	/// on <a href="http://www.informit.com">informit.com</a>. 
	/// </summary>
	/// <remarks>
	/// <p>
	///	This id generation strategy is specified in the mapping file as 
	///	<code>&lt;generator class="guid.comb" /&gt;</code>
	/// </p>
	/// <p>
	/// The <c>comb</c> algorithm is designed to make the use of GUIDs as Primary Keys, Foreign Keys, 
	/// and Indexes nearly as efficient as ints.
	/// </p>
	/// <p>
	/// This code was contributed by Donald Mull.
	/// </p>
	/// </remarks>
	public partial class GuidCombGenerator : IIdentifierGenerator
	{
		private static readonly long BaseDateTicks = new DateTime(1900, 1, 1).Ticks;

		#region IIdentifierGenerator Members

		/// <summary>
		/// Generate a new <see cref="Guid"/> using the comb algorithm.
		/// </summary>
		/// <param name="session">The <see cref="ISessionImplementor"/> this id is being generated in.</param>
		/// <param name="obj">The entity for which the id is being generated.</param>
		/// <returns>The new identifier as a <see cref="Guid"/>.</returns>
		public object Generate(ISessionImplementor session, object obj)
		{
			return GenerateComb(Guid.NewGuid(), DateTime.UtcNow);
		}

		/// <summary>
		/// Generate a new <see cref="Guid"/> using the comb algorithm.
		/// </summary>
		protected static Guid GenerateComb(in Guid guid, DateTime utcNow)
		{
#if NET8_0_OR_GREATER
			Span<byte> guidArray = stackalloc byte[16];
			Span<byte> msecsArray = stackalloc byte[sizeof(long)];
			Span<byte> daysArray = stackalloc byte[sizeof(int)];

			var bytesWritten = guid.TryWriteBytes(guidArray);
			Debug.Assert(bytesWritten);

			// Get the days and milliseconds which will be used to build the byte string 
			TimeSpan days = new TimeSpan(utcNow.Ticks - BaseDateTicks);
			TimeSpan msecs = utcNow.TimeOfDay;

			// Convert to a byte array 
			// Note that SQL Server is accurate to 1/300th of a millisecond so we divide by 3.333333 

			bytesWritten = BitConverter.TryWriteBytes(daysArray, days.Days)
				&& BitConverter.TryWriteBytes(msecsArray, (long)(msecs.TotalMilliseconds / 3.333333));
			Debug.Assert(bytesWritten);

			msecsArray.Reverse();

			// Copy the bytes into the guid 
			//Array.Copy(daysArray, daysArray.Length - 2, guidArray, guidArray.Length - 6, 2);
			guidArray[10] = daysArray[1];
			guidArray[11] = daysArray[0];

			//Array.Copy(msecsArray, msecsArray.Length - 4, guidArray, guidArray.Length - 4, 4);
			msecsArray[^4..].CopyTo(guidArray[^4..]);
			return new Guid(guidArray);
#else

			byte[] guidArray = guid.ToByteArray();

			// Get the days and milliseconds which will be used to build the byte string 
			TimeSpan days = new TimeSpan(utcNow.Ticks - BaseDateTicks);
			TimeSpan msecs = utcNow.TimeOfDay;

			// Convert to a byte array 
			// Note that SQL Server is accurate to 1/300th of a millisecond so we divide by 3.333333 
			byte[] daysArray = BitConverter.GetBytes(days.Days);
			byte[] msecsArray = BitConverter.GetBytes((long) (msecs.TotalMilliseconds / 3.333333));

			// Reverse the bytes to match SQL Servers ordering 
			Array.Reverse(daysArray);
			Array.Reverse(msecsArray);

			// Copy the bytes into the guid 
			Array.Copy(daysArray, daysArray.Length - 2, guidArray, guidArray.Length - 6, 2);
			Array.Copy(msecsArray, msecsArray.Length - 4, guidArray, guidArray.Length - 4, 4);

			return new Guid(guidArray);
#endif
		}

		#endregion
	}
}