﻿using System;
using NKristek.Smaragd.ViewModels;

namespace NKristek.Smaragd.Commands
{
    /// <summary>
    /// BindableCommand implementation with ViewModel parameters in command methods
    /// </summary>
    /// <typeparam name="TViewModel">Type of the parent ViewModel</typeparam>
    public abstract class ViewModelCommand<TViewModel>
        : BindableCommand where TViewModel : ViewModel
    {
        protected ViewModelCommand(TViewModel parent)
        {
            Parent = parent ?? throw new ArgumentNullException(nameof(parent));
        }

        private WeakReference<TViewModel> _parent;
        /// <summary>
        /// Parent of this ViewModelCommand which is used when calling CanExecute/Execute or OnThrownException
        /// </summary>
        public TViewModel Parent
        {
            get
            {
                if (_parent != null && _parent.TryGetTarget(out TViewModel parent))
                    return parent;
                return null;
            }

            private set
            {
                if (Parent == value) return;
                _parent = value != null ? new WeakReference<TViewModel>(value) : null;
            }
        }

        protected virtual bool CanExecute(TViewModel viewModel, object parameter)
        {
            return true;
        }

        protected abstract void DoExecute(TViewModel viewModel, object parameter);

        /// <summary>
        /// Will be called when Execute throws an exception
        /// </summary>
        /// <returns></returns>
        protected virtual void OnThrownException(TViewModel viewModel, object parameter, Exception exception) { }

        public sealed override bool CanExecute(object parameter)
        {
            return CanExecute(Parent, parameter);
        }

        protected sealed override void DoExecute(object parameter)
        {
            DoExecute(Parent, parameter);
        }

        protected sealed override void OnThrownException(object parameter, Exception exception)
        {
            OnThrownException(Parent, parameter, exception);
        }
    }
}
