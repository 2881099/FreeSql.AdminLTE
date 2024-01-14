using FreeSql;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Web;

public static class AdminExtensions
{

    public static string GetQueryStringValue(this NavigationManager nav, string name)
	{
        var obj = HttpUtility.ParseQueryString(new Uri(nav.Uri).Query);
		return obj[name] ?? "";
    }
    public static string[] GetQueryStringValues(this NavigationManager nav, string name)
	{
        var obj = HttpUtility.ParseQueryString(new Uri(nav.Uri).Query);
        return obj.GetValues(name) ?? new string[0];
    }

    public static void CopyTo<T>(this IFreeSql fsql, T from ,T to)
    {
        foreach (var col in fsql.CodeFirst.GetTableByEntity(typeof(T)).ColumnsByPosition)
            col.SetValue(to, col.GetValue(from));
    }

    record ConfirmResult(bool isConfirmed);
    async public static Task<bool> Confirm(this IJSRuntime JS, string title, string text = "")
    {
        var jsr = await JS.InvokeAsync<ConfirmResult>("Swal.fire", new
        {
            title = title,
            text = text,
            icon = "warning",
            showCancelButton = true,
            confirmButtonColor = "#3085d6",
            cancelButtonColor = "#d33",
            confirmButtonText = "确定",
            cancelButtonText = "取消"
        });
        return jsr.isConfirmed;
    }
    async public static Task Success(this IJSRuntime JS, string title)
    {
        await JS.InvokeVoidAsync("Swal.fire", new
        {
            //position = "top-end",
            title = title,
            icon = "success",
            showConfirmButton = false,
            timer = 1500
        });
    }
}
