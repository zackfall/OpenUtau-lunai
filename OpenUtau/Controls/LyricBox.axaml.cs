using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using OpenUtau.App;
using OpenUtau.App.ViewModels;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;

namespace OpenUtau.App.Controls {
    public partial class LyricBox : UserControl {
        private LyricBoxViewModel viewModel;
        private TextBox box;
        private TextBox? blendBox;
        private ListBox listBox;
        private DispatcherTimer? focusTimer;
        private int scrollStyleApplyGeneration;
        private bool selectAllOnNextGotFocus;
        private bool restoringInputFocus;

        public LyricBox() {
            InitializeComponent();
            DataContext = viewModel = new LyricBoxViewModel();
            box = PART_Box;
            blendBox = PART_BlendBox;
            listBox = PART_Suggestions;
            IsVisible = false;
        }

        }

        }

        private void ListBox_KeyDown(object? sender, KeyEventArgs e) {
            switch (e.Key) {
                case Key.Enter:
                    if (listBox.SelectedItem is LyricBoxViewModel.SuggestionItem item) {
                        ApplySuggestionToBoxes(item.Alias);
                    }
                    EndEdit(true);
                    e.Handled = true;
                    break;
                case Key.Escape:
                    EndEdit(false);
                    e.Handled = true;
                    break;
                case Key.Tab:
                    }
                    OnTab(e.KeyModifiers);
                    e.Handled = true;
                    break;
                case Key.Up:
                    ListBoxSelect(listBox.SelectedIndex - 1);
                    e.Handled = true;
                    break;
                case Key.Down:
                    ListBoxSelect(listBox.SelectedIndex + 1);
                    e.Handled = true;
                    break;
                case Key.PageUp:
                    ListBoxSelect(listBox.SelectedIndex - 8);
                    e.Handled = true;
                    break;
                case Key.PageDown:
                    ListBoxSelect(listBox.SelectedIndex + 8);
                    e.Handled = true;
                    break;
                default:
                    break;
            }
        }

        private void ListBoxSelect(int index) {
            if (index < 0) {
                if (listBox.SelectedIndex == 0) {
                    index = listBox.ItemCount - 1;
                } else {
                    index = 0;
                }
            } else if (index >= listBox.ItemCount) {
                if (listBox.SelectedIndex == listBox.ItemCount - 1) {
                    index = 0;
                } else {
                    index = listBox.ItemCount - 1;
                }
            }
            listBox.SelectedIndex = index;
        }

        private void Box_KeyDown(object? sender, KeyEventArgs e) {
            switch (e.Key) {
                case Key.Enter:
                    EndEdit(true);
                    e.Handled = true;
                    break;
                case Key.Escape:
                    EndEdit(false);
                    e.Handled = true;
                    break;
                case Key.Tab:
                    OnTab(e.KeyModifiers);
                    e.Handled = true;
                    break;
                case Key.Up:
                case Key.Down:
                case Key.PageUp:
                case Key.PageDown:
                    listBox.Focus();
                    listBox.SelectedIndex = 0;
                    e.Handled = true;
                    break;
                case Key.Left:
                    if (sender is TextBox tbLeft && tbLeft.SelectionStart < tbLeft.SelectionEnd)
                        tbLeft.SelectionEnd = tbLeft.SelectionStart;
                    break;
                case Key.Right:
                    if (sender is TextBox tbRight && tbRight.SelectionStart > tbRight.SelectionEnd)
                        tbRight.SelectionEnd = tbRight.SelectionStart;
                    break;
                default:
                    break;
            }
        }

        private void Save_Click(object? sender, RoutedEventArgs e) {
            EndEdit(true);
        }

        /// <summary>
        /// Apply a suggestion into both the ViewModel and the focused TextBox.
        /// EndEdit(true) re-reads TextBox.Text, so ViewModel-only updates would be overwritten.
        /// </summary>
        void ApplySuggestionToBoxes(string alias) {
            viewModel.ApplySuggestion(alias);
            if (viewModel.SuggestionFromBlend) {
                if (blendBox != null) {
                    blendBox.Text = alias;
                }
            } else {
                box.Text = alias;
            }
        }

        private void OnTab(KeyModifiers keyModifiers) {
            UVoicePart? part = viewModel.Part;
            var tabFrom = viewModel.NoteOrPhoneme;
            LyricBoxNoteOrPhoneme? tabTo = null;
            UNote? focusNote = null;
            string? text = null;
            if (tabFrom is LyricBoxNote noteBox) {
                UNote? note = noteBox.Unwrap();
                note = keyModifiers == KeyModifiers.None ? note.Next
                    : keyModifiers == KeyModifiers.Shift ? note.Prev
                    : null;
                if (note != null) {
                    tabTo = new LyricBoxNote(note);
                    focusNote = note;
                    text = note.lyric;
                }
            } else if (tabFrom is LyricBoxPhoneme phonemeBox) {
                UPhoneme? phoneme = phonemeBox.Unwrap();
                phoneme = keyModifiers == KeyModifiers.None ? phoneme.Next
                    : keyModifiers == KeyModifiers.Shift ? phoneme.Prev
                    : null;
                if (phoneme != null) {
                    tabTo = new LyricBoxPhoneme(phoneme);
                    focusNote = phoneme.Parent;
                    text = phoneme.phoneme;
                }
            }
            EndEdit(true);
            if (tabTo != null && focusNote != null && text != null && part != null) {
                DocManager.Inst.ExecuteCmd(new FocusNoteNotification(part, focusNote));
                Show(part, tabTo, text);
            }
        }

        public void Suggestions_PointerPressed(object? sender, PointerPressedEventArgs args) {
            if (!args.GetCurrentPoint(listBox).Properties.IsLeftButtonPressed) {
                return;
            }
            for (var visual = args.Source as Visual; visual != null && visual != listBox; visual = visual.GetVisualParent()) {
                if (visual is Control { DataContext: LyricBoxViewModel.SuggestionItem item }) {
                    ApplySuggestionToBoxes(item.Alias);
                    EndEdit(true);
                    args.Handled = true;
                    return;
                }
            }
        }

        public void Show(UVoicePart part, LyricBoxNoteOrPhoneme noteOrPhoneme, string text) {
            Show(part, noteOrPhoneme, text, false, false, null);
        }

        /// <param name="editTagOnly">When true, text is tag only; commit builds full = text + "/" + otherPart.</param>
        /// <param name="editPhonemeOnly">When true, text is phoneme only; commit builds full = (otherPart + "/") + text.</param>
        /// <param name="otherPart">The other part (phoneme-only when editTagOnly, tag when editPhonemeOnly).</param>
        public void Show(UVoicePart part, LyricBoxNoteOrPhoneme noteOrPhoneme, string text, bool editTagOnly, bool editPhonemeOnly, string? otherPart) {
            viewModel.Part = part;
            viewModel.NoteOrPhoneme = noteOrPhoneme;
            viewModel.Text = text;
            viewModel.EditPhonemeTagOnly = editTagOnly;
            viewModel.EditPhonemePhonemeOnly = editPhonemeOnly;
            viewModel.PhonemeOtherPart = otherPart ?? string.Empty;
            viewModel.SuggestionFromBlend = false;
            if (noteOrPhoneme is LyricBoxPhoneme lyricPhoneme) {
                var ph = lyricPhoneme.Unwrap();
                viewModel.BlendText = ph.blendPhoneme ?? string.Empty;
                viewModel.BlendWeight = Math.Clamp(ph.blendWeight, 0, 100);
            } else {
                viewModel.BlendText = string.Empty;
                viewModel.BlendWeight = 0;
            }
            viewModel.IsVisible = true;
            selectAllOnNextGotFocus = true;
            ScheduleApplyScrollStyle();
            focusTimer = new DispatcherTimer(
                TimeSpan.FromMilliseconds(15),
                DispatcherPriority.Normal,
                FocusTimer_Tick);
            focusTimer.Start();
        }

        private void FocusTimer_Tick(object? sender, System.EventArgs e) {
            box.Focus();
            box.SelectAll();
            if (focusTimer != null) {
                focusTimer.Tick -= FocusTimer_Tick;
                focusTimer.Stop();
                focusTimer = null;
            }
        }

        /// <summary>
        /// Closes the lyric/phoneme editor.
        /// Pass true to commit (Save / Enter). Default discards (click outside / Escape).
        /// </summary>
        public void EndEdit(bool commit = false) {
            if (!viewModel.IsVisible) {
                return;
            }
            if (commit) {
                // Sync TextBox text before commit in case binding hasn't flushed yet.
                viewModel.Text = box.Text;
                if (blendBox != null) {
                    viewModel.BlendText = blendBox.Text;
                }
                viewModel.Commit();
            }
            viewModel.Part = null;
            viewModel.NoteOrPhoneme = null;
            viewModel.IsVisible = false;
            viewModel.Text = string.Empty;
            viewModel.BlendText = string.Empty;
            viewModel.BlendWeight = 0;
            viewModel.SuggestionFromBlend = false;
            viewModel.EditPhonemeTagOnly = false;
            viewModel.EditPhonemePhonemeOnly = false;
            viewModel.PhonemeOtherPart = null;
            this.Focus();
        }
    }
}
