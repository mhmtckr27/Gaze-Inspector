using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal class ColorUsageDecorator : AttributeDrawer<ColorUsageAttribute>
    {
        private ColorUsageAttribute _attribute;

        protected internal override void Initialize()
        {
            base.Initialize();
            _attribute = Attribute as ColorUsageAttribute;
        }

        private void SetData()
        {
            var data = Data.AdditionalData.GetOrCreate<ColorUsageData>();
            data.showAlpha = _attribute.showAlpha;
            data.isHdr = _attribute.hdr;
        }

        public override void Draw(GUIContent label)
        {
            if (!TryGetAdditionalData(out ColorUsageData _))
                SetData();
            
            Inner.Draw(label);
        }
    }

    internal class ColorUsageData : ContextItem
    {
        internal bool showAlpha = true;
        internal bool isHdr = false;
            
        public override void Reset()
        {
        }
    }
}