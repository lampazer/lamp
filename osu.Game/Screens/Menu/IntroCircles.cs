// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Sample;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using osu.Framework.Screens;
using osu.Framework.Utils;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Menu
{
    public partial class IntroCircles : IntroScreen
    {
        protected override string BeatmapHash => "3c8b1fcc9434dbb29e2fb613d3b9eada9d7bb6c125ceb32396c3b53437280c83";
        protected override string BeatmapFile => "circles.osz";

        public const double TRACK_START_DELAY = 600;
        private const double delay_for_menu = 7000; // 7 Segundos exactos de intro caótica

        private Sample welcome;
        private Container<BouncingLogo> logoContainer;
        private Texture lampTexture;

        public IntroCircles([CanBeNull] Func<MainMenu> createNextScreen = null)
            : base(createNextScreen)
        {
        }

        [BackgroundDependencyLoader]
        private void load(AudioManager audio, TextureStore textures)
        {
            if (MenuVoice.Value)
                welcome = audio.Samples.Get(@"Intro/welcome");

            lampTexture = tryGetCustomLogoTexture(textures);

            // Fondo negro absoluto y contenedor de logos
            InternalChild = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.Black 
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
            
            // Escondemos el logo principal original para que no estorbe la intro
            logo.Alpha = 0; 
            logo.AlwaysPresent = true;

            if (!resuming)
            {
                welcome?.Play();

                Scheduler.AddDelayed(delegate
                {
                    StartTrack();
                    PrepareMenuLoad();
                    Scheduler.AddDelayed(LoadMenu, delay_for_menu - TRACK_START_DELAY);
                }, TRACK_START_DELAY);

                // --- INICIA EL CAOS ---
                var primerLogo = new BouncingLogo(lampTexture)
                {
                    Position = new Vector2(0.5f, 0.5f),
                    RelativePositionAxes = Axes.Both,
                    Velocity = new Vector2(RNG.NextSingle(-0.8f, 0.8f), RNG.NextSingle(-0.8f, 0.8f))
                };
                logoContainer.Add(primerLogo);

                // A los 2.5 segundos (2500ms) empieza a clonarse
                Scheduler.AddDelayed(() =>
                {
                    Scheduler.AddDelayed(duplicarLogos, 400, true);
                }, 2500);
                
                logo.PlayIntro();
            }
        }

        private void duplicarLogos()
        {
            if (logoContainer.Count > 300) return; // Límite para no crashear la PC

            int cantidadActual = logoContainer.Count;
            for (int i = 0; i < cantidadActual; i++)
            {
                var logoExistente = logoContainer[i];
                var logoClon = new BouncingLogo(lampTexture)
                {
                    Position = logoExistente.Position,
                    Velocity = new Vector2(RNG.NextSingle(-1.2f, 1.2f), RNG.NextSingle(-1.2f, 1.2f))
                };
                logoContainer.Add(logoClon);
            }
        }

        // --- FÍSICAS DE REBOTE ---
        private partial class BouncingLogo : Sprite
        {
            public Vector2 Velocity;

            public BouncingLogo(Texture texture)
            {
                Texture = texture;
                Origin = Anchor.Centre;
                Size = new Vector2(150); // Tamaño miniatura
                FillMode = FillMode.Fit;
            }

            protected override void Update()
            {
                base.Update();
                if (Parent == null || Parent.DrawSize == Vector2.Zero) return;

                Position += Velocity * (float)Time.Elapsed;

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