# JavaScript Canvas Plugin for Artemis RGB

Execute JavaScript to create custom LED effects with a Canvas-like API for [Artemis RGB](https://github.com/Artemis-RGB/Artemis).

## Overview

This plugin adds a **JavaScript Canvas** layer brush to Artemis, allowing you to script dynamic LED effects using an API modeled after HTML5 Canvas 2D and rendered via SkiaSharp. Create everything from simple color waves to complex audio-reactive visualizations with real-time JavaScript execution.

## ✨ Features

- JavaScript-driven LED effects using a Canvas-style drawing context. 
- Rich built-in editor with syntax highlighting, live preview, error display, and size presets. 
- Global scripts that automatically appear in all new brush instances. 
- Frame skip control to balance performance and smoothness. 
- Default effect library: rainbow waves, breathing, moving gradients, fire, scan line, and more. 

## 📦 Installation

1. Build or download `Artemis.Plugins.LayerBrushes.JavascriptCanvas.dll`
2. Copy the DLL (and its dependencies) into your Artemis plugins folder:
   - Windows: typically under `%AppData%\Artemis\plugins`.  
   - Linux/macOS: under your Artemis configuration/plugins directory.  
3. Ensure `plugin.json` is placed alongside the DLL.
4. Start Artemis; the **JavaScript Canvas** brush will be registered by the brush provider.

## Usage

## 🚀 Usage

1. In Artemis, open your profile and add a new layer.  
2. Choose the **JavaScript Canvas** brush.
3. Open the brush configuration dialog to edit scripts and preview the canvas.

### Editor Interface

The configuration UI provides:

- **Script selector** - Choose from your scripts or create new ones
- **Add/Delete** - Manage your script library
- **Import/Export** - Share scripts with the community
- **Live preview** - Real-time canvas visualization with playback controls
- **Playback controls** - Play/pause, adjust speed (0.25x - 4x), reset time
- **Canvas size controls** - Adjust dimensions with presets (Ultrawide, Standard, Compact, Tall)
- **Frame skip** - Update every N frames to balance performance (default: 1 = 60 FPS)
- **Error display** - Formatted JavaScript errors with line and column info
- **Script name editor** - Rename scripts inline
- **Unsaved changes indicator** - Visual feedback for modified scripts

## 📚 Scripting API

### Global Variables

Within JavaScript, the following globals are available: 

- `width`: Canvas width in pixels.
- `height`: Canvas height in pixels. 
- `time`: Elapsed time in seconds, accumulated by the brush update. 

### CanvasContext Methods

The `ctx` object implements a subset of the HTML5 Canvas 2D context:

**Shapes and clearing**  
- `ctx.fillRect(x, y, w, h)`  
- `ctx.strokeRect(x, y, w, h)`  
- `ctx.clearRect(x, y, w, h)`  
- `ctx.fillCircle(x, y, r)`  
- `ctx.strokeCircle(x, y, r)`  
- `ctx.clear(r, g, b)` – convenience full-canvas clear.

**Paths**  
- `ctx.beginPath()` / `ctx.closePath()` / `ctx.fill()` / `ctx.stroke()` / `ctx.clip()`  
- `ctx.moveTo(x, y)` / `ctx.lineTo(x, y)` / `ctx.drawLine(x1, y1, x2, y2)`  
- `ctx.arc(...)`, `ctx.arcTo(...)`, `ctx.rect(...)`, `ctx.ellipse(...)`  
- `ctx.quadraticCurveTo(...)`, `ctx.bezierCurveTo(...)`  

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
### Example Script: Fire Effect
```
// Fire effect
for (let x = 0; x < width; x++) {
for (let y = 0; y < height; y++) {
let yPos = y / height;
let noise = Math.sin(x * 0.1 + time * 3) * 0.5 + 0.5;
let intensity = (1 - yPos) * noise;
let r = Math.floor(255 * intensity);
let g = Math.floor(100 * intensity * 0.5);
let b = 0;
ctx.fillStyle(r, g, b);
ctx.fillRect(x, y, 1, 1);
}
}
```
