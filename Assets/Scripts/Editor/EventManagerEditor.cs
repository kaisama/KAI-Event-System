﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.IMGUI.Controls;

public class EventManagerEditor : EditorWindow
{
    public class SearchBar
    {
        public Rect Area;
        public string[] SearchTypes = { "Scenes", "Events", "Listeners", "References"};
        public int CurrentSearchType = 0;

        public void UpdateArea(float x, float y, float w, float h)
        {
            if (Area == null)
            {
                Area = new Rect(x, y, w, h);
            }
            else
            {
                Area.x = x;
                Area.y = y;
                Area.width = w;
                Area.height = h;
            }
        }
    }

    public class Resizer
    {
        public Rect Area;
        public float SizeRatio;
        public bool IsResizing;
        public GUIStyle Style;

        public Resizer(Rect area, float ratio, GUIStyle style)
        {
            Area = area;
            SizeRatio = ratio;
            Style = style;
        }
    }

    private static EventManagerEditor Instance;
    private GameEventManager EventManager;

    private string[] TabTitles = { "Scenes", "Events", "Listeners", "References"};
    private int CurrentTab = 0;

    private SortedSet<string> SceneNames = new SortedSet<string>();
    private string[] DropdownNames;
    private float CoolDown = 4;
    private float Counter = 0;
    private int CurrentScene = 0;

    private Resizer resizer;
    private SearchBar searchBar;
    
    private Rect tabBar;
    private float tabBarHeight = 24;

    private SearchField searchField;
    private string searchString = "";
    private Vector2 scroll;

    private List<int> leftPanelItemsID = new List<int>();

    private string rightPanelTitle;
    private bool showRightPanel;
    private string selectedScene;
    private EventData selectedEvent;
    private GameEventListener selectedListener;
    private EventReference selectedReference;

    private int eventPickerID = -1;
    private int scenePickerID = -1;

    private GameEvent pickedEvent;
    private GameEventCollection gec;

    private SceneAsset toBeDeletedScene;
    private GameEvent toBeDeletedEvent;
    private GameEventCollection collectionToDeleteFrom;

    private SceneAsset pickedScene;

    [MenuItem("Custom Tools/Event Manager")]
    public static void OpenWindow()
    {
        if (Instance == null)
        {
            Instance = GetWindow<EventManagerEditor>();
            Instance.minSize = new Vector2(640, 480);
        }
        else
        {
            EditorWindow.FocusWindowIfItsOpen<EventManagerEditor>();
        }

        Instance.titleContent = new GUIContent("Event Manager");
    }

    private void OnEnable()
    {
        resizer = new Resizer(new Rect(0, 81, 5, position.height * 2), 0.5f, new GUIStyle());

        resizer.Style.normal.background = EditorGUIUtility.Load("icons/d_AvatarBlendBackground.png") as Texture2D;

        searchBar = new SearchBar();
        searchBar.Area = new Rect(2, 60, position.width - 6, 50);
    }

    private void OnInspectorUpdate()
    {
        if (gec && pickedEvent)
        {
            gec.Events.Add(pickedEvent);
            pickedEvent = null;
            gec = null;
        }

        if (pickedScene)
        {
            if (EventManager)
            {
                EventManager._Scenes.Add(pickedScene);
                pickedScene = null;
            }
        }

        if (collectionToDeleteFrom && toBeDeletedEvent)
        {
            collectionToDeleteFrom.RemoveEvent(toBeDeletedEvent);

            toBeDeletedEvent = null;
            collectionToDeleteFrom = null;
        }

        if (toBeDeletedScene)
        {
            if (EventManager)
            {
                EventManager.RemoveScene(toBeDeletedScene);
                toBeDeletedScene = null;
            }
        }
    }

    private void OnGUI()
    {
       // EditorGUILayout.BeginVertical();
       // scroll = EditorGUILayout.BeginScrollView(scroll, false, false);

        DrawEventManagerSection();

        EditorGUILayout.Space();

        DrawSearchSection();

        DrawDataSection();

       // EditorGUILayout.EndScrollView();
        //EditorGUILayout.EndVertical();
        DrawResizer();

        ProcessEvents(Event.current);

        if (GUI.changed) Repaint();
    }

    private void DrawEventManagerSection()
    {
        GUILayout.BeginArea(new Rect(2, 3, position.width - 6, 27), EditorStyles.helpBox);
        GUILayout.Space(3);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Game Event Manager", GUILayout.Width(((position.width - 6) / 4)));
        EventManager = (GameEventManager)EditorGUILayout.ObjectField(EventManager, typeof(GameEventManager), false, GUILayout.Width(((position.width - 6) * 3 / 4) - 15));
        EditorGUILayout.EndHorizontal();       
        GUILayout.EndArea();
    }

    private void DrawSearchSection()
    {
        GUILayout.BeginArea(searchBar.Area, EditorStyles.helpBox);
        searchBar.UpdateArea(2, 31, position.width - 6, 50);

        GUILayout.BeginVertical();

        Rect searchArea = new Rect(5, 5, position.width - 15, 18);
        Rect searchBarArea = new Rect(2, 2, position.width - 15, 18);
        GUILayout.BeginArea(searchBarArea);

        Rect scenesSelectionArea;//= new Rect(searchBarArea.width + 5, searchBarArea.y, (searchBarArea.width + 5), searchBarArea.height);

        if (EventManager)
        {
            //EventManager.RefreshSceneList();

            if ((SceneNames.Count != EventManager._Scenes.Count || Counter >= CoolDown))
            {
                if (searchString.Length == 0 && searchBar.CurrentSearchType == 0)
                {
                    SceneNames = new SortedSet<string>();

                    for (int i = 0; i < EventManager._Scenes.Count; i++)
                    {
                        if (EventManager._Scenes[i] == null)
                        {
                            continue;
                        }

                        SceneNames.Add(EventManager._Scenes[i].name);
                    }
                }


                DropdownNames = new string[SceneNames.Count];
                SceneNames.CopyTo(DropdownNames);

                scenes = new List<SceneAsset>();
                for (int i = 0; i < DropdownNames.Length; i++)
                {
                    for (int j = 0; j < EventManager._Scenes.Count; j++)
                    {
                        if (EventManager._Scenes[j] && EventManager._Scenes[j].name.Equals(DropdownNames[i]))
                        {
                            scenes.Add(EventManager._Scenes[j]);

                            break;
                        }
                    }
                }
            }

            Counter += Time.deltaTime;
            //CurrentScene = EditorGUILayout.Popup(CurrentScene, DropdownNames, GUILayout.Width(((position.width - 6) * 3 / 4) - 15));
        }

        if (searchBar.CurrentSearchType > 1 && EventManager)
        {
            searchBarArea.width = (searchBarArea.width / 2) - 5;
            scenesSelectionArea = new Rect(searchBarArea.width + 5, searchBarArea.y, (searchBarArea.width + 5), searchBarArea.height);

            GUILayout.BeginArea(scenesSelectionArea);
            CurrentScene = EditorGUILayout.MaskField(CurrentScene, DropdownNames, GUILayout.Width(scenesSelectionArea.width));
            GUILayout.EndArea();
        }

        DrawSearchBar(true, searchBarArea);
        GUILayout.EndArea();
        GUILayout.BeginArea(new Rect(2, 25, position.width - 6, 50));
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        int searchType;
        searchType = GUILayout.SelectionGrid(searchBar.CurrentSearchType, searchBar.SearchTypes, searchBar.SearchTypes.Length, EditorStyles.radioButton);

        if (searchType != searchBar.CurrentSearchType)
        {
            searchString = "";
            CurrentScene = 0;
            filter = 0;
        }

        searchBar.CurrentSearchType = searchType;

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void DrawDataSection()
    {
        DrawLeftDataPanel();
        DrawRightDataPanel();
    }

    Vector2 leftScroll;
    List<SceneAsset> scenes = new List<SceneAsset>();
    int filter = 0;
    private void DrawLeftDataPanel()
    {
        tabBar = new Rect(2, 82, position.width * resizer.SizeRatio - 3, tabBarHeight);
        GUILayout.BeginArea(tabBar, EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        //GUILayout.FlexibleSpace();
        int tab = -1;
        tab = GUILayout.Toolbar(CurrentTab, TabTitles);

        if (tab != CurrentTab)
        {
            searchString = "";
            showRightPanel = false;
            CurrentTab = tab;
        }

        //GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        GUILayout.EndArea();
        Rect leftDataArea = new Rect(tabBar.x, tabBar.y + tabBar.height, (position.width * resizer.SizeRatio - 3), position.height - 82 - tabBar.height - 3);

        if (CurrentTab == 0)
        {
            leftDataArea.height -= 25;
        }
        GUILayout.BeginArea(leftDataArea, EditorStyles.helpBox);
        using (var v = new EditorGUILayout.VerticalScope())
        {
            using (var scrollView = new EditorGUILayout.ScrollViewScope(leftScroll, GUILayout.Width(leftDataArea.width - 5), GUILayout.Height(leftDataArea.height - 1)))
            {
                leftScroll = scrollView.scrollPosition;

                switch (CurrentTab)
                {
                    case 0:
                        {
                            EditorGUILayout.Space();
                            float y = 0;
                            float singleH = 30;
                            float h = scenes.Count * 1 * singleH;
                            float nh = leftDataArea.height;
                            float nw = leftDataArea.width;
                            if (leftDataArea.height < h)
                            {
                                float dt = Mathf.Abs(leftDataArea.height - (h));
                                nh += dt;
                                nw -= 25;
                            }
                            else
                            {
                                nh -= 15;
                                nw -= 8;
                            }

                            GUILayout.Label("", GUILayout.Width(leftDataArea.width - 29), GUILayout.Height(nh));

                            leftPanelItemsID = new List<int>();

                            for (int i = 0; i < scenes.Count * 1; i++)
                            {
                                //GUILayout.BeginVertical();
                                GUILayout.BeginArea(new Rect(0, y, nw, 30), EditorStyles.helpBox);
                                GUILayout.Space(3);
                                EditorGUILayout.BeginHorizontal();
                                if(GUILayout.Button(scenes[i].name, GUILayout.Width(leftDataArea.width / 3 - 15)))
                                {
                                    // Right Panel
                                    showRightPanel = true;
                                    selectedScene = scenes[i].name;
                                    rightPanelTitle = "Scene: " + scenes[i].name + " Statistics";
                                }
                                scenes[i] = (SceneAsset)EditorGUILayout.ObjectField(scenes[i], typeof(SceneAsset), false, GUILayout.Width((leftDataArea.width * 2 / 3) - 37));

                                if (GUILayout.Button("X", EditorStyles.toolbarButton))
                                {
                                    toBeDeletedScene = scenes[i];
                                }

                                EditorGUILayout.EndHorizontal();
                                GUILayout.EndArea();
                                int id = GUIUtility.GetControlID(FocusType.Passive);
                                leftPanelItemsID.Add(id);

                                //GUILayout.EndVertical();
                                y += 30;
                            }

                            for (int i = 0; i < leftPanelItemsID.Count; i++)
                            {
                                switch (Event.current.GetTypeForControl(leftPanelItemsID[i]))
                                {
                                    case EventType.MouseUp:
                                        {
                                            if (position.Contains(Event.current.mousePosition))
                                            {
                                                Debug.Log(scenes[i].name);
                                                Event.current.Use();
                                                GUIUtility.hotControl = leftPanelItemsID[i];
                                            }
                                        }
                                        break;                                    
                                    default:
                                        break;
                                }
                            }
                        }
                        break;
                    case 1:
                        {
                            if (EventManager)
                            {
                                //EditorGUILayout.Space();
                                float y = 0;
                                float singleH = 30;
                                float h = EventManager.Events.Count * singleH;
                                float nh = leftDataArea.height;
                                float nw = leftDataArea.width;
                                if (leftDataArea.height < h)
                                {
                                    float dt = Mathf.Abs(leftDataArea.height - (h));
                                    nh += dt;
                                    nw -= 30;
                                }
                                else
                                {
                                    nh -= 27;
                                    nw -= 8;
                                }

                                if (GUILayout.Button("Find All Events in Project"))
                                {
                                    if (EventManager)
                                    {
                                        searchString = "";
                                        EventManager.FindAllEvents();
                                    }
                                }

                                GUILayout.Label("", GUILayout.Width(leftDataArea.width - 29), GUILayout.Height(nh));
                                
                                if (EventManager)
                                {
                                    foreach (var e in EventManager.Events)
                                    {
                                        GUILayout.BeginArea(new Rect(0, y + 27, nw, 30), EditorStyles.helpBox);
                                        GUILayout.Space(3);
                                        EditorGUILayout.BeginHorizontal();
                                        if (GUILayout.Button(e.Value.Event.name, GUILayout.Width(leftDataArea.width / 4 - 15)))
                                        {
                                            // Right Panel
                                            GUI.FocusControl("");
                                            selectedEvent = e.Value;
                                            showRightPanel = true;
                                            rightPanelTitle = "Event: " + selectedEvent.Name + " Details";
                                        }
                                        e.Value.Event = (CustomEvent)EditorGUILayout.ObjectField(e.Value.Event, typeof(CustomEvent), false, GUILayout.Width((leftDataArea.width * 3 / 4) - 23));
                                        EditorGUILayout.EndHorizontal();
                                        GUILayout.EndArea();
                                        //GUILayout.EndVertical();
                                        y += 32;
                                    }
                                }
                            }
                        }
                        break;
                    case 2:
                        {
                            if (EventManager)
                            {
                                //EditorGUILayout.Space();
                                float y = 0;
                                float singleH = 30;
                                float h = EventManager.Listeners.Count * singleH;
                                float nh = leftDataArea.height;
                                float nw = leftDataArea.width;
                                if (leftDataArea.height < h)
                                {
                                    float dt = Mathf.Abs(leftDataArea.height - (h));
                                    nh += dt;
                                    nw -= 30;
                                }
                                else
                                {
                                    nh -= 27;
                                    nw -= 8;
                                }

                                EditorGUILayout.BeginHorizontal();
                                bool clicked = GUILayout.Button("Find Listeners in", EditorStyles.toolbarButton);
                                GUILayout.Space(2);
                                filter = EditorGUILayout.MaskField(filter, DropdownNames, EditorStyles.toolbarPopup);

                                List<string> selectedScenes = new List<string>();

                                for (int i = 0; i < DropdownNames.Length; i++)
                                {
                                    int layer = 1 << i;

                                    if ((filter & layer) != 0)
                                    {
                                        selectedScenes.Add(DropdownNames[i]);
                                    }
                                }

                                if (clicked)
                                {
                                    if (EventManager)
                                    {
                                        EventManager.FindAllListeners(selectedScenes);
                                    }
                                }

                                EditorGUILayout.EndHorizontal();

                                GUILayout.Label("", GUILayout.Width(leftDataArea.width - 29), GUILayout.Height(nh));

                                if (EventManager)
                                {
                                    foreach (var l in EventManager.Listeners)
                                    {
                                        var listener = l;
                                        GUILayout.BeginArea(new Rect(0, y + 27, nw, 30), EditorStyles.helpBox);
                                        GUILayout.Space(3);
                                        EditorGUILayout.BeginHorizontal();
                                        if (GUILayout.Button(listener.name, GUILayout.Width(leftDataArea.width / 4 - 15)))
                                        {
                                            // Right Panel
                                            showRightPanel = true;
                                            rightPanelTitle = "Event Listener: " + listener.name + " Details";
                                            selectedListener = listener;
                                        }
                                        listener = (GameEventListener)EditorGUILayout.ObjectField(listener, typeof(GameEventListener), false, GUILayout.Width((leftDataArea.width * 3 / 4) - 23));
                                        EditorGUILayout.EndHorizontal();
                                        GUILayout.EndArea();
                                        //GUILayout.EndVertical();
                                        y += 32;
                                    }
                                }
                            }
                        }
                        break;
                    case 3:
                        {
                            if (EventManager)
                            {
                                //EditorGUILayout.Space();
                                float y = 0;
                                float singleH = 30;
                                float h = EventManager.References.Count * singleH;
                                float nh = leftDataArea.height;
                                float nw = leftDataArea.width;
                                if (leftDataArea.height < h)
                                {
                                    float dt = Mathf.Abs(leftDataArea.height - (h));
                                    nh += dt;
                                    nw -= 30;
                                }
                                else
                                {
                                    nh -= 27;
                                    nw -= 8;
                                }

                                EditorGUILayout.BeginHorizontal();
                                bool clicked = GUILayout.Button("Find References in", EditorStyles.toolbarButton);
                                GUILayout.Space(2);
                                filter = EditorGUILayout.MaskField(filter, DropdownNames, EditorStyles.toolbarPopup);

                                List<string> selectedScenes = new List<string>();

                                for (int i = 0; i < DropdownNames.Length; i++)
                                {
                                    int layer = 1 << i;

                                    if ((filter & layer) != 0)
                                    {
                                        selectedScenes.Add(DropdownNames[i]);
                                    }
                                }

                                if (clicked)
                                {
                                    if (EventManager)
                                    {
                                        searchString = "";
                                        EventManager.FindAllReferences(selectedScenes);
                                    }
                                }

                                EditorGUILayout.EndHorizontal();

                                GUILayout.Label("", GUILayout.Width(leftDataArea.width - 29), GUILayout.Height(nh));

                                if (EventManager)
                                {
                                    foreach (var r in EventManager.References)
                                    {
                                        var reference = r;
                                        GUILayout.BeginArea(new Rect(0, y + 27, nw, 30), EditorStyles.helpBox);
                                        GUILayout.Space(3);
                                        EditorGUILayout.BeginHorizontal();
                                        if (GUILayout.Button(reference.Reference.name, GUILayout.Width(leftDataArea.width / 4 - 15)))
                                        {
                                            // Right Panel
                                            showRightPanel = true;
                                            rightPanelTitle = "Event Reference: " + reference.Reference.name + " Details";
                                            selectedReference = reference;
                                        }
                                        reference.Reference = (MonoBehaviour)EditorGUILayout.ObjectField(reference.Reference, typeof(MonoBehaviour), false, GUILayout.Width((leftDataArea.width * 3 / 4) - 23));
                                        EditorGUILayout.EndHorizontal();
                                        GUILayout.EndArea();
                                        //GUILayout.EndVertical();
                                        y += 32;
                                    }
                                }
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
        }
        
        GUILayout.EndArea();

        switch (CurrentTab)
        {
            case 0:
                {
                    GUILayout.BeginArea(new Rect(leftDataArea.x, leftDataArea.height + leftDataArea.y + 3, leftDataArea.width, 25));
                    if (GUILayout.Button("Add Scene"))
                    {
                        scenePickerID = EditorGUIUtility.GetControlID(FocusType.Keyboard);

                        EditorGUIUtility.ShowObjectPicker<SceneAsset>(null, false, "", scenePickerID);
                    }

                    if (Event.current.commandName == "ObjectSelectorUpdated" && EditorGUIUtility.GetObjectPickerControlID() == scenePickerID)
                    {
                        scenePickerID = -1;
                    }else if (Event.current.commandName == "ObjectSelectorClosed" && EditorGUIUtility.GetObjectPickerControlID() == scenePickerID)
                    {
                        pickedScene = (SceneAsset)EditorGUIUtility.GetObjectPickerObject();
                    }

                    GUILayout.EndArea();
                }
                break;
            default:
                break;
        }
    }
    


    Vector2 rightScroll;
    Vector2 eventScroll;
    private void DrawRightDataPanel()
    {
        if (showRightPanel && EventManager)
        {
            //////////
            ///TitleArea///
            //////////
            Rect titleBar = new Rect((position.width * resizer.SizeRatio) + resizer.Area.width + 1, 82, position.width - (position.width * resizer.SizeRatio) - 10, 25);
            GUILayout.BeginArea(titleBar, EditorStyles.helpBox);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(rightPanelTitle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            //////////
            ///Body///
            //////////
            Rect rightDataArea = new Rect(titleBar.x, titleBar.y + titleBar.height, position.width - (position.width * resizer.SizeRatio) - 10, position.height - 82 - titleBar.height - 3);
            GUILayout.BeginArea(rightDataArea, EditorStyles.helpBox);
            using (var v = new EditorGUILayout.VerticalScope())
            {
                using (var scrollView = new EditorGUILayout.ScrollViewScope(rightScroll, GUILayout.Width(rightDataArea.width - 5), GUILayout.Height(rightDataArea.height - 1)))
                {
                    rightScroll = scrollView.scrollPosition;                  

                    switch (CurrentTab)
                    {
                        case 0:
                            {
                                SceneStatistics statistics = EventManager.GetSceneStatistics(selectedScene);
                                float labelTitleWidth = rightDataArea.width / 4;
                                float labelDataWidth = (rightDataArea.width * 3 / 4) - 20;
                                string eventsText = "This scene is using " + statistics.NumberOfEvents + " referenced across all scripts in the scene.";
                                GUILayout.BeginHorizontal();
                                EditorGUILayout.LabelField("Event Details", EditorStyles.helpBox, GUILayout.Width(labelTitleWidth));
                                EditorGUILayout.LabelField(eventsText, EditorStyles.helpBox, GUILayout.Width(labelDataWidth));
                                GUILayout.EndHorizontal();
                                EditorStyles.helpBox.fontSize = 12;
                                EditorStyles.helpBox.fontStyle = FontStyle.Bold;

                                EditorGUILayout.Space();
                                EditorGUILayout.Space();

                                string listenersText = "This scene is using " + statistics.NumberOfListeners;
                                GUILayout.BeginHorizontal();
                                EditorGUILayout.LabelField("Event Listener Details", EditorStyles.helpBox, GUILayout.Width(labelTitleWidth));
                                EditorGUILayout.LabelField(listenersText, EditorStyles.helpBox, GUILayout.Width(labelDataWidth));
                                GUILayout.EndHorizontal();

                                EditorGUILayout.Space();
                                EditorGUILayout.Space();

                                string referencesText = "This scene is using " + statistics.NumberOfReferences + " across all scripts in the scene.";
                                GUILayout.BeginHorizontal();
                                EditorGUILayout.LabelField("Event Reference Details", EditorStyles.helpBox, GUILayout.Width(labelTitleWidth));
                                EditorGUILayout.LabelField(referencesText, EditorStyles.helpBox, GUILayout.Width(labelDataWidth));
                                GUILayout.EndHorizontal();
                            }
                            break;
                        case 1:
                            {
                                var refs = EventManager.FindReference(selectedEvent.Event);
                                float singleH = 25;
                                float nh = rightDataArea.height;
                                float h = (nh / 2 + 70) + ((selectedEvent.Event.listeners.Count + refs.Count) * 1 * singleH);
                                if (selectedEvent.Event.Type == CustomEventType.Game_Event_Collection)
                                {
                                    var eventCol = (GameEventCollection)selectedEvent.Event;
                                    h = (nh / 2 + 140) + ((eventCol.Events.Count + selectedEvent.Event.listeners.Count + refs.Count) * 1 * singleH);
                                }
                                float nw = rightDataArea.width;
                                if (rightDataArea.height < h)
                                {
                                    float dt = Mathf.Abs((rightDataArea.height) - (h));
                                    nh += dt - nh / 2 - 15;
                                    nw -= 23;
                                }
                                else
                                {
                                    nh -= 15 + nh / 2 + 30;
                                    nw -= 8;
                                }

                                float labelTitleWidth = rightDataArea.width / 4;
                                float labelHeight = 25;
                                float labelDataWidth = (nw * 3 / 4) - 20;

                                GUILayout.BeginHorizontal();
                                
                                EditorGUILayout.LabelField("Event Name", EditorStyles.helpBox, GUILayout.Width(labelTitleWidth), GUILayout.Height(labelHeight));
                                EditorGUILayout.LabelField(selectedEvent.Name, EditorStyles.helpBox, GUILayout.Width(labelDataWidth), GUILayout.Height(labelHeight));
                                GUILayout.EndHorizontal();
                                EditorStyles.helpBox.fontSize = 12;
                                EditorStyles.helpBox.fontStyle = FontStyle.Bold;

                                EditorGUILayout.Space();
                                EditorGUILayout.Space();

                                GUILayout.BeginHorizontal();
                                EditorGUILayout.LabelField("Event Description", EditorStyles.helpBox, GUILayout.Width(labelTitleWidth), GUILayout.Height(50));
                                selectedEvent.Event.Description = EditorGUILayout.TextArea(selectedEvent.Event.Description, EditorStyles.textArea, GUILayout.Width(labelDataWidth), GUILayout.Height(50));
                                GUILayout.EndHorizontal();

                                EditorUtility.IsDirty(selectedEvent.Event);

                                EditorGUILayout.Space();
                                EditorGUILayout.Space();

                                GUILayout.BeginHorizontal();
                                EditorGUILayout.LabelField("Event Type", EditorStyles.helpBox, GUILayout.Width(labelTitleWidth), GUILayout.Height(labelHeight));
                                EditorGUILayout.LabelField(selectedEvent.Event.Type.ToString(), EditorStyles.helpBox, GUILayout.Width(labelDataWidth), GUILayout.Height(labelHeight));
                                GUILayout.EndHorizontal();

                                EditorGUILayout.Space();
                                EditorGUILayout.Space();
                                float y = 0;
                                if (selectedEvent.Event.Type == CustomEventType.Game_Event_Collection)
                                {
                                    EditorStyles.toolbar.fontSize = 13;
                                    y = rightDataArea.height / 2 + 20;
                                    var eventsCol = (GameEventCollection)selectedEvent.Event;
                                    GUILayout.BeginArea(new Rect(0, y - 30, rightDataArea.width, 25));
                                    GUILayout.Label("Events (" + eventsCol.Events.Count + " Events)", EditorStyles.toolbar, GUILayout.Width(nw));
                                    GUILayout.EndArea();
                                    for (int i = 0; i < eventsCol.Events.Count; i++)
                                    {
                                        Rect listenerArea = new Rect(0, y, nw, 25);
                                        EditorGUILayout.Space();

                                        if (eventsCol.Events[i] == null)
                                        {
                                            continue;
                                        }
                                        EditorStyles.helpBox.alignment = TextAnchor.MiddleCenter;
                                        EditorStyles.helpBox.padding.top = 2;

                                        GUILayout.BeginArea(listenerArea, EditorStyles.helpBox);
                                        EditorGUILayout.BeginHorizontal();
                                        GUILayout.Label(eventsCol.Events[i].name, EditorStyles.toolbar, GUILayout.Width(listenerArea.width / 4));
                                        eventsCol.Events[i] = (GameEvent)EditorGUILayout.ObjectField(eventsCol.Events[i], typeof(GameEvent), true, GUILayout.Width((listenerArea.width * 3 / 4) - 40));
                                        if (GUILayout.Button("X", EditorStyles.toolbarButton))
                                        {
                                            toBeDeletedEvent = eventsCol.Events[i];
                                            collectionToDeleteFrom = eventsCol;
                                        }
                                        EditorGUILayout.EndHorizontal();
                                        GUILayout.EndArea();
                                        //Repaint();
                                        
                                        y += 32;
                                    }

                                    bool showPicker = false;

                                    GUILayout.BeginArea(new Rect(0, y + 5, nw, 20));
                                    if (GUILayout.Button("Add Event To Collection"))
                                    {
                                        showPicker = true;
                                    }
                                    GUILayout.EndArea();

                                    if (showPicker)
                                    {
                                        eventPickerID = EditorGUIUtility.GetControlID(FocusType.Keyboard);

                                        EditorGUIUtility.ShowObjectPicker<GameEvent>(null, false, "", eventPickerID);
                                    }

                                    if (Event.current.commandName == "ObjectSelectorUpdated" && EditorGUIUtility.GetObjectPickerControlID() == eventPickerID)
                                    {
                                        pickedEvent = (GameEvent)EditorGUIUtility.GetObjectPickerObject();

                                        eventPickerID = -1;
                                    }else if (Event.current.commandName == "ObjectSelectorClosed")
                                    {
                                        gec = eventsCol;
                                    }
                                }

                                //GUILayout.FlexibleSpace();
                                GUIContent listenerTitle = new GUIContent();
                                listenerTitle.text = "Listeners (" + selectedEvent.Event.listeners.Count + " Listeners)";
                                EditorStyles.toolbar.fontSize = 13;
                                if (y == 0)
                                {
                                    y = rightDataArea.height / 2 + 20;
                                    GUILayout.BeginArea(new Rect(0, y - 30, rightDataArea.width, 25));
                                    GUILayout.Label(listenerTitle, EditorStyles.toolbar, GUILayout.Width(nw));
                                    GUILayout.EndArea();
                                }
                                else
                                {
                                    GUILayout.BeginArea(new Rect(0, y + 35, rightDataArea.width, 25));
                                    GUILayout.Label(listenerTitle, EditorStyles.toolbar, GUILayout.Width(nw));
                                    GUILayout.EndArea();
                                    y += 65;
                                }

                                for (int i = 0; i < selectedEvent.Event.listeners.Count; i++)
                                {
                                    if (selectedEvent.Event.listeners[i] == null)
                                    {
                                        selectedEvent.Event.listeners.RemoveAt(i);
                                        i--;
                                    }
                                }

                                GUILayout.Label("", GUILayout.Width(rightDataArea.width - 30), GUILayout.Height(nh));
                                for (int i = 0; i < selectedEvent.Event.listeners.Count; i++)
                                {
                                    Rect listenerArea = new Rect(0, y, nw, 25);
                                    EditorGUILayout.Space();
                                    if (selectedEvent.Event == null || selectedEvent.Event.listeners[i] == null)
                                    {
                                        continue;
                                    }
                                    EditorStyles.helpBox.alignment = TextAnchor.MiddleCenter;
                                    EditorStyles.helpBox.padding.top = 2;
                                    
                                    GUILayout.BeginArea(listenerArea, EditorStyles.helpBox);
                                    GUILayout.BeginHorizontal();

                                    GUILayout.Label(selectedEvent.Event.listeners[i].name, EditorStyles.toolbar, GUILayout.Width(listenerArea.width / 4));
                                    selectedEvent.Event.listeners[i] = (GameEventListener)EditorGUILayout.ObjectField(selectedEvent.Event.listeners[i], typeof(GameEventListener), true, GUILayout.Width((listenerArea.width * 3 / 4) - 20));
                                    GUILayout.EndHorizontal();
                                    GUILayout.EndArea();
                                    y += 32;
                                }
                                GUILayout.BeginArea(new Rect(0, y + 10, rightDataArea.width, 25));
                                GUILayout.Label("References (" + refs.Count + " References)", EditorStyles.toolbar, GUILayout.Width(nw));
                                GUILayout.EndArea();
                                y += 40;

                                EditorStyles.toolbar.fontSize = 13;


                                foreach (var item in refs)
                                {
                                    var reference = item;
                                    Rect listenerArea = new Rect(0, y, nw, 25);
                                    EditorGUILayout.Space();
                                    if (reference == null)
                                    {
                                        continue;
                                    }
                                    EditorStyles.helpBox.alignment = TextAnchor.MiddleCenter;
                                    EditorStyles.helpBox.padding.top = 2;

                                    GUILayout.BeginArea(listenerArea, EditorStyles.helpBox);
                                    GUILayout.BeginHorizontal();

                                    GUILayout.Label(reference.name, EditorStyles.toolbar, GUILayout.Width(listenerArea.width / 4));
                                    reference = (MonoBehaviour)EditorGUILayout.ObjectField(item, typeof(MonoBehaviour), true, GUILayout.Width((listenerArea.width * 3 / 4) - 20));
                                    GUILayout.EndHorizontal();
                                    GUILayout.EndArea();
                                    y += 32;
                                }
                            }
                            break;
                        case 2:
                            {
                                float labelTitleWidth = rightDataArea.width / 4;
                                float labelDataWidth = (rightDataArea.width * 3 / 4) - 20;
                                string sceneText = "This Listener is in The Scene: " + selectedListener.gameObject.scene.name;
                                GUILayout.BeginHorizontal();
                                EditorGUILayout.LabelField("Scene", EditorStyles.helpBox, GUILayout.Width(labelTitleWidth));
                                EditorGUILayout.LabelField(sceneText, EditorStyles.helpBox, GUILayout.Width(labelDataWidth));
                                GUILayout.EndHorizontal();
                                EditorStyles.helpBox.fontSize = 12;
                                EditorStyles.helpBox.fontStyle = FontStyle.Bold;

                                EditorGUILayout.Space();
                                EditorGUILayout.Space();

                                string objectText = "This Listener is on The GameObject: " + selectedListener.gameObject.name;
                                GUILayout.BeginHorizontal();
                                EditorGUILayout.LabelField("Game Object", EditorStyles.helpBox, GUILayout.Width(labelTitleWidth));
                                EditorGUILayout.LabelField(objectText, EditorStyles.helpBox, GUILayout.Width(labelDataWidth));
                                GUILayout.EndHorizontal();
                                EditorStyles.helpBox.fontSize = 12;
                                EditorStyles.helpBox.fontStyle = FontStyle.Bold;

                                EditorGUILayout.Space();
                                EditorGUILayout.Space();

                                string eventText = "This Listener is Listening waiting for \"" + selectedListener.Event.name + "\"";
                                GUILayout.BeginHorizontal();
                                EditorGUILayout.LabelField("Event", EditorStyles.helpBox, GUILayout.Width(labelTitleWidth));
                                EditorGUILayout.LabelField(eventText, EditorStyles.helpBox, GUILayout.Width(labelDataWidth));
                                GUILayout.EndHorizontal();

                                EditorGUILayout.Space();
                                EditorGUILayout.Space();

                                string responseText = "This Listener Will Activate " + selectedListener.response.GetPersistentEventCount() + " When The Event is Raised.";
                                GUILayout.BeginHorizontal();
                                EditorGUILayout.LabelField("Response", EditorStyles.helpBox, GUILayout.Width(labelTitleWidth));
                                EditorGUILayout.LabelField(responseText, EditorStyles.helpBox, GUILayout.Width(labelDataWidth));
                                GUILayout.EndHorizontal();
                            }
                            break;
                        case 3:
                            {
                                if (selectedReference != null && selectedReference.Reference)
                                {
                                    EventManager.RefreshReference(selectedReference);
                                    float singleH = 25;
                                    float nh = rightDataArea.height;
                                    float h = (nh / 2 + 40) + ((selectedReference.ReferenceNames.Count) * 1 * singleH);
                                
                                    float nw = rightDataArea.width;
                                    if (rightDataArea.height < h)
                                    {
                                        float dt = Mathf.Abs((rightDataArea.height) - (h));
                                        nh += dt - nh / 2 - 15;
                                        nw -= 23;
                                    }
                                    else
                                    {
                                        nh -= 15 + nh / 2;
                                        nw -= 8;
                                    }
                                
                                    float y = 0;

                                    float labelTitleWidth = nw / 4;
                                    float labelDataWidth = (nw * 3 / 4) - 20;

                                    string sceneText = "This Reference is in The Scene: \"" + selectedReference.Reference.gameObject.scene.name + "\"";
                                    GUILayout.BeginHorizontal();
                                    EditorGUILayout.LabelField("Scene", EditorStyles.helpBox, GUILayout.Width(labelTitleWidth), GUILayout.Height(50));
                                    EditorGUILayout.LabelField(sceneText, EditorStyles.helpBox, GUILayout.Width(labelDataWidth), GUILayout.Height(50));
                                    GUILayout.EndHorizontal();
                                    EditorStyles.helpBox.fontSize = 12;
                                    EditorStyles.helpBox.fontStyle = FontStyle.Bold;

                                    EditorGUILayout.Space();
                                    EditorGUILayout.Space();

                                    string objectText = "This Reference is on The Game Object: \"" + selectedReference.Reference.gameObject.name + "\"";
                                    GUILayout.BeginHorizontal();
                                    EditorGUILayout.LabelField("Game Object", EditorStyles.helpBox, GUILayout.Width(labelTitleWidth), GUILayout.Height(50));
                                    EditorGUILayout.LabelField(objectText, EditorStyles.helpBox, GUILayout.Width(labelDataWidth), GUILayout.Height(50));
                                    GUILayout.EndHorizontal();
                                    EditorStyles.helpBox.fontSize = 12;
                                    EditorStyles.helpBox.fontStyle = FontStyle.Bold;

                                    EditorGUILayout.Space();
                                    EditorGUILayout.Space();
                               
                                    //GUILayout.FlexibleSpace();
                                    GUIContent listenerTitle = new GUIContent();
                                    listenerTitle.text = "Fields (" + selectedReference.ReferenceNames.Count + " Fields)";
                                    EditorStyles.toolbar.fontSize = 13;
                                    if (y == 0)
                                    {
                                        y = rightDataArea.height / 2 + 10;
                                        GUILayout.Label(listenerTitle, EditorStyles.toolbar, GUILayout.Width(nw));
                                    }
                                
                                    GUILayout.Label("", GUILayout.Width(rightDataArea.width - 30), GUILayout.Height(nh));
                                    for (int i = 0; i < selectedReference.ReferenceNames.Count; i++)
                                    {
                                        Rect listenerArea = new Rect(0, y, nw, 25);
                                        EditorGUILayout.Space();
                                        //if (selectedReference.ReferenceNames[i].Length == 0)
                                        //{
                                        //    continue;
                                        //}
                                        EditorStyles.helpBox.alignment = TextAnchor.MiddleCenter;
                                        EditorStyles.helpBox.padding.top = 2;

                                        GUILayout.BeginArea(listenerArea, EditorStyles.helpBox);
                                        GUILayout.BeginHorizontal();

                                        EditorStyles.toolbar.fontSize = 10;
                                        EditorStyles.toolbar.padding.top = 2;
                                        GUILayout.Label("Field Name: " + selectedReference.ReferenceNames[i], EditorStyles.toolbar, GUILayout.Width(listenerArea.width / 3));
                                        //selectedReference.Reference.gett
                                        selectedReference.Events[i] = (CustomEvent)EditorGUILayout.ObjectField(selectedReference.Events[i], typeof(CustomEvent), true, GUILayout.Width((listenerArea.width * 2 / 3) - 20));
                                        selectedReference.Fields[i].SetValue(selectedReference.Reference, selectedReference.Events[i]);
                                        GUILayout.EndHorizontal();
                                        GUILayout.EndArea();
                                        y += 32;
                                    }                               
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            GUILayout.EndArea();
        }
    }

    void DrawSearchBar(bool asToolbar, Rect area)
    {
        DoSearchField(area, asToolbar);
    }

    void DoSearchField(Rect rect, bool asToolbar)
    {
        if (searchField == null)
        {
            searchField = new SearchField();
            //searchField.downOrUpArrowKeyPressed += OnDownOrUpArrowKeyPressed;
        }
        
        var result = asToolbar
            ? searchField.OnToolbarGUI(rect, searchString)
            : searchField.OnGUI(rect, searchString);

        if (result != searchString)
        {
            //onInputChanged(result);
            //selectedIndex = -1;
            //showResults = true;
            CurrentTab = searchBar.CurrentSearchType;
            switch (searchBar.CurrentSearchType)
            {
                case 0:
                    {
                        if (result.Length > 0)
                        {
                            SceneNames = new SortedSet<string>();

                            for (int i = 0; i < EventManager._Scenes.Count; i++)
                            {
                                if (EventManager._Scenes[i] == null)
                                {
                                    continue;
                                }

                                SceneNames.Add(EventManager._Scenes[i].name);
                            }

                            SortedSet<string> scenes = new SortedSet<string>();
                            foreach (var item in SceneNames)
                            {
                                if (item.ToLower().Contains(result.ToLower()))
                                {
                                    scenes.Add(item);
                                }
                            }
                            SceneNames = new SortedSet<string>();
                            foreach (var item in scenes)
                            {
                                SceneNames.Add(item);

                            }
                        }
                    }
                    break;
                case 1:
                    {
                        if (EventManager)
                        {
                            EventManager.FindAllEvents();
                        }

                        Dictionary<string, EventData> results = new Dictionary<string, EventData>();
                        foreach (var item in EventManager.Events)
                        {
                            if (item.Key.ToLower().Contains(result.ToLower()))
                            {
                                results.Add(item.Key, item.Value);
                            }
                        }

                        EventManager.Events = results;
                    }
                    break;
                case 2:
                    {
                        filter = CurrentScene;
                        
                        List<string> selectedScenes = new List<string>();

                        for (int i = 0; i < DropdownNames.Length; i++)
                        {
                            int layer = 1 << i;

                            if ((filter & layer) != 0)
                            {
                                selectedScenes.Add(DropdownNames[i]);
                            }
                        }

                        {
                            if (EventManager)
                            {
                                EventManager.FindAllListeners(selectedScenes);

                                List<GameEventListener> newResult = new List<GameEventListener>();

                                for (int i = 0; i < EventManager.Listeners.Count; i++)
                                {
                                    if (EventManager.Listeners[i].name.ToLower().Contains(result.ToLower()))
                                    {
                                        newResult.Add(EventManager.Listeners[i]);
                                    }
                                }

                                EventManager.Listeners = newResult;
                            }
                        }
                    }
                    break;
                case 3:
                    {
                        filter = CurrentScene;

                        List<string> selectedScenes = new List<string>();

                        for (int i = 0; i < DropdownNames.Length; i++)
                        {
                            int layer = 1 << i;

                            if ((filter & layer) != 0)
                            {
                                selectedScenes.Add(DropdownNames[i]);
                            }
                        }

                        {
                            if (EventManager)
                            {
                                EventManager.FindAllReferences(selectedScenes);

                                List<EventReference> newResult = new List<EventReference>();

                                for (int i = 0; i < EventManager.References.Count; i++)
                                {
                                    if (EventManager.References[i].Reference.name.ToLower().Contains(result.ToLower()))
                                    {
                                        newResult.Add(EventManager.References[i]);
                                    }
                                }

                                EventManager.References = newResult;
                            }
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        searchString = result;

        //if (HasSearchbarFocused())
        //{
        //    RepaintFocusedWindow();
        //}
    }

    private void DrawResizer()
    {
        resizer.Area.x = (position.width * resizer.SizeRatio);

        GUILayout.BeginArea(resizer.Area, resizer.Style);
        GUILayout.EndArea();

        EditorGUIUtility.AddCursorRect(resizer.Area, MouseCursor.ResizeHorizontal);
    }

    private void DrawSeparator(float width)
    {
        string sep = "";

        for (int j = 0; j < width; j++)
        {
            sep += '-';
        }

        EditorGUILayout.LabelField(sep, GUILayout.Width(width));
    }

    private void ProcessEvents(Event e)
    {
        switch (e.type)
        {
            case EventType.MouseDown:
                if (e.button == 0 && resizer.Area.Contains(e.mousePosition))
                {
                    resizer.IsResizing = true;
                }
                break;

            case EventType.MouseUp:
                resizer.IsResizing = false;
                break;
        }

        Resize(e);
    }

    private void Resize(Event e)
    {
        if (resizer.IsResizing)
        {
            resizer.SizeRatio = e.mousePosition.x / position.width;
            if (resizer.SizeRatio < 0)
            {
                resizer.SizeRatio = 0.01f;
            }

            if (resizer.SizeRatio > 1)
            {
                resizer.SizeRatio = 0.95f;
            }
            Repaint();
        }
    }
}
