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
            textBlock.Inlines!.Add(CreateRun("📖 JAVASCRIPT CANVAS API REFERENCE\n\n",
                Brushes.White, FontWeight.Bold));
            textBlock.Inlines.Add(CreateRun("══════════════════════════════════════════════════════════════════════\n\n",
                "#7F8C8D"));

            // Global Variables
            AddSection(textBlock, "🌍 GLOBAL VARIABLES",
                " time      // Current animation time in seconds (auto-incrementing)\n" +
                " width     // Canvas width in pixels\n" +
                " height    // Canvas height in pixels\n" +
                "\n" +
                " Example: let x = Math.sin(time) * width / 2 + width / 2;\n\n");

            // Time Control
            AddSection(textBlock, "⏱️ TIME CONTROL (Preview Only - Scriptable)",
                " timeControl.Speed           // Get/set speed (0.0 to 10.0)\n" +
                " timeControl.IsPaused        // Get/set pause state (true/false)\n" +
                " timeControl.Current         // Get current time (read-only)\n" +
                " timeControl.SetSpeed(num)   // Set animation speed\n" +
                " timeControl.Pause()         // Pause animation\n" +
                " timeControl.Resume()        // Resume animation\n" +
                " timeControl.Toggle()        // Toggle pause/play\n" +
                "\n" +
                " Examples:\n" +
                "   // Speed based on audio\n" +
                "   timeControl.Speed = 0.5 + audio.Volume * 3;\n" +
                "   \n" +
                "   // Pause on bass drop\n" +
                "   if (audio.Bass > 0.8) timeControl.Pause();\n" +
                "   else timeControl.Resume();\n\n");

            // Audio Reactivity
            AddSection(textBlock, "🎵 AUDIO REACTIVITY (Live Audio Capture)",
                " audio.Bass         // Bass frequencies (0.0-1.0)\n" +
                " audio.Midrange     // Mid frequencies (0.0-1.0)\n" +
                " audio.Treble       // High frequencies (0.0-1.0)\n" +
                " audio.Volume       // Overall volume (0.0-1.0)\n" +
                " audio.IsEnabled    // Audio capture active? (boolean)\n" +
                " audio.GetBand(i)   // Get specific frequency band (0-31)\n" +
                " audio.GetRange(s,e)// Get average of bands s to e\n" +
                "\n" +
                " Examples:\n" +
                "   let pulse = audio.Bass * 100;  // 0-100 pixels\n" +
                "   ctx.fillCircle(width/2, height/2, 50 + pulse);\n" +
                "   \n" +
                "   // Use specific frequency range\n" +
                "   let lowBass = audio.GetRange(0, 4);\n\n");

            // Drawing Shapes
            AddSection(textBlock, "🎨 DRAWING SHAPES (Basic Primitives)",
                " ctx.clear(r,g,b)              // Clear entire canvas (RGB: 0-255)\n" +
                " ctx.fillRect(x,y,w,h)         // Draw filled rectangle\n" +
                " ctx.strokeRect(x,y,w,h)       // Draw rectangle outline\n" +
                " ctx.clearRect(x,y,w,h)        // Erase rectangle area\n" +
                " ctx.fillCircle(x,y,radius)    // Draw filled circle\n" +
                " ctx.strokeCircle(x,y,radius)  // Draw circle outline\n" +
                " ctx.drawLine(x1,y1,x2,y2)     // Draw line between points\n" +
                "\n" +
                " Example:\n" +
                "   ctx.clear(0, 0, 0);  // Black background\n" +
                "   ctx.fillStyle(255, 0, 0);  // Red color\n" +
                "   ctx.fillCircle(width/2, height/2, 50);\n\n");

            // Paths
            AddSection(textBlock, "📐 PATHS (Advanced Shapes)",
                " ctx.beginPath()                         // Start new path\n" +
                " ctx.closePath()                         // Close path to start\n" +
                " ctx.moveTo(x,y)                         // Move without drawing\n" +
                " ctx.lineTo(x,y)                         // Draw line to point\n" +
                " ctx.arc(x,y,r,start,end,ccw)           // Add arc (angles in radians)\n" +
                " ctx.arcTo(x1,y1,x2,y2,radius)           // Arc with control points\n" +
                " ctx.rect(x,y,w,h)                       // Add rectangle to path\n" +
                " ctx.ellipse(x,y,rx,ry,rot,s,e,ccw)     // Add ellipse\n" +
                " ctx.quadraticCurveTo(cpx,cpy,x,y)       // Quadratic bezier curve\n" +
                " ctx.bezierCurveTo(cp1x,cp1y,cp2x,cp2y,x,y) // Cubic bezier\n" +
                " ctx.fill()                              // Fill the path\n" +
                " ctx.stroke()                            // Outline the path\n" +
                " ctx.clip()                              // Use path as clipping mask\n" +
                " ctx.isPointInPath(x,y)                  // Check if point in path\n" +
                " ctx.isPointInStroke(x,y)                // Check if point on stroke\n" +
                "\n" +
                " Example - Triangle:\n" +
                "   ctx.beginPath();\n" +
                "   ctx.moveTo(width/2, 50);\n" +
                "   ctx.lineTo(width/2+50, 150);\n" +
                "   ctx.lineTo(width/2-50, 150);\n" +
                "   ctx.closePath();\n" +
                "   ctx.fill();\n\n");

            // Styles
            AddSection(textBlock, "🎨 COLORS & STYLES",
                " ctx.fillStyle(r,g,b,a)        // Set fill color (a=alpha optional)\n" +
                " ctx.strokeStyle(r,g,b,a)      // Set stroke color\n" +
                " ctx.lineWidth(width)          // Set line thickness (pixels)\n" +
                " ctx.lineCap(style)            // Line end style: 'butt','round','square'\n" +
                " ctx.lineJoin(style)           // Line corner: 'miter','round','bevel'\n" +
                " ctx.miterLimit(limit)         // Max miter length for sharp corners\n" +
                " ctx.globalAlpha(alpha)        // Global transparency (0.0-1.0)\n" +
                " ctx.globalCompositeOperation(mode) // Blend mode (see below)\n" +
                "\n" +
                " Example:\n" +
                "   ctx.fillStyle(255, 128, 0, 0.5);  // Orange, 50% transparent\n" +
                "   ctx.lineWidth(5);\n" +
                "   ctx.lineCap('round');\n\n");

            // Gradients
            AddSection(textBlock, "🌈 GRADIENTS (Color Transitions)",
                " // Create gradient object:\n" +
                " let grad = ctx.createLinearGradient(x0,y0,x1,y1);\n" +
                " let grad = ctx.createRadialGradient(x0,y0,r0,x1,y1,r1);\n" +
                " \n" +
                " // Add color stops (position 0.0 to 1.0):\n" +
                " grad.addColorStop(0.0, 255,0,0);      // Red at start\n" +
                " grad.addColorStop(0.5, 0,255,0);      // Green at middle\n" +
                " grad.addColorStop(1.0, 0,0,255);      // Blue at end\n" +
                " \n" +
                " // Use gradient:\n" +
                " ctx.fillStyleGradient(grad);\n" +
                " ctx.strokeStyleGradient(grad);\n" +
                "\n" +
                " Example - Vertical rainbow:\n" +
                "   let g = ctx.createLinearGradient(0, 0, 0, height);\n" +
                "   g.addColorStop(0, 255,0,0);\n" +
                "   g.addColorStop(1, 0,0,255);\n" +
                "   ctx.fillStyleGradient(g);\n" +
                "   ctx.fillRect(0, 0, width, height);\n\n");

            // Shadows
            AddSection(textBlock, "💫 SHADOWS & EFFECTS",
                " ctx.shadowBlur(blur)          // Shadow blur radius (pixels)\n" +
                " ctx.shadowColor(r,g,b,a)      // Shadow color\n" +
                " ctx.shadowOffsetX(x)          // Horizontal shadow offset\n" +
                " ctx.shadowOffsetY(y)          // Vertical shadow offset\n" +
                "\n" +
                " Example - Glowing circle:\n" +
                "   ctx.shadowBlur(20);\n" +
                "   ctx.shadowColor(0, 255, 255, 1);\n" +
                "   ctx.fillStyle(0, 255, 255);\n" +
                "   ctx.fillCircle(width/2, height/2, 30);\n\n");

            // Transformations
            AddSection(textBlock, "🔄 TRANSFORMATIONS (Coordinate System)",
                " ctx.save()                    // Save current transform state\n" +
                " ctx.restore()                 // Restore saved state\n" +
                " ctx.translate(x,y)            // Move origin point\n" +
                " ctx.rotate(angle)             // Rotate (radians, use Math.PI)\n" +
                " ctx.scale(sx,sy)              // Scale x and y axis\n" +
                " ctx.resetTransform()          // Reset to identity matrix\n" +
                " ctx.transform(a,b,c,d,e,f)    // Apply transform matrix\n" +
                " ctx.setTransform(a,b,c,d,e,f) // Replace transform matrix\n" +
                "\n" +
                " Example - Rotating square:\n" +
                "   ctx.save();\n" +
                "   ctx.translate(width/2, height/2);    // Move to center\n" +
                "   ctx.rotate(time * Math.PI / 2);      // Rotate over time\n" +
                "   ctx.fillRect(-25, -25, 50, 50);      // Draw centered square\n" +
                "   ctx.restore();\n\n");

            // Text
            AddSection(textBlock, "📝 TEXT RENDERING",
                " ctx.Font('24px Arial')        // Set font (CSS format)\n" +
                " ctx.textAlign(align)          // 'left','center','right','start','end'\n" +
                " ctx.textBaseline(baseline)    // 'top','middle','bottom','alphabetic'\n" +
                " ctx.fillText(text,x,y)        // Draw filled text\n" +
                " ctx.strokeText(text,x,y)      // Draw outlined text\n" +
                " ctx.measureText(text)         // Returns {width: pixels}\n" +
                "\n" +
                " Example:\n" +
                "   ctx.Font('48px Arial');\n" +
                "   ctx.textAlign('center');\n" +
                "   ctx.fillStyle(255, 255, 255);\n" +
                "   ctx.fillText('Hello!', width/2, height/2);\n\n");

            // Compositing
            AddSection(textBlock, "🎭 BLEND MODES (globalCompositeOperation)",
                " 'source-over'    // Default - draw on top\n" +
                " 'multiply'       // Multiply colors (darker)\n" +
                " 'screen'         // Screen colors (lighter)\n" +
                " 'overlay'        // Combination of multiply/screen\n" +
                " 'darken'         // Keep darkest pixels\n" +
                " 'lighten'        // Keep lightest pixels\n" +
                " 'color-dodge'    // Brighten based on color\n" +
                " 'color-burn'     // Darken based on color\n" +
                " 'hard-light'     // Intense overlay\n" +
                " 'soft-light'     // Subtle overlay\n" +
                " 'difference'     // Subtract colors\n" +
                " 'exclusion'      // Similar to difference, lower contrast\n" +
                " 'hue'            // Use hue of source\n" +
                " 'saturation'     // Use saturation of source\n" +
                " 'color'          // Use hue & saturation of source\n" +
                " 'luminosity'     // Use luminosity of source\n" +
                "\n" +
                " Example:\n" +
                "   ctx.globalCompositeOperation('multiply');\n\n");

            // Helper Functions
            AddSection(textBlock, "🛠️ HELPER FUNCTIONS",
                " ctx.hslToRgb(h,s,l)           // Convert HSL to RGB\n" +
                "   // h, s, l: 0.0-1.0  →  returns {r, g, b} 0-255\n" +
                " \n" +
                " ctx.rgbToHsl(r,g,b)           // Convert RGB to HSL\n" +
                "   // r, g, b: 0-255  →  returns {h, s, l} 0.0-1.0\n" +
                "\n" +
                " Example - Rainbow over time:\n" +
                "   for (let x = 0; x < width; x++) {\n" +
                "     let hue = (x / width + time * 0.1) % 1.0;\n" +
                "     let rgb = ctx.hslToRgb(hue, 1.0, 0.5);\n" +
                "     ctx.fillStyle(rgb.r, rgb.g, rgb.b);\n" +
                "     ctx.fillRect(x, 0, 1, height);\n" +
                "   }\n\n");

            // Quick Examples
            AddSection(textBlock, "💡 QUICK START EXAMPLES",
                " // Pulsing circle:\n" +
                "   ctx.clear(0, 0, 0);\n" +
                "   let radius = 50 + Math.sin(time * 2) * 20;\n" +
                "   ctx.fillStyle(255, 0, 128);\n" +
                "   ctx.fillCircle(width/2, height/2, radius);\n" +
                "\n" +
                " // Audio-reactive bars:\n" +
                "   ctx.clear(0, 0, 0);\n" +
                "   for (let i = 0; i < 32; i++) {\n" +
                "     let h = audio.GetBand(i) * height;\n" +
                "     ctx.fillStyle(i * 8, 100, 255);\n" +
                "     ctx.fillRect(i * width/32, height - h, width/32, h);\n" +
                "   }\n" +
                "\n" +
                " // Spinning gradient:\n" +
                "   ctx.save();\n" +
                "   ctx.translate(width/2, height/2);\n" +
                "   ctx.rotate(time);\n" +
                "   let g = ctx.createLinearGradient(-100, 0, 100, 0);\n" +
                "   g.addColorStop(0, 255,0,0);\n" +
                "   g.addColorStop(1, 0,0,255);\n" +
                "   ctx.fillStyleGradient(g);\n" +
                "   ctx.fillRect(-100, -100, 200, 200);\n" +
                "   ctx.restore();\n\n");

            textBlock.Inlines.Add(CreateRun("══════════════════════════════════════════════════════════════════════\n",
                "#7F8C8D"));
        }

        private void AddSection(SelectableTextBlock textBlock, string title, string content)
        {
            textBlock.Inlines!.Add(CreateRun(title + "\n", "#FFD700", FontWeight.Bold));
            textBlock.Inlines.Add(CreateRun(content, "#A8E6CF"));
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
