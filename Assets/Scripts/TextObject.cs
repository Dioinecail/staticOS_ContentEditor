using UnityEngine;
using UnityEngine.UIElements;

namespace Project.StaticOSEditor
{
    public class TextObject : ContentObject
    {
        public override string Contents 
        { 
            get => m_Text.text;
            set => m_Text.value = value
                .Replace("<br>", "\n")
                .Replace("“", "\"")
                .Replace("”", "\""); 
        }

        [SerializeField] private TextField m_Text;



        public TextObject(VisualElement element) : base(element)
        {
            m_Text = element.Q<TextField>("blog-text");
            m_Text.RegisterValueChangedCallback(HandleTextChanged);
        }

        public override JSONObject ToJson()
        {
            var json = base.ToJson();
            var parsedString = Contents
                .Replace("\n", "<br>")
                .Replace(" \"", " “")
                .Replace("\n\"", "\n“")
                .Replace(".\"", ".“")
                .Replace(",\"", ",“")
                .Replace("\" ", "” ")
                .Replace("\"\n", "”\n")
                .Replace("\".", "”.")
                .Replace("\",", "”,");

            if (parsedString.EndsWith('\"'))
                parsedString = parsedString.Replace('\"', '”');

            json.SetField("contents", parsedString);

            return json;
        }

        private void HandleTextChanged(ChangeEvent<string> _)
        {
            InvokeChanged();
        }
    }
}