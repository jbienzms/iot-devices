using System.Collections;
using System.Windows;
using System;
using System.Linq;
using System.Linq.Expressions;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;
using System.Collections.Specialized;
using Windows.UI.Xaml.Controls;

namespace SolarSystem.Helpers
{
    /// <summary>
    /// Assists with bindings
    /// </summary>
    static public class BindingHelper
    {
        #region Nested Classes
        /// <summary>
        /// A synchronization manager.
        /// </summary>
        private class Synchronizer
        {
            private IList collection;
            private INotifyCollectionChanged collectionEvents;
            private readonly ListViewBase list;
            private bool started;
            private bool suspended;

            /// <summary>
            /// Initializes a new instance of the <see cref="Synchronizer"/> class.
            /// </summary>
            internal Synchronizer(ListViewBase list, IList collection, INotifyCollectionChanged collectionEvents)
            {
                this.list = list;
                this.collection = collection;
                this.collectionEvents = collectionEvents;
            }

            private void Collection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                // Suspend event handling
                if (suspended) { return; }
                suspended = true;

                // Clear list selection
                list.SelectedItems.Clear();

                // Add items
                foreach (object i in e.NewItems)
                {
                    list.SelectedItems.Add(i);
                }

                // Resume event handling
                suspended = false;
            }

            private void List_SelectionChanged(object sender, Windows.UI.Xaml.Controls.SelectionChangedEventArgs e)
            {
                // Suspend event handling
                if (suspended) { return; }
                suspended = true;

                // Remove
                foreach (var r in e.RemovedItems)
                {
                    collection.Remove(r);
                }

                // Add
                foreach (var a in e.AddedItems)
                {
                    collection.Add(a);
                }

                // Resume event handling
                suspended = false;
            }

            /// <summary>
            /// Starts synchronizing the list.
            /// </summary>
            public void Start()
            {
                if (started) { return; }
                started = true;

                // Subscribe to events
                collectionEvents.CollectionChanged += Collection_CollectionChanged;
                list.SelectionChanged += List_SelectionChanged;
            }

            /// <summary>
            /// Stops synchronizing the list.
            /// </summary>
            public void Stop()
            {
                if (!started) { return; }
                started = false;

                // Unsubscribe from events
                collectionEvents.CollectionChanged -= Collection_CollectionChanged;
                list.SelectionChanged -= List_SelectionChanged;
            }
        }
        #endregion // Nested Classes

        #region Dependency Property Definitions
        static public readonly DependencyProperty SelectedItems = DependencyProperty.RegisterAttached("SelectedItems", typeof(IList), typeof(BindingHelper), new PropertyMetadata(null, OnSelectedItemsChanged));
        static private readonly DependencyProperty SynchronizerProperty = DependencyProperty.RegisterAttached("Synchronizer", typeof(Synchronizer), typeof(BindingHelper), new PropertyMetadata(null));
        #endregion // Dependency Property Definitions

        #region Overrides / Event Handlers
        private static void OnSelectedItemsChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue != null)
            {
                Synchronizer synchronizer = GetSynchronizer(dependencyObject);
                synchronizer.Stop();

                SetSynchronizer(dependencyObject, null);
            }

            var list = dependencyObject as ListViewBase;
            var collection = e.NewValue as IList;
            var collectionEvents = e.NewValue as INotifyCollectionChanged;

            // check that this property is an IList, and that it is being set on a ListBox
            if (list != null && collection != null)
            {
                Synchronizer synchronizer = GetSynchronizer(dependencyObject);
                if (synchronizer == null)
                {
                    synchronizer = new Synchronizer(list, collection, collectionEvents);
                    SetSynchronizer(dependencyObject, synchronizer);
                }

                synchronizer.Start();
            }
        }
        #endregion // Overrides / Event Handlers

        #region Internal Methods
        /// <summary>
        /// Gets the synchronizer for the list.
        /// </summary>
        private static Synchronizer GetSynchronizer(DependencyObject dependencyObject)
        {
            return (Synchronizer)dependencyObject.GetValue(SynchronizerProperty);
        }

        /// <summary>
        /// Sets the synchronizer for the list.
        /// </summary>
        private static void SetSynchronizer(DependencyObject dependencyObject, Synchronizer value)
        {
            dependencyObject.SetValue(SynchronizerProperty, value);
        }
        #endregion // Internal Methods

        #region Public Methods
        /// <summary>
        /// Gets the selected items collection.
        /// </summary>
        public static IList GetSelectedItems(DependencyObject dependencyObject)
        {
            return (IList)dependencyObject.GetValue(SelectedItems);
        }

        /// <summary>
        /// Sets the selected items collection.
        /// </summary>
        public static void SetSelectedItems(DependencyObject dependencyObject, IList value)
        {
            dependencyObject.SetValue(SelectedItems, value);
        }
        #endregion // Public Methods
        
    }
}
