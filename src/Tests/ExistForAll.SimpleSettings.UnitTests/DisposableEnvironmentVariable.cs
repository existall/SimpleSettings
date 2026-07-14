namespace ExistForAll.SimpleSettings.UnitTests
{
	internal class DisposableEnvironmentVariable : IDisposable
	{
		private readonly string _name;

		public DisposableEnvironmentVariable(string name, string value)
		{
			_name = name;
			Environment.SetEnvironmentVariable(name, value);
		}

		public void Dispose()
		{
			Environment.SetEnvironmentVariable(_name, null);
			
			if (Environment.GetEnvironmentVariable(_name) != null)
			{
				Thread.Sleep(200);
			}
			
		}
	}
}