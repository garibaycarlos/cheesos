using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cheesos.Extensions
{
    public static class IEnumerableExtension
    {
        public static IEnumerable<SelectListItem> ToSelectListItem<T>(this IEnumerable<T> items, string text, string value, int selectedValue)
        {
            return items.Select(i => new SelectListItem
            {
                Text = i.GetPropertyValue(text),
                Value = i.GetPropertyValue(value),
                Selected = i.GetPropertyValue(value).Equals(selectedValue.ToString())
            });
        }
    }
}