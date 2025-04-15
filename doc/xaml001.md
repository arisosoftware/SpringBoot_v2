Ahh I see—you have **multiple ListBoxes**, all driven by different `string[]` sources, and you want a **cleaner, simpler** way to define them without repeating a lot of XAML and code. Totally makes sense!

Here are a few ways to simplify things, depending on how dynamic your UI needs to be:

---

## ✅ **Option 1: Use a Reusable `ItemTemplate` (with shared structure)**

You can define a shared `ItemTemplate` in a `Resource`, then reuse it across multiple `ListBox`es.

### 🔧 XAML Resource

```xml
<Window.Resources>
    <DataTemplate x:Key="CheckBoxLabelTemplate">
        <StackPanel Orientation="Horizontal">
            <CheckBox Margin="5"/>
            <TextBlock Text="{Binding}" Margin="5"/>
        </StackPanel>
    </DataTemplate>
</Window.Resources>
```

### 📦 Apply It to Multiple ListBoxes

```xml
<ListBox Name="ListBox1" ItemTemplate="{StaticResource CheckBoxLabelTemplate}" />
<ListBox Name="ListBox2" ItemTemplate="{StaticResource CheckBoxLabelTemplate}" />
<ListBox Name="ListBox3" ItemTemplate="{StaticResource CheckBoxLabelTemplate}" />
```

### 💡 Then in code-behind:
```csharp
ListBox1.ItemsSource = new[] { "Apple", "Banana", "Cherry" };
ListBox2.ItemsSource = new[] { "Dog", "Cat", "Horse" };
ListBox3.ItemsSource = new[] { "Red", "Green", "Blue" };
```

---

## ✅ **Option 2: Use MVVM Binding (More Scalable)**

If you want to cleanly handle dozens of listboxes with dynamic sources and minimize code-behind, consider using **MVVM pattern**:

```csharp
public class MainViewModel
{
    public string[] Fruits { get; } = { "Apple", "Banana", "Cherry" };
    public string[] Animals { get; } = { "Dog", "Cat", "Horse" };
    public string[] Colors { get; } = { "Red", "Green", "Blue" };
}
```

### Bind from XAML:

```xml
<Window.DataContext>
    <local:MainViewModel/>
</Window.DataContext>

<Grid>
    <StackPanel>
        <ListBox ItemsSource="{Binding Fruits}" ItemTemplate="{StaticResource CheckBoxLabelTemplate}"/>
        <ListBox ItemsSource="{Binding Animals}" ItemTemplate="{StaticResource CheckBoxLabelTemplate}"/>
        <ListBox ItemsSource="{Binding Colors}" ItemTemplate="{StaticResource CheckBoxLabelTemplate}"/>
    </StackPanel>
</Grid>
```

---

## 🧼 Summary – Cleanest Setup

- Define one `ItemTemplate` resource
- Use `ItemsSource` binding via `string[]` properties
- Use MVVM if you're managing many data sources

---

Would you like a quick working sample with MVVM + dynamic lists? Or do you prefer to stick with code-behind for now?
