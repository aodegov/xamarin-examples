﻿using System;
using System.ComponentModel;
using CoreGraphics;
using Foundation;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using XF.Material.Forms.UI;
using XF.Material.iOS.Renderers;

[assembly: ExportRenderer(typeof(MaterialLabel), typeof(MaterialLabelRenderer))]

namespace XF.Material.iOS.Renderers
{
    public class MaterialLabelRenderer : LabelRenderer
    {
        public new MaterialLabel Element => base.Element as MaterialLabel;

        public NSMutableAttributedString AttributedString => Control?.AttributedText as NSMutableAttributedString;

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();

            CheckIfSingleLine();
            UpdateLetterSpacing(Control, Element.LetterSpacing);
            EnsureLineBreakMode();
        }

        private void CheckIfSingleLine()
        {
            if (Control == null || Control.Frame.Size.Height == 0)
            {
                return;
            }

            var textSize = new CGSize(Control.Frame.Size.Width, nfloat.MaxValue);
            var rHeight = Control.SizeThatFits(textSize).Height;
            var charSize = Control.Font.LineHeight;
            var lines = Convert.ToInt32(rHeight / charSize);

            if (lines == 1)
            {
                Element.LineHeight = 1;
            }
        }
        private void EnsureLineBreakMode()
        {
            if (Element == null || Control == null)
            {
                return;
            }

            var lbm = Element.LineBreakMode;
            switch (lbm)
            {
                case Xamarin.Forms.LineBreakMode.NoWrap:
                    Control.LineBreakMode = UIKit.UILineBreakMode.Clip;
                    break;
                case Xamarin.Forms.LineBreakMode.WordWrap:
                    Control.LineBreakMode = UIKit.UILineBreakMode.WordWrap;
                    break;
                case Xamarin.Forms.LineBreakMode.CharacterWrap:
                    Control.LineBreakMode = UIKit.UILineBreakMode.CharacterWrap;
                    break;
                case Xamarin.Forms.LineBreakMode.HeadTruncation:
                    Control.LineBreakMode = UIKit.UILineBreakMode.HeadTruncation;
                    break;
                case Xamarin.Forms.LineBreakMode.TailTruncation:
                    Control.LineBreakMode = UIKit.UILineBreakMode.TailTruncation;
                    break;
                case Xamarin.Forms.LineBreakMode.MiddleTruncation:
                    Control.LineBreakMode = UIKit.UILineBreakMode.MiddleTruncation;
                    break;
                default:
                    break;
            }
        }

        protected override void OnElementChanged(ElementChangedEventArgs<Label> e)
        {
            base.OnElementChanged(e);

            if (e?.NewElement != null)
            {
                UpdateLetterSpacing(Control, Element.LetterSpacing);
                EnsureLineBreakMode();
            }
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            switch (e?.PropertyName)
            {
                case nameof(MaterialLabel.LetterSpacing):
                case nameof(Label.Text):
                    UpdateLetterSpacing(Control, Element.LetterSpacing);
                    CheckIfSingleLine();
                    EnsureLineBreakMode();
                    break;
            }
        }

        private void UpdateLetterSpacing(UILabel uiLabel, double letterSpacing)
        {
            if (uiLabel == null || AttributedString == null)
            {
                return;
            }

            var range = new NSRange(0, uiLabel.Text?.Length ?? 0);
            var attr = new NSMutableAttributedString(Control.AttributedText);
            attr.AddAttribute(UIStringAttributeKey.KerningAdjustment, FromObject((float)letterSpacing), range);

            Control.AttributedText = attr;
        }
    }
}