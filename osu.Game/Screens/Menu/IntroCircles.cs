// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Sample;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using osu.Framework.Screens;
using osu.Framework.Utils;
using osuTK;
using osuTK.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Game.Screens.Menu
{
    public partial class IntroCircles : IntroScreen
    {
        protected override string BeatmapHash => "3c8b1fcc9434dbb29e2fb613d3b9eada9d7bb6c125ceb32396c3b53437280c83";
        protected override string BeatmapFile => "bee.osz"; 

        public const double TRACK_START_DELAY = 600;
        private const double delay_for_menu = 7000;

        // --- LA MEMORIA CACHÉ DE LA CÁMARA PRE-LANZAMIENTO ---
        public static byte[] ScreenPixels;
        public static int ScreenWidth;
        public static int ScreenHeight;

        private Sample welcome;
        private Container<BouncingLogo> logoContainer;
        private Texture lampTexture;
        private Sprite desktopBackground;

        public IntroCircles([CanBeNull] Func<MainMenu> createNextScreen = null)
            : base(createNextScreen)
        {
        }

        [BackgroundDependencyLoader]
        private void load(AudioManager audio, TextureStore textures, IRenderer renderer)
        {
            if (MenuVoice.Value)
                welcome = audio.Samples.Get(@"Intro/welcome");

            lampTexture = tryGetCustomLogoTexture(textures);

            // Rescatamos la foto que se tomó ANTES de abrir el juego
            Texture screenshot = null;
            if (ScreenPixels != null && ScreenWidth > 0 && ScreenHeight > 0)
            {
                try
                {
                    var image = SixLabors.ImageSharp.Image.LoadPixelData<Rgba32>(ScreenPixels, ScreenWidth, ScreenHeight);
                    screenshot = renderer.CreateTexture(ScreenWidth, ScreenHeight);
                    screenshot.SetData(new TextureUpload(image));
                    ScreenPixels = null; // ¡Limpiamos la RAM cruda porque ya está en la Tarjeta de Video!
                }
                catch { }
            }

            Drawable backgroundLayer;
            if (screenshot != null)
            {
                backgroundLayer = desktopBackground = new Sprite
                {
                    RelativeSizeAxes = Axes.Both,
                    Texture = screenshot,
                    FillMode = FillMode.Stretch,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre
                };
            }
            else
            {
                backgroundLayer = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.Black
                };
            }

            InternalChild = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    backgroundLayer,
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = new Color4(0f, 0f, 0f, 0.5f) 
                    },
                    logoContainer = new Container<BouncingLogo>
                    {
                        RelativeSizeAxes = Axes.Both
                    }
                }
            };
        }

        protected override void LogoArriving(OsuLogo logo, bool resuming)
        {
            base.LogoArriving(logo, resuming);
            
            logo.Alpha = 0; 
            logo.AlwaysPresent = true;

            if (!resuming)
            {
                welcome?.Play();

                Scheduler.AddDelayed(delegate
                {
                    StartTrack();
                    PrepareMenuLoad();

                    Scheduler.AddDelayed(() =>
                    {
                        desktopBackground?.FadeOut(300);
                    }, delay_for_menu - TRACK_START_DELAY - 300);

                    Scheduler.AddDelayed(() =>
                    {
                        desktopBackground?.Texture?.Dispose(); 
                        desktopBackground?.Expire();           
                        LoadMenu();
                    }, delay_for_menu - TRACK_START_DELAY);

                }, TRACK_START_DELAY);

                var primerLogo = new BouncingLogo(lampTexture)
                {
                    Velocity = Vector2.Zero 
                };
                logoContainer.Add(primerLogo);

                Scheduler.AddDelayed(() =>
                {
                    primerLogo.Velocity = new Vector2(RNG.NextSingle(-700f, 700f), RNG.NextSingle(-700f, 700f));
                    Scheduler.AddDelayed(duplicarLogos, 400, true);
                }, 2500);
                
                logo.PlayIntro();
            }
        }

        private void duplicarLogos()
        {
            if (logoContainer.Count > 150) return;

            int cantidadActual = logoContainer.Count;
            for (int i = 0; i < cantidadActual; i++)
            {
                var logoExistente = logoContainer[i];
                var logoClon = new BouncingLogo(lampTexture)
                {
                    Position = logoExistente.Position,
                    Velocity = new Vector2(RNG.NextSingle(-700f, 700f), RNG.NextSingle(-700f, 700f))
                };
                logoContainer.Add(logoClon);
            }
        }

        private partial class BouncingLogo : Sprite
        {
            public Vector2 Velocity;
            private bool inicializado;

            public BouncingLogo(Texture texture)
            {
                Texture = texture;
                Origin = Anchor.Centre;
                Anchor = Anchor.TopLeft; 
                Size = new Vector2(150); 
                FillMode = FillMode.Fit;
            }

            protected override void Update()
            {
                base.Update();
                if (Parent == null || Parent.DrawSize == Vector2.Zero) return;

                if (!inicializado)
                {
                    Position = Parent.DrawSize / 2;
                    inicializado = true;
                }

                if (Velocity == Vector2.Zero) return;

                Position += Velocity * (float)(Time.Elapsed / 1000.0);

                float mitadAncho = DrawWidth / 2;
                float mitadAlto = DrawHeight / 2;

                if (Position.X - mitadAncho < 0)
                {
                    Position = new Vector2(mitadAncho, Position.Y);
                    Velocity.X = Math.Abs(Velocity.X);
                }
                else if (Position.X + mitadAncho > Parent.DrawWidth)
                {
                    Position = new Vector2(Parent.DrawWidth - mitadAncho, Position.Y);
                    Velocity.X = -Math.Abs(Velocity.X);
                }

                if (Position.Y - mitadAlto < 0)
                {
                    Position = new Vector2(Position.X, mitadAlto);
                    Velocity.Y = Math.Abs(Velocity.Y);
                }
                else if (Position.Y + mitadAlto > Parent.DrawHeight)
                {
                    Position = new Vector2(Position.X, Parent.DrawHeight - mitadAlto);
                    Velocity.Y = -Math.Abs(Velocity.Y);
                }
            }
        }

        // ---------------------------------------------------------------------
        // --- EL GATILLO GLOBAL DE LA CÁMARA (P/Invoke) ---
        // ---------------------------------------------------------------------
        [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
        [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr hDC);
        [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int nWidth, int nHeight);
        [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);
        [DllImport("gdi32.dll")] private static extern bool BitBlt(IntPtr hObject, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hObjectSource, int nXSrc, int nYSrc, int dwRop);
        [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr hDC);
        [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr hObject);
        [DllImport("gdi32.dll")] private static extern int GetDIBits(IntPtr hdc, IntPtr hbmp, uint uStartScan, uint cScanLines, [Out] byte[] lpvBits, ref BITMAPINFO lpbi, uint uUsage);
        [DllImport("user32.dll")] private static extern int GetSystemMetrics(int nIndex);

        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAPINFOHEADER
        {
            public uint biSize; public int biWidth; public int biHeight; public ushort biPlanes; public ushort biBitCount;
            public uint biCompression; public uint biSizeImage; public int biXPelsPerMeter; public int biYPelsPerMeter;
            public uint biClrUsed; public uint biClrImportant;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAPINFO
        {
            public BITMAPINFOHEADER bmiHeader;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)] public uint[] bmiColors;
        }

        // ¡Esta función es pública y estática para llamarla desde afuera!
        public static void TakePrelaunchScreenshot()
        {
            if (!OperatingSystem.IsWindows()) return;

            try
            {
                ScreenWidth = GetSystemMetrics(0); 
                ScreenHeight = GetSystemMetrics(1); 

                IntPtr hdcSrc = GetDC(IntPtr.Zero); 
                IntPtr hdcDest = CreateCompatibleDC(hdcSrc);
                IntPtr hBitmap = CreateCompatibleBitmap(hdcSrc, ScreenWidth, ScreenHeight);
                IntPtr hOld = SelectObject(hdcDest, hBitmap);

                BitBlt(hdcDest, 0, 0, ScreenWidth, ScreenHeight, hdcSrc, 0, 0, 0x00CC0020 | 0x40000000); 

                SelectObject(hdcDest, hOld);

                BITMAPINFO info = new BITMAPINFO();
                info.bmiHeader = new BITMAPINFOHEADER();
                info.bmiHeader.biSize = (uint)Marshal.SizeOf(info.bmiHeader);
                info.bmiHeader.biWidth = ScreenWidth;
                info.bmiHeader.biHeight = -ScreenHeight; 
                info.bmiHeader.biPlanes = 1;
                info.bmiHeader.biBitCount = 32;
                info.bmiHeader.biCompression = 0;

                byte[] pixels = new byte[ScreenWidth * ScreenHeight * 4];
                GetDIBits(hdcDest, hBitmap, 0, (uint)ScreenHeight, pixels, ref info, 0);

                DeleteDC(hdcDest);
                ReleaseDC(IntPtr.Zero, hdcSrc);
                DeleteObject(hBitmap);

                for (int i = 0; i < pixels.Length; i += 4)
                {
                    byte b = pixels[i];
                    pixels[i] = pixels[i + 2]; 
                    pixels[i + 2] = b;
                    pixels[i + 3] = 255; 
                }

                ScreenPixels = pixels; // Guardamos la foto en la RAM hasta que el motor despierte
            }
            catch { }
        }

        // --- HACK DE CARGA DE ASSETS ---
        private static Texture tryGetCustomLogoTexture(TextureStore textures)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string distPath = Path.Combine(baseDir, "assets", "lazer.png");
            string devPath = findPathInCurrentOrParents("assets/lazer.png") ?? findPathInCurrentOrParents("lazer.png");
            string finalPath = File.Exists(distPath) ? distPath : devPath;

            if (finalPath != null && File.Exists(finalPath))
            {
                byte[] imageBytes = File.ReadAllBytes(finalPath);
                textures.AddTextureSource(new TextureLoaderStore(new SingleLogoResourceStore("logo_lamp_intro", imageBytes)));
                return textures.Get("logo_lamp_intro");
            }
            return textures.Get(@"Menu/logo");
        }

        private static string findPathInCurrentOrParents(string filename)
        {
            string current = Environment.CurrentDirectory;
            for (int i = 0; i < 10; i++)
            {
                if (string.IsNullOrEmpty(current)) break;
                string candidate = Path.Combine(current, filename);
                if (File.Exists(candidate)) return candidate;
                var parent = Directory.GetParent(current);
                if (parent == null || parent.FullName == current) break;
                current = parent.FullName;
            }
            return null;
        }

        private sealed class SingleLogoResourceStore : IResourceStore<byte[]>
        {
            private readonly string textureLookup;
            private readonly byte[] imageBytes;
            public SingleLogoResourceStore(string textureLookup, byte[] imageBytes)
            {
                this.textureLookup = textureLookup;
                this.imageBytes = imageBytes;
            }
            public byte[] Get(string name) => string.Equals(name, textureLookup, StringComparison.OrdinalIgnoreCase) ? imageBytes : null;
            public Task<byte[]> GetAsync(string name, CancellationToken cancellationToken = default) => Task.FromResult(Get(name));
            public Stream GetStream(string name) => Get(name) != null ? new MemoryStream(imageBytes, writable: false) : null;
            public System.Collections.Generic.IEnumerable<string> GetAvailableResources() => new[] { textureLookup };
            public void Dispose() { }
        }

        public override void OnSuspending(ScreenTransitionEvent e)
        {
            this.FadeOut(300);
            base.OnSuspending(e);
        }
    }
}