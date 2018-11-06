# Smaragd
This library contains base classes for implementing a C# .NET application using the MVVM architecture.

For an example project, please visit my other project [Stein](https://github.com/nkristek/Stein).

## Prerequisites

The nuget package and [DLL](https://github.com/nkristek/Smaragd/releases) are built for .NET Standard 2.0, but you can compile the library yourself to fit your needs.

## Installation

The recommended way to use this library is via [Nuget](https://www.nuget.org/packages/NKristek.Smaragd/), but you also can either download the DLL from the latest [release](https://github.com/nkristek/Smaragd/releases/latest) or compile it yourself.

## How to use

To get started, create a subclass of `ViewModel` like shown below.

```csharp
public class MyViewModel : ViewModel
{
    public MyViewModel()
    {
        DoStuffCommand = new DoStuffCommand(this);
    }

    private int _firstProperty;
    
    public int FirstProperty
    {
        get => _firstProperty;
        set
        {
            if (SetProperty(ref _firstProperty, value, out _))
                SecondProperty = FirstProperty + 1;
        }
    }

    private int _secondProperty;
    
    public int SecondProperty
    {
        get => _secondProperty;
        set => SetProperty(ref _secondProperty, value, out _);
    }

    [PropertySource(nameof(FirstProperty), nameof(SecondProperty))]
    public int ThirdProperty => FirstProperty + SecondProperty;
}
```

### SetProperty

There are a few things to notice here. Firstly, in setters of properties the use of `SetProperty()` is **highly** recommended. It will check if the new value is different from the existing value in the field and sets it if different. If the value changed it will return true and raise an event on `INotifyPropertyChanged.PropertyChanged` with the property name using `[CallerMemberName]`.

Depending on the type of the `ViewModel` (e.g. `ValidatingViewModel`) additional logic will be executed.

In this example, when `FirstProperty` is set to 1, `SecondProperty` will be set to 2. 

### RaisePropertyChanged

If you want to manually raise an event on `INotifyPropertyChanged.PropertyChanged`, you can use `RaisePropertyChanged()` with the name of the property. Under normal conditions, this should not be necessary, since `SetProperty()` already does this.
Using `RaisePropertyChanged()` instead of raising an event on `INotifyPropertyChanged.PropertyChanged` directly is **highly** recommended, otherwise `PropertySourceAttribute` etc. won't work correctly.

### PropertySource

`ThirdProperty` uses `PropertySourceAttribute` with the names of both `FirstProperty` and `SecondProperty`. 
The `ViewModel` will **automatically** raise an event on `INotifyPropertyChanged.PropertyChanged` for `ThirdProperty` when one of the two properties is changed. 

### CommandCanExecuteSource

TODO

`DoStuffCommand` uses the `CommandCanExecuteSourceAttribute`, which indicates, that the `CanExecute()` method depends on the value of the properties named. When a `PropertyChanged` event for `ThirdProperty` is invoked, a `CanExecuteChanged` event is raised on the command (when using custom command implementations, also implement the `IRaiseCanExecuteChanged` interface for this functionality to work).

You can also use the `CollectionCanExecuteSourceCollection` attribute. See the documentation for `PropertySourceCollection` above for more details.

### IsDirty

`ViewModel` implements an `IsDirty` property which is initially false.
If `SetProperty()` changes a value and `IsDirtyIgnoredAttribute` is not defined on the property, it will set `IsDirty` to true. 

For example:
```csharp
private bool _testProperty;

[IsDirtyIgnored]
public bool TestProperty
{
    get => _testProperty;
    set => SetProperty(ref _testProperty, value, out _);
}
```

Now, when `TestProperty` changes, `IsDirty` will not be automatically set to true.

### Parent

The `Parent` property on `ViewModel` uses a `WeakReference` internally.

### IsReadOnly

The `IsReadOnly` property does what it implies, if set to true, `SetProperty()` will now longer set any property or raise events on `INotifyPropertyChanged.PropertyChanged` except for the `IsReadOnly` property itself.

### ValidatingViewModel

Your viewmodel may also inherit from `ValidatingViewModel` which implements `IDataErrorInfo` and `INotifyDataErrorInfo`. 

You may simply add Validations in the class constructor via 
```csharp
AddValidation(() => MyProperty, new PredicateValidation<int>(value => value >= 5, "Value has to be at least 5"));
```

This will execute this validation everytime `SetProperty()` changes this property.
You can call `Validate()` to execute all validations again.

For most validations the `PredicateValidation<T>` should suffice, but if you need something more advanced, you should inherit from `Validation<T>`.

If you want to perform batch operations and want to pause the validation, you can use `SuspendValidation()`. Don't forget to dispose the returned `IDisposable` to continue validation.

### TreeViewModel

This `ViewModel` provides an `IsChecked` implementation to use in a TreeView. It will update its parent `TreeViewModel` and children `TreeViewModel` with appropriate states for `IsChecked`.
If the `TreeViewModel` has children, `TreeChildren` should be overridden accordingly.

Example:
```csharp
private class FolderViewModel
    : TreeViewModel
{
    public ObservableCollection<FolderViewModel> Subfolders { get; } = new ObservableCollection<FolderViewModel>();

    protected override IEnumerable<TreeViewModel> TreeChildren => Subfolders;
}
```

**Please note:** The indeterminate state of `IsChecked` should only be set by updates from child ViewModel's. The `IsChecked` property will be set to `false` if trying to set it to `null`. If you want to set the `IsChecked` property to `null`, you have to use `TreeViewModel.SetIsChecked()`.

### DialogModel

There is also a `DialogModel` class, which inherits from `ValidatingViewModel` and implements a `Title` property to use in your dialog.

### Command

`ViewModelCommand` and `AsyncViewModelCommand` provide base implementations for `ICommand`, `IAsyncCommand` and `IRaiseCanExecuteChanged`.

## Overview

This library provides the following classes:

### ViewModels namespace

Attributes:
- `PropertySource`: usable on properties of classes inheriting from `ComputedBindableBase` (e.g. `ViewModel`)
- `CommandCanExecuteSource`: usable on ICommand.CanExecute of classes implementing `IRaiseCanExecuteChanged` (the parent has to inherit from `ComputedBindableBase` (e.g. `ViewModel`))
- `IsDirtyIgnored`: usable on properties of classes inheriting from `ViewModel`

`INotifyPropertyChanged`:
- `BindableBase`
- `ComputedBindableBase`
- `ViewModel`
- `ValidatingViewModel`
- `DialogModel`
- `TreeViewModel`

### Validation

Interfaces:
- `IValidation`

`IValidation`:
- `Validation<T>`
- `PredicateValidation<T>`

### Commands namespace

Interfaces:
- `IRaiseCanExecuteChanged`
- `IAsyncCommand`

`ICommand`:
 - `BindableCommand`
 - `ViewModelCommand`

`IAsyncCommand`:
 - `AsyncBindableCommand`
 - `AsyncViewModelCommand`

## Contribution

If you find a bug feel free to open an issue. Contributions are also appreciated.
