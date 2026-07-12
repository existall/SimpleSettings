namespace ExistForAll.SimpleSettings.Benchmark
{
	// Fixtures resolved explicitly by type. They deliberately carry no [SettingsSection] attribute and
	// no "Settings" suffix, so they are NOT discovered by ScanBenchmark's assembly scan — that keeps the
	// scan fixture (the IPerformanceInterfaceN set) isolated from the resolve/shape fixtures below.
	// The default section-name formatter strips a leading "I", so IProps10 -> section "Props10", etc.

	public interface IProps1
	{
		string P0 { get; set; }
	}

	public interface IProps10
	{
		string P0 { get; set; } string P1 { get; set; } string P2 { get; set; } string P3 { get; set; } string P4 { get; set; }
		string P5 { get; set; } string P6 { get; set; } string P7 { get; set; } string P8 { get; set; } string P9 { get; set; }
	}

	public interface IProps50
	{
		string P0 { get; set; } string P1 { get; set; } string P2 { get; set; } string P3 { get; set; } string P4 { get; set; }
		string P5 { get; set; } string P6 { get; set; } string P7 { get; set; } string P8 { get; set; } string P9 { get; set; }
		string P10 { get; set; } string P11 { get; set; } string P12 { get; set; } string P13 { get; set; } string P14 { get; set; }
		string P15 { get; set; } string P16 { get; set; } string P17 { get; set; } string P18 { get; set; } string P19 { get; set; }
		string P20 { get; set; } string P21 { get; set; } string P22 { get; set; } string P23 { get; set; } string P24 { get; set; }
		string P25 { get; set; } string P26 { get; set; } string P27 { get; set; } string P28 { get; set; } string P29 { get; set; }
		string P30 { get; set; } string P31 { get; set; } string P32 { get; set; } string P33 { get; set; } string P34 { get; set; }
		string P35 { get; set; } string P36 { get; set; } string P37 { get; set; } string P38 { get; set; } string P39 { get; set; }
		string P40 { get; set; } string P41 { get; set; } string P42 { get; set; } string P43 { get; set; } string P44 { get; set; }
		string P45 { get; set; } string P46 { get; set; } string P47 { get; set; } string P48 { get; set; } string P49 { get; set; }
	}

	// Exercises converter selection across the built-in converters (DateTime / Uri / Enum / Default).
	public interface ITypedBag
	{
		int IntValue { get; set; }
		double DoubleValue { get; set; }
		bool BoolValue { get; set; }
		System.DateTime DateValue { get; set; }
		System.Uri UriValue { get; set; }
		System.DayOfWeek EnumValue { get; set; }
	}

	// Exercises the array + enumerable converters (delimited-string -> collection).
	public interface IArrayBag
	{
		int[] Ints { get; set; }
		string[] Strings { get; set; }
		System.Collections.Generic.IEnumerable<int> IntSeq { get; set; }
	}

	// Deep interface-inheritance chain: property extraction gathers members from every base interface.
	public interface ILevel0 { string L0 { get; set; } }
	public interface ILevel1 : ILevel0 { string L1 { get; set; } }
	public interface ILevel2 : ILevel1 { string L2 { get; set; } }
	public interface ILevel3 : ILevel2 { string L3 { get; set; } }
}
