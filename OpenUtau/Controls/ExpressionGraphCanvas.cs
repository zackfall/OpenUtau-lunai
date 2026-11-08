using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using OpenUtau.Core;
using OpenUtau.Core.ExpressionGraph;
using OpenUtau.Core.Ustx;

namespace OpenUtau.App.Controls {
    /// <summary>
    /// Edits one expression graph as nodes on a pannable canvas. It shows a copy of the graph; every change goes
    /// through <see cref="ExpressionGraphEdits.Apply"/> as one undoable command, and the owner calls
    /// <see cref="Show"/> again when the document changes.
    /// </summary>
    public class ExpressionGraphCanvas : UserControl {
        // Nodes size to their contents: linked inputs hide their fields, so a fully linked node is narrow.
        const double NodeMinWidth = 80;
        const double NumberWidth = 48;
        const double ExpressionPickerWidth = 150;

        /// <summary>
        /// A dropdown wide enough for its longest option, so the node keeps its size whichever is selected.
        /// </summary>
        static double ChoiceWidth(IEnumerable<string> options) {
            double text = options.Select(option => {
                var block = new TextBlock { Text = option };
                block.Measure(Size.Infinity);
                return block.DesiredSize.Width;
            }).DefaultIfEmpty(0).Max();
            // Room for the arrow and the padding.
            return Math.Ceiling(text) + 40;
        }
        const double DotSize = 10;

                var pen = new Pen(brush, 2);
                foreach (var link in graph.links) {
                    if (owner.outputDots.TryGetValue((link.from, owner.OutputIndexOf(link)), out var fromDot)
                            && link.toPort != null && owner.inputDots.TryGetValue((link.to, link.toPort), out var toDot)
                            && owner.DotCenter(fromDot) is Point a && owner.DotCenter(toDot) is Point b) {
                        DrawLink(context, pen, a, b);
                    }
                }
                if (owner.PendingLinkEnd is Point end && owner.outputDots.TryGetValue(owner.PendingLinkFrom, out var dot)
                        && owner.DotCenter(dot) is Point start) {
                    DrawLink(context, new Pen(brush, 2, new DashStyle(new double[] { 3, 2 }, 0)), start, end);
                }
            }

            static void DrawLink(DrawingContext context, Pen pen, Point a, Point b) {
                double bend = Math.Max(40, Math.Abs(b.X - a.X) / 2);
                var geometry = new StreamGeometry();
                using (var g = geometry.Open()) {
                    g.BeginFigure(a, false);
                    g.CubicBezierTo(new Point(a.X + bend, a.Y), new Point(b.X - bend, b.Y), b);
                    g.EndFigure(false);
                }
                context.DrawGeometry(null, pen, geometry);
            }
        }

        #endregion
    }
}
