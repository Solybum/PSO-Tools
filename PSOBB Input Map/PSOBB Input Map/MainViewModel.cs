using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using Binarysharp.MemoryManagement;
using MVVM;

namespace PSOBB_Input_Map
{
    public class MainViewModel : ViewModelBase
    {
        private static string processname = "psobb";
        private static IntPtr keyAddress = new IntPtr(0x00A9CB60);
        private static IntPtr joyPointer = new IntPtr(0x00A9CCD4);
        private MemorySharp ms;

        public ObservableCollection<string> KeyActions
        {
            get { return GetValue<ObservableCollection<string>>(); }
            set { SetValue(value); }
        }
        public ObservableCollection<string> Keys
        {
            get { return GetValue<ObservableCollection<string>>(); }
            set { SetValue(value); }
        }
        public ObservableCollection<string> JoyActions
        {
            get { return GetValue<ObservableCollection<string>>(); }
            set { SetValue(value); }
        }
        public ObservableCollection<string> Joys
        {
            get { return GetValue<ObservableCollection<string>>(); }
            set { SetValue(value); }
        }

        public int SelectedKeyAction
        {
            get { return GetValue<int>(); }
            set
            {
                SetValue(value);
                if (this.SelectedKeyAction != -1)
                {
                    if (RefreshMemorySharp())
                    {
                        try
                        {
                            int keyValue = this.ms.Read<int>(keyAddress + this.keyActionsOffset[this.SelectedKeyAction], false);
                            SetValue(this.keyButtonsValue.IndexOf(keyValue), nameof(this.SelectedKey));
                        }
                        catch
                        {
                            MessageBox.Show("Could not access process memory", "Memory Sharp");
                            this.ms = null;
                        }
                    }
                }
            }
        }
        public int SelectedKey
        {
            get { return GetValue<int>(); }
            set
            {
                SetValue(value);
                if (this.SelectedKeyAction != -1)
                {
                    if (RefreshMemorySharp())
                    {
                        try
                        {
                            this.ms.Write(keyAddress + this.keyActionsOffset[this.SelectedKeyAction], this.keyButtonsValue[this.SelectedKey], false);
                        }
                        catch
                        {
                            MessageBox.Show("Could not access process memory", "Memory Sharp");
                            this.ms = null;
                        }
                    }
                }
            }
        }

        public int SelectedJoyAction
        {
            get { return GetValue<int>(); }
            set
            {
                SetValue(value);

                if (this.SelectedJoyAction >= 0 && this.SelectedJoyAction < 4)
                {
                    this.Joys.Clear();
                    this.Joys = new ObservableCollection<string>(this.joyAxis);

                    if (RefreshMemorySharp())
                    {
                        try
                        {
                            IntPtr address = this.ms.Read<IntPtr>(joyPointer, false);
                            if (address != IntPtr.Zero)
                            {
                                byte joyValue = this.ms.Read<byte>(address + this.SelectedJoyAction, false);
                                SetValue(this.joyAxisValue.IndexOf(joyValue), nameof(this.SelectedJoy));
                            }
                            else
                            {
                                MessageBox.Show("Joystick memory not found.\nPlease open the joystic menu before using these options", "PSOBB Input Map");
                            }
                        }
                        catch
                        {
                            MessageBox.Show("Could not access process memory", "Memory Sharp");
                            this.ms = null;
                        }
                    }
                }
                else if (this.SelectedJoyAction >= 4 && this.SelectedJoyAction < 16)
                {
                    this.Joys.Clear();
                    this.Joys = new ObservableCollection<string>(this.joyButtons);

                    if (RefreshMemorySharp())
                    {
                        try
                        {
                            IntPtr address = this.ms.Read<IntPtr>(joyPointer, false);
                            if (address != IntPtr.Zero)
                            {
                                int joyValue = this.ms.Read<int>(address + 4 + ((this.SelectedJoyAction - 4) * 4), false);
                                SetValue(this.joyButtonsValue.IndexOf(joyValue), nameof(this.SelectedJoy));
                            }
                            else
                            {
                                MessageBox.Show("Joystick memory not found.\nPlease open the joystic menu before using these options", "PSOBB Input Map");
                            }
                        }
                        catch
                        {
                            MessageBox.Show("Could not access process memory", "Memory Sharp");
                            this.ms = null;
                        }
                    }
                }
            }
        }
        public int SelectedJoy
        {
            get { return GetValue<int>(); }
            set
            {
                SetValue(value);
                if (this.SelectedJoy != -1)
                {
                    if (this.SelectedJoyAction >= 0 && this.SelectedJoyAction < 4)
                    {
                        if (RefreshMemorySharp())
                        {
                            try
                            {
                                IntPtr address = this.ms.Read<IntPtr>(joyPointer, false);
                                if (address != IntPtr.Zero)
                                {
                                    this.ms.Write(address + this.SelectedJoyAction, this.joyAxisValue[this.SelectedJoy], false);
                                }
                                else
                                {
                                    MessageBox.Show("Joystick memory not found.\nPlease open the joystic menu before using these options", "PSOBB Input Map");
                                }
                            }
                            catch
                            {
                                MessageBox.Show("Could not access process memory", "Memory Sharp");
                                this.ms = null;
                            }
                        }
                    }
                    else if (this.SelectedJoyAction >= 4 && this.SelectedJoyAction < 16)
                    {
                        if (RefreshMemorySharp())
                        {
                            try
                            {
                                IntPtr address = this.ms.Read<IntPtr>(joyPointer, false);
                                if (address != IntPtr.Zero)
                                {
                                    this.ms.Write(address + 4 + ((this.SelectedJoyAction - 4) * 4), this.joyButtonsValue[this.SelectedJoy], false);
                                }
                                else
                                {
                                    MessageBox.Show("Joystick memory not found.\nPlease open the joystic menu before using these options", "PSOBB Input Map");
                                }
                            }
                            catch
                            {
                                MessageBox.Show("Could not access process memory", "Memory Sharp");
                                this.ms = null;
                            }
                        }
                    }
                }
            }
        }

        public MainViewModel()
        {
            this.KeyActions = new ObservableCollection<string>();
            this.Keys = new ObservableCollection<string>();

            this.KeyActions = new ObservableCollection<string>(this.keyActionsData);
            this.Keys = new ObservableCollection<string>(this.keyButtons);

            this.SelectedKeyAction = -1;
            this.SelectedKey = -1;

            this.JoyActions = new ObservableCollection<string>();
            this.Joys = new ObservableCollection<string>();

            // Joysticks
            this.JoyActions = new ObservableCollection<string>(this.joyActionsData);

            this.SelectedJoyAction = -1;
            this.SelectedJoy = -1;
        }

        private bool RefreshMemorySharp()
        {
            try
            {
                if (this.ms == null)
                {
                    this.ms = new MemorySharp(Process.GetProcessesByName(processname).First());
                }
                return true;
            }
            catch
            {
                this.ms = null;
                MessageBox.Show("Could not find process", "PSOBB Input Map");
            }
            return false;
        }

        #region Data
        List<string> keyActionsData = new List<string>()
        {
            "Forward",
            "Backward",
            "Left",
            "Right",
            "Run",
            "Walk",
            "Action Palette Left",
            "Action Palette Middle",
            "Action Palette Right",
            "Symbol Chat Open",
            "Camera",
            "Decide",
            "Cancel",
            "Menu Open/Close",
            "Input Start",
            "Action Palette Change",
            "Select Up",
            "Select Down",
            "Select Left",
            "Select Right",
        };
        List<int> keyActionsOffset = new List<int>()
        {
            0x04,
            0x0C,
            0x14,
            0x1C,
            0x24,
            0x2C,
            0x84,
            0x7C,
            0x8C,
            0x74,
            0x9C,
            0x34,
            0x3C,
            0x44,
            0x4C,
            0x94,
            0x54,
            0x5C,
            0x64,
            0x6C,
        };
        List<string> keyButtons = new List<string>()
        {
            /* 0x05 */ "FN",
            /* 0x06 */ "HOME",
            /* 0x07 */ "END",
            /* 0x08 */ "PAGE UP",
            /* 0x09 */ "PAGE DOWN",
            /* 0x0A */ "SCROLL LOCK",
            /* 0x10 */ "a",
            /* 0x11 */ "b",
            /* 0x12 */ "c",
            /* 0x13 */ "d",
            /* 0x14 */ "e",
            /* 0x15 */ "f",
            /* 0x16 */ "g",
            /* 0x17 */ "h",
            /* 0x18 */ "i",
            /* 0x19 */ "j",
            /* 0x1A */ "k",
            /* 0x1B */ "l",
            /* 0x1C */ "m",
            /* 0x1D */ "n",
            /* 0x1E */ "o",
            /* 0x1F */ "p",
            /* 0x20 */ "q",
            /* 0x21 */ "r",
            /* 0x22 */ "s",
            /* 0x23 */ "t",
            /* 0x24 */ "u",
            /* 0x25 */ "v",
            /* 0x26 */ "w",
            /* 0x27 */ "x",
            /* 0x28 */ "y",
            /* 0x29 */ "z",
            /* 0x2A */ "1",
            /* 0x2B */ "2",
            /* 0x2C */ "3",
            /* 0x2D */ "4",
            /* 0x2E */ "5",
            /* 0x2F */ "6",
            /* 0x30 */ "7",
            /* 0x31 */ "8",
            /* 0x32 */ "9",
            /* 0x33 */ "0",
            /* 0x34 */ "-",
            /* 0x35 */ "^",
            /* 0x36 */ "\\",
            /* 0x37 */ "@",
            /* 0x38 */ "[",
            /* 0x39 */ ";",
            /* 0x3A */ ":",
            /* 0x3B */ "",
            /* 0x3C */ ",",
            /* 0x3D */ ".",
            /* 0x3E */ "/",
            /* 0x40 */ "F1",
            /* 0x41 */ "F2",
            /* 0x42 */ "F3",
            /* 0x43 */ "F4",
            /* 0x44 */ "F5",
            /* 0x45 */ "F6",
            /* 0x46 */ "F7",
            /* 0x47 */ "F8",
            /* 0x48 */ "F9",
            /* 0x49 */ "F10",
            /* 0x4A */ "F11",
            /* 0x4B */ "F12",
            /* 0x4C */ "ESC",
            /* 0x4D */ "INSERT",
            /* 0x4E */ "DELETE",
            /* 0x50 */ "BACK SPACE",
            /* 0x51 */ "TAB",
            /* 0x52 */ "BACKSLASH",
            /* 0x53 */ "CAPS LOCK",
            /* 0x54 */ "L SHIFT",
            /* 0x55 */ "R SHIFT",
            /* 0x56 */ "CTRL",
            /* 0x57 */ "ALT",
            /* 0x59 */ "SPACE",
            /* 0x5C */ "←",
            /* 0x5D */ "↓",
            /* 0x5E */ "↑",
            /* 0x5F */ "→",
            /* 0x61 */ "ENTER",
            /* 0x62 */ "* (Numpad?)",
            /* 0x63 */ "+ (Numpad?)",
            /* 0x64 */ "- (Numpad?)",
            /* 0x66 */ "NUM LOCK",
            /* 0x67 */ "0 (Numpad)",
            /* 0x68 */ "1 (Numpad)",
            /* 0x69 */ "2 (Numpad)",
            /* 0x70 */ "3 (Numpad)",
            /* 0x71 */ "4 (Numpad)",
            /* 0x72 */ "5 (Numpad)",
            /* 0x73 */ "6 (Numpad)",
            /* 0x74 */ "7 (Numpad)",
            /* 0x75 */ "8 (Numpad)",
            /* 0x76 */ "9 (Numpad)",
        };
        List<int> keyButtonsValue = new List<int>()
        {
            0x05,
            0x06,
            0x07,
            0x08,
            0x09,
            0x0A,
            0x10,
            0x11,
            0x12,
            0x13,
            0x14,
            0x15,
            0x16,
            0x17,
            0x18,
            0x19,
            0x1A,
            0x1B,
            0x1C,
            0x1D,
            0x1E,
            0x1F,
            0x20,
            0x21,
            0x22,
            0x23,
            0x24,
            0x25,
            0x26,
            0x27,
            0x28,
            0x29,
            0x2A,
            0x2B,
            0x2C,
            0x2D,
            0x2E,
            0x2F,
            0x30,
            0x31,
            0x32,
            0x33,
            0x34,
            0x35,
            0x36,
            0x37,
            0x38,
            0x39,
            0x3A,
            0x3B,
            0x3C,
            0x3D,
            0x3E,
            0x40,
            0x41,
            0x42,
            0x43,
            0x44,
            0x45,
            0x46,
            0x47,
            0x48,
            0x49,
            0x4A,
            0x4B,
            0x4C,
            0x4D,
            0x4E,
            0x50,
            0x51,
            0x52,
            0x53,
            0x54,
            0x55,
            0x56,
            0x57,
            0x59,
            0x5C,
            0x5D,
            0x5E,
            0x5F,
            0x61,
            0x62,
            0x63,
            0x64,
            0x66,
            0x67,
            0x68,
            0x69,
            0x70,
            0x71,
            0x72,
            0x73,
            0x74,
            0x75,
            0x76,
        };


        List<string> joyActionsData = new List<string>()
        {
            "Move Left/Right",
            "Move Forward/Backward",
            "Right Analog Left/Right",
            "Right Analog Forward/Backward",
            "Select Up",
            "Select Down",
            "Select Left",
            "Select Right",
            "Action Palette Middle/Decide",
            "Action Palette Right",
            "Action Palette Left",
            "Action Palette Top",
            "Menu Open/Close",
            "Prev Page / Camera",
            "Next page / Action Palette Change",
            "Menu Open / Menu Decide",
        };

        List<string> joyAxis = new List<string>()
        {
            "X Axis +",     // 00
            "Y Axis +",     // 01
            "Z Axis +",     // 02
            "X Rotate +",   // 03
            "Y Rotate +",   // 04
            "Z Rotate +",   // 05
            "Slider +",     // 06
            "X Axis +",     // 07
            "Y Axis +",     // 08
            "Z Axis +",     // 09
            "X Rotate +",   // 0A
            "Y Rotate +",   // 0B
            "Z Rotate +",   // 0C
            "Slider +",     // 0D
            "None",         // FF
        };

        List<byte> joyAxisValue = new List<byte>()
        {
            0x00,
            0x01,
            0x02,
            0x03,
            0x04,
            0x05,
            0x06,
            0x07,
            0x08,
            0x09,
            0x0A,
            0x0B,
            0x0C,
            0x0D,
            0xFF,
        };

        List<string> joyButtons = new List<string>()
        {
            "None",
            "B1",
            "B2",
            "B3",
            "B4",
            "B5",
            "B6",
            "B7",
            "B8",
            "B9",
            "B10",
            "B11",
            "B12",
            "B13",
            "B14",
            "B15",
            "B16",
            "POV Y +",
            "POV Y -",
            "POV X +",
            "POV X -",
        };

        List<int> joyButtonsValue = new List<int>()
        {
            0x00000000,
            0x00000001,
            0x00000002,
            0x00000004,
            0x00000008,
            0x00000010,
            0x00000020,
            0x00000040,
            0x00000080,
            0x00000100,
            0x00000200,
            0x00000400,
            0x00000800,
            0x00001000,
            0x00002000,
            0x00004000,
            0x00008000,
            0x00010000,
            0x00020000,
            0x00040000,
            0x00080000,
        };
        #endregion
    }
}
