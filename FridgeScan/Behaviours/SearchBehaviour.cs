using Syncfusion.Maui.Inputs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FridgeScan.Behaviours
{
    public class SearchBehavior : IAutocompleteFilterBehavior
    {
        public async Task<object> GetMatchingItemsAsync(SfAutocomplete source, AutocompleteFilterInfo filterInfo)
        {
            IEnumerable itemssource = source.ItemsSource as IEnumerable;
            var filteredItems = (from GroceryItem item in itemssource
                                 where item.Name.StartsWith(filterInfo.Text, StringComparison.CurrentCultureIgnoreCase)
                                 select item);

            return await Task.FromResult(filteredItems);
        }
    }
}
