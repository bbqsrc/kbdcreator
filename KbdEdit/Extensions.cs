using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace KbdEdit
{
    public static class Extensions
    {
        public static IObservable<EventPattern<RoutedEventArgs>>
        ReactiveClick(this Button btn)
        {
            return Observable.FromEventPattern<RoutedEventHandler, RoutedEventArgs>(
                h => btn.Click += h,
                h => btn.Click -= h);
        }

        public static IObservable<EventPattern<TextChangedEventArgs>>
        ReactiveTextChanged(this TextBox txt)
        {
            return Observable.FromEventPattern<TextChangedEventHandler, TextChangedEventArgs>(
                h => txt.TextChanged += h,
                h => txt.TextChanged -= h);
        }

        public static IObservable<EventPattern<KeyEventArgs>>
        ReactiveKeyDown(this UIElement element)
        {
            return Observable.FromEventPattern<KeyEventHandler, KeyEventArgs>(
                h => element.KeyDown += h,
                h => element.KeyDown -= h);
        }
        
        public static TValue GetOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key) where TValue: new()
        {
            if (!dict.ContainsKey(key))
            {
                dict[key] = new TValue();
            }

            return dict[key];
        }

        public static TValue Get<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue fallback)
        {
            if (dict.ContainsKey(key))
            {
                return dict[key];
            }

            return fallback;
        }
    }
}
