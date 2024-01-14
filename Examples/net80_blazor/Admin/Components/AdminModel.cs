namespace net80_blazor.Admin.Components
{
    public class ItemSelected<T>
    {
        public T Item { get; set; }
        public bool Selected { get; set; }

        public ItemSelected(T item) => Item = item;
    }
    public class QueryOptions
    {
        public QueryOptions() { }
        public QueryOptions(SearchFilterInfo[] filters) => Filters = filters;

        public string SearchText { get; set; }
        public SearchFilterInfo[] Filters { get; set; }
        long _total;
        public long Total
        {
            get => _total;
            set
            {
                if (value < 0) value = 0;
                if (value != _total)
                {
                    _total = value;
                    MaxPageNumber = (int)Math.Ceiling(1.0 * _total / _pageSize);
                    if (_pageNumber > MaxPageNumber) _pageNumber = MaxPageNumber;
                }
            }
        }
        int _pageNumber = 1;
        public int PageNumber
        {
            get => _pageNumber;
            set
            {
                if (value <= 0) value = 1;
                if (value > MaxPageNumber) value = MaxPageNumber;
                _pageNumber = value;
            }
        }
        int _pageSize = 20;
        public int PageSize
        {
            get => _pageSize;
            set
            {
                if (value <= 0) value = 1;
                if (value != _pageSize)
                {
                    _pageSize = value;
                    MaxPageNumber = (int)Math.Ceiling(1.0 * _total / _pageSize);
                    if (MaxPageNumber <= 0) MaxPageNumber = 1;
                    if (_pageNumber > MaxPageNumber) _pageNumber = MaxPageNumber;
                }
            }
        }
        public int MaxPageNumber { get; private set; }
        public string PageNumberQueryStringName { get; set; } = "page";
        public string SearchTextQueryStringName { get; set; } = "search";
        public Func<Task> InvokeQueryAsync { get; set; }
    }
    public class SearchFilterInfo
    {
        public string Label { get; set; }
        public string QueryStringName { get; set; }
        public bool Multiple { get; set; }
        public ItemSelected<string>[] Texts { get; set; }
        public string[] Values { get; set; }
        public bool HasValue => Texts.Where(a => a.Selected).Any();
        public T[] SelectedValues<T>() => Texts.Select((a, b) => a.Selected ? Values[b] : null).Where(a => a != null).Select(a => a.ConvertTo<T>()).ToArray();
        public T SelectedValue<T>() => SelectedValues<T>().FirstOrDefault();

        public SearchFilterInfo(string label, string queryStringName, string texts, string values) : this(label, queryStringName, false, texts, values) { }
        public SearchFilterInfo(string label, string queryStringName, bool multiple, string texts, string values)
        {
            Label = label;
            QueryStringName = queryStringName;
            Multiple = multiple;
            Texts = texts.Split(',').Select(a => new ItemSelected<string>(a)).ToArray();
            Values = values.Split(',');
        }
    }
}
