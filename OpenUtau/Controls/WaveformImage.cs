using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using OpenUtau.App.ViewModels;
using OpenUtau.Core;

namespace OpenUtau.App.Controls {
    /// <summary>
    /// The rendered audio of the open part, drawn behind the notes as a
    /// <see cref="WaveformEnvelope"/> on a grid from TickOrigin, rebuilt on newly
    /// rendered audio.
    /// </summary>
    class WaveformImage : Control {
        const float DiffSingerAmplitudeGain = 1.2f;
        const float VerticalFill = 0.92f;
        public static readonly DirectProperty<WaveformImage, double> TickWidthProperty =
            AvaloniaProperty.RegisterDirect<WaveformImage, double>(
                nameof(TickWidth),
                o => o.TickWidth,
                (o, v) => o.TickWidth = v);
        public static readonly DirectProperty<WaveformImage, double> TickOffsetProperty =
            AvaloniaProperty.RegisterDirect<WaveformImage, double>(
                nameof(TickOffset),
                o => o.TickOffset,
                (o, v) => o.TickOffset = v);
        public static readonly DirectProperty<WaveformImage, bool> ShowWaveformProperty =
            AvaloniaProperty.RegisterDirect<WaveformImage, bool>(
                nameof(ShowWaveform),
                o => o.ShowWaveform,
                (o, v) => o.ShowWaveform = v);

        public double TickWidth {
            get => tickWidth;
            set => SetAndRaise(TickWidthProperty, ref tickWidth, value);
        }
        public double TickOffset {
            get { return tickOffset; }
            set { SetAndRaise(TickOffsetProperty, ref tickOffset, value); }
        }
        public bool ShowWaveform {
            get { return showWaveform; }
            set { SetAndRaise(ShowWaveformProperty, ref showWaveform, value); }
        }

        private const int SampleRate = 44100;
        private const int Channels = 2;
        private static readonly IBrush Fill = new SolidColorBrush(Color.FromArgb(0x7F, 0x7F, 0x7F, 0x7F));

        private double tickWidth;
        private double tickOffset;
        private bool showWaveform;

        private readonly WaveformEnvelope envelope = new WaveformEnvelope();
        private float[] sampleData = new float[0];

        public WaveformImage() {
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change) {
            base.OnPropertyChanged(change);
            if (change.Property == DataContextProperty ||
                change.Property == TickWidthProperty ||
                change.Property == TickOffsetProperty ||
                change.Property == ShowWaveformProperty ||
                change.Property == BoundsProperty) {
                InvalidateVisual();
            }
        }

        public override void Render(DrawingContext context) {
            base.Render(context);
            if (DataContext is not NotesViewModel viewModel || double.IsNaN(viewModel.TickOffset) ||
                !ShowWaveform || viewModel.TickWidth <= ViewConstants.PianoRollTickWidthShowDetails) {
                return;
            }

                    }
                }
                int s0 = Math.Clamp(SampleIndex(fromMs) - firstSample, 0, sampleCount);
                int s1 = Math.Clamp(SampleIndex(toMs) - firstSample, 0, sampleCount);
                if (!covered || fromMs < 0) {
                    columnMin[i] = columnMax[i] = float.NaN;
                    if (s1 > 0) {
                        lastValue = sampleData[s1 - 1];
                    }
                    continue;
                }
                float min, max;
                if (s1 > s0) {
                    min = float.MaxValue;
                    max = float.MinValue;
                    for (int s = s0; s < s1; ++s) {
                        float v = sampleData[s];
                        if (v < min) min = v;
                        if (v > max) max = v;
                    }
                    lastValue = sampleData[s1 - 1];
                } else {
                    // Zoomed in past one sample per column: hold the last sample.
                    min = max = lastValue;
                }
                columnMin[i] = min;
                columnMax[i] = max;
            }
        }

        }
    }
}
