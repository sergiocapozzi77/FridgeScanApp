using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FridgeScan.ViewModels
{
    internal partial class ActivitiesViewModel : BaseViewModel
    {
        private readonly ActivityService activityService;

        [ObservableProperty]
        public ObservableCollection<Models.Activity> activities;


        public ActivitiesViewModel(ActivityService activityService)
        {
            this.activityService = activityService;

            _ = LoadActivitiesAsync();
        }

        public async Task LoadActivitiesAsync()
        {
            var activities = await activityService.GetActivitiesAsync();
            Activities = new ObservableCollection<Models.Activity>(activities);
        }
    }
}
