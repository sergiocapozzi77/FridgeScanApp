using System.Collections.ObjectModel;
using System.ComponentModel;

namespace FridgeScan.Models
{


    public class ListViewFoodCategory : INotifyPropertyChanged
    {
        #region Fields

        private string foodCategory;
        private bool isExpanded;
        private string foodIcon;
        private ObservableCollection<Product> foodMenuCollection;

        #endregion

        #region Properties

        public string FoodCategory
        {
            get { return foodCategory; }
            set
            {
                foodCategory = value;
                this.RaisedOnPropertyChanged("FoodCategory");
            }
        }

        public bool IsExpanded
        {
            get { return isExpanded; }
            set
            {
                isExpanded = value;
                this.RaisedOnPropertyChanged("IsExpanded");
            }
        }

        public string FoodIcon
        {
            get { return foodIcon; }
            set
            {
                foodIcon = value;
                this.RaisedOnPropertyChanged("FoodIcon");
            }
        }

        public ObservableCollection<Product> FoodMenuCollection
        {
            get { return foodMenuCollection; }
            set
            {
                foodMenuCollection = value;
                RaisedOnPropertyChanged("FoodMenuCollection");
            }
        }

        #endregion

        #region Constructor

        public ListViewFoodCategory(string foodCategory, List<Product> products)
        {
            this.FoodCategory = foodCategory;
            this.FoodMenuCollection = new ObservableCollection<Product>(products);
            IsExpanded = true;
        }

        #endregion

        #region Interface Member

        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisedOnPropertyChanged(string _PropertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(_PropertyName));
            }
        }

        #endregion
    }
}
