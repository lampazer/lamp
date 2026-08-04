// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osuTK;

namespace osu.Game.Screens.Menu
{
    public partial class IntroSequence : Container
    {
        private OsuSpriteText welcomeText;

        public IntroSequence()
        {
            RelativeSizeAxes = Axes.Both;
            Alpha = 0;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Children = new Drawable[]
            {
                welcomeText = new OsuSpriteText
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Text = "welcome to lamp",
                    Font = OsuFont.GetFont(weight: FontWeight.Light, size: 42),
                    Alpha = 0,
                    Spacing = new Vector2(5),
                }
            };
        }

        public void Start(double length)
        {
            // Solo hacemos un pequeño fade in del texto y lo desaparecemos
            using (BeginDelayedSequence(250))
            {
                welcomeText.FadeIn(700);
                welcomeText.TransformSpacingTo(new Vector2(20, 0), length - TransformDelay, Easing.Out);
            }
        }
    }
}