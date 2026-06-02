# Products Page Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Redesign the Products page with M3 styling, add expiry/frozen tracking, replace +/-/delete with an edit button, and create a ProductDetailPage for editing.

**Architecture:** Product model gains expiry/frozen fields and computed properties for badge display. ProductService reads/writes these new Appwrite columns. A new ProductDetailPage provides full editing. ProductsPage XAML is redesigned per M3 tokens.

**Tech Stack:** .NET MAUI, Syncfusion SfListView, CommunityToolkit.Mvvm, Appwrite Tables API

---

### Task 1: Product model — add expiry/frozen fields, remove +/-/delete commands

**Files:**
- Modify: `FridgeScan/Models/Product.cs`

- [ ] **Add new fields and computed properties, remove old commands**

Current constructor assigns `DecreaseCommand`, `IncreaseCommand`, `RemoveCommand`. Remove all three command properties and their assignments. Add `ExpiryDate`, `IsFrozen` with `[NotifyPropertyChangedFor]` on computed properties. Add computed properties.

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;

namespace FridgeScan.Models;

public partial class Product : ObservableRecipient
{
    public Product(string name, string? category, int quantity)
    {
        this.name = name;
        this.category = category ?? "Other";
        this.quantity = quantity;
    }

    [ObservableProperty]
    private string rowId;

    [ObservableProperty]
    public string name;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    private int quantity;

    [ObservableProperty]
    private string category;

    [ObservableProperty]
    public bool isSelected;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DaysUntilExpiry))]
    [NotifyPropertyChangedFor(nameof(ShowExpiryBadge))]
    [NotifyPropertyChangedFor(nameof(ExpiryDisplayText))]
    [NotifyPropertyChangedFor(nameof(ExpiryColor))]
    private DateTime? expiryDate;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowFrozenIcon))]
    private bool isFrozen;

    // Computed properties

    public int? DaysUntilExpiry =>
        expiryDate.HasValue
            ? (int?)(expiryDate.Value.Date - DateTime.Today.Date).TotalDays
            : null;

    public bool ShowExpiryBadge =>
        DaysUntilExpiry.HasValue && DaysUntilExpiry.Value <= 3;

    public string ExpiryDisplayText => DaysUntilExpiry switch
    {
        < 0 => "Expired",
        0   => "Today",
        <= 3 => $"{DaysUntilExpiry}d left",
        _   => null
    };

    public Color ExpiryColor => DaysUntilExpiry switch
    {
        < 0 => Color.FromArgb("#E74C3C"),
        _   => Color.FromArgb("#F39C12"),
    };

    public bool ShowFrozenIcon => isFrozen;

    [RelayCommand]
    private void ToggleSelect()
    {
        IsSelected = !IsSelected;
    }

    public override string ToString() => $"{Name} ({Quantity})";
}
```

- [ ] **Commit**

```bash
git add FridgeScan/Models/Product.cs
git commit -m "feat(product): add expiry/frozen fields, computed properties, remove +/-/delete commands"
```

---

### Task 2: ProductService — map expiry and frozen from Appwrite

**Files:**
- Modify: `FridgeScan/Services/ProductService.cs`

- [ ] **Add expiry/frozen to AppwriteRow, update read/write logic**

```csharp
// In AppwriteRow class (around line 240), add:
public string Expiry { get; set; }     // stored as ISO string in Appwrite
public bool Frozen { get; set; }

// In GetProductsAsync where Product is constructed (around line 85), change to:
return allRows.Select(r =>
{
    DateTime? expiry = null;
    if (!string.IsNullOrEmpty(r.Expiry) && DateTime.TryParse(r.Expiry, out var parsed))
        expiry = parsed;

    return new Product(r.Name, r.Category, r.Quantity)
    {
        RowId = r.Id,
        ExpiryDate = expiry,
        IsFrozen = r.Frozen
    };
}).ToList();

// In AddProductAsync request body (around line 125), add expiry + frozen:
data = new
{
    name = product.Name,
    quantity = product.Quantity,
    category = product.Category,
    expiry = product.ExpiryDate?.ToString("o"),
    frozen = product.IsFrozen
}

// In UpdateProductAsync request body (around line 185), add expiry + frozen:
data = new
{
    quantity = product.Quantity,
    category = product.Category,
    expiry = product.ExpiryDate?.ToString("o"),
    frozen = product.IsFrozen
}
```

- [ ] **Commit**

```bash
git add FridgeScan/Services/ProductService.cs
git commit -m "feat(service): map expiry and frozen fields from Appwrite"
```

---

### Task 3: ProductsPage — M3 redesign with new product row layout

**Files:**
- Modify: `FridgeScan/Views/ProductsPage.xaml`

- [ ] **Rewrite ProductsPage.xaml with M3 styling and new row layout**

Replace the entire content of ProductsPage.xaml:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage
    x:Class="FridgeScan.Views.ProductsPage"
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:behaviours="clr-namespace:FridgeScan.Behaviours"
    xmlns:converters="clr-namespace:FridgeScan.Converters"
    xmlns:editors="clr-namespace:Syncfusion.Maui.Inputs;assembly=Syncfusion.Maui.Inputs"
    xmlns:models="clr-namespace:FridgeScan.Models"
    xmlns:pulltoRefresh="clr-namespace:Syncfusion.Maui.PullToRefresh;assembly=Syncfusion.Maui.PullToRefresh"
    xmlns:sf="clr-namespace:Syncfusion.Maui.ListView;assembly=Syncfusion.Maui.ListView"
    xmlns:toolkit="http://schemas.microsoft.com/dotnet/2022/maui/toolkit"
    xmlns:vm="clr-namespace:FridgeScan.ViewModels"
    x:Name="ProductsPageRoot"
    x:DataType="vm:ProductsViewModel"
    BackgroundColor="#0D0D2B">

    <ContentPage.Resources>
        <ResourceDictionary>
            <converters:ExpandCollapseIconConverter x:Key="ExpandCollapseIconConverter" />
        </ResourceDictionary>
    </ContentPage.Resources>

    <Grid Padding="10">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>

        <!--  AUTOCOMPLETE + BARCODE  -->
        <Grid>
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*" />
                <ColumnDefinition Width="Auto" />
            </Grid.ColumnDefinitions>
            <editors:SfAutocomplete
                Completed="SfAutocomplete_Completed"
                DisplayMemberPath="Name"
                ItemsSource="{Binding GrocerySuggestions}"
                MaximumSuggestion="3"
                MinimumPrefixCharacters="1"
                Placeholder="+ Add Item"
                SelectedItem="{Binding SelectedGrocerySuggestion}"
                Text="{Binding NewItemName, Mode=TwoWay}"
                TextMemberPath="Name">
                <editors:SfAutocomplete.FilterBehavior>
                    <behaviours:SearchBehavior />
                </editors:SfAutocomplete.FilterBehavior>
            </editors:SfAutocomplete>
            <Label
                Grid.Column="1"
                FontFamily="Material"
                FontSize="24"
                HeightRequest="30"
                HorizontalTextAlignment="Center"
                Text="barcode"
                VerticalTextAlignment="Center"
                WidthRequest="30"
                ZIndex="10">
                <Label.GestureRecognizers>
                    <TapGestureRecognizer Command="{Binding BarcodeCommand}" CommandParameter="{Binding}" />
                </Label.GestureRecognizers>
            </Label>
        </Grid>

        <Entry
            x:Name="hiddenEntry"
            IsVisible="False"
            Text="{Binding NewItemName, Mode=TwoWay}" />

        <pulltoRefresh:SfPullToRefresh
            x:Name="pullToRefresh"
            Grid.Row="1"
            IsRefreshing="False"
            PullingThreshold="150"
            RefreshViewHeight="50"
            RefreshViewThreshold="30"
            RefreshViewWidth="50"
            Refreshing="pullToRefresh_Refreshing"
            TransitionMode="SlideOnTop">
            <pulltoRefresh:SfPullToRefresh.PullableContent>
                <Grid x:Name="mainGrid">
                    <sf:SfListView
                        x:Name="listView"
                        x:DataType="vm:ProductsViewModel"
                        AutoFitMode="DynamicHeight"
                        IsStickyGroupHeader="True"
                        ItemsSource="{Binding GroupedProducts}"
                        SelectionMode="None">
                        <sf:SfListView.ItemTemplate>
                            <DataTemplate x:DataType="models:ListViewFoodCategory">
                                <Grid>
                                    <Grid.RowDefinitions>
                                        <RowDefinition Height="30" />
                                        <RowDefinition Height="Auto" />
                                    </Grid.RowDefinitions>

                                    <!-- Category group header (Option A: subtle section label) -->
                                    <Grid Grid.Row="0" Padding="4,0,4,0">
                                        <Grid.GestureRecognizers>
                                            <TapGestureRecognizer Command="{Binding ToggleExpandCommand}" />
                                        </Grid.GestureRecognizers>
                                        <Grid.ColumnDefinitions>
                                            <ColumnDefinition Width="Auto" />
                                            <ColumnDefinition Width="Auto" />
                                            <ColumnDefinition Width="*" />
                                            <ColumnDefinition Width="Auto" />
                                        </Grid.ColumnDefinitions>
                                        <Label
                                            Grid.Column="0"
                                            FontSize="12"
                                            FontAttributes="Bold"
                                            CharacterSpacing="0.5"
                                            TextColor="#8888AA"
                                            Text="{Binding FoodCategory}" />
                                        <Label
                                            Grid.Column="1"
                                            Margin="4,0,0,0"
                                            FontSize="11"
                                            TextColor="#666688"
                                            Text="{Binding FoodMenuCollection.Count, StringFormat='· {0} items'}" />
                                        <Label
                                            Grid.Column="3"
                                            FontFamily="Material"
                                            FontSize="12"
                                            TextColor="#666688"
                                            Text="{Binding IsExpanded, Converter={StaticResource ExpandCollapseIconConverter}}" />
                                    </Grid>

                                    <!-- Product list (visible when expanded) -->
                                    <StackLayout
                                        Grid.Row="1"
                                        BindableLayout.ItemsSource="{Binding FoodMenuCollection}"
                                        IsVisible="{Binding IsExpanded}">
                                        <BindableLayout.ItemTemplate>
                                            <DataTemplate x:DataType="models:Product">
                                                <Border
                                                    BackgroundColor="#14142E"
                                                    StrokeShape="RoundRectangle 12"
                                                    Stroke="Transparent"
                                                    Padding="12,0"
                                                    Margin="0,0,0,6"
                                                    HeightRequest="56">
                                                    <Border.GestureRecognizers>
                                                        <TapGestureRecognizer Command="{Binding ToggleSelectCommand}" />
                                                    </Border.GestureRecognizers>
                                                    <Grid ColumnDefinitions="*,Auto,Auto,Auto" VerticalOptions="Center">
                                                        <!-- Product name -->
                                                        <Label
                                                            Grid.Column="0"
                                                            FontFamily="Roboto-Regular"
                                                            FontSize="14"
                                                            TextColor="White"
                                                            VerticalOptions="Center"
                                                            LineBreakMode="TailTruncation"
                                                            Text="{Binding Name}" />

                                                        <!-- Frozen icon (if frozen) -->
                                                        <Label
                                                            Grid.Column="1"
                                                            FontFamily="Material"
                                                            FontSize="16"
                                                            TextColor="#8888AA"
                                                            VerticalOptions="Center"
                                                            IsVisible="{Binding ShowFrozenIcon}"
                                                            Text="ac_unit" />

                                                        <!-- Expiry badge -->
                                                        <Border
                                                            Grid.Column="2"
                                                            Margin="6,0,0,0"
                                                            StrokeShape="RoundRectangle 10"
                                                            Stroke="Transparent"
                                                            Padding="6,2,6,2"
                                                            IsVisible="{Binding ShowExpiryBadge}"
                                                            BackgroundColor="{Binding ExpiryColor}"
                                                            VerticalOptions="Center">
                                                            <Label
                                                                FontSize="11"
                                                                FontAttributes="Bold"
                                                                TextColor="White"
                                                                Text="{Binding ExpiryDisplayText}" />
                                                        </Border>

                                                        <!-- Edit button -->
                                                        <Border
                                                            Grid.Column="3"
                                                            Margin="6,0,0,0"
                                                            BackgroundColor="#1E1E3A"
                                                            StrokeShape="RoundRectangle 20"
                                                            Stroke="Transparent"
                                                            WidthRequest="40"
                                                            HeightRequest="40"
                                                            VerticalOptions="Center">
                                                            <Border.GestureRecognizers>
                                                                <TapGestureRecognizer Command="{Binding Source={RelativeSource AncestorType={x:Type vm:ProductsViewModel}}, Path=EditProductCommand}" CommandParameter="{Binding}" />
                                                            </Border.GestureRecognizers>
                                                            <Label
                                                                Text="&#xe3c9;"
                                                                FontFamily="Material"
                                                                FontSize="18"
                                                                TextColor="#CCCCDD"
                                                                HorizontalOptions="Center"
                                                                VerticalOptions="Center" />
                                                        </Border>
                                                    </Grid>
                                                </Border>
                                            </DataTemplate>
                                        </BindableLayout.ItemTemplate>
                                    </StackLayout>
                                </Grid>
                            </DataTemplate>
                        </sf:SfListView.ItemTemplate>
                    </sf:SfListView>
                </Grid>
            </pulltoRefresh:SfPullToRefresh.PullableContent>
        </pulltoRefresh:SfPullToRefresh>
    </Grid>
</ContentPage>
```

- [ ] **Commit**

```bash
git add FridgeScan/Views/ProductsPage.xaml
git commit -m "feat(ui): M3 redesign of ProductsPage with expiry badges and edit button"
```

---

### Task 4: ProductsViewModel — add EditProductCommand, remove quantity handler, add ToggleExpandCommand to ListViewFoodCategory

**Files:**
- Modify: `FridgeScan/ViewModels/ProductsViewModel.cs`
- Modify: `FridgeScan/Models/ListViewFoodCategory.cs`

- [ ] **Update ListViewFoodCategory with ToggleExpandCommand**

Add a relay command to `ListViewFoodCategory.cs`:

```csharp
[RelayCommand]
private void ToggleExpand()
{
    IsExpanded = !IsExpanded;
}
```

Also add the required using: `using CommunityToolkit.Mvvm.Input;` at the top.

Since ListViewFoodCategory doesn't inherit from ObservableObject, change it to inherit from `ObservableObject` instead of manual INotifyPropertyChanged. Or simpler: just add the command manually.

Actually, looking at it more carefully, `ListViewFoodCategory` uses manual `INotifyPropertyChanged`. To keep things simple, let's add a manual command:

```csharp
public ICommand ToggleExpandCommand { get; }

// In constructor:
this.ToggleExpandCommand = new Command(() => IsExpanded = !IsExpanded);
```

And add `using System.Windows.Input;` at the top.

- [ ] **Update ProductsViewModel — add EditProductCommand, clean up quantity handler**

Remove the `WeakReferenceMessenger.Default.Register<PropertyChangedMessage<int>>` registration and the `OnQuantityChanged`/`HandleQuantityChanged` methods. Instead, make `AddItem` explicitly sync when incrementing an existing product:

```csharp
// In AddItem, replace the existing duplicate path:
if (existing != null)
{
    existing.Quantity += 1;
    await productService.UpdateProductAsync(existing);
    return;
}
```

Add the `EditProductCommand`:

```csharp
[RelayCommand]
private async Task EditProduct(Product product)
{
    if (product == null) return;
    await Shell.Current.GoToAsync(nameof(ProductDetailPage), new Dictionary<string, object>
    {
        { "productId", product.RowId }
    });
}
```

Also update the messenger import — remove `using CommunityToolkit.Mvvm.Messaging;` and `using CommunityToolkit.Mvvm.Messaging.Messages;` if they're no longer needed.

- [ ] **Commit**

```bash
git add FridgeScan/ViewModels/ProductsViewModel.cs FridgeScan/Models/ListViewFoodCategory.cs
git commit -m "feat(vm): add EditProductCommand, remove quantity messenger handler, add ToggleExpand"
```

---

### Task 5: ProductDetailViewModel — new ViewModel for editing

**Files:**
- Create: `FridgeScan/ViewModels/ProductDetailViewModel.cs`

- [ ] **Create ProductDetailViewModel**

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FridgeScan.Models;
using FridgeScan.Services;

namespace FridgeScan.ViewModels;

public partial class ProductDetailViewModel : BaseViewModel, IQueryAttributable
{
    private readonly ProductService productService;
    private readonly ProductsManager productsManager;
    private Product originalProduct;

    [ObservableProperty]
    private string productName;

    [ObservableProperty]
    private int quantity;

    [ObservableProperty]
    private DateTime? expiryDate;

    [ObservableProperty]
    private bool isFrozen;

    public ProductDetailViewModel(ProductService productService, ProductsManager productsManager)
    {
        this.productService = productService;
        this.productsManager = productsManager;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("productId", out var id))
        {
            var productId = id?.ToString();
            if (string.IsNullOrEmpty(productId)) return;

            originalProduct = productsManager.Products.FirstOrDefault(p => p.RowId == productId);
            if (originalProduct != null)
            {
                ProductName = originalProduct.Name;
                Quantity = originalProduct.Quantity;
                ExpiryDate = originalProduct.ExpiryDate;
                IsFrozen = originalProduct.IsFrozen;
            }
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        if (originalProduct == null) return;

        originalProduct.Name = ProductName;
        originalProduct.Quantity = Quantity;
        originalProduct.ExpiryDate = ExpiryDate;
        originalProduct.IsFrozen = IsFrozen;

        await productService.UpdateProductAsync(originalProduct);
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private void ClearExpiry()
    {
        ExpiryDate = null;
    }

    [RelayCommand]
    private async Task Delete()
    {
        if (originalProduct == null) return;

        bool confirmed = await Shell.Current.DisplayAlert(
            "Delete Product",
            $"Are you sure you want to delete \"{originalProduct.Name}\"?",
            "Delete", "Cancel");

        if (!confirmed) return;

        productsManager.RemoveProduct(originalProduct);
        await productService.DeleteProductAsync(originalProduct.RowId);
        await Shell.Current.GoToAsync("//products");
    }
}
```

- [ ] **Commit**

```bash
git add FridgeScan/ViewModels/ProductDetailViewModel.cs
git commit -m "feat(vm): add ProductDetailViewModel for editing product fields"
```

---

### Task 6: ProductDetailPage — new XAML page for editing

**Files:**
- Create: `FridgeScan/Views/ProductDetailPage.xaml`
- Create: `FridgeScan/Views/ProductDetailPage.xaml.cs`

- [ ] **Create ProductDetailPage.xaml**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage
    x:Class="FridgeScan.Views.ProductDetailPage"
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:vm="clr-namespace:FridgeScan.ViewModels"
    xmlns:toolkit="http://schemas.microsoft.com/dotnet/2022/maui/toolkit"
    x:DataType="vm:ProductDetailViewModel"
    BackgroundColor="#0D0D2B"
    Title="Edit Product">

    <Grid RowDefinitions="Auto,*" Padding="20,16,20,20">

        <!-- Header -->
        <Grid Grid.Row="0"
              ColumnDefinitions="48,*,Auto"
              Padding="0,0,0,24">
            <!-- Back arrow -->
            <Label Grid.Column="0"
                   Text="&#xe5c4;"
                   FontFamily="Material"
                   FontSize="24"
                   TextColor="#CCCCDD"
                   WidthRequest="48"
                   HeightRequest="48"
                   Margin="-12,0,0,0"
                   HorizontalTextAlignment="Center"
                   VerticalTextAlignment="Center">
                <Label.GestureRecognizers>
                    <TapGestureRecognizer Tapped="OnBackClicked" />
                </Label.GestureRecognizers>
            </Label>

            <!-- Title -->
            <Label Grid.Column="1"
                   Text="Edit Product"
                   FontSize="22"
                   FontAttributes="Bold"
                   TextColor="White"
                   VerticalOptions="Center" />
        </Grid>

        <!-- Form -->
        <ScrollView Grid.Row="1">
            <VerticalStackLayout Spacing="20">

                <!-- NAME -->
                <VerticalStackLayout Spacing="6">
                    <Label Text="NAME"
                           FontSize="12"
                           FontAttributes="Bold"
                           TextColor="#8888AA"
                           CharacterSpacing="0.5" />
                    <Border BackgroundColor="#14142E"
                            StrokeShape="RoundRectangle 12"
                            Stroke="Transparent"
                            Padding="12,0"
                            HeightRequest="48">
                        <Entry Text="{Binding ProductName}"
                               BackgroundColor="Transparent"
                               TextColor="White"
                               FontSize="16"
                               Placeholder="Product name"
                               PlaceholderColor="#666688" />
                    </Border>
                </VerticalStackLayout>

                <!-- QUANTITY -->
                <VerticalStackLayout Spacing="6">
                    <Label Text="QUANTITY"
                           FontSize="12"
                           FontAttributes="Bold"
                           TextColor="#8888AA"
                           CharacterSpacing="0.5" />
                    <HorizontalStackLayout Spacing="12" VerticalOptions="Center">
                        <Border BackgroundColor="#1E1E3A"
                                StrokeShape="RoundRectangle 20"
                                Stroke="Transparent"
                                WidthRequest="40"
                                HeightRequest="40">
                            <Border.GestureRecognizers>
                                <TapGestureRecognizer Command="{Binding DecreaseQuantityCommand}" />
                            </Border.GestureRecognizers>
                            <Label Text="&#xe15b;"
                                   FontFamily="Material"
                                   FontSize="20"
                                   TextColor="#CCCCDD"
                                   HorizontalOptions="Center"
                                   VerticalOptions="Center" />
                        </Border>
                        <Label Text="{Binding Quantity}"
                               FontSize="20"
                               FontAttributes="Bold"
                               TextColor="White"
                               WidthRequest="36"
                               HorizontalTextAlignment="Center"
                               VerticalOptions="Center" />
                        <Border BackgroundColor="#1E1E3A"
                                StrokeShape="RoundRectangle 20"
                                Stroke="Transparent"
                                WidthRequest="40"
                                HeightRequest="40">
                            <Border.GestureRecognizers>
                                <TapGestureRecognizer Command="{Binding IncreaseQuantityCommand}" />
                            </Border.GestureRecognizers>
                            <Label Text="&#xe145;"
                                   FontFamily="Material"
                                   FontSize="20"
                                   TextColor="#CCCCDD"
                                   HorizontalOptions="Center"
                                   VerticalOptions="Center" />
                        </Border>
                    </HorizontalStackLayout>
                </VerticalStackLayout>

                <!-- EXPIRY DATE -->
                <VerticalStackLayout Spacing="6">
                    <Label Text="EXPIRY DATE"
                           FontSize="12"
                           FontAttributes="Bold"
                           TextColor="#8888AA"
                           CharacterSpacing="0.5" />
                    <Border BackgroundColor="#14142E"
                            StrokeShape="RoundRectangle 12"
                            Stroke="Transparent"
                            Padding="12,0"
                            HeightRequest="48">
                        <Grid ColumnDefinitions="*,Auto" VerticalOptions="Center">
                            <DatePicker Grid.Column="0"
                                        Date="{Binding ExpiryDate}"
                                        TextColor="White"
                                        FontSize="16"
                                        BackgroundColor="Transparent" />
                            <Label Grid.Column="1"
                                   Text="&#xe5c9;"
                                   FontFamily="Material"
                                   FontSize="18"
                                   TextColor="#666"
                                   VerticalOptions="Center">
                                <Label.GestureRecognizers>
                                    <TapGestureRecognizer Command="{Binding ClearExpiryCommand}" />
                                </Label.GestureRecognizers>
                            </Label>
                        </Grid>
                    </Border>
                    <Label Text="Clear expiry date"
                           FontSize="12"
                           TextColor="#ff6b6b"
                           HorizontalOptions="Start"
                           Padding="4,2">
                        <Label.GestureRecognizers>
                            <TapGestureRecognizer Command="{Binding ClearExpiryCommand}" />
                        </Label.GestureRecognizers>
                    </Label>
                </VerticalStackLayout>

                <!-- FROZEN TOGGLE -->
                <VerticalStackLayout Spacing="6">
                    <Label Text="STORAGE"
                           FontSize="12"
                           FontAttributes="Bold"
                           TextColor="#8888AA"
                           CharacterSpacing="0.5" />
                    <Border BackgroundColor="#14142E"
                            StrokeShape="RoundRectangle 12"
                            Stroke="Transparent"
                            Padding="12,0"
                            HeightRequest="48">
                        <Grid ColumnDefinitions="Auto,*,Auto" ColumnSpacing="10" VerticalOptions="Center">
                            <Label Grid.Column="0"
                                   Text="ac_unit"
                                   FontFamily="Material"
                                   FontSize="18"
                                   TextColor="#8888AA" />
                            <Label Grid.Column="1"
                                   Text="Frozen"
                                   FontSize="16"
                                   TextColor="White"
                                   VerticalOptions="Center" />
                            <Switch Grid.Column="2"
                                    IsToggled="{Binding IsFrozen}"
                                    OnColor="#27AE60" />
                        </Grid>
                    </Border>
                </VerticalStackLayout>

                <!-- SAVE BUTTON -->
                <Border BackgroundColor="#1E1E3A"
                        StrokeShape="RoundRectangle 20"
                        Stroke="Transparent"
                        HeightRequest="48"
                        Padding="18,0">
                    <Border.GestureRecognizers>
                        <TapGestureRecognizer Command="{Binding SaveCommand}" />
                    </Border.GestureRecognizers>
                    <Label Text="Save"
                           FontSize="14"
                           FontAttributes="Bold"
                           TextColor="#CCCCDD"
                           VerticalOptions="Center"
                           HorizontalOptions="Center" />
                </Border>

                <!-- DELETE BUTTON -->
                <Border BackgroundColor="#2A1E1E"
                        StrokeShape="RoundRectangle 20"
                        Stroke="Transparent"
                        HeightRequest="48"
                        Padding="18,0"
                        Margin="0,0,0,40">
                    <Border.GestureRecognizers>
                        <TapGestureRecognizer Command="{Binding DeleteCommand}" />
                    </Border.GestureRecognizers>
                    <Label Text="Delete Product"
                           FontSize="14"
                           FontAttributes="Bold"
                           TextColor="#ff6b6b"
                           VerticalOptions="Center"
                           HorizontalOptions="Center" />
                </Border>

            </VerticalStackLayout>
        </ScrollView>
    </Grid>
</ContentPage>
```

- [ ] **Create ProductDetailPage.xaml.cs**

```csharp
using FridgeScan.ViewModels;

namespace FridgeScan.Views;

public partial class ProductDetailPage : ContentPage
{
    public ProductDetailPage(ProductDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
```

- [ ] **Commit**

```bash
git add FridgeScan/Views/ProductDetailPage.xaml FridgeScan/Views/ProductDetailPage.xaml.cs
git commit -m "feat(ui): add ProductDetailPage for editing product fields"
```

---

### Task 7: Add DecreaseQuantity/IncreaseQuantity commands to ProductDetailViewModel

**Files:**
- Modify: `FridgeScan/ViewModels/ProductDetailViewModel.cs`

- [ ] **Add quantity stepper commands**

```csharp
[RelayCommand]
private void DecreaseQuantity()
{
    if (Quantity > 0)
        Quantity--;
}

[RelayCommand]
private void IncreaseQuantity()
{
    Quantity++;
}
```

Also add the `using CommunityToolkit.Mvvm.Input;` if not already present (it's likely already there via the `[RelayCommand]` base).

- [ ] **Commit**

```bash
git add FridgeScan/ViewModels/ProductDetailViewModel.cs
git commit -m "feat(vm): add quantity stepper commands to ProductDetailViewModel"
```

---

### Task 8: Register DI and Shell routing

**Files:**
- Modify: `FridgeScan/MauiProgram.cs`
- Modify: `FridgeScan/AppShell.xaml.cs`

- [ ] **Register ProductDetailViewModel and ProductDetailPage in MauiProgram.cs**

```csharp
// After existing ViewModel registrations:
builder.Services.AddSingleton<ProductDetailViewModel>();

// After existing Page registrations:
builder.Services.AddTransient<Views.ProductDetailPage>();
```

- [ ] **Register route in AppShell.xaml.cs**

```csharp
using FridgeScan.Views;

namespace FridgeScan;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(RecipeDetailsPage), typeof(RecipeDetailsPage));
        Routing.RegisterRoute(nameof(SharedRecipePage), typeof(SharedRecipePage));
        Routing.RegisterRoute(nameof(CookbookDetailPage), typeof(CookbookDetailPage));
        Routing.RegisterRoute(nameof(RecipePreviewPage), typeof(RecipePreviewPage));
        Routing.RegisterRoute(nameof(SavedRecipeDetailPage), typeof(SavedRecipeDetailPage));
        Routing.RegisterRoute(nameof(ProductDetailPage), typeof(ProductDetailPage));
    }
}
```

- [ ] **Commit**

```bash
git add FridgeScan/MauiProgram.cs FridgeScan/AppShell.xaml.cs
git commit -m "feat(di): register ProductDetailPage + ViewModel in DI and shell routing"
```

---

### Task 9: Handle DatePicker null — DatePicker doesn't support nullable Date, wrap in ViewModel

**Files:**
- Modify: `FridgeScan/ViewModels/ProductDetailViewModel.cs`

- [ ] **Handle DatePicker non-nullable Date property**

MAUI `DatePicker.Date` is not nullable. We need to handle the empty state. Update the `ExpiryDate` property to work with `DatePicker`:

```csharp
// Replace the [ObservableProperty] private DateTime? expiryDate; with:
[ObservableProperty]
private DateTime expiryDateValue = DateTime.Today;

[ObservableProperty]
private bool hasExpiryDate;

partial void OnHasExpiryDateChanged(bool value)
{
    if (!value)
        ExpiryDateValue = DateTime.Today;
}

public void ApplyQueryAttributes(IDictionary<string, object> query)
{
    if (query.TryGetValue("productId", out var id))
    {
        var productId = id?.ToString();
        if (string.IsNullOrEmpty(productId)) return;

        originalProduct = productsManager.Products.FirstOrDefault(p => p.RowId == productId);
        if (originalProduct != null)
        {
            ProductName = originalProduct.Name;
            Quantity = originalProduct.Quantity;
            HasExpiryDate = originalProduct.ExpiryDate.HasValue;
            if (originalProduct.ExpiryDate.HasValue)
                ExpiryDateValue = originalProduct.ExpiryDate.Value;
            IsFrozen = originalProduct.IsFrozen;
        }
    }
}

[RelayCommand]
private void ClearExpiry()
{
    HasExpiryDate = false;
}

[RelayCommand]
private async Task Save()
{
    if (originalProduct == null) return;

    originalProduct.Name = ProductName;
    originalProduct.Quantity = Quantity;
    originalProduct.ExpiryDate = HasExpiryDate ? ExpiryDateValue : null;
    originalProduct.IsFrozen = IsFrozen;

    await productService.UpdateProductAsync(originalProduct);
    await Shell.Current.GoToAsync("..");
}
```

And update the XAML so the DatePicker is only visible when `HasExpiryDate` is true, and the clear button always visible:

```xml
<!-- EXPIRY DATE -->
<VerticalStackLayout Spacing="6">
    <Label Text="EXPIRY DATE"
           FontSize="12"
           FontAttributes="Bold"
           TextColor="#8888AA"
           CharacterSpacing="0.5" />
    <Border BackgroundColor="#14142E"
            StrokeShape="RoundRectangle 12"
            Stroke="Transparent"
            Padding="12,0"
            HeightRequest="48">
        <Grid ColumnDefinitions="*,Auto" VerticalOptions="Center">
            <DatePicker Grid.Column="0"
                        Date="{Binding ExpiryDateValue}"
                        IsVisible="{Binding HasExpiryDate}"
                        TextColor="White"
                        FontSize="16"
                        BackgroundColor="Transparent" />
            <Label Grid.Column="0"
                   Text="No expiry set"
                   TextColor="#666688"
                   FontSize="14"
                   IsVisible="{Binding HasExpiryDate, Converter={StaticResource InvertBoolConverter}}" />
            <Label Grid.Column="1"
                   Text="&#xe5c9;"
                   FontFamily="Material"
                   FontSize="18"
                   TextColor="#666"
                   VerticalOptions="Center">
                <Label.GestureRecognizers>
                    <TapGestureRecognizer Command="{Binding ClearExpiryCommand}" />
                </Label.GestureRecognizers>
            </Label>
        </Grid>
    </Border>
</VerticalStackLayout>
```

Add an `InvertBoolConverter` to the page or as a global resource. Since there might already be one, or we need to create it. Let's add it as a page-level resource in ProductDetailPage.xaml.

Actually, we can use the `CommunityToolkit.Maui` which has `InvertedBoolConverter`. Let's check if it's available.

From CommunityToolkit.Maui, we can use:
```xml
<toolkit:InvertedBoolConverter x:Key="InvertBoolConverter" />
```

Where `toolkit` is already imported as `xmlns:toolkit="http://schemas.microsoft.com/dotnet/2022/maui/toolkit"`.

- [ ] **Update ProductDetailPage.xaml with HasExpiryDate logic and inverted bool converter**

Add to ContentPage.Resources:
```xml
<ContentPage.Resources>
    <ResourceDictionary>
        <toolkit:InvertedBoolConverter x:Key="InvertBoolConverter" />
    </ResourceDictionary>
</ContentPage.Resources>
```

Replace the EXPIRY DATE section with the version above.

- [ ] **Commit**

```bash
git add FridgeScan/ViewModels/ProductDetailViewModel.cs FridgeScan/Views/ProductDetailPage.xaml
git commit -m "fix(ui): handle DatePicker null state with HasExpiryDate toggle"
```
