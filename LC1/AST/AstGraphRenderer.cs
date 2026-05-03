using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace LC1.Ast
{
    public static class AstGraphRenderer
    {
        private const double ModW = 124;
        private const double ModH = 58;
        private const double IdW = 148;
        private const double IdH = 58;
        private const double TypeW = 182;
        private const double TypeH = 64;
        private const double LitW = 182;
        private const double LitH = 64;
        private const double DeclHeaderH = 48;
        private const double GapH = 72;
        private const double GapTypeLit = 76;
        private const double GapVSmall = 80;
        private const double GapDecl = 180;
        private const double Pad = 112;
        private const double GapProgramToDecl = 168;
        private const double TrunkOffset = 40;

        private static readonly Color ColorProgram = Color.FromRgb(41, 98, 155);
        private static readonly Color ColorDecl = Color.FromRgb(94, 53, 140);
        private static readonly Color ColorConst = Color.FromRgb(21, 122, 74);
        private static readonly Color ColorVal = Color.FromRgb(12, 110, 96);
        private static readonly Color ColorId = Color.FromRgb(163, 86, 14);
        private static readonly Color ColorType = Color.FromRgb(15, 108, 120);
        private static readonly Color ColorLit = Color.FromRgb(120, 55, 15);
        private static readonly Brush EdgeBrush = new SolidColorBrush(Color.FromRgb(70, 70, 70));

        public static void Render(Canvas canvas, ProgramNode program)
        {
            canvas.Children.Clear();

            int n = program.Declarations.Count;
            double rowModW = ModW + GapH + ModW + GapH + IdW;
            double rowTypeW = TypeW + GapTypeLit + LitW;
            double innerW = Math.Max(rowModW, rowTypeW);
            double groupW = innerW + 2 * Pad;
            double totalW = Math.Max(880, n * groupW + Math.Max(0, n - 1) * GapDecl + 220);
            double yProg = 40;
            double yDecl = yProg + GapProgramToDecl;
            double yMod = yDecl + DeclHeaderH + GapVSmall;
            double yTyp = yMod + ModH + GapVSmall;
            // Шина ближе к нижнему ряду — в середине зазора между модификаторами и типом/литералом
            double yHub = yMod + ModH + (yTyp - (yMod + ModH)) * 0.78;
            double canvasH = yTyp + TypeH + 100;
            canvas.Width = totalW;
            canvas.Height = canvasH;

            double cx = totalW / 2;
            double progW = 236;
            var progPt = AddNodeBox(canvas, "ProgramNode", $"объявлений: {n}", cx - progW / 2, yProg, progW, 58, ColorProgram);
            if (n == 0)
                return;

            double rowW = n * groupW + (n - 1) * GapDecl;
            double startX = cx - rowW / 2;

            for (int i = 0; i < n; i++)
            {
                var decl = program.Declarations[i];
                double gx = startX + i * (groupW + GapDecl) + Pad;
                double trunkX = gx - TrunkOffset;

                var declPt = AddNodeBox(canvas, "ConstDeclNode", "объявление константы", gx, yDecl, innerW, DeclHeaderH, ColorDecl);
                AddArrow(canvas, progPt.X, progPt.Y, declPt.X, yDecl);

                double rowTop = yMod;
                double modRowOffset = (innerW - rowModW) / 2;
                double typeRowOffset = (innerW - rowTypeW) / 2;
                double mx = gx + modRowOffset;
                double tx = gx + typeRowOffset;

                var ptConst = AddNodeBox(canvas, "KeywordModifierNode", "«const»", mx, rowTop, ModW, ModH, ColorConst);
                var ptVal = AddNodeBox(canvas, "KeywordModifierNode", "«val»", mx + ModW + GapH, rowTop, ModW, ModH, ColorVal);
                var ptId = AddNodeBox(canvas, "IdentifierNode", "\"" + decl.Identifier.Name + "\"", mx + ModW + GapH + ModW + GapH, rowTop, IdW, IdH, ColorId);

                AddVerticalArrowToChild(canvas, ptConst.X, declPt.Y, rowTop);
                AddVerticalArrowToChild(canvas, ptVal.X, declPt.Y, rowTop);
                AddVerticalArrowToChild(canvas, ptId.X, declPt.Y, rowTop);

                double typeLeft = tx;
                double litLeft = tx + TypeW + GapTypeLit;
                var ptType = AddNodeBox(canvas, "DoubleTypeNode", decl.Type.TypeName, typeLeft, yTyp, TypeW, TypeH, ColorType);
                string litText = FormatLit(decl.Value);
                var ptLit = AddNodeBox(canvas, "DoubleLiteralNode", litText, litLeft, yTyp, LitW, LitH, ColorLit);

                // Стык снизу объявления: от левого нижнего угла до ствола (иначе «рвётся» линия у фиолетового блока)
                double declBottomY = declPt.Y;
                AddPlainLine(canvas, gx, declBottomY, trunkX, declBottomY);
                AddPlainLine(canvas, trunkX, declBottomY, trunkX, yHub);
                double xBusRight = Math.Max(typeLeft + TypeW, litLeft + LitW);
                AddPlainLine(canvas, trunkX, yHub, xBusRight, yHub);
                AddVerticalArrowToChild(canvas, ptType.X, yHub, yTyp);
                AddVerticalArrowToChild(canvas, ptLit.X, yHub, yTyp);
            }
        }

        private static void AddVerticalArrowToChild(Canvas canvas, double x, double yFrom, double yChildTop)
        {
            if (yChildTop <= yFrom + 0.5)
                return;
            AddArrow(canvas, x, yFrom, x, yChildTop);
        }

        private static void AddPlainLine(Canvas canvas, double x1, double y1, double x2, double y2)
        {
            var line = new Line
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Stroke = EdgeBrush,
                StrokeThickness = 1.35
            };
            Panel.SetZIndex(line, 0);
            canvas.Children.Add(line);
        }

        private static string FormatLit(DoubleLiteralNode v)
        {
            string num = double.IsFinite(v.Value)
                ? v.Value.ToString("G12", System.Globalization.CultureInfo.InvariantCulture)
                : v.Value.ToString();
            return num + Environment.NewLine + "текст: «" + v.RawText + "»";
        }

        private static Point AddNodeBox(Canvas canvas, string title, string subtitle, double x, double y, double w, double h, Color strokeColor)
        {
            var border = new Border
            {
                Width = w,
                Height = h,
                BorderBrush = new SolidColorBrush(strokeColor),
                BorderThickness = new Thickness(2),
                Background = new SolidColorBrush(Color.FromArgb(30, strokeColor.R, strokeColor.G, strokeColor.B)),
                CornerRadius = new CornerRadius(4),
                SnapsToDevicePixels = true
            };

            var sp = new StackPanel { Margin = new Thickness(6, 4, 6, 4) };
            sp.Children.Add(new TextBlock
            {
                Text = title,
                FontWeight = FontWeights.SemiBold,
                FontSize = 11,
                TextAlignment = TextAlignment.Center,
                Foreground = new SolidColorBrush(strokeColor)
            });
            sp.Children.Add(new TextBlock
            {
                Text = subtitle,
                FontSize = 9.5,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(30, 30, 30))
            });
            border.Child = sp;

            Canvas.SetLeft(border, x);
            Canvas.SetTop(border, y);
            Panel.SetZIndex(border, 2);
            canvas.Children.Add(border);
            return new Point(x + w / 2, y + h);
        }

        private static void AddArrow(Canvas canvas, double x1, double y1, double x2, double y2)
        {
            double dx = x2 - x1;
            double dy = y2 - y1;
            double len = Math.Sqrt(dx * dx + dy * dy);
            if (len < 1)
                return;

            double ux = dx / len;
            double uy = dy / len;
            const double tip = 9;
            const double wing = 5.5;
            double xTip = x2;
            double yTip = y2;
            double xBase = xTip - ux * tip;
            double yBase = yTip - uy * tip;
            double px = -uy * wing;
            double py = ux * wing;

            var line = new Line
            {
                X1 = x1,
                Y1 = y1,
                X2 = xBase,
                Y2 = yBase,
                Stroke = EdgeBrush,
                StrokeThickness = 1.35
            };
            Panel.SetZIndex(line, 1);
            canvas.Children.Add(line);

            var head = new Polygon
            {
                Fill = EdgeBrush,
                Points = new PointCollection
                {
                    new Point(xTip, yTip),
                    new Point(xBase + px, yBase + py),
                    new Point(xBase - px, yBase - py)
                }
            };
            Panel.SetZIndex(head, 1);
            canvas.Children.Add(head);
        }
    }
}
