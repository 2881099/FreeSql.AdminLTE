//using FreeSql.Internal.CommonProvider;
//using FreeSql.Internal.Model;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Linq.Expressions;
//using System.Text;

//namespace FreeSql.AdminLTE.ResponseModel
//{
//    public class TopicController 
//    {
//        public ListPage<object> List(int pageNumber, int pageSize, DynamicFilterInfo dynamicFilter)
//        {
//            IFreeSql fsql = null;
//            var list = fsql.Select<object>().WhereDynamic(dynamicFilter).Page(pageNumber, pageSize).ToList();
//            return new ListPage<object>()
//                .SetOptions(new ListPageOptions
//                {
//                })
//                .AddTableColumn()
//        }
//    }

//    public class BaseController
//    {
        
//    }

//    public class ListPage<T> where T : class
//    {
//        public ListPageOptions Options { get; set; }
//        public Pagination Pagination { get; set; }
//        public List<TableColumn> Headers { get; set; }
//        public List<Func<T, object>> CellTextSelector { get; set; }
//        public List<object[]> Rows { get; set; }

//        public ListPage<T> SetOptions(ListPageOptions options)
//        {
//            this.Options = options;
//            return this;
//        }
//        public ListPage<T> SetPagination(int pageNumber, int pageSize, int total)
//        {
//            this.Pagination = new Pagination { PageNumber = pageNumber, PageSize = pageSize, Total = total };
//            return this;
//        }
//        public ListPage<T> AddTableColumn(string headerName, string heawderText, string headerClass, int width, 
//            string cellClass, Func<T, object> cellTextSelector)
//        {
//            this.Headers.Add(new TableColumn { HeaderName = headerName, HeaderText = heawderText, HeaderClass = headerClass, Width = width, CellClass = cellClass });
//            this.CellTextSelector.Add(cellTextSelector);
//            return this;
//        }
//        public ListPage<T> AddTableColumn(string text, Func<T, object> cellValueSelector) => this.AddTableColumn(text, null, null, 0, null, cellValueSelector);
//        public ListPage<T> AddTableColumn(string text, int width, Func<T, object> cellValueSelector) => this.AddTableColumn(text, null, null, width, null, cellValueSelector);
//        public ListPage<T> AddTableRow(T item)
//        {
//            this.Rows.Add(CellTextSelector.Select(a => a?.Invoke(item)).ToArray());
//            return this;
//        }
//    }
//    public class ListPageOptions
//    {
//        public Filter[] Filters { get; set; }
//        public bool IsDynamicFilter { get; set; }
//        public bool IsInsert { get; set; }
//        public bool IsUpdate { get; set; }
//        public bool IsDelete { get; set; }
//        public bool IsBatchDelete { get; set; }
//        public bool IsPage { get; set; }
//        public class Filter
//        {
//            public string Name { get; set; }
//            public string Text { get; set; }
//            public DynamicFilterOperator[] Operators { get; set; }
//        }
//    }
//    public class Pagination
//    {
//        public int Total { get; set; }
//        public int PageNumber { get; set; } = 1;
//        public int PageSize { get; set; } = 20;
//        public int PageTotal => (int)Math.Ceiling(1.0 * Total / PageSize);
//    }
//    public class TableColumn
//    {
//        public string HeaderName { get; set; }
//        public string HeaderText { get; set; }
//        public string HeaderClass { get; set; }
//        public string CellClass { get; set; }
//        public int Width { get; set; }
//    }


//    public class TreeNode<T>
//    {
//        public List<TreeNode<T>> Childs { get; } = new List<TreeNode<T>>();

//        public T Item { get; set; }
//    }
//    public class MenuTreeNodeItem
//    {
//        public string Id { get; set; }
//        public int Level { get; set; }
//        public string Icon { get; set; }
//        public string Link { get; set; }
//    }

//    public class GridView
//    {
//        public List<GridViewColumn> Columns { get; } = new List<GridViewColumn>();
//    }
//    public class GridViewColumn
//    {
//        public GridViewColumnType ColumnType { get; set; }
//        public string ColumnHeaderText { get; set; }
//    }
//    public enum GridViewColumnType { Boolean, String, DateTime, Number, Money, Button }
//}
