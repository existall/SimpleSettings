namespace ExistForAll.SimpleSettings
{
    internal class SettingsBuilderFactory : ISettingsBuilderFactory
    {
        private int _counter;
        private readonly SortedList<int, ISectionBinder> _binders = new SortedList<int, ISectionBinder>();

        public IEnumerable<ISectionBinder> Binders => _binders.Values;

        public SettingsOptions Options { get; } = new SettingsOptions();

        public void AddSectionBinder(ISectionBinder sectionBinder)
        {
            if (sectionBinder == null) throw new ArgumentNullException(nameof(sectionBinder));
            _binders.Add(_counter++, sectionBinder);
        }
    }
}