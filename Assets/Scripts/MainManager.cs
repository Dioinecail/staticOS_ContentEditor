using SFB;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Content;
using Unity.VisualScripting;
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
        [SerializeField] private string m_FriendsEditorScene;
        [SerializeField] private string m_PortfolioEditorScene;

        private VisualElement m_ContentContainer;
        private Label m_LocalPathContainer;
        private TextField m_ContentPathInput;
        private Coroutine m_CoroutineLoadScene;
        private string m_CurrentEditorScene;



        private IEnumerator Start()
        {
            Screen.SetResolution(1280, 680, false);

            yield return null;

            m_ContentContainer = m_DomElement.rootVisualElement.Q("content-view");
            var buttonOpenPath = m_DomElement.rootVisualElement.Q<Button>("button-open-path");
            var buttonOpenContents = m_DomElement.rootVisualElement.Q<Button>("button-open-folder");
            m_ContentPathInput = m_DomElement.rootVisualElement.Q<TextField>("input-path");
            var versionLabel = m_DomElement.rootVisualElement.Q<Label>("version-label");
            m_LocalPathContainer = m_DomElement.rootVisualElement.Q<Label>("local-path");

            buttonOpenPath.clicked += HandlePathClicked;
            buttonOpenContents.clicked += () => HandleFolderClicked("");

            var oldPath = PlayerPrefs.GetString(c_ContentRootKey, string.Empty);

            if (!string.IsNullOrEmpty(oldPath))
            {
                m_ContentPathInput.value = oldPath;
                HandleFolderClicked("");
            }

            versionLabel.text = $"v{Application.version}";
        }

        private VisualElement CreateButton(JSONObject content, string folder)
        {
            var folderElem = m_ButtonTemplate.CloneTree();
            var banner = GetImage(Path.Combine(folder, content["banner"].str));

            folderElem.Q<Label>("header").text = content["title"].str;

            if (banner != null)
            {
                folderElem.Q("content-container").style.backgroundImage = banner;
            }

            m_ContentContainer.Add(folderElem);

            return folderElem;
        }

        private Texture2D GetImage(string bannerPath)
        {
            var absolutePath = Path.Combine(m_ContentPathInput.value, bannerPath);

            var bannerUrlJpg = $"{absolutePath}.jpg";
            var bannerUrlPng = $"{absolutePath}.png";
            var bannerUrlJpeg = $"{absolutePath}.jpeg";

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

        private IEnumerator DoLoadContent(string contentEditorScene, string contentAbsolutePath, string localPath)
        {
            m_DomElement.enabled = false;
            m_CurrentEditorScene = contentEditorScene;

            var loadOperation = SceneManager.LoadSceneAsync(contentEditorScene, LoadSceneMode.Additive);

            while (!loadOperation.isDone)
            {
                yield return null;
            }

            ContentEditorBase manager = null;

            while (manager == null)
            {
                manager = FindObjectOfType<ContentEditorBase>(true);

                yield return null;
            }

            manager.LoadContent(contentAbsolutePath, localPath);

            yield return null;

            var buttonBack = manager.DomElement.Q<Button>("button-back");

            buttonBack.clicked += HandleBackClicked;

            m_CoroutineLoadScene = null;
        }

        private IEnumerator DoUnloadContent(string contentEditorScene)
        {
            var unloadOperation = SceneManager.UnloadSceneAsync(contentEditorScene);

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
                    CreateFolder(folderInput.value, folder, isFolder);
            };

            buttonCancel.clicked += () =>
            {
                m_ModalCreateFolder.enabled = false;
            };
        }

        private void HandleFolderClicked(string localPath)
        {
            m_ContentContainer.Clear();

            var foldersArray = localPath.Split('/');

            if (localPath != "")
            {
                CreateBackButton().Q<Button>().clicked += () =>
                {
                    var upperPath = foldersArray.Length > 1
                            ? Path.Combine(foldersArray.Take(foldersArray.Length - 1).ToArray())
                            : "";

                    HandleFolderClicked(upperPath);
                };
            }

            var contentJsonPath = Path.Combine(m_ContentPathInput.value, localPath, "content.json");
            var json = JSONObject.Create(File.ReadAllText(contentJsonPath));

            var contents = json["contents"];

            m_LocalPathContainer.text = $"local path: '{localPath}'";

            foreach (var c in contents)
            {
                var contentButton = CreateButton(c, localPath);

                contentButton.Q<Button>().clicked += () => HandleContentClicked(c, localPath);
            }

            if (localPath != "")
            {
                CreateNewButton().Q<Button>().clicked += () => StartCoroutine(HandleCreateNewFolderButtonClicked(localPath));
            }
        }

        private void CreateFolder(string name, string root, bool isFolder)
        {
            var path = Path.Combine(m_ContentPathInput.value, root, name);
            var newDirectoryInfo = Directory.CreateDirectory(path);

            m_ModalCreateFolder.enabled = false;

            var contentJsonPath = Path.Combine(m_ContentPathInput.value, root, "content.json");
            var json = JSONObject.Create(File.ReadAllText(contentJsonPath));

            var dataArray = json["contents"];
            var newContentOrFolder = JSONObject.Create(JSONObject.Type.OBJECT);

            newContentOrFolder.SetField("index", dataArray.Count);
            newContentOrFolder.SetField("title", name);
            newContentOrFolder.SetField("banner", "");
            newContentOrFolder.SetField("path", Path.Combine(root, name).Replace('\\', '/'));
            newContentOrFolder.SetField("type", isFolder ? "folder" : "content");

            dataArray.Add(newContentOrFolder);
            json["contents"] = dataArray;

            File.WriteAllText(contentJsonPath, json.Print(true));

            var internalContentJsonPath = Path.Combine(m_ContentPathInput.value, root, name, "content.json");
            var internalContentJson = JSONObject.Create(JSONObject.Type.OBJECT);

            internalContentJson.SetField(isFolder ? "contents" : "content", JSONObject.Create(JSONObject.Type.ARRAY));

            File.WriteAllText(internalContentJsonPath, internalContentJson.Print(true));

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

        private void HandleContentClicked(JSONObject content, string folder)
        {
            var path = content["path"].str;

            switch (content["type"].str)
            {
                case "folder":
                    if (path == "music")
                        return;

                    HandleFolderClicked(path);
                    break;
                case "content":
                    if (m_CoroutineLoadScene != null)
                        return;

                    var contentAbsolutePath = Path.Combine(m_ContentPathInput.value, path);

                    if (path.StartsWith("blog") || path.StartsWith("art"))
                    {
                        m_CoroutineLoadScene = StartCoroutine(DoLoadContent(m_ContentEditorScene, contentAbsolutePath, path));
                    }

                    if (path.StartsWith("friends"))
                    {
                        m_CoroutineLoadScene = StartCoroutine(DoLoadContent(m_FriendsEditorScene, contentAbsolutePath, path));
                    }

                    if (path.StartsWith("portfolio"))
                    {
                        m_CoroutineLoadScene = StartCoroutine(DoLoadContent(m_PortfolioEditorScene, contentAbsolutePath, path));
                    }

                    break;
                default:
                    return;
            }
        }

        private void HandleBackClicked()
        {
            if (m_CoroutineLoadScene != null)
                return;

            m_CoroutineLoadScene = StartCoroutine(DoUnloadContent(m_CurrentEditorScene));
        }
    }
}