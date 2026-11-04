# Third-party notices

FancyZonesClone includes or derives from the following third-party software.

## Microsoft PowerToys (FancyZones)

The zone layout arithmetic, zone selection rules, keyboard snapping, window filtering, monitor identification, and window placement are ported from [microsoft/PowerToys](https://github.com/microsoft/PowerToys) `src/modules/fancyzones/FancyZonesLib`. Ported files say so in a header comment.

```text
The MIT License

Copyright (c) Microsoft Corporation. All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
```

## Distributed with the executable

The release links these packages. Native AOT compiles their managed code into the executable, and their native libraries, `libSkiaSharp.dll`, `libHarfBuzzSharp.dll`, and `av_libglesv2.dll`, ship beside it. Each is used under its own licence, which ships in its NuGet package.

| Component                        | License       | Source                                                        |
| -------------------------------- | ------------- | ------------------------------------------------------------- |
| Avalonia                         | MIT           | [AvaloniaUI/Avalonia](https://github.com/AvaloniaUI/Avalonia) |
| SkiaSharp (and the Skia library) | MIT (BSD-3)   | [mono/SkiaSharp](https://github.com/mono/SkiaSharp)           |
| HarfBuzzSharp (and HarfBuzz)     | MIT (Old MIT) | [mono/SkiaSharp](https://github.com/mono/SkiaSharp)           |
| ANGLE (`av_libglesv2.dll`)       | BSD-3-Clause  | [google/angle](https://chromium.googlesource.com/angle/angle) |
| .NET runtime                     | MIT           | [dotnet/runtime](https://github.com/dotnet/runtime)           |
