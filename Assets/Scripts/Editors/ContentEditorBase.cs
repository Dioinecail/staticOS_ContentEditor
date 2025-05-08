using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.StaticOSEditor
{
    public abstract class ContentEditorBase : MonoBehaviour
    {
        public VisualElement DomElement => m_DomElement.rootVisualElement;

        [SerializeField] protected UIDocument m_DomElement;
        
        protected Label m_PathToContentText;
        protected JSONObject m_ContentJson;



        public abstract void LoadContent(string contentPath, string localPath);
        
        public string GetPathToContent()
        {
            return m_PathToContentText.text;
        }

        public string GetPathToFoldersJson()
        {
            var dirInfo = new DirectoryInfo(m_PathToContentText.text);
            var path = Path.Combine(dirInfo.Parent.FullName, "content.json");

            return path;
        }

        public string GetPathToContentJson()
        {
            var dirInfo = new DirectoryInfo(m_PathToContentText.text);
            var path = Path.Combine(dirInfo.FullName, "content.json");

            return path;
        }
        
        public string GetPathToBanner(string extension)
        {
            var dirInfo = new DirectoryInfo(m_PathToContentText.text);

            return Path.Combine(m_PathToContentText.text, "../", $"{dirInfo.Name}{extension}");
        }
    }
}