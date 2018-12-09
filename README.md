# Smaragd

[![NuGet version](https://img.shields.io/nuget/v/NKristek.Smaragd.svg)](https://www.nuget.org/packages/NKristek.Smaragd/)
[![NuGet downloads](https://img.shields.io/nuget/dt/NKristek.Smaragd.svg)](https://www.nuget.org/packages/NKristek.Smaragd/)
![.NET Standard version: >= 2.0](https://img.shields.io/badge/.NET%20Standard-%3E%3D%202.0-green.svg)
[![GitHub license](https://img.shields.io/github/license/nkristek/Smaragd.svg)](https://github.com/nkristek/Smaragd/blob/master/LICENSE)
[![Build Status](https://travis-ci.org/nkristek/Smaragd.svg?branch=master)](https://travis-ci.org/nkristek/Smaragd)

This library contains base classes for implementing a C# .NET application using the MVVM architecture.

For an example project, please visit my other project [Stein](https://github.com/nkristek/Stein).

## Installation

The recommended way to use this library is via [Nuget](https://www.nuget.org/packages/NKristek.Smaragd/), but you also can either download the DLL from the latest [release](https://github.com/nkristek/Smaragd/releases/latest) or compile it yourself.

## Getting started

Create a subclass of `ViewModel` and optionally a command subclassing `ViewModelCommand<>` like shown below.

```csharp
public class MyViewModel : ViewModel
{
    public MyViewModel()
    {
        var testCommand = new TestCommand(this);
        Commands[testCommand.Name] = testCommand;
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

public class TestCommand
    : ViewModelCommand<MyViewModel>
{
    public TestCommand(MyViewModel parent) : base(parent) { }

    [CanExecuteSource(nameof(MyViewModel.ThirdProperty))]
    protected override bool CanExecute(MyViewModel viewModel, object parameter)
    {
        return viewModel.ThirdProperty > 0;
    }

    protected override void Execute(MyViewModel viewModel, object parameter)
    {
        // execute...
    }
}
```

### SetProperty

There are a few things to notice here. Firstly, using `SetProperty()` in setters of properties is **highly** recommended. It will check if the new value is different from the existing value in the field and sets it if different. If the value changed it will return true and raise an event on `INotifyPropertyChanged.PropertyChanged` with the property name using `[CallerMemberName]`.

Depending on the type of the `ViewModel` additional logic will be executed, like validation (`ValidatingViewModel`).

In this example, when `FirstProperty` is set to 1, `SecondProperty` will be set to 2. 

### RaisePropertyChanged

If you want to manually raise an event on `INotifyPropertyChanged.PropertyChanged`, you can use `RaisePropertyChanged()` with the name of the property. Under normal conditions, this should not be necessary, since `SetProperty()` already does this.
Using `RaisePropertyChanged()` instead of raising an event on `INotifyPropertyChanged.PropertyChanged` directly is **highly** recommended, otherwise `PropertySourceAttribute` etc. won't work correctly.

### PropertySourceAttribute

`ThirdProperty` uses `PropertySourceAttribute` with the names of both `FirstProperty` and `SecondProperty`. 
The `ViewModel` will **automatically** raise an event on `INotifyPropertyChanged.PropertyChanged` for `ThirdProperty` when one of the two properties is changed. 

### CanExecuteSourceAttribute

`TestCommand` uses the `CanExecuteSourceAttribute`, which indicates, that `CanExecute()` depends on the value of the properties named. 

When a `PropertyChanged` event for `ThirdProperty` is invoked, an event is raised on `CanExecuteChanged` (when using custom command implementations implement the `IRaiseCanExecuteChanged` interface for this functionality to work).

The command also has to be added to the `Commands` dictionary on the `ViewModel`.

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
This will also work for properties implementing `INotifyCollectionChanged` when the `CollectionChanged` event occurs.

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

If you want to perform batch operations and want to pause the validation, you can use `SuspendValidation()`. Don't forget to dispose the `IDisposable` after your work is done to continue validation.

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

### Commands

`ViewModelCommand` and `AsyncViewModelCommand` provide base implementations for `ICommand`, `IAsyncCommand` and `IRaiseCanExecuteChanged`.

`ViewModel` has a `Commands` property and it is **highly** recommended to add all available commands to that dictionary. Otherwise the `CommandCanExecuteSourceAttribute` won't work.

## Overview

This library provides the following classes/interfaces:

### ViewModels

Interfaces:
- `IRaisePropertyChanging: INotifyPropertyChanging`
- `IRaisePropertyChanged: INotifyPropertyChanged`
- `IViewModel: IRaisePropertyChanging, IRaisePropertyChanged`
- `IRaiseErrorsChanged: INotifyDataErrorInfo`
- `IValidatingViewModel: IViewModel, IDataErrorInfo, IRaiseErrorsChanged`
- `IDialogModel: IViewModel`
- `ITreeViewModel: IViewModel`

Classes:
- `Bindable: IRaisePropertyChanging, IRaisePropertyChanged`
- `ComputedBindable: Bindable`
- `ViewModel: ComputedBindable, IViewModel`
- `ValidatingViewModel: ViewModel, IValidatingViewModel`
- `DialogModel: ValidatingViewModel, IDialogModel`
- `TreeViewModel: ViewModel, ITreeViewModel`

### Attributes:

Classes:
- `PropertySourceAttribute: Attribute`: usable on properties of classes inheriting from `ComputedBindable` (e.g. `ViewModel`).
- `CommandCanExecuteSourceAttribute: Attribute`: usable on any method called "CanExecute" in a class inheriting from either `ViewModelCommand<TViewModel>` or `AsyncViewModelCommand<T>`.
- `IsDirtyIgnoredAttribute: Attribute`: usable on properties of classes inheriting from `ViewModel`.

### Commands

Interfaces:
- `IRaiseCanExecuteChanged: ICommand`
- `IAsyncCommand: ICommand`
- `INamedCommand: ICommand`

Classes:
- `ViewModelCommand: Bindable, INamedCommand, IRaiseCanExecuteChanged`
- `AsyncViewModelCommand: Bindable, INamedCommand, IAsyncCommand, IRaiseCanExecuteChanged`

### Validation

Interfaces:
- `IValidation`

Classes:
- `Validation<T>: IValidation`
- `PredicateValidation<T>: Validation<T>`

## Contribution

If you find a bug feel free to open an issue. Contributions are also appreciated.
