using System;
using Melody.Data.Enums;

namespace Melody.Data.Exceptions
{
	public sealed class TrackNotFoundException : Exception
	{
		public override string Message { get; }

		public TrackNotFoundException(string query, MelodySearchProvider searchProvider)
		{
			this.Message = $"No tracks were found on {searchProvider} for the search query provided:\n{query}";
		}
	}
}