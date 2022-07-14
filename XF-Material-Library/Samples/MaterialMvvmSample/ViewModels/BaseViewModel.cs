﻿using CommonServiceLocator;
using MaterialMvvmSample.Utilities;

namespace MaterialMvvmSample.ViewModels
{
    public abstract class BaseViewModel : PropertyChangeAware, ICleanUp
    {
        protected INavigationService Navigation { get; }

        protected IServiceLocator ServiceLocator { get; }

        protected BaseViewModel()
        {
            ServiceLocator = CommonServiceLocator.ServiceLocator.Current;
            Navigation = ServiceLocator.GetInstance<INavigationService>();
        }

        /// <summary>
        /// When overriden, allow to add additional logic to this view model when the view where it was attached was pushed using <see cref="INavigationService.PushAsync(string, object)"/>.
        /// </summary>
        /// <param name="navigationParameter">The navigation parameter to pass after the view was pushed.</param>
        public virtual void OnViewPushed(object navigationParameter = null) { }

        /// <summary>
        /// When overriden, allow to add additional logic to this view model when the view where it was attached was popped using <see cref="INavigationService.PopAsync"/>.
        /// </summary>
        public virtual void OnViewPopped()
        {
            CleanUp();
        }

        public virtual void CleanUp() { }
    }
}
