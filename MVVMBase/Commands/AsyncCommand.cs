﻿using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace nkristek.MVVMBase.Commands
{
    /// <summary>
    /// IAsyncCommand implementation
    /// </summary>
    public abstract class AsyncCommand
        : ICommand, IRaiseCanExecuteChanged
    {
        /// <summary>
        /// Indicates if <see cref="ExecuteAsync(object)"/> is working
        /// </summary>
        public bool IsWorking { get; private set; }

        /// <summary>
        /// Override this method to indicate if <see cref="Execute(object)"/> is allowed to execute
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public virtual bool CanExecute(object parameter)
        {
            return true;
        }

        /// <summary>
        /// This method executes <see cref="ExecuteAsync(object)"/>
        /// </summary>
        /// <param name="parameter"></param>
        public async void Execute(object parameter)
        {
            try
            {
                IsWorking = true;
                await ExecuteAsync(parameter);
            }
            catch (Exception exception)
            {
                try
                {
                    OnThrownException(parameter, exception);
                }
                catch { }
            }
            finally
            {
                IsWorking = false;
            }
        }

        /// <summary>
        /// Asynchronous <see cref="ICommand.Execute(object)"/>
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        protected abstract Task ExecuteAsync(object parameter);

        /// <summary>
        /// Will be called when <see cref="ExecuteAsync(object)"/> throws an <see cref="Exception"/>
        /// </summary>
        protected virtual void OnThrownException(object parameter, Exception exception) { }
        
        private EventHandler _internalCanExecuteChanged;

        /// <summary>
        /// This event will be raised when the result of <see cref="CanExecute(object)"/> probably changed and will need to be reevaluated
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add
            {
                _internalCanExecuteChanged += value;
                CommandManager.RequerySuggested += value;
            }

            remove
            {
                _internalCanExecuteChanged -= value;
                CommandManager.RequerySuggested -= value;
            }
        }

        /// <summary>
        /// Raise an event that <see cref="CanExecute(object)"/> needs to be reevaluated
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            _internalCanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
