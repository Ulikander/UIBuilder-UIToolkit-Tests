using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System.IO;
using System;
using System.Linq;

public class RpgItemDatabase : EditorWindow
{
    private Sprite m_DefaultIcon;
    private static List<RpgItem> m_itemDatabase = new List<RpgItem>();

    private VisualElement m_ItemsTab;
    private static VisualTreeAsset m_ItemRowTemplate;
    private ListView m_ItemListView;
    private float m_ItemHeight = 40;

    private ScrollView m_DetailSection;
    private VisualElement m_LargeDisplayIcon;
    private RpgItem m_ActiveItem;

    [MenuItem("RPG Test/Item Database")]
    public static void Init()
    {
        RpgItemDatabase wnd = (RpgItemDatabase)GetWindow(typeof(RpgItemDatabase)); //GetWindow<RpgItemDatabase>();
        wnd.titleContent = new GUIContent("Item Database");

        Vector2 size = new Vector2(800, 475);
        wnd.minSize = size;
        wnd.maxSize = size;

        wnd.Show();
    }

    public void CreateGUI()
    {
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/RPG/RpgItemDatabase.uxml");
        VisualElement rootFromUXML = visualTree.Instantiate();
        rootVisualElement.Add(rootFromUXML);

        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/RPG/RpgItemDatabase.uss");
        rootVisualElement.styleSheets.Add(styleSheet);

        m_DefaultIcon = (Sprite)AssetDatabase.LoadAssetAtPath("Assets/RPG/blasphemy.jpg", typeof(Sprite));

        m_ItemRowTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/RPG/RpgItemRowTemplate.uxml");

        LoadAllItems();

        m_ItemsTab = rootVisualElement.Q<VisualElement>("ItemsTab");
        GenerateListView();

        m_DetailSection = rootVisualElement.Q<ScrollView>("DetailsContainer");
        m_DetailSection.style.visibility = Visibility.Hidden;
        m_LargeDisplayIcon = m_DetailSection.Q<VisualElement>("DetailsIcon");

        rootVisualElement.Q<Button>("ButtonAdd").clicked += AddItem_OnClick;

        m_DetailSection.Q<TextField>("DetailsName").RegisterValueChangedCallback((evt) =>
        {
            m_ActiveItem.FriendlyName = evt.newValue;
            m_ItemListView.Refresh();
        });

        m_DetailSection.Q<ObjectField>("DetailsIcon").RegisterValueChangedCallback((evt) =>
        {
            Sprite newSprite = evt.newValue as Sprite;

            m_LargeDisplayIcon.style.backgroundImage = newSprite.texture;

            m_ActiveItem.Icon = newSprite;
            m_ItemListView.Refresh();
        });

        rootVisualElement.Q<Button>("ButtonDelete").clicked += DeleteItem_OnClick;
    }

    private void LoadAllItems()
    {
        m_itemDatabase.Clear();

        string[] allPaths = Directory.GetFiles("Assets/RPG/Items", "*.asset", SearchOption.AllDirectories);

        foreach (string path in allPaths)
        {
            string cleanedPath = path.Replace("\\", "/");
            m_itemDatabase.Add((RpgItem)AssetDatabase.LoadAssetAtPath(cleanedPath, typeof(RpgItem)));
        }
    }

    private void GenerateListView()
    {
        Func<VisualElement> makeItem = () => m_ItemRowTemplate.CloneTree();

        Action<VisualElement, int> bindItem = (e, i) =>
        {
            e.Q<VisualElement>("Icon").style.backgroundImage =
                m_itemDatabase[i].Icon == null ? m_DefaultIcon.texture : m_itemDatabase[i].Icon.texture;
            e.Q<Label>("Name").text = m_itemDatabase[i].FriendlyName;
        };

        m_ItemListView = new ListView(m_itemDatabase, 35, makeItem, bindItem);
        m_ItemListView.selectionType = SelectionType.Single;
        m_ItemListView.style.height = m_itemDatabase.Count * m_ItemHeight;
        m_ItemsTab.Add(m_ItemListView);

        m_ItemListView.onSelectionChange += ListView_OnSelectionChange;
    }

    private void ListView_OnSelectionChange(IEnumerable<object> selectedItems)
    {
        m_ActiveItem = (RpgItem)selectedItems.First();

        SerializedObject so = new SerializedObject(m_ActiveItem);
        m_DetailSection.Bind(so);

        if (m_ActiveItem.Icon != null) m_LargeDisplayIcon.style.backgroundImage = m_ActiveItem.Icon.texture;
        else m_LargeDisplayIcon.style.backgroundImage = m_DefaultIcon.texture;

        m_DetailSection.style.visibility = Visibility.Visible;
    }

    private void AddItem_OnClick()
    {
        RpgItem newItem = CreateInstance<RpgItem>();
        newItem.FriendlyName = "New Item";

        AssetDatabase.CreateAsset(newItem, $"Assets/RPG/Items/{newItem.ID}.asset");

        m_itemDatabase.Add(newItem);

        m_ItemListView.Refresh();
        m_ItemListView.style.height = m_itemDatabase.Count * m_ItemHeight;
    }

    private void DeleteItem_OnClick()
    {
        string path = AssetDatabase.GetAssetPath(m_ActiveItem);
        AssetDatabase.DeleteAsset(path);

        m_itemDatabase.Remove(m_ActiveItem);
        m_ItemListView.Refresh();

        m_DetailSection.style.visibility = Visibility.Hidden;
    }
}
