using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Styling;
using OpenUtau.App;
using OpenUtau.App.ViewModels;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using OpenUtau.Core.Util;
using ReactiveUI;
using ReactiveUI.Primitives;

namespace OpenUtau.App.Controls {
    class PhonemeCanvas : Control {
        private static readonly IPen ErrorPen = new Pen(Brushes.Red, 1);

        public static readonly DirectProperty<PhonemeCanvas, IBrush> BackgroundProperty =
            AvaloniaProperty.RegisterDirect<PhonemeCanvas, IBrush>(
                nameof(Background),
                o => o.Background,
                (o, v) => o.Background = v);
        public static readonly DirectProperty<PhonemeCanvas, double> TickWidthProperty =
            AvaloniaProperty.RegisterDirect<PhonemeCanvas, double>(
                nameof(TickWidth),
                o => o.TickWidth,
                (o, v) => o.TickWidth = v);
        public static readonly DirectProperty<PhonemeCanvas, double> TickOffsetProperty =
            AvaloniaProperty.RegisterDirect<PhonemeCanvas, double>(
                nameof(TickOffset),
                o => o.TickOffset,
                (o, v) => o.TickOffset = v);
        public static readonly DirectProperty<PhonemeCanvas, UVoicePart?> PartProperty =
            AvaloniaProperty.RegisterDirect<PhonemeCanvas, UVoicePart?>(
                nameof(Part),
                o => o.Part,
                (o, v) => o.Part = v);
        public static readonly DirectProperty<PhonemeCanvas, bool> ShowPhonemeProperty =
            AvaloniaProperty.RegisterDirect<PhonemeCanvas, bool>(
                nameof(ShowPhoneme),
                o => o.ShowPhoneme,
                (o, v) => o.ShowPhoneme = v);

        public IBrush Background {
            get => background;
            private set => SetAndRaise(BackgroundProperty, ref background, value);
        }
        public double TickWidth {
            get => tickWidth;
            private set => SetAndRaise(TickWidthProperty, ref tickWidth, value);
        }
        public double TickOffset {
            get => tickOffset;
            private set => SetAndRaise(TickOffsetProperty, ref tickOffset, value);
        }
        public UVoicePart? Part {
            get => part;
            set => SetAndRaise(PartProperty, ref part, value);
        }
        public bool ShowPhoneme {
            get => showPhoneme;
            private set => SetAndRaise(ShowPhonemeProperty, ref showPhoneme, value);
        }

        private IBrush background = Brushes.Transparent;
        private double tickWidth;
        private double tickOffset;
        private UVoicePart? part;
        private bool showPhoneme = true;

        private HashSet<UNote> selectedNotes = new HashSet<UNote>();
        private Geometry pointGeometry;
        private UPhoneme? mouseoverPhoneme;
        private IBrush? embeddedBackgroundBrush;

        public PhonemeCanvas() {
            ClipToBounds = true;
            pointGeometry = new EllipseGeometry(new Rect(-2.5, -2.5, 5, 5));
            MessageBus.Current.Listen<NotesRefreshEvent>()
                .Subscribe(_ => InvalidateVisual());
            MessageBus.Current.Listen<NotesSelectionEvent>()
                .Subscribe(e => {
                    selectedNotes.Clear();
                    selectedNotes.UnionWith(e.selectedNotes);
                    selectedNotes.UnionWith(e.tempSelectedNotes);
                    InvalidateVisual();
                });
            MessageBus.Current.Listen<PhonemeMouseoverEvent>()
                .Subscribe(e => {
                    if (mouseoverPhoneme != e.mouseoverPhoneme) {
                        mouseoverPhoneme = e.mouseoverPhoneme;
                        InvalidateVisual();
                    }
                });
            MessageBus.Current.Listen<ThemeChangedEvent>()
                .Subscribe(_ => {
                    embeddedBackgroundBrush = null;
                    InvalidateVisual();
                });
        }

        IBrush GetEmbeddedBackgroundBrush() {
            if (embeddedBackgroundBrush != null) {
                return embeddedBackgroundBrush;
            }
            var canvasColor = Avalonia.Media.Colors.Transparent;
            if (Application.Current?.TryGetResource("WorkspaceCanvasColor", ActualThemeVariant, out var resource) == true
                && resource is Color color) {
                canvasColor = color;
            }
            var transparent = Color.FromArgb(0, canvasColor.R, canvasColor.G, canvasColor.B);
            byte bottomAlpha = (byte)Math.Round(255 * ViewConstants.PhonemeEmbeddedBackgroundBottomOpacity);
            var bottom = Color.FromArgb(bottomAlpha, canvasColor.R, canvasColor.G, canvasColor.B);
            embeddedBackgroundBrush = new LinearGradientBrush {
                StartPoint = new RelativePoint(0.5, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(0.5, 1, RelativeUnit.Relative),
                GradientStops = {
                    new GradientStop(transparent, 0),
                    new GradientStop(bottom, 1),
                },
            };
            return embeddedBackgroundBrush;
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change) {
            base.OnPropertyChanged(change);
            InvalidateVisual();
        }

        public override void Render(DrawingContext context) {
            base.Render(context);
            if (Part == null || !ShowPhoneme) {
                return;
            }
            string langCode = PhonemeUIRender.getLangCode(Part);
            var viewModel = ((PianoRollViewModel?)DataContext)?.NotesViewModel;
            if (viewModel == null) {
                return;
            }
            double leftTick = TickOffset - 480;
            double rightTick = TickOffset + Bounds.Width / TickWidth + 480;
            bool raiseText = false;
            double lastTextEndX = double.NegativeInfinity;

            // In DiffSinger mode: tag strip at top (hidden when DiffSingerLangCodeHide), bars below; in UTAU mode: use original layout
            double tagStripHeight = 0, barY, barHeight;
            if (diffSingerMode) {
                tagStripHeight = hideLangPrefix ? 0 : ViewConstants.PhonemeTagStripHeight;
                barY = tagStripHeight;
                barHeight = Math.Max(0, Bounds.Height - tagStripHeight);
            } else {
                (barY, barHeight) = ViewConstants.GetClassicPhonemeEnvelopeLayout();
            }
            foreach (var phoneme in Part.phonemes) {
                double leftBound = viewModel.Project.timeAxis.MsPosToTickPos(phoneme.PositionMs - phoneme.preutter) - Part.position;
                double rightBound = phoneme.End;
                if (leftBound > rightTick || rightBound < leftTick || phoneme.Parent.OverlapError) {
                    continue;
                }
                var timeAxis = viewModel.Project.timeAxis;
                double x = Math.Round(viewModel.TickToneToPoint(phoneme.position, 0).X) + 0.5;
                double posMs = phoneme.PositionMs;
                if (!phoneme.Error) {

                        var polyline = new PolylineGeometry(new Point[] { point0, point1, point2, point3, point4 }, true);
                        context.DrawGeometry(brush, pen, polyline);

                            context.DrawGeometry(brush, pen, pointGeometry);
                        }
                                context.DrawGeometry(brush, pen, pointGeometry);
                            }
                        }
                    }
                }

                var penPos = ThemeManager.NoteBorderPen;
                if (phoneme.rawPosition != phoneme.position) {
                    penPos = ThemeManager.NoteBorderPenThickness3;
                }
                if (diffSingerMode) {
                    // Left border already drawn in diffSingerMode block; use penPos for timing indicator (thicker when moved)
                    var xLeft = viewModel.TickToneToPoint(phoneme.position, 0).X;
                    context.DrawLine(penPos, new Point(xLeft, barY), new Point(xLeft, barY + barHeight));
                } else {
                    context.DrawLine(penPos, new Point(x, barY), new Point(x, barY + barHeight));
                }

                // FIXME: Changing code below may break `HitTestAlias`.
                if (viewModel.TickWidth > ViewConstants.PianoRollTickWidthShowDetails) {
                    string phonemeText = !string.IsNullOrEmpty(phoneme.phonemeMapped) ? phoneme.phonemeMapped : phoneme.phoneme;
                    if (!string.IsNullOrEmpty(phonemeText)) {
                        }
                    }
                }
            }
        }
    }
}
