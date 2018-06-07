﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using NKristek.Smaragd.Attributes;
using NKristek.Smaragd.Validation;

namespace NKristek.Smaragd.ViewModels
{
    /// <summary>
    /// This <see cref="ViewModel"/> implements <see cref="IDataErrorInfo"/> and <see cref="INotifyDataErrorInfo"/>
    /// </summary>
    public abstract class ValidatingViewModel
        : ViewModel, IDataErrorInfo, INotifyDataErrorInfo
    {
        private readonly object _lockObject = new object();
        
        private readonly Dictionary<string, IList<IValidation>> _validations = new Dictionary<string, IList<IValidation>>();

        private readonly Dictionary<string, IList<string>> _validationErrors = new Dictionary<string, IList<string>>();

        protected ValidatingViewModel()
        {
            ((INotifyCollectionChanged) Children).CollectionChanged += OnChildrenCollectionChanged;
        }

        private void OnChildrenCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems != null)
                    {
                        foreach (var newItem in e.NewItems.OfType<ValidatingViewModel>())
                            newItem.ErrorsChanged += OnChildErrorsChanged;
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems != null)
                    {
                        foreach (var oldItem in e.OldItems.OfType<ValidatingViewModel>())
                            oldItem.ErrorsChanged -= OnChildErrorsChanged;
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    if (e.OldItems != null)
                    {
                        foreach (var oldItem in e.OldItems.OfType<ValidatingViewModel>())
                            oldItem.ErrorsChanged -= OnChildErrorsChanged;
                    }
                    if (e.NewItems != null)
                    {
                        foreach (var newItem in e.NewItems.OfType<ValidatingViewModel>())
                            newItem.ErrorsChanged += OnChildErrorsChanged;
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    Validate();
                    break;
                case NotifyCollectionChangedAction.Move:
                    break;
            }
        }

        private void OnChildErrorsChanged(object sender, DataErrorsChangedEventArgs e)
        {
            RaiseErrorsChanged(nameof(Children));
        }

        #region IDataErrorInfo
        
        /// <summary>
        /// Get validation error by the name of the property
        /// </summary>
        /// <param name="propertyName">Name of the property. If the property name is null or empty, all validation errors will be returned.</param>
        /// <returns>Validation errors of the property. If the property name is null or empty, all validation errors will be returned.</returns>
        public string this[string propertyName]
        {
            get
            {
                if (String.IsNullOrEmpty(propertyName))
                    return Error;

                if (_validationErrors.ContainsKey(propertyName))
                    return String.Join(Environment.NewLine, _validationErrors[propertyName]);

                if (nameof(Children).Equals(propertyName))
                    return String.Join(Environment.NewLine, GetChildrenErrors());

                return String.Empty;
            }
        }

        /// <summary>
        /// All validation errors concatenated with <see cref="Environment.NewLine"/>
        /// </summary>
        public string Error => String.Join(Environment.NewLine, GetAllErrors());

        /// <summary>
        /// Gets all validation errors from this <see cref="ValidatingViewModel"/> and all <see cref="ViewModel.Children"/> of type <see cref="ValidatingViewModel"/>
        /// </summary>
        /// <returns>All validation errors from this <see cref="ValidatingViewModel"/> and all <see cref="ViewModel.Children"/> of type <see cref="ValidatingViewModel"/></returns>
        private IEnumerable<string> GetAllErrors()
        {
            var errors = _validationErrors.SelectMany(kvp => kvp.Value).Where(e => !String.IsNullOrEmpty(e));
            var childrenErrors = GetChildrenErrors();
            return errors.Concat(childrenErrors);
        }

        /// <summary>
        /// Gets all validation errors from all <see cref="ViewModel.Children"/> of type <see cref="ValidatingViewModel"/>
        /// </summary>
        /// <returns>All validation errors from all <see cref="ViewModel.Children"/> of type <see cref="ValidatingViewModel"/></returns>
        private IEnumerable<string> GetChildrenErrors()
        {
            return Children.OfType<ValidatingViewModel>().SelectMany(c => c.GetErrors(null).OfType<string>()).Where(e => !String.IsNullOrEmpty(e));
        }

        #endregion

        #region INotifyDataErrorInfo

        /// <summary>
        /// Returns if there are any validation errors
        /// </summary>
        [IsDirtyIgnored]
        [PropertySource(nameof(Children), NotifyCollectionChangedAction.Add, NotifyCollectionChangedAction.Remove, NotifyCollectionChangedAction.Replace, NotifyCollectionChangedAction.Reset)]
        public bool HasErrors => _validationErrors.Any() || Children.OfType<ValidatingViewModel>().Any(c => c.HasErrors);

        /// <summary>
        /// Gets validation errors from a specified property
        /// </summary>
        /// <param name="propertyName">Name of the property</param>
        /// <returns>Validation errors from the property</returns>
        public System.Collections.IEnumerable GetErrors(string propertyName)
        {
            if (String.IsNullOrEmpty(propertyName))
                return GetAllErrors();

            if (_validationErrors.ContainsKey(propertyName))
                return _validationErrors[propertyName];

            if (nameof(Children).Equals(propertyName))
                return GetChildrenErrors();

            return Enumerable.Empty<string>();
        }

        /// <summary>
        /// Event that gets fired when the validation errors change
        /// </summary>
        public virtual event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        /// <summary>
        /// Raises an event on the ErrorsChanged event
        /// </summary>
        private void RaiseErrorsChanged(string propertyName = null)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            RaisePropertyChanged(nameof(HasErrors));
        }

        #endregion

        private bool _validationSuspended;

        /// <summary>
        /// If the validation is temporarily suspended. Dispose the <see cref="IDisposable"/> from <see cref="SuspendValidation"/> to unsuspend. Setting this property will propagate the value to all <see cref="ValidatingViewModel"/> items in the <see cref="ViewModel.Children"/> collection.
        /// </summary>
        public bool ValidationSuspended
        {
            get
            {
                lock (_lockObject)
                {
                    return _validationSuspended;
                }
            }

            internal set
            {
                lock (_lockObject)
                {
                    _validationSuspended = value;
                    foreach (var validatingChild in Children.OfType<ValidatingViewModel>())
                        validatingChild.ValidationSuspended = value;
                }
            }
        }

        /// <summary>
        /// If data in this <see cref="ViewModel"/> is valid
        /// </summary>
        [IsDirtyIgnored]
        [PropertySource(nameof(HasErrors))]
        public bool IsValid => !HasErrors;

        /// <summary>
        /// Set multiple validation errors of the property
        /// </summary>
        /// <param name="propertyName">Name of the property which validates with this error</param>
        /// <param name="errors">These are the validation errors and has to be empty if no validation error occured</param>
        private void SetValidationErrors(string propertyName, IEnumerable<string> errors)
        {
            if (propertyName == null)
                return;

            var errorList = errors.Where(e => !String.IsNullOrEmpty(e)).ToList();
            if (errorList.Any())
            {
                _validationErrors[propertyName] = errorList;
                RaiseErrorsChanged(propertyName);
            }
            else
            {
                if (_validationErrors.Remove(propertyName))
                    RaiseErrorsChanged(propertyName);
            }

        }
        
        /// <summary>
        /// All validation logic will be executed, even when <see cref="ValidationSuspended"/> is set to true.
        /// </summary>
        public void Validate()
        {
            var type = GetType();
            foreach (var propertyValidation in _validations)
            {
                var valueProperty = type.GetProperty(propertyValidation.Key);
                if (valueProperty == null)
                    continue;

                var value = valueProperty.GetValue(this, null);
                Validate(propertyValidation.Key, value, propertyValidation.Value);
            }

            foreach (var validatingChild in Children.OfType<ValidatingViewModel>())
                validatingChild.Validate();
        }
        
        /// <summary>
        /// Add a validation for the property returned by the lambda expression
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propertySelector">Lambda expression to select the property. eg.: () => MyProperty</param>
        /// <param name="validation">Validation to add</param>
        protected void AddValidation<T>(Expression<Func<T>> propertySelector, Validation<T> validation)
        {
            var propertyName = GetPropertyName(propertySelector);
            var initialValue = propertySelector.Compile()();

            if (_validations.TryGetValue(propertyName, out var existingValidations))
            {
                existingValidations.Add(validation);
            }
            else
            {
                existingValidations = new List<IValidation> { validation };
                _validations.Add(propertyName, existingValidations);
            }

            Validate(propertyName, initialValue, existingValidations.OfType<Validation<T>>());
        }
        
        /// <summary>
        /// Removes a specific validation for the property returned by the expression
        /// </summary>
        /// <typeparam name="T">Type of the property to validate</typeparam>
        /// <param name="propertySelector">Expression to select the property. eg.: () => MyProperty</param>
        /// <param name="validation">Validation to remove</param>
        /// <returns>If the validation was found and successfully removed</returns>
        protected bool RemoveValidation<T>(Expression<Func<T>> propertySelector, Validation<T> validation)
        {
            var propertyName = GetPropertyName(propertySelector);
            if (!_validations.TryGetValue(propertyName, out var validations))
                return false;

            var validationWasRemoved = validations.Remove(validation);
            if (!validations.Any())
                _validations.Remove(propertyName);
            return validationWasRemoved;
        }

        /// <summary>
        /// Removes all validations for the property returned by the expression
        /// </summary>
        /// <typeparam name="T">Type of the validating property</typeparam>
        /// <param name="propertySelector">Expression to select the property. eg.: () => MyProperty</param>
        /// <returns>If the validation was found and successfully removed</returns>
        protected bool RemoveValidations<T>(Expression<Func<T>> propertySelector)
        {
            var propertyName = GetPropertyName(propertySelector);
            return _validations.Remove(propertyName);
        }

        /// <summary>
        /// Get all validations. Key is the name of the property, value are all validations for the property.
        /// </summary>
        /// <returns>All validations. Key is the name of the property, value are all validations for the property.</returns>
        public IEnumerable<KeyValuePair<string, IList<IValidation>>> Validations()
        {
            return _validations.ToList();
        }

        /// <summary>
        /// Get all validations for the property returned by the expression
        /// </summary>
        /// <typeparam name="T">Type of the validating property</typeparam>
        /// <param name="propertySelector">Expression to select the property. eg.: () => MyProperty</param>
        /// <returns>All validations for the property</returns>
        public IEnumerable<Validation<T>> Validations<T>(Expression<Func<T>> propertySelector)
        {
            var propertyName = GetPropertyName(propertySelector);
            return _validations.ContainsKey(propertyName)
                ? _validations[propertyName].OfType<Validation<T>>()
                : Enumerable.Empty<Validation<T>>();
        }

        /// <summary>
        /// Gets the property name of the property in the given expression
        /// </summary>
        /// <typeparam name="T">Type of the property</typeparam>
        /// <param name="propertyExpression">Expression which points to the property</param>
        /// <returns>Name of the property in the given expression</returns>
        private static string GetPropertyName<T>(Expression<Func<T>> propertyExpression)
        {
            var memberExpression = propertyExpression.Body as MemberExpression;
            if (memberExpression == null)
                throw new Exception("Expression body is not of type MemberExpression");

            return memberExpression.Member.Name;
        }

        /// <summary>
        /// Sets a property if <see cref="ViewModel.IsReadOnly"/> is not true and the value is different and raises an event on the <see cref="PropertyChangedEventHandler"/>.
        /// It will execute the appropriate validations for this property.
        /// </summary>
        /// <typeparam name="T">Type of the property to set</typeparam>
        /// <param name="storage">Reference to the storage variable</param>
        /// <param name="value">New value to set</param>
        /// <param name="oldValue">The old value of the property</param>
        /// <param name="propertyName">Name of the property</param>
        /// <returns>True if the value was different from the storage variable and the PropertyChanged event was raised</returns>
        protected override bool SetProperty<T>(ref T storage, T value, out T oldValue, [CallerMemberName] string propertyName = "")
        {
            var propertyWasChanged = base.SetProperty(ref storage, value, out oldValue, propertyName);
            if (propertyWasChanged && !ValidationSuspended && _validations.TryGetValue(propertyName, out var validations))
                Validate(propertyName, value, validations.OfType<Validation<T>>());
            return propertyWasChanged;
        }

        /// <summary>
        /// Validates the given validations and sets the validation error
        /// </summary>
        /// <typeparam name="T">Type of the property to validate</typeparam>
        /// <param name="propertyName">Name of the property to validate</param>
        /// <param name="value">Value of the property to validate</param>
        /// <param name="validations">Validations of the property to validate</param>
        private void Validate<T>(string propertyName, T value, IEnumerable<Validation<T>> validations)
        {
            var errors = new List<string>();
            foreach (var validation in validations)
            {
                if (!validation.IsValid(value, out var errorMessage))
                    errors.Add(errorMessage);
            }
            SetValidationErrors(propertyName, errors);
        }

        /// <summary>
        /// Validates the given validations and sets the validation error
        /// </summary>
        /// <param name="propertyName">Name of the property to validate</param>
        /// <param name="value">Value of the property to validate</param>
        /// <param name="validations">Validations of the property to validate</param>
        private void Validate(string propertyName, object value, IEnumerable<IValidation> validations)
        {
            var errors = new List<string>();
            foreach (var validation in validations)
            {
                if (!validation.IsValid(value, out var errorMessage))
                    errors.Add(errorMessage);
            }
            SetValidationErrors(propertyName, errors);
        }

        /// <summary>
        /// Temporarily suspends validation. This could be used in a batch update to prevent validation overhead. This will propagate to all <see cref="ValidatingViewModel"/> items in the <see cref="ViewModel.Children"/> collection.
        /// </summary>
        /// <returns><see cref="IDisposable"/> which unsuspends validation when disposed.</returns>
        public IDisposable SuspendValidation()
        {
            return new SuspendValidationDisposable(this);
        }
    }
}
