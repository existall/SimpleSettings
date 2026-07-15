namespace ExistForAll.SimpleSettings.Validations
{
    public class ValidationContext
    {
        public ValidationContext(object settings)
        {
            Settings = settings;
        }

        public object? Settings { get; }
    }
}
