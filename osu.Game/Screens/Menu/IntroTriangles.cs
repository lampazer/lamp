// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using JetBrains.Annotations;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Sample;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Screens;
using osu.Framework.Timing;
using osu.Framework.Utils;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Menu
{
    public partial class IntroTriangles : IntroScreen
    {
        protected override string BeatmapHash => "a1556d0801b3a6b175dda32ef546f0ec812b400499f575c44fccbe9c67f9b1e5";
        protected override string BeatmapFile => "bee.osz";

        [Resolved]
        private AudioManager audio { get; set; }
        private Sample welcome;
        private TrianglesIntroSequence intro;

        public IntroTriangles([CanBeNull] Func<MainMenu> createNextScreen = null) : base(createNextScreen) { }

        [BackgroundDependencyLoader]
        private void load()
        {
            if (MenuVoice.Value)
                welcome = audio.Samples.Get(@"Intro/welcome");
        }

        protected override void LogoArriving(OsuLogo logo, bool resuming)
        {
            base.LogoArriving(logo, resuming);

            if (!resuming)
            {
                PrepareMenuLoad();
                var decouplingClock = new DecouplingFramedClock(UsingThemedIntro ? Track : null);

                LoadComponentAsync(intro = new TrianglesIntroSequence(logo, () => FadeInBackground())
                {
                    RelativeSizeAxes = Axes.Both,
                    Clock = new InterpolatingFramedClock(decouplingClock),
                    LoadMenu = LoadMenu
                }, _ =>
                {
                    AddInternal(intro);
                    if (DidLoadMenu) return;

                    if (!UsingThemedIntro)
                    {
                        welcome?.Play();
                        Scheduler.AddDelayed(StartTrack, IntroCircles.TRACK_START_DELAY);
                    }
                    else StartTrack();

                    decouplingClock.Start();
                });
            }
        }

        public override void OnSuspending(ScreenTransitionEvent e)
        {
            base.OnSuspending(e);
            intro.Expire();
        }

        private partial class TrianglesIntroSequence : CompositeDrawable
        {
            private readonly OsuLogo logo;
            private readonly Action showBackgroundAction;
            private OsuSpriteText welcomeText;
            private GlitchingTriangles triangles;
            public Action LoadMenu;

            public TrianglesIntroSequence(OsuLogo logo, Action showBackgroundAction)
            {
                this.logo = logo;
                this.showBackgroundAction = showBackgroundAction;
            }

            [Resolved]
            private OsuGameBase game { get; set; }

            [BackgroundDependencyLoader]
            private void load()
            {
                InternalChildren = new Drawable[]
                {
                    triangles = new GlitchingTriangles
                    {
                        Alpha = 0, Anchor = Anchor.Centre, Origin = Anchor.Centre, Size = new Vector2(0.4f, 0.16f)
                    },
                    welcomeText = new OsuSpriteText
                    {
                        Anchor = Anchor.Centre, Origin = Anchor.Centre, Padding = new MarginPadding { Bottom = 10 },
                        Font = OsuFont.GetFont(weight: FontWeight.Light, size: 42), Alpha = 1, Spacing = new Vector2(5),
                    }
                };
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                using (BeginAbsoluteSequence(0))
                {
                    using (BeginDelayedSequence(200))
                        welcomeText.FadeIn().OnComplete(t => t.Text = "wel");

                    using (BeginDelayedSequence(400))
                        welcomeText.FadeIn().OnComplete(t => t.Text = "welcome");

                    using (BeginDelayedSequence(700))
                        welcomeText.FadeIn().OnComplete(t => t.Text = "welcome to");

                    using (BeginDelayedSequence(900))
                    {
                        // AQUÍ ESTÁ EL CAMBIO A LAMP
                        welcomeText.FadeIn().OnComplete(t => t.Text = "welcome to lamp");
                        welcomeText.TransformTo(nameof(welcomeText.Spacing), new Vector2(50, 0), 5000);
                    }

                    using (BeginDelayedSequence(1060))
                        triangles.FadeIn();

                    // Acortamos los tiempos porque borramos la animación larga de Lazer
                    using (BeginDelayedSequence(2200)) 
                    {
                        welcomeText.FadeOut().Expire();
                        triangles.FadeOut().Expire();
                        
                        logo.FadeIn();
                        showBackgroundAction();
                        game.Add(new GameWideFlash());
                        LoadMenu();
                    }
                }
            }

            private partial class GameWideFlash : Box
            {
                public GameWideFlash()
                {
                    Colour = Color4.White; RelativeSizeAxes = Axes.Both; Blending = BlendingParameters.Additive;
                }
                protected override void LoadComplete()
                {
                    base.LoadComplete();
                    this.FadeOutFromOne(1000, Easing.Out);
                }
            }

            private partial class GlitchingTriangles : CompositeDrawable
            {
                public GlitchingTriangles() { RelativeSizeAxes = Axes.Both; }
                private double? lastGenTime;
                protected override void Update()
                {
                    base.Update();
                    if (lastGenTime == null || Time.Current - lastGenTime > 22)
                    {
                        lastGenTime = (lastGenTime ?? Time.Current) + 22;
                        Drawable triangle = new OutlineTriangle(RNG.NextBool(), (RNG.NextSingle() + 0.2f) * 80)
                        {
                            RelativePositionAxes = Axes.Both, Position = new Vector2(RNG.NextSingle(), RNG.NextSingle()),
                        };
                        AddInternal(triangle);
                        triangle.FadeOutFromOne(120);
                    }
                }

                public partial class OutlineTriangle : BufferedContainer
                {
                    public OutlineTriangle(bool outlineOnly, float size) : base(cachedFrameBuffer: true)
                    {
                        Size = new Vector2(size);
                        InternalChildren = new Drawable[] { new Triangle { RelativeSizeAxes = Axes.Both } };
                        if (outlineOnly)
                        {
                            AddInternal(new Triangle { Anchor = Anchor.Centre, Origin = Anchor.Centre, Colour = Color4.Black, Size = new Vector2(size - 5), Blending = BlendingParameters.None });
                        }
                        Blending = BlendingParameters.Additive;
                    }
                }
            }
        }
    }
}