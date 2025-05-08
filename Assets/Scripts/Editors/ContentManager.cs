using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using System;
using SFB;
using System.IO;
using System.Linq;
using UnityEngine.UIElements;

namespace Project.StaticOSEditor
{
    /// <summary>
    /// Blog/Art content editor
    /// </summary>
    public class ContentManager : ContentEditorBase
    {
        [SerializeField] private VisualTreeAsset m_PicturePrefab;
        [SerializeField] private VisualTreeAsset m_TextPrefab;
        [SerializeField] private UIDocument m_RightClickMenu;

        private Label m_ContentJsonPreview;
        private TextField m_TitleInput;
        private VisualElement m_BannerImage;
        private Button m_ButtonChangeBanner;
        private VisualElement m_WorkingArea;
        private Label m_TitlePreview;
        private Toggle m_ToggleIsPublished;

        private List<ContentObject> m_ContentObjects = new List<ContentObject>();
        private JSONObject m_FoldersJson;
        private JSONObject m_FolderInfo;
        private ContentObject m_ObjectForDeletion;
        private Vector3 m_CursorPositionForPicture;
        private ExtensionFilter[] m_ImageFilters = new ExtensionFilter[] {
            new ExtensionFilter("", new string[]
            {
                "png", "jpg", "jpeg"
            })
        };
        private bool m_IsBlogPublished = true;
        private int m_ContentIndex = -1;
        private string m_LocalPath;



        public override void LoadContent(string contentPath, string localPath)
        {
            StartCoroutine(DoLoadContent(contentPath, localPath));
        }

        private IEnumerator DoLoadContent(string contentPath, string localPath)
        {
            yield return null;

            m_LocalPath = localPath;

            m_WorkingArea = DomElement.Q("working-area-container");
            m_PathToContentText = DomElement.Q<Label>("content-path");
            m_ContentJsonPreview = DomElement.Q<Label>("config-preview");
            m_TitleInput = DomElement.Q<TextField>("title-input");
            m_BannerImage = DomElement.Q("banner-preview");
            m_ButtonChangeBanner = DomElement.Q<Button>("button-banner");
            m_TitlePreview = DomElement.Q<Label>("folder-title");
            m_ToggleIsPublished = DomElement.Q<Toggle>("toggle-is-published");

            m_TitleInput.RegisterValueChangedCallback(HandleTitleInputChanged);
            m_ToggleIsPublished.RegisterValueChangedCallback(HandleToggleIsPublishedChanged);

            m_PathToContentText.text = contentPath;
            m_FoldersJson = JSONObject.Create(File.ReadAllText(GetPathToFoldersJson()));
            m_ContentIndex = -1;

            for (int i = 0; i < m_FoldersJson["contents"].Count; i++)
            {
                if (m_FoldersJson["contents"][i]["path"].str == localPath)
                {
                    m_ContentIndex = i;
                    break;
                }
            }

            if (m_ContentIndex == -1)
            {
                throw new Exception($"Could not find index for '{localPath}'");
            }

            m_FolderInfo = m_FoldersJson["contents"][m_ContentIndex];
            m_TitleInput.value = m_FolderInfo["title"].str;

            var bannerLocalPath = m_FolderInfo["banner"].str;

            if (!string.IsNullOrEmpty(bannerLocalPath))
            {
                var imageExt = Path.GetExtension(m_FolderInfo["banner"].str);
                var bannerPath = GetPathToBanner(imageExt);

                Texture2D picture = null;
                byte[] pictureBytes = null;

                if (File.Exists(bannerPath))
                {
                    picture = new Texture2D(2, 2);
                    pictureBytes = File.ReadAllBytes(bannerPath);
                }

                if (picture != null)
                {
                    picture.LoadImage(pictureBytes);

                    m_BannerImage.style.backgroundImage = picture;
                }
            }

            if (m_ContentObjects != null && m_ContentObjects.Count > 0)
                yield break;

            if (!Directory.Exists(m_PathToContentText.text))
                throw new Exception("Path to content doesn't exist!");

            var jsonFilePath = GetPathToContentJson();

            if (!File.Exists(jsonFilePath))
            {
                jsonFilePath = Path.Combine(m_PathToContentText.text, "content.json");

                var emptyContent = JSONObject.Create(JSONObject.Type.OBJECT);
                emptyContent.SetField("content", JSONObject.Create(JSONObject.Type.ARRAY));

                File.WriteAllText(jsonFilePath, emptyContent.Print());
            }

            var json = File.ReadAllText(jsonFilePath);

            if (string.IsNullOrEmpty(json))
                throw new Exception("Json file not found!");

            m_ContentJson = JSONObject.Create(json);

            m_IsBlogPublished = m_FoldersJson["contents"][m_ContentIndex].HasField("isPublished")
                ? m_FoldersJson["contents"][m_ContentIndex]["isPublished"]
                : true;

            m_ToggleIsPublished.SetValueWithoutNotify(m_IsBlogPublished);

            foreach (var c in m_ContentJson["content"])
            {
                var index = (int)c["index"].i;
                var type = c["type"].str;
                var data = c["contents"].str;

                ContentObject contentObject = null;
                VisualElement contentElement = null;

                switch (type)
                {
                    case "pic":
                        contentElement = m_PicturePrefab.CloneTree();
                        contentObject = new PictureObject(contentElement);
                        break;
                    case "text":
                        contentElement = m_TextPrefab.CloneTree();
                        contentObject = new TextObject(contentElement);
                        break;
                    default:
                        break;
                }

                contentObject.Type = type;
                contentObject.Index = index;
                contentElement.userData = contentObject;

                if (type == "pic")
                {
                    contentObject.Contents = Path.GetFileName(data);
                }
                else
                {
                    contentObject.Contents = data;
                }

                m_ContentObjects.Add(contentObject);
                m_WorkingArea.Add(contentElement);

                contentElement.Q<Button>("button-delete").clicked += (() =>
                {
                    StartCoroutine(HandleContentObjectClicked(contentElement));
                });
            }

            m_WorkingArea.RegisterCallback<PointerDownEvent>((evt) =>
            {
                if (evt.button != 1)
                    return;

                StartCoroutine(HandleWorkingAreaRightClicked());
            }, TrickleDown.NoTrickleDown);

            m_ButtonChangeBanner.clicked += HandleChangeBannerClicked;
            HandleContentChanged(null);
        }

        public void Delete(ContentObject target)
        {
            m_RightClickMenu.enabled = true;
            m_ObjectForDeletion = target;
        }

        private void OnEnable()
        {
            ContentObject.OnChanged -= HandleContentChanged;
            ContentObject.OnChanged += HandleContentChanged;
        }

        private IEnumerator HandleWorkingAreaRightClicked()
        {
            m_RightClickMenu.enabled = true;

            yield return null;

            var buttonText = m_RightClickMenu.rootVisualElement.Q<Button>("button-0");
            var buttonPicture = m_RightClickMenu.rootVisualElement.Q<Button>("button-1");
            var buttonCancel = m_RightClickMenu.rootVisualElement.Q<Button>("button-2");

            m_RightClickMenu.rootVisualElement.Q<Label>("description").text = "What content would you like to add?";

            buttonText.text = "Text";
            buttonPicture.text = "Picture";
            buttonCancel.text = "Cancel";

            buttonText.clicked += () => HandleRightClickMenuSelected(0);
            buttonPicture.clicked += () => HandleRightClickMenuSelected(1);
            buttonCancel.clicked += () =>
            {
                m_RightClickMenu.enabled = false;
            };
        }

        private IEnumerator HandleContentObjectClicked(VisualElement target)
        {
            m_RightClickMenu.enabled = true;
            m_ObjectForDeletion = (ContentObject)target.userData;

            yield return null;

            var buttonDelete = m_RightClickMenu.rootVisualElement.Q<Button>("button-0");
            var buttonCancel = m_RightClickMenu.rootVisualElement.Q<Button>("button-2");
            m_RightClickMenu.rootVisualElement.Q<Button>("button-1").style.display = DisplayStyle.None;
            m_RightClickMenu.rootVisualElement.Q<Label>("description").text = "Are you sure you want to remove this?";

            buttonDelete.text = "Delete?";
            buttonCancel.text = "Cancel";

            buttonDelete.clicked += HandleDeleteMenuSelected;
            buttonCancel.clicked += () =>
            {
                m_RightClickMenu.enabled = false;
            };
        }

        private void HandleRightClickMenuSelected(int optionIndex)
        {
            if (optionIndex == 2)
                return;

            ContentObject newObjectPrefab = null;
            VisualElement newObjectElement = null;
            string destinationImagePath = null;

            switch (optionIndex)
            {
                case 0:
                    newObjectElement = m_TextPrefab.CloneTree();
                    newObjectPrefab = new TextObject(newObjectElement);
                    break;
                case 1:
                    newObjectElement = m_PicturePrefab.CloneTree();
                    newObjectPrefab = new PictureObject(newObjectElement);

                    var paths = StandaloneFileBrowser.OpenFilePanel("Select image", "", m_ImageFilters, false);

                    if (paths == null || paths.Length == 0)
                    {
                        return;
                    }
                    else
                    {
                        var imagePath = paths[0];
                        var ext = Path.GetExtension(imagePath);

                        destinationImagePath = Path.Combine(m_PathToContentText.text, $"{Guid.NewGuid()}{ext}");
                        Debug.Log(destinationImagePath);

                        File.Copy(imagePath, destinationImagePath);
                    }
                    break;
                default:
                    break;
            }

            newObjectPrefab.Index = m_ContentObjects.Count;
            newObjectPrefab.Type = optionIndex == 0 ? "text" : "pic";

            newObjectElement.userData = newObjectPrefab;
            newObjectElement.Q<Button>("button-delete").clicked += (() =>
            {
                StartCoroutine(HandleContentObjectClicked(newObjectElement));
            });

            if (!string.IsNullOrEmpty(destinationImagePath))
            {
                newObjectPrefab.Contents = Path.GetFileName(destinationImagePath);
            }

            m_ContentObjects.Add(newObjectPrefab);
            m_WorkingArea.Add(newObjectElement);
            HandleContentChanged(null);
            m_RightClickMenu.enabled = false;
        }

        private void HandleDeleteMenuSelected()
        {
            m_ContentObjects.Remove(m_ObjectForDeletion);
            m_WorkingArea.Remove(m_ObjectForDeletion.Element);

            if (m_ObjectForDeletion is PictureObject)
            {
                var picturePath = Path.Combine(GetPathToContent(), m_ObjectForDeletion.Contents);

                if (File.Exists(picturePath))
                    File.Delete(picturePath);
            }

            m_ObjectForDeletion = null;
            HandleContentChanged(null);
            m_RightClickMenu.enabled = false;
        }

        private void HandleContentChanged(ContentObject _)
        {
            m_ContentJson = JSONObject.Create(JSONObject.Type.OBJECT);
            m_ContentJson.SetField("content", JSONObject.Create(JSONObject.Type.ARRAY));

            foreach (var c in m_ContentObjects)
            {
                m_ContentJson["content"].Add(c.ToJson());
            }

            var jsonPreview = m_ContentJson.Print(true);
            var jsonStr = m_ContentJson.Print(false);

            m_ContentJsonPreview.text = jsonPreview;

            var jsonPath = m_PathToContentText.text + "/content.json";

            File.WriteAllText(jsonPath, jsonStr);
        }

        private void HandleTitleInputChanged(ChangeEvent<string> titleChangedEvt)
        {
            var newTitle = titleChangedEvt.newValue;

            m_FoldersJson["contents"][m_ContentIndex].SetField("title", newTitle);

            var toJson = m_FoldersJson.Print(true);
            var folderInfoPath = GetPathToFoldersJson();

            m_TitlePreview.text = newTitle;

            File.WriteAllText(folderInfoPath, toJson);
        }

        private void HandleChangeBannerClicked()
        {
            var paths = StandaloneFileBrowser.OpenFilePanel("Select image", "", m_ImageFilters, false);

            if (paths == null || paths.Length == 0)
            {
                return;
            }

            var imagePath = paths[0];
            var ext = Path.GetExtension(imagePath);
            var bannerPath = GetPathToBanner(ext);

            if (File.Exists(bannerPath))
            {
                File.Delete(bannerPath);
            }

            if (m_BannerImage.style.backgroundImage != null)
            {
                Destroy(m_BannerImage.style.backgroundImage.value.texture);
                m_BannerImage.style.backgroundImage = null;
            }

            File.Copy(imagePath, bannerPath);

            var picture = new Texture2D(2, 2);

            picture.LoadImage(File.ReadAllBytes(bannerPath));

            m_BannerImage.style.backgroundImage = picture;
            m_FoldersJson["contents"][m_ContentIndex].SetField("banner", $"{m_LocalPath}{ext}");

            var toJson = m_FoldersJson.Print(true);
            var folderInfoPath = GetPathToFoldersJson();

            File.WriteAllText(folderInfoPath, toJson);
        }

        private void HandleToggleIsPublishedChanged(ChangeEvent<bool> evt)
        {
            m_IsBlogPublished = evt.newValue;
            m_FoldersJson["contents"][m_ContentIndex].SetField("isPublished", evt.newValue);

            var toJson = m_FoldersJson.Print(true);
            var folderInfoPath = GetPathToFoldersJson();

            File.WriteAllText(folderInfoPath, toJson);
        }
    }
}