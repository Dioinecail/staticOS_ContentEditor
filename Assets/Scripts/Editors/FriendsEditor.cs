using SFB;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.StaticOSEditor
{
    public class FriendsEditor : ContentEditorBase
    {
        [SerializeField] private VisualTreeAsset m_FriendEditCard;

        private VisualElement m_FriendsGrid;
        private ExtensionFilter[] m_ImageFilters = new ExtensionFilter[] {
            new ExtensionFilter("", new string[]
            {
                "png", "jpg", "jpeg"
            })
        };
        private Label m_ContentJsonPreview;



        public override void LoadContent(string contentPath, string _)
        {
            StartCoroutine(DoLoadContent(contentPath, _));
        }

        private IEnumerator DoLoadContent(string contentPath, string _)
        {
            yield return null;

            m_FriendsGrid = DomElement.Q("friends-grid");
            m_PathToContentText = DomElement.Q<Label>("content-path");
            m_ContentJsonPreview = DomElement.Q<Label   >("config-preview");

            m_PathToContentText.text = contentPath;

            var buttonAddFriend = DomElement.Q<Button>("button-add");

            buttonAddFriend.clicked += () =>
            {
                var json = JSONObject.Create(JSONObject.Type.OBJECT);

                json.SetField("url", string.Empty);
                json.SetField("title", string.Empty);
                json.SetField("img", string.Empty);

                CreateFriend(json);
            };

            var json = File.ReadAllText(GetPathToContentJson());

            if (string.IsNullOrEmpty(json))
                throw new Exception($"Json file not found at '{GetPathToContentJson()}'!");

            m_ContentJson = JSONObject.Create(json);
            m_ContentJsonPreview.text = m_ContentJson.Print(true);
            
            foreach (var friendJson in m_ContentJson["content"])
            {
                CreateFriend(friendJson);
            }
        }

        private void CreateFriend(JSONObject friendJson)
        {
            var template = m_FriendEditCard.CloneTree().Q("friend-root");

            var buttonBanner = template.Q<Button>("button-img");
            var inputUrl = template.Q<TextField>("url-field");
            var inputTitle = template.Q<TextField>("title-field");

            var buttonRemoveFriend = template.Q<Button>("button-remove");
            var buttonMoveUp = template.Q<Button>("button-move-up");
            var buttonMoveDown = template.Q<Button>("button-move-down");

            if (!string.IsNullOrEmpty(friendJson["url"].str))
                inputUrl.value = friendJson["url"].str;

            if (!string.IsNullOrEmpty(friendJson["title"].str))
                inputTitle.value = friendJson["title"].str;

            var bannerFilePath = Path.Combine(GameObject.FindObjectOfType<ContentEditorBase>().GetPathToContent(), friendJson["img"].str);

            if (File.Exists(bannerFilePath))
            {
                var tex = new Texture2D(2, 2);
                var pictureBytes = File.ReadAllBytes(bannerFilePath);

                tex.LoadImage(pictureBytes);
                tex.Apply();

                buttonBanner.style.backgroundImage = tex;
            }

            template.userData = friendJson;

            buttonRemoveFriend.clicked += () =>
            {
                template.parent.Remove(template);
                HandleContentChanged();
            };

            buttonMoveUp.clicked += () =>
            {
                var index = template.parent.IndexOf(template);

                template.parent.Insert(index - 1, template);
                HandleContentChanged();
            };

            buttonMoveDown.clicked += () =>
            {
                var index = template.parent.IndexOf(template);

                template.parent.Insert(index + 1, template);
                HandleContentChanged();
            };

            buttonBanner.clicked += () =>
            {
                var destinationImagePath = "";
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

                if (!string.IsNullOrEmpty(destinationImagePath))
                {
                    var tex = new Texture2D(2, 2);
                    var pictureBytes = File.ReadAllBytes(destinationImagePath);

                    tex.LoadImage(pictureBytes);
                    tex.Apply();

                    buttonBanner.style.backgroundImage = tex;
                    var bannerUrl = Path.GetFileName(destinationImagePath);

                    friendJson.SetField("img", bannerUrl);
                    HandleContentChanged();
                }
            };

            inputUrl.RegisterValueChangedCallback((evt) => {
                friendJson.SetField("url", evt.newValue);
                HandleContentChanged();
            });

            inputTitle.RegisterValueChangedCallback((evt) => {
                friendJson.SetField("title", evt.newValue);
                HandleContentChanged();
            });

            m_FriendsGrid.Add(template);
        }

        private void HandleContentChanged()
        {
            m_ContentJson = JSONObject.Create(JSONObject.Type.OBJECT);
            m_ContentJson.SetField("content", JSONObject.Create(JSONObject.Type.ARRAY));

            foreach (var c in m_FriendsGrid.Children())
            {
                m_ContentJson["content"].Add(c.userData as JSONObject);
            }

            var jsonPreview = m_ContentJson.Print(true);
            var jsonStr = m_ContentJson.Print(false);

            m_ContentJsonPreview.text = jsonPreview;

            var jsonPath = m_PathToContentText.text + "/content.json";

            File.WriteAllText(jsonPath, jsonStr);
        }
    }
}