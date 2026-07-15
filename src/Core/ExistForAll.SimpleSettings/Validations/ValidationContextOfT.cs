namespace ExistForAll.SimpleSettings.Validations
{
    public class ValidationContext<T> : ValidationContext
    {
        public ValidationContext(T settings)
            : base(settings!)
        {
            Settings = settings;
        }

        public new T? Settings { get; }
    }
}
