using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace KbdEdit
{
    

    public enum EKeyType
    {
        Normal,
        Shift
    }

    public class KeyboardKey
    {
        public readonly static string[] VK_ISO;

        static KeyboardKey()
        {
            VK_ISO = new string[0xFF];

            VK_ISO[0xC0] = "E00";
            VK_ISO[0x31] = "E01";
            VK_ISO[0x32] = "E02";
            VK_ISO[0x33] = "E03";
            VK_ISO[0x34] = "E04";
            VK_ISO[0x35] = "E05";
            VK_ISO[0x36] = "E06";
            VK_ISO[0x37] = "E07";
            VK_ISO[0x38] = "E08";
            VK_ISO[0x39] = "E09";
            VK_ISO[0x30] = "E10";
            // TODO: finish constants
        }

        public string IsoCode;
        public double Weight;
        public EKeyType Type;

        public KeyboardKey(string isoCode, double weight)
        {
            IsoCode = isoCode;
            Weight = weight;
            Type = EKeyType.Normal;
        }

        public KeyboardKey(string isoCode, double weight, EKeyType type)
        {
            IsoCode = isoCode;
            Weight = weight;
            Type = type;
        }

        public override string ToString()
        {
            return base.ToString() + " {" + IsoCode + ", " + Weight + ", " + Type + "}";
        }
    }

    public class KeyboardRow
    {
        public List<KeyboardKey> Keys;

        public KeyboardRow()
        {
            Keys = new List<KeyboardKey>();
        }

        internal void Add(string isoCode, double weight)
        {
            Keys.Add(new KeyboardKey(isoCode, weight));
        }

        internal void Add(string isoCode, double weight, EKeyType type)
        {
            Keys.Add(new KeyboardKey(isoCode, weight, type));
        }

        internal void Add(string isoPrefix, int i, double weight)
        {
            Keys.Add(new KeyboardKey(string.Format("{0}{1:d2}", isoPrefix, i), weight));
        }
    }

    public abstract class Keyboard
    {
        public Button this[string isoCode]
        {
            get
            {
                return KeyViews.First(btn =>
                {
                    var key = (KeyboardKey)btn.Tag;
                    return key.IsoCode == isoCode;
                });
            }
        }

        public IEnumerable<Button> KeyViews
        {
            get
            {
                foreach (StackPanel row in View.Children)
                {
                    foreach (Button btn in row.Children)
                    {
                        yield return btn;
                    }
                }
            }
        }

        internal Button GenerateKeyView(KeyboardKey key)
        {
            var btn = new Button();
            if (key.Type == EKeyType.Shift)
            {
                btn.Content = "Shift";
            }
            else
            {
                btn.Content = key.IsoCode;
            }
            btn.Width = 32.0 * key.Weight;
            btn.Height = 32;
            btn.Margin = new Thickness(4);
            btn.Tag = key;
            return btn;
        }

        internal StackPanel GenerateView()
        {
            var keyboard = new StackPanel();
            keyboard.Orientation = Orientation.Vertical;
            keyboard.HorizontalAlignment = HorizontalAlignment.Center;

            foreach (var row in Rows)
            {
                var keyRow = new StackPanel();
                keyRow.Orientation = Orientation.Horizontal;
                keyRow.HorizontalAlignment = HorizontalAlignment.Center;

                foreach (var key in row.Keys)
                {
                    keyRow.Children.Add(GenerateKeyView(key));
                }

                keyboard.Children.Add(keyRow);
            }

            return keyboard;
        }

        protected abstract List<KeyboardRow> GenerateRows();

        public Keyboard()
        {
            Rows = GenerateRows();
            View = GenerateView();
        }

        public StackPanel View { get; private set; }
        public List<KeyboardRow> Rows { get; protected set; }
    }

    public class IsoKeyboard : Keyboard
    {
        protected override List<KeyboardRow> GenerateRows()
        {
            // Row E
            var rowE = new KeyboardRow();
            for (int i = 0; i <= 12; ++i)
            {
                rowE.Add("E", i, 1);
            }
            rowE.Add("E13", 1.5);

            // Row D
            var rowD = new KeyboardRow();
            rowD.Add("D00", 1.5);
            for (int i = 1; i <= 13; ++i)
            {
                rowD.Add("D", i, 1);
            }

            // Row C
            var rowC = new KeyboardRow();
            rowC.Add("C00", 1.875);
            for (int i = 1; i <= 12; ++i)
            {
                rowC.Add("C", i, 1);
            }
            rowC.Add("C13", 0.625);

            // Row B
            var rowB = new KeyboardRow();
            rowB.Add("B99", 1.25, EKeyType.Shift);
            for (int i = 0; i <= 10; ++i)
            {
                rowB.Add("B", i, 1);
            }
            rowB.Add("B11", 2.5, EKeyType.Shift);

            // Row A
            var rowA = new KeyboardRow();
            rowA.Add("A99", 1.25);
            for (int i = 0; i <= 2; ++i)
            {
                rowA.Add("A", i, 1);
            }
            rowA.Add("A03", 6);
            for (int i = 8; i <= 11; ++i)
            {
                rowA.Add("A", i, 1);
            }
            rowA.Add("A12", 1.25);

            return new List<KeyboardRow>
            {
                rowE, rowD, rowC, rowB, rowA
            };
        }
    }

    public class AnsiKeyboard : Keyboard
    {
        protected override List<KeyboardRow> GenerateRows()
        {
            // Row E
            var rowE = new KeyboardRow();
            for (int i = 0; i <= 12; ++i)
            {
                rowE.Add("E", i, 1);
            }
            rowE.Add("E13", 1.5);

            // Row D
            var rowD = new KeyboardRow();
            rowD.Add("D00", 1.5);
            for (int i = 1; i <= 13; ++i)
            {
                rowD.Add("D", i, 1);
            }

            // Row C
            var rowC = new KeyboardRow();
            rowC.Add("C00", 1.875);
            for (int i = 1; i <= 11; ++i)
            {
                rowC.Add("C", i, 1);
            }
            rowC.Add("C12", 1.875);

            // Row B
            var rowB = new KeyboardRow();
            rowB.Add("B99", 2.5);
            for (int i = 1; i <= 10; ++i)
            {
                rowB.Add("B", i, 1);
            }
            rowB.Add("B11", 2.5);

            // Row A
            var rowA = new KeyboardRow();
            rowA.Add("A99", 1.25);
            for (int i = 0; i <= 2; ++i)
            {
                rowA.Add("A", i, 1);
            }
            rowA.Add("A03", 6);
            for (int i = 8; i <= 11; ++i)
            {
                rowA.Add("A", i, 1);
            }
            rowA.Add("A12", 1.25);

            return new List<KeyboardRow>
            {
                rowE, rowD, rowC, rowB, rowA
            };
        }
    }
}
