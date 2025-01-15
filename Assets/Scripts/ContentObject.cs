using System;
using UnityEngine.UIElements;

namespace Project.StaticOSEditor
{
    [Serializable]
    public abstract class ContentObject
    {
        public static event Action<ContentObject> OnChanged;

        public int Index { get; set; }
        public string Type { get; set; }
        public abstract string Contents { get; set; }

        public VisualElement Element { get; set; }



        public ContentObject(VisualElement element)
        {
            Element = element;
        }

        public virtual JSONObject ToJson()
        {
            var json = JSONObject.Create(JSONObject.Type.OBJECT);

            json.SetField("index", Index);
            json.SetField("type", Type);
            json.SetField("contents", Contents);

            return json;
        }

        protected void InvokeChanged()
        {
            OnChanged?.Invoke(this);
        }
    }
}