namespace ExistForAll.SimpleSettings.Validations
{
    public class ValidationContext<T> : ValidationContext
    {
        public new T? Settings { get; }
    }
}