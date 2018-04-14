﻿using System;
using nkristek.MVVMBase.ViewModels;

namespace nkristek.MVVMBase.Commands
{
    /// <summary>
    /// BindableCommand implementation with ViewModel parameters in command methods
    /// </summary>
    /// <typeparam name="TViewModel">Type of the parent ViewModel</typeparam>
    public abstract class ViewModelCommand<TViewModel>
        : BindableCommand where TViewModel : ViewModel
    {
        public ViewModelCommand(TViewModel parent)
        {
            Parent = parent;
        }

        private WeakReference<TViewModel> _Parent;
        /// <summary>
        /// Parent of this ViewModelCommand which is used when calling CanExecute/Execute or OnThrownException
        /// </summary>
        public TViewModel Parent
        {
            get
            {
                if (_Parent != null && _Parent.TryGetTarget(out TViewModel parent))
                    return parent;
                return null;
            }

            private set
            {
                if (Parent == value) return;
                _Parent = value != null ? new WeakReference<TViewModel>(value) : null;
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

        public override sealed bool CanExecute(object parameter)
        {
            return CanExecute(Parent, parameter);
        }

        public override sealed void DoExecute(object parameter)
        {
            DoExecute(Parent, parameter);
        }

        protected override sealed void OnThrownException(object parameter, Exception exception)
        {
            OnThrownException(Parent, parameter, exception);
        }
    }
}
