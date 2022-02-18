﻿using System.Text;

using Android.Views;

namespace Sharpnado.CollectionView.Droid.Helpers
{
    public class ViewHierarchyStringBuilder
    {
        private readonly StringBuilder _stringBuilder;

        private readonly string _indentSpace;

        public ViewHierarchyStringBuilder(int indentCount)
        {
            _stringBuilder = new StringBuilder();
            _indentSpace = string.Empty.PadLeft(indentCount);
        }

        public void Add(View view, int depth, bool isVerbose = false)
        {
            for (int i = 0; i < depth; i++)
            {
                _stringBuilder.Append(_indentSpace);
            }

            _stringBuilder.Append($"<{view.GetType().Name}");

            if (isVerbose)
            {
                _stringBuilder.Append($" X: {view.GetX()}, Y: {view.GetY()}, Width: {view.Width}, Height: {view.Height}");

                string layoutParams;
                if (view.LayoutParameters is ViewGroup.MarginLayoutParams marginLayout)
                {
                    layoutParams = $", MarginLayoutParams: {{ {view.LayoutParameters.Width}, {view.LayoutParameters.Height}, {marginLayout.LeftMargin}l, {marginLayout.TopMargin}t, {marginLayout.RightMargin}r, {marginLayout.BottomMargin}b }}";
                }
                else
                {
                    layoutParams = view.LayoutParameters != null
                        ? $", LayoutParams: {{ {view.LayoutParameters.Width}, {view.LayoutParameters.Height} }}"
                        : ", LayoutParams is null";
                }

                _stringBuilder.Append(layoutParams);
            }

            _stringBuilder.AppendLine(">");
        }

        public override string ToString()
        {
            return _stringBuilder.ToString();
        }
    }
}
