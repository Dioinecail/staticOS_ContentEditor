using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.StaticOSEditor
{
    public class PictureObject : ContentObject
    {
        public override string Contents 
        { 
            get => m_PicturePath; 
            set 
            {
                if (m_PicturePath == value)
                    return;

                m_PicturePath = value;

                var url = Path.Combine(ContentManager.Instance.GetPathToContent(), value);

                if (Picture != null)
                    GameObject.Destroy(Picture);

                Picture = new Texture2D(2, 2);

                if (!File.Exists(url))
                    throw new System.Exception($"File does not exist at path: {url}");

                var pictureBytes = File.ReadAllBytes(url);

                Picture.LoadImage(pictureBytes);

                var aspect = Picture.height / (float)Picture.width;

                m_Picture.style.backgroundImage = Picture;
                m_Picture.style.height = 400f * aspect;
            } 
        }

        public Texture2D Picture { get; set; }

        private string m_PicturePath;
        private VisualElement m_Picture;



        public PictureObject(VisualElement element) : base(element)
        {
            m_Picture = element.Q("blog-image-container");
        }
    }
}