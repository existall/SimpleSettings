using System;

namespace ExistForAll.SimpleSettings
{
	// Common base for every exception the library throws, so consumers can catch the whole family with a single
	// catch (SimpleSettingsException). Abstract: it is a grouping/marker base — always throw a specific subtype.
	// Only the two forwarding ctors are needed; no parameterless ctor (every subtype carries required context)
	// and no [Serializable]/SerializationInfo ctor (BinaryFormatter is obsolete on the net8/net10 targets).
	// See C2 in FIX-PLAN.md.
	public abstract class SimpleSettingsException : Exception
	{
		protected SimpleSettingsException(string message)
			: base(message)
		{
		}

		protected SimpleSettingsException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}
}
