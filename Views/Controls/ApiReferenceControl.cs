using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace Artemis.Plugins.LayerBrushes.JavascriptCanvas.Views.Controls
{
    public class ApiReferenceControl : Border
    {
        public ApiReferenceControl()
        {
            Background = new SolidColorBrush(Color.Parse("#2C3E50"));
            Padding = new Thickness(10);
            CornerRadius = new CornerRadius(4);
            Margin = new Thickness(0, 10, 0, 0);

            var apiTextBlock = new SelectableTextBlock
            {
                FontFamily = new FontFamily("Consolas"),
                FontSize = 10,
                TextWrapping = TextWrapping.Wrap
            };

            AddApiContent(apiTextBlock);

            Child = new ScrollViewer
            {
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                Content = apiTextBlock
            };
        }

        private void AddApiContent(SelectableTextBlock textBlock)
        {
            textBlock.Inlines!.Add(CreateRun("✨ JAVASCRIPT CANVAS API REFERENCE\n", Brushes.White, FontWeight.Bold));
            textBlock.Inlines.Add(CreateRun("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n", "#7F8C8D"));

            // PERSISTENT STATE
            AddSection(textBlock, "🔄 PERSISTENT STATE",
                @"Store data that survives between frames:

state.variableName = value;      // Store any value
if (state.x === undefined) {     // Check if initialized
    state.x = 0;                 // Initialize on first frame
}

Examples:
  // Position that persists
  if (state.x === undefined) state.x = width/2;
  state.x += 2;  // Increments every frame

  // Smooth audio
  if (state.smoothBass === undefined) state.smoothBass = 0;
  state.smoothBass += (audio.Bass - state.smoothBass) * 0.2;

⚠️ Note: Always check undefined before first use!
");

            // Global Variables
            AddSection(textBlock, "🌐 GLOBAL VARIABLES",
                @"time          Current animation time (seconds, auto-incrementing)
width         Canvas width in pixels
height        Canvas height in pixels

Example:
  let x = Math.sin(time) * (width / 2) + (width / 2);
");

            // Time Control
            AddSection(textBlock, "⏱️ TIME CONTROL",
                @"timeControl.Speed           Get/set speed (0.0 to 10.0)
timeControl.IsPaused        Get/set pause state (true/false)
timeControl.Current         Get current time (read-only)
timeControl.SetSpeed(num)   Set animation speed
timeControl.Pause()         Pause animation
timeControl.Resume()        Resume animation
timeControl.Toggle()        Toggle pause/play
");

            // Audio
            AddSection(textBlock, "🎵 AUDIO REACTIVITY",
                @"audio.Bass              Bass frequencies (0.0-1.0)
audio.Midrange          Mid frequencies (0.0-1.0)
audio.Treble           High frequencies (0.0-1.0)
audio.Volume           Overall volume (0.0-1.0)
audio.IsEnabled        Audio capture active? (boolean)
audio.GetBand(i)       Get specific frequency band 0-31
audio.GetRange(s, e)   Get average of bands s to e

Example:
  let pulse = audio.Bass * 100;
  ctx.fillCircle(width/2, height/2, 50 + pulse);
");

            // Drawing Shapes
            AddSection(textBlock, "🎨 DRAWING SHAPES",
                @"ctx.clear(r, g, b)              Clear entire canvas (RGB 0-255)
ctx.fillRect(x, y, w, h)        Draw filled rectangle
ctx.strokeRect(x, y, w, h)      Draw rectangle outline
ctx.clearRect(x, y, w, h)       Erase rectangle area
ctx.fillCircle(x, y, radius)    Draw filled circle
ctx.strokeCircle(x, y, radius)  Draw circle outline
ctx.drawLine(x1, y1, x2, y2)    Draw line between points
");

            // Paths
            AddSection(textBlock, "📐 PATHS (Advanced Shapes)",
                @"ctx.beginPath()                           Start new path
ctx.closePath()                           Close path to start
ctx.moveTo(x, y)                          Move without drawing
ctx.lineTo(x, y)                          Draw line to point
ctx.arc(x, y, r, start, end, ccw)         Add arc (radians)
ctx.arcTo(x1, y1, x2, y2, radius)         Arc with control points
ctx.rect(x, y, w, h)                      Add rectangle to path
ctx.ellipse(x, y, rx, ry, rot, s, e, ccw) Add ellipse
ctx.quadraticCurveTo(cpx, cpy, x, y)      Quadratic bezier
ctx.bezierCurveTo(cp1x, cp1y, cp2x, cp2y, x, y)  Cubic bezier
ctx.fill()                                Fill the path
ctx.stroke()                              Outline the path
ctx.clip()                                Use path as clipping mask
");

            // Colors & Styles - UPDATED TO SHOW FUNCTIONS
            AddSection(textBlock, "🎨 COLORS & STYLES",
                @"ctx.fillStyle(r, g, b, a)       Set fill color (a=alpha optional)
ctx.strokeStyle(r, g, b, a)     Set stroke color
ctx.lineWidth(width)            Set line thickness (pixels)
ctx.lineCap(style)              ""butt"", ""round"", ""square""
ctx.lineJoin(style)             ""miter"", ""round"", ""bevel""
ctx.miterLimit(limit)           Max miter length
ctx.globalAlpha(alpha)          Global transparency (0.0-1.0)

Example:
  ctx.fillStyle(255, 128, 0, 0.5);
  ctx.lineWidth(5);
  ctx.lineCap(""round"");
");

            // Text - UPDATED WITH CORRECT FUNCTION NAMES
            AddSection(textBlock, "📝 TEXT RENDERING",
                @"ctx.setFont(""size family"")     Set font (e.g. ""24px Arial"")
ctx.textAlign(""align"")        ""left"", ""center"", ""right"", ""start"", ""end""
ctx.textBaseline(""base"")      ""top"", ""middle"", ""bottom"", ""alphabetic""
ctx.fillText(text, x, y)        Draw filled text
ctx.strokeText(text, x, y)      Draw outlined text
ctx.measureText(text)           Returns text width (pixels)

Examples:
  // 1. Set Style
  ctx.setFont(""48px Arial"");
  ctx.textAlign(""center"");
  ctx.textBaseline(""middle"");
  
  // 2. Draw
  ctx.fillStyle(255, 255, 255);
  ctx.fillText(""Hello!"", width/2, height/2);

  // 3. Dynamic Text
  ctx.fillText(""Score: "" + state.score, 10, 30);
");

            // Gradients
            AddSection(textBlock, "🌈 GRADIENTS",
                @"Create gradient:
  let grad = ctx.createLinearGradient(x0, y0, x1, y1);
  let grad = ctx.createRadialGradient(x0, y0, r0, x1, y1, r1);

Add stops & Use:
  grad.addColorStop(0.0, 255, 0, 0);
  grad.addColorStop(1.0, 0, 0, 255);
  
  ctx.fillStyleGradient(grad);
  ctx.strokeStyleGradient(grad);
");

            // Shadows
            AddSection(textBlock, "✨ SHADOWS & EFFECTS",
                @"ctx.shadowBlur(radius)          Set shadow blur radius
ctx.shadowColor(r, g, b, a)     Set shadow color
ctx.shadowOffsetX(x)            Horizontal offset
ctx.shadowOffsetY(y)            Vertical offset

Example - Glowing text:
  ctx.shadowBlur(15);
  ctx.shadowColor(255, 0, 0, 255); // Red glow
  ctx.fillStyle(255, 255, 255);
  ctx.fillText(""GLOW"", 50, 50);
");

            // Transformations
            AddSection(textBlock, "🔄 TRANSFORMATIONS",
                @"ctx.save()                  Save current state
ctx.restore()               Restore saved state
ctx.translate(x, y)         Move origin
ctx.rotate(angle)           Rotate (radians)
ctx.scale(sx, sy)           Scale
ctx.resetTransform()        Reset matrix
");

            // Helper Functions
            AddSection(textBlock, "🛠️ HELPER FUNCTIONS",
                @"ctx.hslToRgb(h, s, l)   Convert HSL (0-1) to RGB (0-255)
ctx.rgbToHsl(r, g, b)   Convert RGB (0-255) to HSL (0-1)
console.log(msg)        Print to Artemis Debug Log

Example - Rainbow:
  let rgb = ctx.hslToRgb(time * 0.1, 1.0, 0.5);
  ctx.fillStyle(rgb.r, rgb.g, rgb.b);
  ctx.fillRect(0, 0, width, height);
");

            textBlock.Inlines.Add(CreateRun("\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n", "#7F8C8D"));
            textBlock.Inlines.Add(CreateRun("💡 Tip: Use 'state' object for animations!", "#FFD700"));
        }

        private void AddSection(SelectableTextBlock textBlock, string title, string content)
        {
            textBlock.Inlines!.Add(CreateRun($"{title}\n", "#FFD700", FontWeight.Bold));
            textBlock.Inlines.Add(CreateRun($"{content}\n\n", "#A8E6CF"));
        }

        private Run CreateRun(string text, string color, FontWeight? weight = null)
        {
            return CreateRun(text, new SolidColorBrush(Color.Parse(color)), weight);
        }

        private Run CreateRun(string text, IBrush brush, FontWeight? weight = null)
        {
            var run = new Run { Text = text, Foreground = brush };
            if (weight.HasValue)
                run.FontWeight = weight.Value;
            return run;
        }
    }
}