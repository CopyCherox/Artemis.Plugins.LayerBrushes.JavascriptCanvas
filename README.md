# JavaScript Canvas Plugin for Artemis

Execute JavaScript to create custom LED effects with a Canvas-like API for Artemis RGB.

## Overview

This plugin adds a **JavaScript Canvas** layer brush to Artemis, allowing you to script LED effects using an API modeled after `CanvasRenderingContext2D` and rendered via SkiaSharp. Scripts are edited live inside Artemis with real-time previews and can be shared globally across profiles. 

## Features

- JavaScript-driven LED effects using a Canvas-style drawing context. 
- Rich built-in editor with syntax highlighting, live preview, error display, and size presets. 
- Global scripts that automatically appear in all new brush instances. 
- Frame skip control to balance performance and smoothness. 
- Default effect library: rainbow waves, breathing, moving gradients, fire, scan line, and more. 

## Installation

1. Build or download `Artemis.Plugins.LayerBrushes.JavascriptCanvas.dll`
2. Copy the DLL (and its dependencies) into your Artemis plugins folder:
   - Windows: typically under `%AppData%\Artemis\plugins`.  
   - Linux/macOS: under your Artemis configuration/plugins directory.  
3. Ensure `plugin.json` is placed alongside the DLL.
4. Start Artemis; the **JavaScript Canvas** brush will be registered by the brush provider.

## Usage

### Adding the JavaScript Canvas Brush

1. In Artemis, open your profile and add a new layer.  
2. Choose the **JavaScript Canvas** brush.
3. Open the brush configuration dialog to edit scripts and preview the canvas.

### Editor & Preview

The configuration UI provides:

- **Script list**: Choose among multiple scripts, add custom scripts, delete, and mark as *Global*. 
- **Canvas size controls**: Width/height inputs and presets such as *Ultrawide*, *Standard*, *Compact*, and *Tall*.
- **Frame skip**: “Update every N frames” to reduce script execution frequency for performance.
- **Live preview**: A Skia-based canvas preview updated on a timer as `time` progresses.
- **Error display**: Shows formatted JavaScript errors with line and column information.

## Scripting API

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

**Styles & colors**  
- `ctx.fillStyle(r, g, b, a?)` and `ctx.strokeStyle(r, g, b, a?)`. 
- `ctx.lineWidth(w)` / `ctx.lineCap(...)` / `ctx.lineJoin(...)` / `ctx.miterLimit(limit)`  
- `ctx.globalAlpha(alpha)`  
- `ctx.globalCompositeOperation(mode)` – supports modes like `source-over`, `multiply`, `screen`, `overlay`, `darken`, `lighten`, `difference`, `hue`, `color`, etc. 

**Gradients**  
- `let g = ctx.createLinearGradient(x0, y0, x1, y1)`  
- `let g2 = ctx.createRadialGradient(x0, y0, r0, x1, y1, r1)`  
- `g.addColorStop(offset, r, g, b, a?)`  
- `ctx.fillStyleGradient(g)` / `ctx.strokeStyleGradient(g)`  

**Transforms**  
- `ctx.save()` / `ctx.restore()`  
- `ctx.resetTransform()`  
- `ctx.translate(x, y)` / `ctx.rotate(angle)` / `ctx.scale(x, y)`  
- `ctx.transform(a, b, c, d, e, f)` / `ctx.setTransform(a, b, c, d, e, f)`  

**Text**  
- `ctx.font("24px Arial")`  
- `ctx.textAlign("left" | "center" | "right" | "start" | "end")`  
- `ctx.textBaseline("top" | "middle" | "alphabetic" | "bottom")`  
- `ctx.fillText(text, x, y)` / `ctx.strokeText(text, x, y)`  
- `ctx.measureText(text)` (returns metrics with `width`)  

**Shadows**  
- `ctx.shadowBlur(blur)`  
- `ctx.shadowColor(r, g, b, a)`  
- `ctx.shadowOffsetX(x)` / `ctx.shadowOffsetY(y)`  

**Helpers**  
- `ctx.hslToRgb(h, s, l)` (h,s,l in 0–1, returns `{ r, g, b }` with RGB 0–255). 
- `ctx.rgbToHsl(r, g, b)` (RGB 0–255, returns `{ h, s, l }` in 0–1).

### Example Script: Moving Rainbow Wave
```
// Moving rainbow wave
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
