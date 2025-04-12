using SFB;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Project.StaticOSEditor
{
    public class MainManager : MonoBehaviour
    {
        private const string c_ContentRootKey = "static-os-content-root";

        public static event Action<string> OnContentRootOpened;

        [SerializeField] private UIDocument m_DomElement;
        [SerializeField] private UIDocument m_ModalCreateFolder;
        [SerializeField] private VisualTreeAsset m_ButtonTemplate;
        [SerializeField] private string m_ContentEditorScene;

        private VisualElement m_ContentContainer;
        private TextField m_ContentPathInput;
        private Coroutine m_CoroutineLoadScene;



        private IEnumerator Start()
        {
            Screen.SetResolution(1280, 680, false);

            yield return null;

            m_ContentContainer = m_DomElement.rootVisualElement.Q("content-view");
            var buttonOpenPath = m_DomElement.rootVisualElement.Q<Button>("button-open-path");
            var buttonOpenContents = m_DomElement.rootVisualElement.Q<Button>("button-open-folder");
            m_ContentPathInput = m_DomElement.rootVisualElement.Q<TextField>("input-path");
            var versionLabel = m_DomElement.rootVisualElement.Q<Label>("version-label");

            buttonOpenPath.clicked += HandlePathClicked;
            buttonOpenContents.clicked += HandleOpenContentsClicked;

            var oldPath = PlayerPrefs.GetString(c_ContentRootKey, string.Empty);

            if (!string.IsNullOrEmpty(oldPath))
            {
                m_ContentPathInput.value = oldPath;
                HandleOpenContentsClicked();
            }

            versionLabel.text = $"v{Application.version}";
        }

        private VisualElement CreateButton(string folder)
        {
            var dirInfo = new DirectoryInfo(folder);
            var folderElem = m_ButtonTemplate.CloneTree();
            var banner = GetImage(folder);

            folderElem.Q<Label>("header").text = dirInfo.Name;

            if (banner != null)
            {
                folderElem.Q("content-container").style.backgroundImage = banner;
            }

            m_ContentContainer.Add(folderElem);

            return folderElem;
        }

        private Texture2D GetImage(string folder)
        {
            var bannerUrlJpg = $"{folder}.jpg";
            var bannerUrlPng = $"{folder}.png";
            var bannerUrlJpeg = $"{folder}.jpeg";

            Texture2D picture = null;
            byte[] pictureBytes = null;

            if (File.Exists(bannerUrlJpg))
            {
                picture = new Texture2D(2, 2);
                pictureBytes = File.ReadAllBytes(bannerUrlJpg);
            }
            else if (File.Exists(bannerUrlPng))
            {
                picture = new Texture2D(2, 2);
                pictureBytes = File.ReadAllBytes(bannerUrlPng);
            }
            else if (File.Exists(bannerUrlJpeg))
            {
                picture = new Texture2D(2, 2);
                pictureBytes = File.ReadAllBytes(bannerUrlJpeg);
            }

            if (picture != null)
            {
                picture.LoadImage(pictureBytes);
            }

            return picture;
        }

        private VisualElement CreateBackButton()
        {
            var folderElem = m_ButtonTemplate.CloneTree();

            folderElem.Q<Label>("header").text = "<- Back";

            m_ContentContainer.Add(folderElem);

            return folderElem;
        }

        private VisualElement CreateNewButton()
        {
            var folderElem = m_ButtonTemplate.CloneTree();

            folderElem.Q<Label>("header").text = "+ CreateNew";

            m_ContentContainer.Add(folderElem);

            return folderElem;
        }

        private IEnumerator DoLoadContent(string contentPath)
        {
            m_DomElement.enabled = false;

            var loadOperation = SceneManager.LoadSceneAsync(m_ContentEditorScene, LoadSceneMode.Additive);

            while (!loadOperation.isDone)
            {
                yield return null;
            }

            ContentManager manager = null;

            while (manager == null)
            {
                manager = ContentManager.Instance;
            }

            manager.LoadContent(contentPath);

            yield return null;

            var buttonBack = manager.DomElement.Q<Button>("button-back");

            buttonBack.clicked += HandleBackClicked;

            m_CoroutineLoadScene = null;
        }

        private IEnumerator DoUnloadContent()
        {
            var unloadOperation = SceneManager.UnloadSceneAsync(m_ContentEditorScene);

            while (!unloadOperation.isDone)
            {
                yield return null;
            }

            m_DomElement.enabled = true;

            yield return Start();

            m_CoroutineLoadScene = null;
        }

        private IEnumerator HandleCreateNewFolderButtonClicked(string folder)
        {
            m_ModalCreateFolder.enabled = true;

            yield return null;

            var isFolder = false;

            var folderInput = m_ModalCreateFolder.rootVisualElement.Q<TextField>();
            var buttonCreate = m_ModalCreateFolder.rootVisualElement.Q<Button>("button-create");
            var buttonCancel = m_ModalCreateFolder.rootVisualElement.Q<Button>("button-cancel");
            var buttonType = m_ModalCreateFolder.rootVisualElement.Q<Button>("button-type");

            buttonType.clicked += () =>
            {
                isFolder = !isFolder;

                buttonType.text = isFolder ? "Type: Folder" : "Type: Content";
            };

            buttonCreate.clicked += () =>
            {
                if (folderInput.value != "filler text")
                    CreateFolder(folderInput.value, folder);
            };

            buttonCancel.clicked += () =>
            {
                m_ModalCreateFolder.enabled = false;
            };
        }

        private void HandleFolderClicked(string folder)
        {
            m_ContentContainer.Clear();

            var contentFolders = Directory.GetDirectories(folder);
            var buttonBack = CreateBackButton();

            buttonBack.Q<Button>().clicked += HandleOpenContentsClicked;

            foreach (var contentPath in contentFolders)
            {
                var contentButton = CreateButton(contentPath);

                contentButton.Q<Button>().clicked += () => HandleContentClicked(contentPath);
            }

            var newContentButton = CreateNewButton();

            newContentButton.Q<Button>().clicked += () => StartCoroutine(HandleCreateNewFolderButtonClicked(folder));
        }

        private void CreateFolder(string name, string root)
        {
            var path = Path.Combine(root, name);
            var newDirectoryInfo = Directory.CreateDirectory(path);

            m_ModalCreateFolder.enabled = false;

            HandleFolderClicked(root);
        }

        private void HandlePathClicked()
        {
            var oldPath = PlayerPrefs.GetString(c_ContentRootKey, "");
            var pathToContent = StandaloneFileBrowser.OpenFolderPanel("Select content folder", oldPath, false);

            if (pathToContent == null || pathToContent.Length == 0)
            {
                return;
            }

            var path = pathToContent[0];

            m_ContentPathInput.value = path;

            PlayerPrefs.SetString(c_ContentRootKey, path);
            PlayerPrefs.Save();
        }

        private void HandleOpenContentsClicked()
        {
            var path = m_ContentPathInput.value;

            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                return;
            }

            m_ContentContainer.Clear();

            var folders = Directory.GetDirectories(path);

            foreach (var folder in folders)
            {
                if (folder.Contains(".git"))
                    continue;

                var folderButton = CreateButton(folder);

                folderButton.Q<Button>().clicked += () => HandleFolderClicked(folder);
            }

            OnContentRootOpened?.Invoke(path);
        }

        private void HandleContentClicked(string contentPath)
        {
            if (m_CoroutineLoadScene != null)
                return;

            m_CoroutineLoadScene = StartCoroutine(DoLoadContent(contentPath));
        }

        private void HandleBackClicked()
        {
            if (m_CoroutineLoadScene != null)
                return;

            m_CoroutineLoadScene = StartCoroutine(DoUnloadContent());
        }
    }
}