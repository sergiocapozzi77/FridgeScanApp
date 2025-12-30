using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FridgeScan.Behaviours
{
    class GridBehavior : Behavior<Grid>
    {
        public Syncfusion.Maui.ListView.SfListView ListView { get; set; }
        private TapGestureRecognizer tapGestureRecognizer;

        protected override void OnAttachedTo(BindableObject bindable)
        {
            tapGestureRecognizer = new TapGestureRecognizer();
            tapGestureRecognizer.Tapped += OnItemTapped;
            (bindable as Grid).GestureRecognizers.Add(tapGestureRecognizer);
#if ANDROID
            // Todo Breaking in https://github.com/essential-studio/maui-listview/pull/358/files
            // ListView.RefreshView();
#endif
            base.OnAttachedTo(bindable);
        }

        private void OnItemTapped(object sender, EventArgs e)
        {
            var dataItem = (sender as Grid).BindingContext as ListViewFoodCategory;
            var currentIndex = ListView.DataSource.DisplayItems.IndexOf(dataItem);

            if (dataItem.IsExpanded)
            {
                dataItem.IsExpanded = false;
            }
            else
            {
                dataItem.IsExpanded = true;
            }
            ListView.RefreshItem(currentIndex, currentIndex, false);
        }

        protected override void OnDetachingFrom(BindableObject bindable)
        {
            base.OnDetachingFrom(bindable);
            ListView = null;
            tapGestureRecognizer.Tapped -= OnItemTapped;
        }
    }
}
