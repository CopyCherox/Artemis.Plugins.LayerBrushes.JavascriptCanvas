# Javascript Canvas Plugin for Artemis RGB

Execute Javascript to create custom LED effects with a Canvas-like API for [Artemis RGB](https://github.com/Artemis-RGB/Artemis).

## Overview

This plugin adds a **Javascript Canvas** layer brush to Artemis, allowing you to script dynamic LED effects using an API modeled after HTML5 Canvas 2D and rendered via SkiaSharp. Create everything from simple color waves to complex audio-reactive visualizations with real-time JavaScript execution.

## ✨ Features

- **Javascript-driven LED effects** using a Canvas-style drawing context
- **Audio reactivity** - Access real-time bass, midrange, treble, and frequency bands
- **Time control** - Dynamically adjust animation speed and pause/resume effects
- **Rich built-in editor** with syntax highlighting, live preview, and error display
- **Live preview canvas** with playback controls (play/pause, speed adjustment)
- **Script management** - Create, edit, rename, delete, import, and export scripts
- **Global script sharing** - Scripts automatically sync across all layers
- **Performance optimized** - Highly efficient rendering engine with configurable frame skip
- **Performance monitoring** - View real-time metrics in Artemis debugger
- **Default effect library** - Rainbow waves, breathing, plasma, fire, and more

## 📦 Installation

### Option 1: Artemis Plugin Workshop (Recommended)
1. Open Artemis
2. Go to **Settings → Plugins → Workshop**
3. Search for "Javascript Canvas"
4. Click **Install**

### Option 2: Manual Installation
1. Download the latest release from the [Releases](https://github.com/CopyCherox/Artemis.Plugins.LayerBrushes.JavascriptCanvas/releases) page
2. Extract the files to your Artemis plugins folder:
   - **Windows**: `%ProgramData%\Artemis\Plugins\`
   - **Linux/macOS**: `~/.config/Artemis/plugins/`
3. Restart Artemis

## 🚀 Usage

### Adding the Brush
1. Open your Artemis profile
2. Add a new layer
3. Select **Javascript Canvas** as the layer brush under General
4. Click the brush settings to open the editor

### Editor Interface

The configuration UI provides:

- **Script selector** - Choose from your scripts or create new ones
- **Add/Delete** - Manage your script library
- **Import/Export** - Share scripts with the community
- **Live preview** - Real-time canvas visualization with playback controls
- **Playback controls** - Play/pause, adjust speed (0.25x - 4x), reset time
- **Canvas size controls** - Adjust dimensions with presets (Ultrawide, Standard, Compact, Tall)
- **Frame skip** - Update every N frames to balance performance (default: 1 = 60 FPS)
- **Error display** - Formatted Javascript errors with line and column info
- **Script name editor** - Rename scripts inline
- **Unsaved changes indicator** - Visual feedback for modified scripts

## 📚 Scripting API

### Global Variables

```javascript
width          // Canvas width in pixels
height         // Canvas height in pixels
time           // Elapsed time in seconds
ctx            // CanvasContext object (drawing API)
audio          // AudioContext object (audio reactivity)
timeControl    // TimeControl object (playback control)
```

### Audio Reactivity

```javascript
audio.Bass        // 0-1: Bass frequency energy
audio.Midrange    // 0-1: Mid frequency energy
audio.Treble      // 0-1: High frequency energy
audio.Volume      // 0-1: Overall volume level
audio.GetBand(i)  // 0-1: Energy of frequency band i (0-31)
```

**Example:**
```javascript
let pulse = audio.Bass * 0.5 + 0.5;
let hue = (time * 0.2 + audio.Treble * 0.3) % 1.0;
```

### Time Control

```javascript
timeControl.Speed      // Get/set animation speed (0.0 - 10.0)
timeControl.IsPaused   // Get/set pause state
timeControl.Current    // Get current time value
timeControl.SetSpeed(speed)  // Set speed programmatically
timeControl.Pause()    // Pause animation
timeControl.Resume()   // Resume animation
timeControl.Toggle()   // Toggle pause/resume
```

**Example:**
```javascript
// Make animation speed react to audio
timeControl.Speed = 0.5 + audio.Volume * 2.5;
```

### CanvasContext (ctx) Methods

#### Shapes & Clearing
```javascript
ctx.fillRect(x, y, w, h)
ctx.strokeRect(x, y, w, h)
ctx.clearRect(x, y, w, h)
ctx.fillCircle(x, y, radius)
ctx.strokeCircle(x, y, radius)
ctx.clear(r, g, b)  // Full-canvas clear
```

#### Paths
```javascript
ctx.beginPath()
ctx.closePath()
ctx.moveTo(x, y)
ctx.lineTo(x, y)
ctx.arc(x, y, radius, startAngle, endAngle, counterclockwise)
ctx.arcTo(x1, y1, x2, y2, radius)
ctx.quadraticCurveTo(cpx, cpy, x, y)
ctx.bezierCurveTo(cp1x, cp1y, cp2x, cp2y, x, y)
ctx.rect(x, y, w, h)
ctx.ellipse(x, y, radiusX, radiusY, rotation, startAngle, endAngle, counterclockwise)
ctx.fill()
ctx.stroke()
ctx.clip()
```

#### Styles & Colors
```javascript
ctx.fillStyle(r, g, b, a)     // Set fill color (RGB 0-255, A 0-1)
ctx.strokeStyle(r, g, b, a)   // Set stroke color
ctx.lineWidth(width)
ctx.lineCap('butt' | 'round' | 'square')
ctx.lineJoin('miter' | 'round' | 'bevel')
ctx.miterLimit(limit)
ctx.globalAlpha(alpha)        // 0-1
ctx.globalCompositeOperation(mode)  // 'source-over', 'multiply', 'screen', etc.
```

#### Gradients
```javascript
let gradient = ctx.createLinearGradient(x0, y0, x1, y1);
gradient.addColorStop(offset, r, g, b, a);
ctx.fillStyleGradient(gradient);

let radial = ctx.createRadialGradient(x0, y0, r0, x1, y1, r1);
radial.addColorStop(0, 255, 0, 0);
ctx.strokeStyleGradient(radial);
```

#### Transforms
```javascript
ctx.save()
ctx.restore()
ctx.translate(x, y)
ctx.rotate(angle)
ctx.scale(x, y)
ctx.resetTransform()
ctx.transform(a, b, c, d, e, f)
ctx.setTransform(a, b, c, d, e, f)
```

#### Text
```javascript
ctx.font = '24px Arial';
ctx.textAlign = 'left' | 'center' | 'right' | 'start' | 'end';
ctx.textBaseline = 'top' | 'middle' | 'alphabetic' | 'bottom';
ctx.fillText(text, x, y);
ctx.strokeText(text, x, y);
let metrics = ctx.measureText(text);  // Returns {width}
```

#### Shadows
```javascript
ctx.shadowBlur = 10;
ctx.shadowColor(r, g, b, a);
ctx.shadowOffsetX = 5;
ctx.shadowOffsetY = 5;
```

#### Color Helpers
```javascript
let rgb = ctx.hslToRgb(h, s, l);  // h,s,l in 0-1, returns {r, g, b} in 0-255
let hsl = ctx.rgbToHsl(r, g, b);  // r,g,b in 0-255, returns {h, s, l} in 0-1
```

## 🎨 Example Scripts

### Audio-Reactive Plasma
```javascript
const scale = 0.015;
const t = time * 0.5;
const bass = audio.Bass * 2.0;

for (let y = 0; y < height; y += 8) {
  for (let x = 0; x < width; x += 8) {
    const plasma = 
      Math.sin((x + t * 50) * scale + bass) +
      Math.cos((y + t * 30) * scale) +
      Math.sin((x + y + t * 40) * scale * 0.5);

    const hue = (plasma * 0.5 + 0.5 + audio.Volume * 0.4) % 1;
    const rgb = ctx.hslToRgb(hue, 1.0, 0.5);

    ctx.fillStyle(rgb.r, rgb.g, rgb.b);
    ctx.fillRect(x, y, 8, 8);
  }
}
```

### Moving Circles Grid
```javascript
const gridCols = 16;
const gridRows = 8;
const cellW = width / gridCols;
const cellH = height / gridRows;

for (let x = 0; x < gridCols; x++) {
  for (let y = 0; y < gridRows; y++) {
    let wave = Math.sin(time * 2.0 - x * 0.8 + y * 0.3) * 0.5 + 0.5;
    let pulse = wave * 0.7 + audio.GetBand((x + y * gridCols) % 32) * 0.3;

    let hue = (x / gridCols - time * 0.2 + y / gridRows * 0.3) % 1.0;
    let rgb = ctx.hslToRgb(hue, 1.0, 0.5);

    let size = (0.2 + pulse * 0.7) * Math.min(cellW, cellH);

    ctx.fillStyle(rgb.r, rgb.g, rgb.b);
    ctx.fillCircle(x * cellW + cellW/2, y * cellH + cellH/2, size);
  }
}
```

### Rainbow Wave
```javascript
for (let x = 0; x < width; x++) {
  let hue = ((x / width) + time * 0.5) % 1.0;
  let rgb = ctx.hslToRgb(hue, 1.0, 0.5);
  ctx.fillStyle(rgb.r, rgb.g, rgb.b);
  ctx.fillRect(x, 0, 1, height);
}
```


**Made with ❤️ for the Artemis RGB community**
