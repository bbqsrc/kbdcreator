using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Feedback;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace KbdEdit
{
    public enum ELayer
    {
        Default,
        Shifted,
        CapsLock,
        ShiftedCapsLock,
        Alt,
        AltShift,
        CapsAlt,
        Control,
        OsxCommand,
        OsxCommandShift,
        OsxCommandAlt,
        OsxCommandAltShift
    }

    public interface TKeyboardEvent { }

    public class KeyboardEvent
    {
        public class ToggleShift : TKeyboardEvent { }

        public class SetLayer : TKeyboardEvent
        {
            readonly public ELayer Value;

            public SetLayer(ELayer value)
            {
                Value = value;
            }

            public override string ToString()
            {
                return base.ToString() + " { " + Value + " }";
            }
        };

        public class SetSelectedKey : TKeyboardEvent
        {
            readonly public KeyboardKey Value;

            public SetSelectedKey(KeyboardKey key)
            {
                Value = key;
            }

            public override string ToString()
            {
                return base.ToString() + " " + Value;
            }
        }

        public class SetKeyValue : TKeyboardEvent
        {
            readonly public ELayer Layer;
            readonly public string Value;

            public SetKeyValue(ELayer layer, string value)
            {
                Layer = layer;
                Value = value;
            }
        }
    }

    public struct KeyboardState
    {
        public KeyboardKey SelectedKey;
        public ELayer Layer;
        public Dictionary<ELayer, Dictionary<string, string>> Data;
    }
    
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        protected Keyboard keyboard;

        public MainWindow()
        {
            InitializeComponent();

            BindKeyboard();
            BindAltHandler();
        }

        private void BindKeyboard()
        {
            keyboard = new IsoKeyboard();
            KeyboardLayoutSystem().Subscribe();
            keyboard.View.VerticalAlignment = VerticalAlignment.Center;
            frmMain.Content = keyboard.View;
        }

        private void BindAltHandler()
        {
            this.ReactiveKeyDown()
                .Where(evt =>
                {
                    var args = evt.EventArgs;
                    return args.Key == Key.System &&
                        args.KeyboardDevice.Modifiers == ModifierKeys.Alt &&
                        args.SystemKey != Key.LeftAlt &&
                        args.SystemKey != Key.RightAlt;
                })
                .Select(evt => KeyInterop.VirtualKeyFromKey(evt.EventArgs.SystemKey))
                .Subscribe(vk =>
                {
                    var isoCode = KeyboardKey.VK_ISO[vk];
                    if (isoCode != null)
                    {
                        keyboard[isoCode].RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    }
                });
        }

        private Dictionary<ELayer, Dictionary<string, string>> GenerateLayers()
        {
            return new Dictionary<ELayer, Dictionary<string, string>>();
        }

        private void AugmentPropField(KeyboardState state, ELayer layer, TextBox txt)
        {
            txt.Text = state.Data
                .GetOrDefault(layer)
                .Get(state.SelectedKey.IsoCode, "");
            
            if (state.Layer == layer)
            {
                txt.Focus();
                txt.CaretIndex = txt.Text.Length;
            }
        }

        private void HighlightSelectedKey(KeyboardKey key)
        {
            foreach (var b in keyboard.KeyViews)
            {
                b.ClearValue(BorderBrushProperty);
                b.ClearValue(BackgroundProperty);
            }

            var btn = keyboard[key.IsoCode];
            btn.BorderBrush = SystemColors.HighlightBrush;
            btn.Background = SystemColors.HighlightBrush;
        }

        FeedbackObserverFunc<KeyboardState, TKeyboardEvent> PropertiesFeedback()
        {
            return UI.Bind<KeyboardState, TKeyboardEvent>(state =>
            {
                return UIBindings<TKeyboardEvent>.Create(
                    state.Where(s => s.SelectedKey != null).Subscribe(s =>
                    {
                        AugmentPropField(s, ELayer.Default, txtKeyDefault);
                        AugmentPropField(s, ELayer.Shifted, txtKeyShifted);
                        HighlightSelectedKey(s.SelectedKey);
                    }),
                    txtKeyDefault.ReactiveTextChanged()
                        .Select(_ => new KeyboardEvent.SetKeyValue(ELayer.Default, txtKeyDefault.Text)),
                    txtKeyShifted.ReactiveTextChanged()
                        .Select(_ => new KeyboardEvent.SetKeyValue(ELayer.Shifted, txtKeyShifted.Text))
                );
            }); 
        }

        FeedbackObserverFunc<KeyboardState, TKeyboardEvent> SelectionFeedback()
        {
            return UI.Bind<KeyboardState, TKeyboardEvent>(state =>
            {
                return UIBindings<TKeyboardEvent>.Create(
                    Observable.CombineLatest(
                        state.Select(s => s.Data.GetOrDefault(s.Layer)),
                        state.Select(s => s.Layer).DistinctUntilChanged(),
                        (a, b) => Tuple.Create(a, b)
                    )
                    .SubscribeOn(state.Scheduler)
                    .Subscribe(t =>
                    {
                        var keyMap = t.Item1;
                        var layer = t.Item2;

                        foreach (var btn in keyboard.KeyViews)
                        {
                            var key = (KeyboardKey)btn.Tag;
                            if (key.Type == EKeyType.Normal)
                            {
                                btn.Content = keyMap.Get(key.IsoCode, "");
                            }
                            else if (key.Type == EKeyType.Shift)
                            {
                                Console.WriteLine("{0} {1}", key.Type, layer);
                                if (layer == ELayer.Shifted)
                                {
                                    btn.BorderBrush = SystemColors.HighlightBrush;
                                    btn.Background = SystemColors.HighlightBrush;
                                }
                                else
                                {
                                    btn.ClearValue(BorderBrushProperty);
                                    btn.ClearValue(BackgroundProperty);
                                }
                            }
                            
                        }
                    }),
                    // All shift buttons
                    Observable.Merge(keyboard.KeyViews
                        .Where(btn => ((KeyboardKey)btn.Tag).Type == EKeyType.Shift)
                        .Select(btn => btn.ReactiveClick())
                    ).Select(_ => new KeyboardEvent.ToggleShift()),
                    // All normal buttons
                    Observable.Merge(keyboard.KeyViews
                        .Where(key => ((KeyboardKey)key.Tag).Type == EKeyType.Normal)
                        .Select(btn => btn.ReactiveClick()))
                    .Select(evt => (Button)evt.Sender)
                    .Select(btn => (KeyboardKey)btn.Tag)
                    .Select(key => new KeyboardEvent.SetSelectedKey(key))
                    );
            });
        }

        IObservable<KeyboardState> KeyboardLayoutSystem()
        {
            return Feedback.System(
                new KeyboardState()
                {
                    Layer = ELayer.Default,
                    Data = GenerateLayers()
                },
                (state, evt) =>
                {
                    Console.WriteLine(evt.ToString());

                    switch (evt)
                    {
                        case KeyboardEvent.ToggleShift v:
                            state.Layer = state.Layer == ELayer.Default
                                ? ELayer.Shifted
                                : ELayer.Default;
                            break;
                        case KeyboardEvent.SetLayer v:
                            state.Layer = v.Value;
                            break;
                        case KeyboardEvent.SetSelectedKey v:
                            state.SelectedKey = v.Value;
                            break;
                        case KeyboardEvent.SetKeyValue v:
                            if (state.SelectedKey == null) break;
                            state.Data.GetOrDefault(v.Layer)[state.SelectedKey.IsoCode] = v.Value;
                            break;
                    }

                    return state;
                },
                SelectionFeedback().Value,
                PropertiesFeedback().Value
            );
        }
    }
}
