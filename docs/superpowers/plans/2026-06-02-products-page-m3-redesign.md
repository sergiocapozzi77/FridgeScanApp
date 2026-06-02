# ProductsPage M3 Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Restyle ProductsPage.xaml to Material 3 Expressive dark theme with tonal surfaces, larger rounded corners, unified 48dp heights, and updated color tokens.

**Architecture:** Purely visual XAML changes to the existing SfListView-based grouped product list. One code-behind change to Product.cs for the updated ExpiryColor values. No structural or logic changes to the ViewModel, models, or services.

**Tech Stack:** .NET MAUI, Syncfusion MAUI (SfListView, SfAutocomplete), CommunityToolkit.Mvvm

---

### Task 1: Update ExpiryColor in Product.cs

**Files:**
- Modify: `FridgeScan/Models/Product.cs:60-64`

The current `ExpiryColor` property uses bright red/orange. Update it to return the new M3 tonal background colors.

- [ ] **Step 1: Modify ExpiryColor to return M3 tonal colors**

Replace the existing property with the 3-state version (expired / today / good):

```csharp
public Color ExpiryColor => DaysUntilExpiry switch
{
    < 0 => Color.FromArgb("#2E1E1E"),   // Error surface (tonal red)
    0   => Color.FromArgb("#3A2E28"),   // Warning surface (tonal amber)
    _   => Color.FromArgb("#2A2E58"),   // Surface container high (tonal neutral)
};
```

- [ ] **Step 2: Verify the file builds**

```bash
cd C:\Users\sergi\source\repos\FridgeScanApp && dotnet build FridgeScan/FridgeScan.csproj -f net9.0-android 2>&1 | tail -20
```

Expected: Build succeeds with no errors.

- [ ] **Step 3: Commit**

```bash
cd C:\Users\sergi\source\repos\FridgeScanApp
git add FridgeScan/Models/Product.cs
git commit -m "feat: update ExpiryColor to M3 tonal palette

Expired → #2E1E1E (tonal red), Today → #3A2E28 (tonal amber),
Good → #2A2E58 (tonal neutral). Prepares for M3 product card restyle.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 2: Restyle Page Background and Outer Layout

**Files:**
- Modify: `FridgeScan/Views/ProductsPage.xaml` (lines 16, 24-30)

Update the page background color from `#0D0D2B` to `#0D1023` and adjust the outer Grid padding.

- [ ] **Step 1: Update page background and outer Grid padding**

Change the ContentPage BackgroundColor and the outer Grid Padding:

```xml
<!-- Line 16: Page background -->
BackgroundColor="#0D1023">
```

```xml
<!-- Lines 26-30: Outer grid padding from 10 to 12 -->
<Grid Padding="12,0" Grid.Row="0">
<Grid.RowDefinitions>
    <RowDefinition Height="Auto" />
    <RowDefinition Height="*" />
</Grid.RowDefinitions>
```

- [ ] **Step 2: Commit**

```bash
cd C:\Users\sergi\source\repos\FridgeScanApp
git add FridgeScan/Views/ProductsPage.xaml
git commit -m "feat: update page background to M3 navy and adjust padding

Page bg #0D0D2B → #0D1023. Outer Grid padding 10 → 12dp.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 3: Add Large Expressive Header

**Files:**
- Modify: `FridgeScan/Views/ProductsPage.xaml` (lines 32-71, the autocomplete + barcode area)

Replace the current minimal header/autocomplete row with the new:
1. Large expressive header with title, subtitle, and tonal icon buttons
2. The SfAutocomplete wrapped inside a tonal card with + icon prefix

The current structure is:

```xml
<!-- Lines 32-71: Current autocomplete + barcode area -->
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*" />
        <ColumnDefinition Width="Auto" />
    </Grid.ColumnDefinitions>
    <editors:SfAutocomplete ... />
    <Label Grid.Column="1" ... Text="barcode" ... />
</Grid>
<Entry x:Name="hiddenEntry" ... />
```

Replace with:

```xml
<!-- Large expressive header -->
<StackLayout Grid.Row="0" Spacing="12" Padding="0,20,0,0">
    <!-- Top row: title + action buttons -->
    <Grid ColumnDefinitions="*,Auto,Auto" Padding="4,0,4,0">
        <!-- Title -->
        <VerticalStackLayout Grid.Column="0" Spacing="4">
            <Label Text="Inventory"
                   FontSize="28"
                   FontAttributes="Bold"
                   TextColor="White"
                   LetterSpacing="-0.3" />
            <Label Text="Keep track of products and expiry dates"
                   FontSize="13"
                   TextColor="#8888AA" />
        </VerticalStackLayout>

        <!-- Search button -->
        <Border Grid.Column="1"
                BackgroundColor="#1E1E3A"
                StrokeShape="RoundRectangle 20"
                Stroke="Transparent"
                WidthRequest="40"
                HeightRequest="40"
                Margin="8,0,0,0">
            <Label Text="&#xe8b6;"
                   FontFamily="Material"
                   FontSize="18"
                   TextColor="#CCCCDD"
                   HorizontalOptions="Center"
                   VerticalOptions="Center" />
        </Border>

        <!-- Barcode button -->
        <Border Grid.Column="2"
                BackgroundColor="#1E1E3A"
                StrokeShape="RoundRectangle 20"
                Stroke="Transparent"
                WidthRequest="40"
                HeightRequest="40"
                Margin="8,0,0,0">
            <Border.GestureRecognizers>
                <TapGestureRecognizer Command="{Binding BarcodeCommand}" CommandParameter="{Binding}" />
            </Border.GestureRecognizers>
            <Label Text="barcode"
                   FontFamily="Material"
                   FontSize="18"
                   TextColor="#CCCCDD"
                   HorizontalOptions="Center"
                   VerticalOptions="Center" />
        </Border>
    </Grid>

    <!-- Add item card (tonal container wrapping SfAutocomplete) -->
    <Border BackgroundColor="#202448"
            StrokeShape="RoundRectangle 14"
            Stroke="Transparent"
            HeightRequest="48"
            Padding="12,0">
        <Grid ColumnDefinitions="Auto,*" VerticalOptions="Center">
            <!-- + icon container -->
            <Border BackgroundColor="#2A2E58"
                    StrokeShape="RoundRectangle 10"
                    Stroke="Transparent"
                    WidthRequest="32"
                    HeightRequest="32"
                    VerticalOptions="Center">
                <Label Text="+"
                       FontSize="18"
                       TextColor="#D0BCFF"
                       FontAttributes="Bold"
                       HorizontalOptions="Center"
                       VerticalOptions="Center" />
            </Border>

            <!-- Autocomplete input -->
            <editors:SfAutocomplete Grid.Column="1"
                                     Completed="SfAutocomplete_Completed"
                                     DisplayMemberPath="Name"
                                     ItemsSource="{Binding GrocerySuggestions}"
                                     MaximumSuggestion="3"
                                     MinimumPrefixCharacters="1"
                                     Placeholder="Add item"
                                     SelectedItem="{Binding SelectedGrocerySuggestion}"
                                     Text="{Binding NewItemName, Mode=TwoWay}"
                                     TextMemberPath="Name"
                                     Margin="10,0,0,0"
                                     VerticalOptions="Center">
                <editors:SfAutocomplete.FilterBehavior>
                    <behaviours:SearchBehavior />
                </editors:SfAutocomplete.FilterBehavior>
            </editors:SfAutocomplete>
        </Grid>
    </Border>
</StackLayout>

<Entry x:Name="hiddenEntry"
       IsVisible="False"
       Text="{Binding NewItemName, Mode=TwoWay}" />
```

- [ ] **Step 1: Replace the header area in ProductsPage.xaml**

Open `ProductsPage.xaml` and replace lines 32-71 with the new header markup above.

- [ ] **Step 2: Verify the file builds**

```bash
cd C:\Users\sergi\source\repos\FridgeScanApp && dotnet build FridgeScan/FridgeScan.csproj -f net9.0-android 2>&1 | tail -20
```

Expected: Build succeeds with no errors.

- [ ] **Step 3: Commit**

```bash
cd C:\Users\sergi\source\repos\FridgeScanApp
git add FridgeScan/Views/ProductsPage.xaml
git commit -m "feat: add large expressive header and tonal add-item card

New header with Inventory title, subtitle, search and barcode tonal
buttons. Add item SfAutocomplete wrapped in 48dp tonal card (#202448)
with 14dp radius and + icon prefix.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 4: Restyle Product List Cards

**Files:**
- Modify: `FridgeScan/Views/ProductsPage.xaml` (lines 138-209, the product DataTemplate)

Update the product card Border: height 56 → 48dp, corner radius 12 → 14dp, background #14142E → #171A35, margin bottom 10 → 8dp. Also update the edit button from 40dp to 32dp circle.

- [ ] **Step 1: Update the product card border and Grid layout**

Find the product DataTemplate inside the BindableLayout.ItemTemplate (lines 138-209) and update:

```xml
<DataTemplate x:DataType="models:Product">
    <Border
        BackgroundColor="#171A35"
        StrokeShape="RoundRectangle 14"
        Stroke="Transparent"
        Padding="14,0"
        Margin="0,0,0,8"
        HeightRequest="48">
        <Border.GestureRecognizers>
            <TapGestureRecognizer Command="{Binding ToggleSelectCommand}" />
        </Border.GestureRecognizers>
        <Grid ColumnDefinitions="*,Auto,Auto,Auto" VerticalOptions="Center">
            <!-- Product name (unchanged) -->
            <Label
                Grid.Column="0"
                FontFamily="Roboto-Regular"
                FontSize="14"
                TextColor="White"
                VerticalOptions="Center"
                LineBreakMode="TailTruncation"
                Text="{Binding Name}" />

            <!-- Frozen icon (unchanged) -->
            <Label
                Grid.Column="1"
                FontFamily="Material"
                FontSize="16"
                TextColor="#8888AA"
                VerticalOptions="Center"
                IsVisible="{Binding ShowFrozenIcon}"
                Text="ac_unit" />

            <!-- Expiry badge (unchanged structure, colors come from ExpiryColor binding) -->
            <Border
                Grid.Column="2"
                Margin="6,0,0,0"
                StrokeShape="RoundRectangle 8"
                Stroke="Transparent"
                Padding="8,2"
                IsVisible="{Binding ShowExpiryBadge}"
                BackgroundColor="{Binding ExpiryColor}"
                VerticalOptions="Center">
                <Label
                    FontSize="11"
                    FontAttributes="Bold"
                    TextColor="White"
                    Text="{Binding ExpiryDisplayText}" />
            </Border>

            <!-- Edit button (reduced to 32dp) -->
            <Border
                Grid.Column="3"
                Margin="6,0,0,0"
                BackgroundColor="#1E1E3A"
                StrokeShape="RoundRectangle 16"
                Stroke="Transparent"
                WidthRequest="32"
                HeightRequest="32"
                VerticalOptions="Center">
                <Border.GestureRecognizers>
                    <TapGestureRecognizer Tapped="OnEditProductTapped" />
                </Border.GestureRecognizers>
                <Label
                    Text="&#xe3c9;"
                    FontFamily="Material"
                    FontSize="14"
                    TextColor="#CCCCDD"
                    HorizontalOptions="Center"
                    VerticalOptions="Center" />
            </Border>
        </Grid>
    </Border>
</DataTemplate>
```

- [ ] **Step 2: Verify the file builds**

```bash
cd C:\Users\sergi\source\repos\FridgeScanApp && dotnet build FridgeScan/FridgeScan.csproj -f net9.0-android 2>&1 | tail -20
```

Expected: Build succeeds with no errors.

- [ ] **Step 3: Commit**

```bash
cd C:\Users\sergi\source\repos\FridgeScanApp
git add FridgeScan/Views/ProductsPage.xaml
git commit -m "feat: restyle product list cards with M3 tonal surfaces

Card height 56→48dp, radius 12→14dp, bg #14142E→#171A35,
margin-bottom 10→8dp. Edit button 40→32dp circle. Expiry badge
radius 10→8dp. Internal row layout unchanged.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 5: Update Section Headers and Group Header

**Files:**
- Modify: `FridgeScan/Views/ProductsPage.xaml` (lines 100-130, the group header DataTemplate)

Update the category group header to use uppercase, letter-spacing, and updated spacing.

- [ ] **Step 1: Update the group header styling**

Replace the group header Grid (lines 100-130) with the updated version:

```xml
<!-- Category group header (M3 style) -->
<Grid Grid.Row="0" Padding="4,2,4,6">
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
        FontSize="13"
        FontAttributes="Bold"
        CharacterSpacing="0.5"
        TextColor="White"
        Text="{Binding FoodCategory}" />
    <Label
        Grid.Column="1"
        Margin="8,0,0,0"
        FontSize="11"
        TextColor="#8888AA"
        Text="{Binding FoodMenuCollection.Count, StringFormat='· {0} items'}" />
    <Label
        Grid.Column="3"
        FontFamily="Material"
        FontSize="12"
        TextColor="#666688"
        Text="{Binding IsExpanded, Converter={StaticResource ExpandCollapseIconConverter}}" />
</Grid>
```

- [ ] **Step 2: Add top margin to section (12dp) via the parent Grid row**

The Grid row definition for the section header is currently at line 96:
```xml
<RowDefinition Height="30" />
```

Change it to `Auto` — the actual spacing comes from padding within the header Grid itself:
```xml
<RowDefinition Height="Auto" />
```

Also add a top margin on the section content by updating the containing Grid padding on the `StackLayout` that wraps the product cards (after the header). The existing StackLayout for products already has margin bottom on each card. Add a `Margin="0,0,0,0"` or just use the spacing from the header padding.

For top margin between sections (e.g., Meat → Dairy), add `Margin="12,0,0,0"` to the header Grid:

Actually, the simplest approach: the header Grid already has `Padding="4,2,4,6"` — add a top margin of 12dp to the header Grid when it's not the first section. But since all headers use the same template, we need each one to have consistent spacing. Let's add `Margin="0,12,0,0"` to all group headers:

Wait, that would add space above the first section too. Looking at the existing code, the group header already has padding. Let me keep it simple — just update the header padding:

```xml
<Grid Grid.Row="0" Padding="4,2,4,6" Margin="0,12,0,0">
```

This adds 12dp above each section (including the first one after the header). But since the first section is close to the add item card (20dp margin), this is fine.

Actually wait, the first section doesn't need an extra 12dp margin because there's already spacing from the add item card bottom margin. Let me think about this differently.

The template is reused for every section. The row definition is Auto now. The header padding has `Padding="4,2,4,6"`. I think the cleanest approach is to keep `Padding="4,2,4,6"` without an extra Margin — the vertical spacing comes from the product card margins below the previous section's last card.

Actually, looking at the current code, the group header row is HeightRequest 30 which is more than enough. Changing it to Auto should work fine.

Let me keep it simple and just change:
```xml
<RowDefinition Height="30" />
```
to:
```xml
<RowDefinition Height="Auto" />
```

And update the header Grid padding. No extra margin needed for the first section — the spacing already works naturally.

Let me simplify:

```xml
<!-- Category group header (M3 style) -->
<Grid Grid.Row="0" Padding="4,2,4,6" Margin="0,12,0,0">
```

Wait, but the first section will also get this margin. Let me reconsider... 

Actually, the first section is the "Meat" section (or whatever comes first alphabetically). It appears right below the add item card. The add item card has some spacing already. Let me think about whether 12dp margin on the first section would look too much...

In the mockup, I had 12dp spacing between sections (top margin before new section). The first section after the add item card had a smaller gap. Let me just use `Margin="0,12,0,0"` for all sections - it adds 12dp above each section header uniformly. This is consistent with the spec which says "Top margin before section: 12dp".

- [ ] **Step 2: Replace the Grid RowDefinition and update header**

Change line 96:
```xml
<RowDefinition Height="30" />
```
to:
```xml
<RowDefinition Height="Auto" />
```

And update the header Grid with the new markup from Step 1 (which includes `Margin="0,12,0,0"`).

- [ ] **Step 3: Verify the file builds**

```bash
cd C:\Users\sergi\source\repos\FridgeScanApp && dotnet build FridgeScan/FridgeScan.csproj -f net9.0-android 2>&1 | tail -20
```

Expected: Build succeeds with no errors.

- [ ] **Step 4: Commit**

```bash
cd C:\Users\sergi\source\repos\FridgeScanApp
git add FridgeScan/Views/ProductsPage.xaml
git commit -m "feat: update section headers to M3 typography and spacing

Headers now use uppercase 13sp Bold with letter-spacing, white text,
updated spacing with 12dp top margin between sections.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Self-Review Checklist

1. **Spec coverage:** All spec requirements covered — page bg, header, add item card, product cards, expiry badges, edit buttons, section headers. Bottom nav bar styling is handled by the existing FloatingBottomBar (spec says visual styling only if needed).
2. **Placeholder scan:** No TBD, TODO, or incomplete steps. Every step has complete code.
3. **Type consistency:** ExpiryColor switch updated to handle 3 states (expired / today / good) matching the spec's 3 badge colors. All property names match existing bindings.
