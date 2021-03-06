﻿using FirstFloor.ModernUI.Windows;
using FirstFloor.ModernUI.Windows.Controls;
using FirstFloor.ModernUI.Windows.Navigation;
using FirstFloor.ModernUI.Presentation;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Diagnostics;

using Xceed.Wpf.Toolkit;
using Engine;

namespace Software.Pages
{
    /// <summary>
    /// Interaction logic for Home.xaml
    /// </summary>
    public partial class Home : UserControl, IContent
    {
        private MainGameEngine m_game;

        private DispatcherTimer resizeTimer = new DispatcherTimer();
        private MainWindow MainWindowInstance;

        private Point initialMoveMousePosGame1;

        private List<TreeViewItem> listTree_Trees, listTree_Models, listTree_Pickups, listTree_Waters, listTree_Lights, listTree_Bots;
        private List<TreeViewItem> listTreeAv_Models, listTreeAv_Pickups, listTreeAv_Waters, listTreeAv_Lights, listTreeAv_Bots;
        private TreeViewItem spawnPoint;

        private bool isMovingGyzmoAxis = false;

        public Home()
        {
            InitializeComponent();
            MainWindowInstance = MainWindow.Instance;

            if (GlobalVars.messageToDisplayInDialog != "")
                ModernDialog.ShowMessage(GlobalVars.messageToDisplayInDialog, "Message", MessageBoxButton.OK);

            m_game = new Engine.MainGameEngine(true, GlobalVars.rootProjectFolder + GlobalVars.contentRootFolder);
            GlobalVars.embeddedGame = m_game;

            listTree_Trees = new List<TreeViewItem>();
            listTree_Models = new List<TreeViewItem>();
            listTree_Pickups = new List<TreeViewItem>();
            listTree_Waters = new List<TreeViewItem>();
            listTree_Lights = new List<TreeViewItem>();
            listTree_Bots = new List<TreeViewItem>();

            listTreeAv_Models = new List<TreeViewItem>();
            listTreeAv_Pickups = new List<TreeViewItem>();
            listTreeAv_Waters = new List<TreeViewItem>();
            listTreeAv_Lights = new List<TreeViewItem>();
            listTreeAv_Bots = new List<TreeViewItem>();

            spawnPoint = new TreeViewItem();

            ShowXNAImage1.Source = m_game.em_WriteableBitmap;

            GameButton1.SizeChanged += ShowXNAImage_SizeChanged;
            GameButton1.MouseWheel += GameButton1_MouseWheel;
            GameButton1.PreviewMouseMove += GameButton1_PreviewMouseMove;
            GameButton1.KeyDown += GameButton1_KeyDown;

            GameButton1.GotFocus += ShowXNAImage1_GotFocus;
            GameButton1.LostFocus += ShowXNAImage1_LostFocus;

            GameButton1.PreviewMouseLeftButtonDown += GameButton1_MouseDown;
            GameButton1.PreviewMouseLeftButtonUp += GameButton1_MouseUp;

            resizeTimer.Interval = new TimeSpan(0, 0, 0, 0, 500);
            resizeTimer.Tick += new EventHandler(disTimer_Tick);

            statusBarView1.Text = "Idle";

            GlobalVars.ReloadGameComponentsTreeView += (s, e) => { LoadGameComponentsToTreeview(); };

            GameComponentsList.SelectedItemChanged += GameComponentsList_SelectedItemChanged;
            AvailableComponentsList.SelectedItemChanged += AvailableGameComponentsList_SelectedItemChanged;

            GlobalVars.selectedToolButton = SelectButton;
            SelectButton.Foreground = new SolidColorBrush((Color)FindResource("AccentColor"));
            SelectButton.Click += ToolButton_Click;
            PositionButton.Click += ToolButton_Click;
            RotateButton.Click += ToolButton_Click;
            ScaleButton.Click += ToolButton_Click;

            PlayButton.Click += PreviewButton_Click;

            LoadGameComponentsToTreeview();
        }

        void GameButton1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F && GlobalVars.selectedElt != null)
                m_game.WPFHandler("centerCamOnObject", new object[] { GlobalVars.selectedElt.eltType, GlobalVars.selectedElt.eltId });
        }

        void PreviewButton_Click(object sender, RoutedEventArgs e)
        {
            GenerateGame();
        }

        private void GenerateGame()
        {
            m_game.shouldNotUpdate = true;
            m_game.em_dispatcherTimer.Stop();
            GlobalVars.gameInfo = (Engine.Game.LevelInfo.LevelData)m_game.WPFHandler("getLevelData", true);
            GlobalVars.SaveGameLevel();

            Process previewProcess = new Process();
            previewProcess.StartInfo.FileName = GlobalVars.rootProjectFolder + GlobalVars.projectData.Properties.ExeName.Replace(".exe", "") + ".exe";
            previewProcess.EnableRaisingEvents = true;

            previewProcess.Exited += (s, e) =>
            {
                Process proc = (Process)s;
                m_game.shouldNotUpdate = false;
                m_game.em_dispatcherTimer.Start();
                m_game.em_dispatcherTimer.Interval = TimeSpan.FromSeconds(1 / 60);
                m_game.em_dispatcherTimer.Tick += new EventHandler(m_game.GameLoop);
            };

            previewProcess.Start();
        }

        void ToolButton_Click(object sender, RoutedEventArgs e)
        {
            GlobalVars.selectedToolButton.Foreground = new SolidColorBrush(Color.FromArgb(255, 209, 209, 209));
            GlobalVars.selectedToolButton = ((ModernButton)sender);
            GlobalVars.selectedToolButton.Foreground = new SolidColorBrush((Color)FindResource("AccentColor"));
            m_game.WPFHandler("changeTool", new object[] { GlobalVars.selectedToolButton.Name });
            m_game.shouldUpdateOnce = true;

            GlobalVars.AddConsoleMsg("Selected tool " + GlobalVars.selectedToolButton.Name, "info");
        }

        void ShowXNAImage1_LostFocus(object sender, RoutedEventArgs e)
        {
            m_game.shouldNotUpdate = true;
            statusBarView1.Text = "Waiting...";
        }

        void ShowXNAImage1_GotFocus(object sender, RoutedEventArgs e)
        {
            m_game.shouldNotUpdate = false;
            statusBarView1.Text = "Idle";
        }

        void GameButton1_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (isMovingGyzmoAxis)
            {
                m_game.WPFHandler("moveObject", new object[] { "drag", Mouse.GetPosition(ShowXNAImage1) });
            }
        }

        void GameButton1_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            float coef = 1;
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                coef /= 4;

            m_game.WPFHandler("moveCameraForward", (float)e.Delta * coef);
        }

        private void GameButton1_MouseRightDown(object sender, MouseButtonEventArgs e)
        {
            m_game.shouldNotUpdate = false;
            statusBarView1.Text = "Moving...";
            m_game.WPFHandler("changeCamFreeze", false);

            initialMoveMousePosGame1 = PointToScreen(Mouse.GetPosition(null));
            Cursor = Cursors.ScrollAll;

            UIElement el = (UIElement)sender;
            el.CaptureMouse();
        }

        private void GameButton1_MouseRightUp(object sender, MouseButtonEventArgs e)
        {
            m_game.shouldNotUpdate = false;
            statusBarView1.Text = "Idle";

            m_game.WPFHandler("changeCamFreeze", true);

            Cursor = Cursors.Arrow;

            UIElement el = (UIElement)sender;
            el.ReleaseMouseCapture();
        }

        void GameButton1_MouseUp(object sender, RoutedEventArgs e)
        {
            if (isMovingGyzmoAxis)
            {
                m_game.WPFHandler("moveObject", new object[] { "stop" });
                ApplyPropertiesWindow();
                isMovingGyzmoAxis = false;
            }
        }
        void GameButton1_MouseDown(object sender, RoutedEventArgs e)
        {
            object value = m_game.WPFHandler("click", Mouse.GetPosition(ShowXNAImage1));
            if (value is object[])
            {
                object[] selectElt = (object[])value;
                if (selectElt.Length == 2)
                {
                    if (GlobalVars.selectedToolButton.Name == "SelectButton")
                    {
                        if (GlobalVars.selectedElt != null)
                            m_game.WPFHandler("unselectObject", new object[] { GlobalVars.selectedElt.eltType, GlobalVars.selectedElt.eltId });

                        if ((string)selectElt[0] == "tree" && selectElt[1] is int)
                        {
                            listTree_Trees[(int)selectElt[1]].IsSelected = true;
                            m_game.WPFHandler("selectObject", new object[] { "tree", (int)selectElt[1], GlobalVars.selectedToolButton.Name });
                            GlobalVars.selectedElt = new GlobalVars.SelectedElement("tree", (int)selectElt[1]);
                        }
                        else if ((string)selectElt[0] == "model" && selectElt[1] is int)
                        {
                            listTree_Models[(int)selectElt[1]].IsSelected = true;

                            m_game.WPFHandler("selectObject", new object[] { "model", (int)selectElt[1], GlobalVars.selectedToolButton.Name });
                            GlobalVars.selectedElt = new GlobalVars.SelectedElement("model", (int)selectElt[1]);
                        }
                        else if ((string)selectElt[0] == "pickup" && selectElt[1] is int)
                        {
                            listTree_Pickups[(int)selectElt[1]].IsSelected = true;

                            m_game.WPFHandler("selectObject", new object[] { "pickup", (int)selectElt[1], GlobalVars.selectedToolButton.Name });
                            GlobalVars.selectedElt = new GlobalVars.SelectedElement("pickup", (int)selectElt[1]);
                        }
                        else if ((string)selectElt[0] == "light" && selectElt[1] is int)
                        {
                            listTree_Lights[(int)selectElt[1]].IsSelected = true;

                            m_game.WPFHandler("selectObject", new object[] { "light", (int)selectElt[1], GlobalVars.selectedToolButton.Name });
                            GlobalVars.selectedElt = new GlobalVars.SelectedElement("light", (int)selectElt[1]);
                        }
                        else if ((string)selectElt[0] == "bot" && selectElt[1] is int)
                        {
                            listTree_Bots[(int)selectElt[1]].IsSelected = true;

                            m_game.WPFHandler("selectObject", new object[] { "bot", (int)selectElt[1], GlobalVars.selectedToolButton.Name });
                            GlobalVars.selectedElt = new GlobalVars.SelectedElement("bot", (int)selectElt[1]);
                        }
                        else if ((string)selectElt[0] == "spawnpoint" && selectElt[1] is int)
                        {
                            m_game.WPFHandler("selectObject", new object[] { "spawnpoint", 0, GlobalVars.selectedToolButton.Name });
                            GlobalVars.selectedElt = new GlobalVars.SelectedElement("spawnpoint", 0);
                        }
                    }
                    else if (GlobalVars.selectedElt != null)
                    {
                        if ((string)selectElt[0] == "gizmo" && selectElt[1] is int)
                        {
                            m_game.WPFHandler("moveObject", new object[] { "start", (int)selectElt[1], GlobalVars.selectedElt.eltType, GlobalVars.selectedElt.eltId, Mouse.GetPosition(ShowXNAImage1) });

                            isMovingGyzmoAxis = true;
                        }
                    }
                }
            }
        }


        void disTimer_Tick(object sender, EventArgs e)
        {
            m_game.ChangeEmbeddedViewport((int)GameButton1.RenderSize.Width, (int)GameButton1.RenderSize.Height);
            ShowXNAImage1.Source = m_game.em_WriteableBitmap;
            resizeTimer.Stop();
            m_game.shouldUpdateOnce = true;
        }

        private void ShowXNAImage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            resizeTimer.Stop();
            resizeTimer.Start();
        }

        private void LoadAvailableComponents()
        {
            AvailableComponentsList.Items.Clear();
        }

        private void LoadGameComponentsToTreeview()
        {
            LoadAvailableGameComponentsToTreeview();

            object gameInfo = m_game.WPFHandler("getLevelData", true);
            if (gameInfo is Engine.Game.LevelInfo.LevelData)
                GlobalVars.gameInfo = (Engine.Game.LevelInfo.LevelData)gameInfo;
            else
            {
                ModernDialog.ShowMessage("Error loading project game level.", "Error", MessageBoxButton.OK);
                GlobalVars.RaiseEvent("SoftwareShouldForceClose");
            }
            GameComponentsList.Items.Clear();
            listTree_Trees.Clear();
            listTree_Models.Clear();
            listTree_Pickups.Clear();
            listTree_Waters.Clear();
            listTree_Lights.Clear();

            TreeViewItem Models = new TreeViewItem();
            TreeViewItem Trees = new TreeViewItem();
            TreeViewItem Water = new TreeViewItem();
            TreeViewItem Terrain = new TreeViewItem();
            TreeViewItem Pickups = new TreeViewItem();
            TreeViewItem Lights = new TreeViewItem();
            TreeViewItem Bots = new TreeViewItem();
            TreeViewItem SpawnPoint = new TreeViewItem();

            Models.Header = "Models";
            Trees.Header = "Trees";
            Water.Header = "Water";
            Terrain.Header = "Terrain";
            Pickups.Header = "Pick-Ups";
            Lights.Header = "Lights";
            Bots.Header = "A.I.";
            SpawnPoint.Header = "Spawn Point";

            spawnPoint = SpawnPoint;

            // Trees
            if (GlobalVars.gameInfo.MapModels != null && GlobalVars.gameInfo.MapModels.Trees != null)
            {
                foreach (Engine.Game.LevelInfo.MapModels_Tree tree in GlobalVars.gameInfo.MapModels.Trees)
                {
                    TreeViewItem treeItem = new TreeViewItem();
                    treeItem.Header = System.IO.Path.GetFileName(tree.Profile);
                    if (treeItem.Header == null)
                        treeItem.Header = tree.Profile;

                    Trees.Items.Add(treeItem);
                    listTree_Trees.Add(treeItem);
                }
            }

            // Models
            if (GlobalVars.gameInfo.MapModels != null && GlobalVars.gameInfo.MapModels.Models != null)
            {
                foreach (Engine.Game.LevelInfo.MapModels_Model model in GlobalVars.gameInfo.MapModels.Models)
                {
                    TreeViewItem treeItem = new TreeViewItem();
                    treeItem.Header = System.IO.Path.GetFileName(model.ModelFile);
                    if (treeItem.Header == null)
                        treeItem.Header = model.ModelFile;

                    Models.Items.Add(treeItem);
                    listTree_Models.Add(treeItem);
                }
            }

            // Pick-Ups
            if (GlobalVars.gameInfo.MapModels != null && GlobalVars.gameInfo.MapModels.Pickups != null)
            {
                foreach (Engine.Game.LevelInfo.MapModels_Pickups pickup in GlobalVars.gameInfo.MapModels.Pickups)
                {
                    TreeViewItem treeItem = new TreeViewItem();
                    treeItem.Header = pickup.WeaponName;
                    if (treeItem.Header == null)
                        treeItem.Header = "Pick-up";

                    Pickups.Items.Add(treeItem);
                    listTree_Pickups.Add(treeItem);
                }
            }

            // Water
            if (GlobalVars.gameInfo.Water != null && GlobalVars.gameInfo.Water.Water != null && GlobalVars.gameInfo.Water.Water.Count > 0)
            {
                foreach (Engine.Game.LevelInfo.Water water in GlobalVars.gameInfo.Water.Water)
                {
                    TreeViewItem treeItem = new TreeViewItem();
                    treeItem.Header = "Water";

                    Water.Items.Add(treeItem);
                    listTree_Waters.Add(treeItem);
                }
            }

            // Lights
            if (GlobalVars.gameInfo.Lights != null && GlobalVars.gameInfo.Lights.LightsList != null && GlobalVars.gameInfo.Lights.LightsList.Count > 0)
            {
                foreach (Engine.Game.LevelInfo.Light light in GlobalVars.gameInfo.Lights.LightsList)
                {
                    TreeViewItem treeItem = new TreeViewItem();
                    treeItem.Header = "Light #" + light.Color;

                    Lights.Items.Add(treeItem);
                    listTree_Lights.Add(treeItem);
                }
            }

            // Bots
            if (GlobalVars.gameInfo.Bots != null && GlobalVars.gameInfo.Bots.Bots != null && GlobalVars.gameInfo.Bots.Bots.Count > 0)
            {
                foreach (Engine.Game.LevelInfo.Bot bot in GlobalVars.gameInfo.Bots.Bots)
                {
                    TreeViewItem treeItem = new TreeViewItem();
                    treeItem.Header = "A.I. - " + bot.Name;

                    Bots.Items.Add(treeItem);
                    listTree_Bots.Add(treeItem);
                }

            }

            // Add items to main tree view
            if (Models.Items.Count > 0)
                GameComponentsList.Items.Add(Models);

            if (Trees.Items.Count > 0)
                GameComponentsList.Items.Add(Trees);

            if (Pickups.Items.Count > 0)
                GameComponentsList.Items.Add(Pickups);

            if (Water.Items.Count > 0)
                GameComponentsList.Items.Add(Water);

            if (Lights.Items.Count > 0)
                GameComponentsList.Items.Add(Lights);

            if (Bots.Items.Count > 0)
                GameComponentsList.Items.Add(Bots);

            if (GlobalVars.gameInfo.Terrain != null && GlobalVars.gameInfo.Terrain.UseTerrain)
                GameComponentsList.Items.Add(Terrain);

            GameComponentsList.Items.Add(SpawnPoint);
        }

        private void LoadAvailableGameComponentsToTreeview()
        {
            object gameInfo = m_game.WPFHandler("getLevelData", true);
            if (gameInfo is Engine.Game.LevelInfo.LevelData)
                GlobalVars.gameInfo = (Engine.Game.LevelInfo.LevelData)gameInfo;
            else
            {
                ModernDialog.ShowMessage("Error loading project game level.", "Error", MessageBoxButton.OK);
                GlobalVars.RaiseEvent("SoftwareShouldForceClose");
            }
            AvailableComponentsList.Items.Clear();
            listTreeAv_Models.Clear();
            listTreeAv_Pickups.Clear();
            listTreeAv_Waters.Clear();
            listTreeAv_Lights.Clear();

            TreeViewItem Models = new TreeViewItem();
            TreeViewItem Water = new TreeViewItem();
            TreeViewItem Terrain = new TreeViewItem();
            TreeViewItem Pickups = new TreeViewItem();
            TreeViewItem Lights = new TreeViewItem();
            TreeViewItem Bots = new TreeViewItem();

            Models.Header = "Models";
            Water.Header = "Water";
            Pickups.Header = "Pick-Ups";
            Lights.Header = "Lights";
            Bots.Header = "A.I.";

            // Models
            if (GlobalVars.gameInfo.MapModels != null && GlobalVars.gameInfo.MapModels.Models != null)
            {
                foreach (Engine.Game.LevelInfo.MapModels_Model model in GlobalVars.gameInfo.MapModels.Models)
                {
                    TreeViewItem treeItem = new TreeViewItem();
                    treeItem.Header = System.IO.Path.GetFileName(model.ModelFile);
                    if (treeItem.Header == null)
                        treeItem.Header = model.ModelFile;

                    Models.Items.Add(treeItem);
                    listTreeAv_Models.Add(treeItem);
                }
            }

            // Pick-Ups
            if (GlobalVars.gameInfo.Weapons.Weapon != null)
            {
                foreach (Engine.Game.LevelInfo.Weapon wep in GlobalVars.gameInfo.Weapons.Weapon)
                {
                    TreeViewItem treeItem = new TreeViewItem();
                    treeItem.Header = wep.Name;
                    if (treeItem.Header == null)
                        treeItem.Header = "Pick-up";

                    Pickups.Items.Add(treeItem);
                    listTreeAv_Pickups.Add(treeItem);
                }
            }

            // Water

            TreeViewItem treeItem2 = new TreeViewItem();
            treeItem2.Header = "Water Instance";

            Water.Items.Add(treeItem2);
            listTreeAv_Waters.Add(treeItem2);

            // Lights
            string[] colors = new string[] { "Green", "Red", "Blue" };
            foreach (string clr in colors)
            {
                TreeViewItem treeItem = new TreeViewItem();
                treeItem.Header = "Light #" + clr;

                Lights.Items.Add(treeItem);
                listTreeAv_Lights.Add(treeItem);
            }

            // Bots
            if (GlobalVars.gameInfo.Bots != null && GlobalVars.gameInfo.Bots.Bots != null && GlobalVars.gameInfo.Bots.Bots.Count > 0)
            {
                foreach (Engine.Game.LevelInfo.Bot bot in GlobalVars.gameInfo.Bots.Bots)
                {
                    TreeViewItem treeItem = new TreeViewItem();
                    treeItem.Header = "A.I. - " + bot.Name;

                    Bots.Items.Add(treeItem);
                    listTreeAv_Bots.Add(treeItem);
                }

            }

            // Add items to main tree view
            if (Models.Items.Count > 0)
                AvailableComponentsList.Items.Add(Models);

            if (Pickups.Items.Count > 0)
                AvailableComponentsList.Items.Add(Pickups);

            if (Water.Items.Count > 0)
                AvailableComponentsList.Items.Add(Water);

            if (Lights.Items.Count > 0)
                AvailableComponentsList.Items.Add(Lights);

            if (Bots.Items.Count > 0)
                AvailableComponentsList.Items.Add(Bots);

            if (GlobalVars.gameInfo.Terrain != null && GlobalVars.gameInfo.Terrain.UseTerrain)
                AvailableComponentsList.Items.Add(Terrain);
        }

        void GameComponentsList_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeViewItem selectedItem = (TreeViewItem)GameComponentsList.SelectedItem;

            // SpawnPoint
            if (selectedItem == spawnPoint)
            {
                m_game.WPFHandler("selectObject", new object[] { "spawnpoint", 0, GlobalVars.selectedToolButton.Name });
                GlobalVars.selectedElt = new GlobalVars.SelectedElement("spawnpoint", 0);
                ApplyPropertiesWindow();
                m_game.shouldUpdateOnce = true;
                return;
            }


            // Models
            for (int i = 0; i < listTree_Models.Count; i++)
            {
                if (listTree_Models[i].IsSelected)
                {
                    if (GlobalVars.selectedElt != null)
                        m_game.WPFHandler("unselectObject", new object[] { GlobalVars.selectedElt.eltType, GlobalVars.selectedElt.eltId });

                    m_game.WPFHandler("selectObject", new object[] { "model", i, GlobalVars.selectedToolButton.Name });
                    GlobalVars.selectedElt = new GlobalVars.SelectedElement("model", i);
                    ApplyPropertiesWindow();
                    m_game.shouldUpdateOnce = true;
                    return;
                }
            }

            // Trees
            for (int i = 0; i < listTree_Trees.Count; i++)
            {
                if (listTree_Trees[i].IsSelected)
                {
                    if (GlobalVars.selectedElt != null)
                        m_game.WPFHandler("unselectObject", new object[] { GlobalVars.selectedElt.eltType, GlobalVars.selectedElt.eltId });

                    m_game.WPFHandler("selectObject", new object[] { "tree", i, GlobalVars.selectedToolButton.Name });
                    GlobalVars.selectedElt = new GlobalVars.SelectedElement("tree", i);
                    ApplyPropertiesWindow();
                    m_game.shouldUpdateOnce = true;
                    return;
                }
            }

            // Pickups
            for (int i = 0; i < listTree_Pickups.Count; i++)
            {
                if (listTree_Pickups[i].IsSelected)
                {
                    if (GlobalVars.selectedElt != null)
                        m_game.WPFHandler("unselectObject", new object[] { GlobalVars.selectedElt.eltType, GlobalVars.selectedElt.eltId });

                    m_game.WPFHandler("selectObject", new object[] { "pickup", i, GlobalVars.selectedToolButton.Name });
                    GlobalVars.selectedElt = new GlobalVars.SelectedElement("pickup", i);
                    ApplyPropertiesWindow();
                    m_game.shouldUpdateOnce = true;
                    return;
                }
            }

            // Water
            for (int i = 0; i < listTree_Waters.Count; i++)
            {
                if (listTree_Waters[i].IsSelected)
                {
                    if (GlobalVars.selectedElt != null)
                        m_game.WPFHandler("unselectObject", new object[] { GlobalVars.selectedElt.eltType, GlobalVars.selectedElt.eltId });

                    m_game.WPFHandler("selectObject", new object[] { "water", i, GlobalVars.selectedToolButton.Name });
                    GlobalVars.selectedElt = new GlobalVars.SelectedElement("water", i);
                    ApplyPropertiesWindow();
                    m_game.shouldUpdateOnce = true;
                    return;
                }
            }

            // Lights
            for (int i = 0; i < listTree_Lights.Count; i++)
            {
                if (listTree_Lights[i].IsSelected)
                {
                    if (GlobalVars.selectedElt != null)
                        m_game.WPFHandler("unselectObject", new object[] { GlobalVars.selectedElt.eltType, GlobalVars.selectedElt.eltId });

                    m_game.WPFHandler("selectObject", new object[] { "light", i, GlobalVars.selectedToolButton.Name });
                    GlobalVars.selectedElt = new GlobalVars.SelectedElement("light", i);
                    ApplyPropertiesWindow();
                    m_game.shouldUpdateOnce = true;
                    return;
                }
            }

            // Bot
            for (int i = 0; i < listTree_Bots.Count; i++)
            {
                if (listTree_Bots[i].IsSelected)
                {
                    if (GlobalVars.selectedElt != null)
                        m_game.WPFHandler("unselectObject", new object[] { GlobalVars.selectedElt.eltType, GlobalVars.selectedElt.eltId });

                    m_game.WPFHandler("selectObject", new object[] { "bot", i, GlobalVars.selectedToolButton.Name });
                    GlobalVars.selectedElt = new GlobalVars.SelectedElement("bot", i);
                    ApplyPropertiesWindow();
                    m_game.shouldUpdateOnce = true;
                    return;
                }
            }

        }

        void AvailableGameComponentsList_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeViewItem selectedItem = (TreeViewItem)GameComponentsList.SelectedItem;

            // Models
            for (int i = 0; i < listTreeAv_Models.Count; i++)
            {
                if (listTreeAv_Models[i].IsSelected)
                {
                    foreach (Engine.Game.LevelInfo.MapModels_Model model in GlobalVars.gameInfo.MapModels.Models)
                        if ((string)listTreeAv_Models[i].Header == System.IO.Path.GetFileName(model.ModelFile) || (string)listTreeAv_Models[i].Header == model.ModelFile)
                        {
                            if (GlobalVars.selectedElt != null)
                                m_game.WPFHandler("unselectObject", new object[] { GlobalVars.selectedElt.eltType, GlobalVars.selectedElt.eltId });

                            int id = (int)m_game.WPFHandler("addElement", new object[] { "model", new Engine.Game.LevelInfo.MapModels_Model { Alpha = model.Alpha, BumpTextures = model.BumpTextures, Explodable = model.Explodable, ModelFile = model.ModelFile, Scale = model.Scale, SpecColor = model.SpecColor, Textures = model.Textures, Rotation = new Engine.Game.LevelInfo.Coordinates(0, 0, 0), Position = new Engine.Game.LevelInfo.Coordinates((Microsoft.Xna.Framework.Vector3)m_game.WPFHandler("forwardVec", 5.0f)) } });

                            m_game.WPFHandler("selectObject", new object[] { "model", id, GlobalVars.selectedToolButton.Name });
                            GlobalVars.selectedElt = new GlobalVars.SelectedElement("model", id);
                            ApplyPropertiesWindow();
                            m_game.shouldUpdateOnce = true;
                            LoadGameComponentsToTreeview();
                            return;
                        }
                }
            }

            // Pickups
            for (int i = 0; i < listTreeAv_Pickups.Count; i++)
            {
                if (listTreeAv_Pickups[i].IsSelected)
                {
                    if (GlobalVars.selectedElt != null)
                        m_game.WPFHandler("unselectObject", new object[] { GlobalVars.selectedElt.eltType, GlobalVars.selectedElt.eltId });

                    int id = (int)m_game.WPFHandler("addElement", new object[] { "pickup", new Engine.Game.LevelInfo.MapModels_Pickups { WeaponName = (string)listTreeAv_Pickups[i].Header, WeaponBullets = 100, Scale = new Engine.Game.LevelInfo.Coordinates(0.5f, 0.5f, 0.5f), Rotation = new Engine.Game.LevelInfo.Coordinates(0, 0, 0), Position = new Engine.Game.LevelInfo.Coordinates((Microsoft.Xna.Framework.Vector3)m_game.WPFHandler("forwardVec", 5.0f)) } });

                    m_game.WPFHandler("selectObject", new object[] { "pickup", id, GlobalVars.selectedToolButton.Name });
                    GlobalVars.selectedElt = new GlobalVars.SelectedElement("pickup", id);
                    ApplyPropertiesWindow();
                    m_game.shouldUpdateOnce = true;
                    LoadGameComponentsToTreeview();
                    return;
                }
            }

            // Water
            for (int i = 0; i < listTreeAv_Waters.Count; i++)
            {
                if (listTreeAv_Waters[i].IsSelected)
                {
                    if (GlobalVars.selectedElt != null)
                        m_game.WPFHandler("unselectObject", new object[] { GlobalVars.selectedElt.eltType, GlobalVars.selectedElt.eltId });

                    int id = (int)m_game.WPFHandler("addElement", new object[] { "water", new Engine.Game.LevelInfo.Water { SizeX = 10, SizeY = 10, Alpha = 1f, DeepestPoint = new Engine.Game.LevelInfo.Coordinates(0, -10f, 0), Coordinates = new Engine.Game.LevelInfo.Coordinates((Microsoft.Xna.Framework.Vector3)m_game.WPFHandler("forwardVec", 5.0f)) } });

                    m_game.WPFHandler("selectObject", new object[] { "water", id, GlobalVars.selectedToolButton.Name });
                    GlobalVars.selectedElt = new GlobalVars.SelectedElement("water", id);
                    ApplyPropertiesWindow();
                    m_game.shouldUpdateOnce = true;
                    LoadGameComponentsToTreeview();
                    return;
                }
            }

            // Lights
            for (int i = 0; i < listTreeAv_Lights.Count; i++)
            {
                if (listTreeAv_Lights[i].IsSelected)
                {
                    if (GlobalVars.selectedElt != null)
                        m_game.WPFHandler("unselectObject", new object[] { GlobalVars.selectedElt.eltType, GlobalVars.selectedElt.eltId });

                    string colStr = "#339966";
                    if (((string)listTreeAv_Lights[i].Header).Contains("Red"))
                        colStr = "#C7292E";
                    else if (((string)listTreeAv_Lights[i].Header).Contains("Blue"))
                        colStr = "#3366FF";

                    int id = (int)m_game.WPFHandler("addElement", new object[] { "light", new Engine.Game.LevelInfo.Light { Attenuation = 10, Color = colStr, Position = new Engine.Game.LevelInfo.Coordinates((Microsoft.Xna.Framework.Vector3)m_game.WPFHandler("forwardVec", 5.0f)) } });

                    m_game.WPFHandler("selectObject", new object[] { "light", id, GlobalVars.selectedToolButton.Name });
                    GlobalVars.selectedElt = new GlobalVars.SelectedElement("light", id);
                    ApplyPropertiesWindow();
                    m_game.shouldUpdateOnce = true;
                    LoadGameComponentsToTreeview();
                    return;
                }
            }

            // Bot
            for (int i = 0; i < listTreeAv_Bots.Count; i++)
            {
                if (listTreeAv_Bots[i].IsSelected)
                {
                    foreach (Engine.Game.LevelInfo.Bot bot in GlobalVars.gameInfo.Bots.Bots)
                        if ((string)listTreeAv_Models[i].Header == bot.Name)
                        {
                            if (GlobalVars.selectedElt != null)
                                m_game.WPFHandler("unselectObject", new object[] { GlobalVars.selectedElt.eltType, GlobalVars.selectedElt.eltId });

                            int id = (int)m_game.WPFHandler("addElement", new object[] { "bot", new Engine.Game.LevelInfo.Bot { IsAggressive = bot.IsAggressive, Life = bot.Life, Name = bot.Name, ModelName = bot.ModelName, ModelTexture = bot.ModelTexture, RangeOfAttack = bot.RangeOfAttack, SpawnRotation = new Engine.Game.LevelInfo.Coordinates(0, 0, 0), Type = bot.Type, Velocity = bot.Velocity, SpawnPosition = new Engine.Game.LevelInfo.Coordinates((Microsoft.Xna.Framework.Vector3)m_game.WPFHandler("forwardVec", 5.0f)) } });

                            m_game.WPFHandler("selectObject", new object[] { "bot", id, GlobalVars.selectedToolButton.Name });
                            GlobalVars.selectedElt = new GlobalVars.SelectedElement("bot", id);
                            ApplyPropertiesWindow();
                            m_game.shouldUpdateOnce = true;
                            LoadGameComponentsToTreeview();
                            return;
                        }
                }
            }
        }

        #region ApplyProperties
        private void ApplyPropertiesWindow()
        {
            if (GlobalVars.selectedElt != null)
            {
                Properties.Children.Clear();
                Dictionary<string, StackPanel> spElements = new Dictionary<string, StackPanel>();

                // Position
                spElements["Position"] = new StackPanel();

                TextBox tbXPos = new TextBox();
                TextBox tbYPos = new TextBox();
                TextBox tbZPos = new TextBox();

                tbXPos.Width = 70;
                tbYPos.Width = 70;
                tbZPos.Width = 70;

                object[] pos = (object[])m_game.WPFHandler("getElementInfo", new object[] { "pos", GlobalVars.selectedElt.eltType, GlobalVars.selectedElt.eltId });
                tbXPos.Text = pos[0].ToString();
                tbYPos.Text = pos[1].ToString();
                tbZPos.Text = pos[2].ToString();

                tbXPos.TextChanged += (s, e) => PropertyChanged(s, e, "pos");
                tbYPos.TextChanged += (s, e) => PropertyChanged(s, e, "pos");
                tbZPos.TextChanged += (s, e) => PropertyChanged(s, e, "pos");

                tbYPos.Margin = new Thickness(5, 0, 0, 0);
                tbZPos.Margin = new Thickness(5, 0, 0, 0);

                Label titlePos = new Label();
                titlePos.Content = "Position:";
                titlePos.Target = tbXPos;

                spElements["Position"].Children.Add(titlePos);
                spElements["Position"].Children.Add(tbXPos);
                spElements["Position"].Children.Add(tbYPos);
                spElements["Position"].Children.Add(tbZPos);

                // Rotation
                spElements["Rotation"] = new StackPanel();

                TextBox tbXRot = new TextBox();
                TextBox tbYRot = new TextBox();
                TextBox tbZRot = new TextBox();

                tbXRot.Width = 70;
                tbYRot.Width = 70;
                tbZRot.Width = 70;

                object[] rot = (object[])m_game.WPFHandler("getElementInfo", new object[] { "rot", GlobalVars.selectedElt.eltType, GlobalVars.selectedElt.eltId });
                tbXRot.Text = rot[0].ToString();
                tbYRot.Text = rot[1].ToString();
                tbZRot.Text = rot[2].ToString();

                tbXRot.TextChanged += (s, e) => PropertyChanged(s, e, "rot");
                tbYRot.TextChanged += (s, e) => PropertyChanged(s, e, "rot");
                tbZRot.TextChanged += (s, e) => PropertyChanged(s, e, "rot");

                tbYRot.Margin = new Thickness(5, 0, 0, 0);
                tbZRot.Margin = new Thickness(5, 0, 0, 0);

                Label titleRot = new Label();
                titleRot.Content = "Rotation:";
                titleRot.Target = tbXRot;

                spElements["Rotation"].Children.Add(titleRot);
                spElements["Rotation"].Children.Add(tbXRot);
                spElements["Rotation"].Children.Add(tbYRot);
                spElements["Rotation"].Children.Add(tbZRot);

                // Scale
                spElements["Scale"] = new StackPanel();

                TextBox tbXScale = new TextBox();
                TextBox tbYScale = new TextBox();
                TextBox tbZScale = new TextBox();

                tbXScale.Width = 70;
                tbYScale.Width = 70;
                tbZScale.Width = 70;

                object[] scale = (object[])m_game.WPFHandler("getElementInfo", new object[] { "scale", GlobalVars.selectedElt.eltType, GlobalVars.selectedElt.eltId });
                tbXScale.Text = scale[0].ToString();
                tbYScale.Text = scale[1].ToString();
                tbZScale.Text = scale[2].ToString();

                tbXScale.TextChanged += (s, e) => PropertyChanged(s, e, "scale");
                tbYScale.TextChanged += (s, e) => PropertyChanged(s, e, "scale");
                tbZScale.TextChanged += (s, e) => PropertyChanged(s, e, "scale");

                tbYScale.Margin = new Thickness(5, 0, 0, 0);
                tbZScale.Margin = new Thickness(5, 0, 0, 0);

                Label titleScale = new Label();
                titleScale.Content = "Scale:";
                titleScale.Target = tbXScale;

                spElements["Scale"].Children.Add(titleScale);
                spElements["Scale"].Children.Add(tbXScale);
                spElements["Scale"].Children.Add(tbYScale);
                spElements["Scale"].Children.Add(tbZScale);

                // Trees
                if (GlobalVars.selectedElt.eltType == "tree")
                {
                    // Profile
                    spElements["TreeProfile"] = new StackPanel();

                    TextBox tbProfile = new TextBox();

                    tbProfile.Width = 150;

                    string treeprofile = (string)m_game.WPFHandler("getElementInfo", new object[] { "treeprofile", GlobalVars.selectedElt.eltType, GlobalVars.selectedElt.eltId });
                    tbProfile.Text = System.IO.Path.GetFileName(treeprofile);
                    tbProfile.Margin = new Thickness(5, 0, 0, 0);
                    tbProfile.IsEnabled = false;

                    Label titleTreeProfile = new Label();
                    titleTreeProfile.Content = "Tree Profile:";
                    titleTreeProfile.Target = tbProfile;

                    spElements["TreeProfile"].Children.Add(titleTreeProfile);
                    spElements["TreeProfile"].Children.Add(tbProfile);

                    // Seed
                    spElements["TreeSeed"] = new StackPanel();

                    TextBox tbSeed = new TextBox();

                    tbSeed.Width = 50;

                    object treeseed = m_game.WPFHandler("getElementInfo", new object[] { "treeseed", GlobalVars.selectedElt.eltType, GlobalVars.selectedElt.eltId });
                    tbSeed.Text = treeseed.ToString();

                    tbSeed.Margin = new Thickness(5, 0, 0, 0);

                    Label titleTreeSeed = new Label();
                    titleTreeSeed.Content = "Tree Seed:";
                    titleTreeSeed.Target = tbSeed;

                    spElements["TreeSeed"].Children.Add(titleTreeSeed);
                    spElements["TreeSeed"].Children.Add(tbSeed);

                    // Duplicate Button
                    spElements["OptionsButtons"] = new StackPanel();

                    Button duplicateButton = new Button();
                    duplicateButton.Content = "Duplicate";
                    duplicateButton.Width = 150;

                    duplicateButton.Click += (s, e) =>
                    {
                        GlobalVars.gameInfo = (Engine.Game.LevelInfo.LevelData)m_game.WPFHandler("getLevelData", true);

                        GlobalVars.embeddedGame.WPFHandler("addElement", new object[] { "tree", GlobalVars.gameInfo.MapModels.Trees[GlobalVars.selectedElt.eltId] });
                        LoadGameComponentsToTreeview();
                    };

                    // Remove Button

                    Button removeButton = new Button();
                    removeButton.Content = "Remove";
                    removeButton.Width = 150;
                    removeButton.Margin = new Thickness(3, 0, 0, 0);

                    removeButton.Click += (s, e) =>
                    {
                        GlobalVars.gameInfo = (Engine.Game.LevelInfo.LevelData)m_game.WPFHandler("getLevelData", true);

                        GlobalVars.embeddedGame.WPFHandler("removeElement", new object[] { "tree", GlobalVars.selectedElt.eltId });
                        if (GlobalVars.selectedElt.eltId > 0)
                            GlobalVars.selectedElt.eltId--;
                        else
                            GlobalVars.selectedElt = null;
                        LoadGameComponentsToTreeview();
                        m_game.shouldUpdateOnce = true;
                    };

                    spElements["OptionsButtons"].Children.Add(duplicateButton);
                    spElements["OptionsButtons"].Children.Add(removeButton);
                }
                else if (GlobalVars.selectedElt.eltType == "pickup")
                {
                    // WeaponName
                    spElements["WeaponName"] = new StackPanel();

                    TextBox tbWN = new TextBox();

                    tbWN.Width = 150;

                    object pickupname = m_game.WPFHandler("getElementInfo", new object[] { "pickupname", GlobalVars.selectedElt.eltType, GlobalVars.selectedElt.eltId });
                    tbWN.Text = pickupname.ToString();

                    tbWN.Margin = new Thickness(5, 0, 0, 0);
                    tbWN.TextChanged += (s, e) => PropertyChanged(s, e, "pickupname");

                    Label titlePickupName = new Label();
                    titlePickupName.Content = "Weapon Name:";
                    titlePickupName.Target = tbWN;

                    spElements["WeaponName"].Children.Add(titlePickupName);
                    spElements["WeaponName"].Children.Add(tbWN);


                    // WeaponName
                    spElements["WeaponBullet"] = new StackPanel();

                    TextBox tbWB = new TextBox();
                    tbWB.Width = 50;

                    object pickupbullet = m_game.WPFHandler("getElementInfo", new object[] { "pickupbullet", GlobalVars.selectedElt.eltType, GlobalVars.selectedElt.eltId });
                    tbWB.Text = pickupbullet.ToString();

                    tbWB.TextChanged += (s, e) => PropertyChanged(s, e, "pickupbullets");

                    tbWB.Margin = new Thickness(5, 0, 0, 0);

                    Label titlePickupBullet = new Label();
                    titlePickupBullet.Content = "Bullets :";
                    titlePickupBullet.Target = tbWB;

                    spElements["WeaponBullet"].Children.Add(titlePickupBullet);
                    spElements["WeaponBullet"].Children.Add(tbWB);

                    // Duplicate Button
                    spElements["OptionsButtons"] = new StackPanel();

                    Button duplicateButton = new Button();
                    duplicateButton.Content = "Duplicate";
                    duplicateButton.Width = 150;

                    duplicateButton.Click += (s, e) =>
                    {
                        GlobalVars.gameInfo = (Engine.Game.LevelInfo.LevelData)m_game.WPFHandler("getLevelData", true);

                        GlobalVars.embeddedGame.WPFHandler("addElement", new object[] { "pickup", GlobalVars.gameInfo.MapModels.Pickups[GlobalVars.selectedElt.eltId] });
                        LoadGameComponentsToTreeview();
                    };

                    // Remove Button
                    Button removeButton = new Button();
                    removeButton.Content = "Remove";
                    removeButton.Width = 150;
                    removeButton.Margin = new Thickness(3, 0, 0, 0);

                    removeButton.Click += (s, e) =>
                    {
                        GlobalVars.gameInfo = (Engine.Game.LevelInfo.LevelData)m_game.WPFHandler("getLevelData", true);

                        GlobalVars.embeddedGame.WPFHandler("removeElement", new object[] { "pickup", GlobalVars.selectedElt.eltId });
                        if (GlobalVars.selectedElt.eltId > 0)
                            GlobalVars.selectedElt.eltId--;
                        else
                            GlobalVars.selectedElt = null;
                        LoadGameComponentsToTreeview();
                        m_game.shouldUpdateOnce = true;
                    };

                    spElements["OptionsButtons"].Children.Add(duplicateButton);
                    spElements["OptionsButtons"].Children.Add(removeButton);
                }
                else if (GlobalVars.selectedElt.eltType == "model")
                {
                    // Duplicate Button
                    spElements["OptionsButtons"] = new StackPanel();

                    Button duplicateButton = new Button();
                    duplicateButton.Content = "Duplicate";
                    duplicateButton.Width = 150;

                    duplicateButton.Click += (s, e) =>
                        {
                            GlobalVars.gameInfo = (Engine.Game.LevelInfo.LevelData)m_game.WPFHandler("getLevelData", true);

                            GlobalVars.embeddedGame.WPFHandler("addElement", new object[] { "model", GlobalVars.gameInfo.MapModels.Models[GlobalVars.selectedElt.eltId] });
                            LoadGameComponentsToTreeview();
                        };


                    // Remove Button
                    Button removeButton = new Button();
                    removeButton.Content = "Remove";
                    removeButton.Width = 150;
                    removeButton.Margin = new Thickness(3, 0, 0, 0);

                    removeButton.Click += (s, e) =>
                    {
                        GlobalVars.embeddedGame.WPFHandler("removeElement", new object[] { "model", GlobalVars.selectedElt.eltId });
                        if (GlobalVars.selectedElt.eltId > 0)
                            GlobalVars.selectedElt.eltId--;
                        else
                            GlobalVars.selectedElt = null;

                        GlobalVars.gameInfo = (Engine.Game.LevelInfo.LevelData)m_game.WPFHandler("getLevelData", true);

                        LoadGameComponentsToTreeview();
                        m_game.shouldUpdateOnce = true;
                    };

                    spElements["OptionsButtons"].Children.Add(duplicateButton);
                    spElements["OptionsButtons"].Children.Add(removeButton);

                    // Explodable
                    spElements["Explodable"] = new StackPanel();

                    CheckBox explodableBox = new CheckBox();
                    explodableBox.Width = 50;

                    object isexpdbl = m_game.WPFHandler("getElementInfo", new object[] { "explodable", GlobalVars.selectedElt.eltType, GlobalVars.selectedElt.eltId });
                    explodableBox.IsChecked = (bool)isexpdbl;

                    explodableBox.Checked += (s, e) => PropertyChecked(s, e, "explodable");

                    explodableBox.Margin = new Thickness(5, 0, 0, 0);

                    Label titleXpd = new Label();
                    titleXpd.Content = "Explodable :";
                    titleXpd.Target = explodableBox;

                    spElements["Explodable"].Children.Add(titleXpd);
                    spElements["Explodable"].Children.Add(explodableBox);
                }

                else if (GlobalVars.selectedElt.eltType == "water")
                {
                    // Duplicate Button
                    spElements["OptionsButtons"] = new StackPanel();

                    Button duplicateButton = new Button();
                    duplicateButton.Content = "Duplicate";
                    duplicateButton.Width = 150;

                    duplicateButton.Click += (s, e) =>
                    {
                        GlobalVars.gameInfo = (Engine.Game.LevelInfo.LevelData)m_game.WPFHandler("getLevelData", true);

                        GlobalVars.embeddedGame.WPFHandler("addElement", new object[] { "water", GlobalVars.gameInfo.Water.Water[GlobalVars.selectedElt.eltId] });
                        LoadGameComponentsToTreeview();
                    };


                    // Remove Button
                    Button removeButton = new Button();
                    removeButton.Content = "Remove";
                    removeButton.Width = 150;
                    removeButton.Margin = new Thickness(3, 0, 0, 0);

                    removeButton.Click += (s, e) =>
                    {
                        GlobalVars.embeddedGame.WPFHandler("removeElement", new object[] { "water", GlobalVars.selectedElt.eltId });
                        if (GlobalVars.selectedElt.eltId > 0)
                            GlobalVars.selectedElt.eltId--;
                        else
                            GlobalVars.selectedElt = null;

                        GlobalVars.gameInfo = (Engine.Game.LevelInfo.LevelData)m_game.WPFHandler("getLevelData", true);

                        LoadGameComponentsToTreeview();
                        m_game.shouldUpdateOnce = true;
                    };

                    spElements["OptionsButtons"].Children.Add(duplicateButton);
                    spElements["OptionsButtons"].Children.Add(removeButton);
                }
                else if (GlobalVars.selectedElt.eltType == "light")
                {
                    // Duplicate Button
                    spElements["OptionsButtons"] = new StackPanel();

                    Button duplicateButton = new Button();
                    duplicateButton.Content = "Duplicate";
                    duplicateButton.Width = 150;

                    duplicateButton.Click += (s, e) =>
                    {
                        GlobalVars.gameInfo = (Engine.Game.LevelInfo.LevelData)m_game.WPFHandler("getLevelData", true);

                        GlobalVars.embeddedGame.WPFHandler("addElement", new object[] { "light", GlobalVars.gameInfo.Lights.LightsList[GlobalVars.selectedElt.eltId] });
                        LoadGameComponentsToTreeview();
                    };


                    // Remove Button
                    Button removeButton = new Button();
                    removeButton.Content = "Remove";
                    removeButton.Width = 150;
                    removeButton.Margin = new Thickness(3, 0, 0, 0);

                    removeButton.Click += (s, e) =>
                    {
                        GlobalVars.embeddedGame.WPFHandler("removeElement", new object[] { "light", GlobalVars.selectedElt.eltId });
                        if (GlobalVars.selectedElt.eltId > 0)
                            GlobalVars.selectedElt.eltId--;
                        else
                            GlobalVars.selectedElt = null;

                        GlobalVars.gameInfo = (Engine.Game.LevelInfo.LevelData)m_game.WPFHandler("getLevelData", true);

                        LoadGameComponentsToTreeview();
                        m_game.shouldUpdateOnce = true;
                    };

                    spElements["OptionsButtons"].Children.Add(duplicateButton);
                    spElements["OptionsButtons"].Children.Add(removeButton);

                    // Light color button
                    spElements["LightColor"] = new StackPanel();

                    object lightColor = m_game.WPFHandler("getElementInfo", new object[] { "lightcolor", GlobalVars.selectedElt.eltType, GlobalVars.selectedElt.eltId });

                    ColorPicker colorPicker = new ColorPicker();
                    colorPicker.ColorMode = ColorMode.ColorCanvas;
                    colorPicker.UsingAlphaChannel = false;
                    colorPicker.ShowDropDownButton = false;
                    colorPicker.SelectedColor = (Color)ColorConverter.ConvertFromString("#FF" + lightColor.ToString());
                    colorPicker.SelectedColorChanged += (s, e) => ColorChanged(s, e, "lightcolor");

                    Label titleColor = new Label();
                    titleColor.Content = "Color :";
                    titleColor.Target = colorPicker;

                    spElements["LightColor"].Children.Add(titleColor);
                    spElements["LightColor"].Children.Add(colorPicker);

                    // Attenuation
                    // WeaponName
                    spElements["Attenuation"] = new StackPanel();

                    TextBox tbWB = new TextBox();
                    tbWB.Width = 50;

                    object attenuation = m_game.WPFHandler("getElementInfo", new object[] { "lightrange", GlobalVars.selectedElt.eltType, GlobalVars.selectedElt.eltId });
                    tbWB.Text = attenuation.ToString();

                    tbWB.TextChanged += (s, e) => PropertyChanged(s, e, "lightrange");

                    tbWB.Margin = new Thickness(5, 0, 0, 0);

                    Label titleAttenuation = new Label();
                    titleAttenuation.Content = "Attenuation :";
                    titleAttenuation.Target = tbWB;

                    spElements["Attenuation"].Children.Add(titleAttenuation);
                    spElements["Attenuation"].Children.Add(tbWB);
                }
                else if (GlobalVars.selectedElt.eltType == "bot")
                {
                    // Duplicate Button
                    spElements["OptionsButtons"] = new StackPanel();

                    Button duplicateButton = new Button();
                    duplicateButton.Content = "Duplicate";
                    duplicateButton.Width = 150;

                    duplicateButton.Click += (s, e) =>
                    {
                        GlobalVars.gameInfo = (Engine.Game.LevelInfo.LevelData)m_game.WPFHandler("getLevelData", true);

                        GlobalVars.embeddedGame.WPFHandler("addElement", new object[] { "bot", GlobalVars.gameInfo.Bots.Bots[GlobalVars.selectedElt.eltId] });
                        LoadGameComponentsToTreeview();
                    };


                    // Remove Button
                    Button removeButton = new Button();
                    removeButton.Content = "Remove";
                    removeButton.Width = 150;
                    removeButton.Margin = new Thickness(3, 0, 0, 0);

                    removeButton.Click += (s, e) =>
                    {
                        GlobalVars.embeddedGame.WPFHandler("removeElement", new object[] { "bot", GlobalVars.selectedElt.eltId });
                        if (GlobalVars.selectedElt.eltId > 0)
                            GlobalVars.selectedElt.eltId--;
                        else
                            GlobalVars.selectedElt = null;

                        GlobalVars.gameInfo = (Engine.Game.LevelInfo.LevelData)m_game.WPFHandler("getLevelData", true);

                        LoadGameComponentsToTreeview();
                        m_game.shouldUpdateOnce = true;
                    };

                    spElements["OptionsButtons"].Children.Add(duplicateButton);
                    spElements["OptionsButtons"].Children.Add(removeButton);

                    // Bot name
                    spElements["BotName"] = new StackPanel();

                    TextBox tbName = new TextBox();
                    tbName.Width = 200;

                    object botname = m_game.WPFHandler("getElementInfo", new object[] { "bot_name", GlobalVars.selectedElt.eltType, GlobalVars.selectedElt.eltId });
                    tbName.Text = botname.ToString();

                    tbName.TextChanged += (s, e) => PropertyChanged(s, e, "bot_name");

                    tbName.Margin = new Thickness(5, 0, 0, 0);

                    Label titleBotName = new Label();
                    titleBotName.Content = "Name :";
                    titleBotName.Target = tbName;

                    spElements["BotName"].Children.Add(titleBotName);
                    spElements["BotName"].Children.Add(tbName);

                    // Bot life
                    spElements["BotLife"] = new StackPanel();

                    TextBox tbLife = new TextBox();
                    tbLife.Width = 50;

                    object botlife = m_game.WPFHandler("getElementInfo", new object[] { "bot_life", GlobalVars.selectedElt.eltType, GlobalVars.selectedElt.eltId });
                    tbLife.Text = botlife.ToString();

                    tbLife.TextChanged += (s, e) => PropertyChanged(s, e, "bot_life");

                    tbLife.Margin = new Thickness(5, 0, 0, 0);

                    Label titleBotLife = new Label();
                    titleBotLife.Content = "Life :";
                    titleBotLife.Target = tbLife;

                    spElements["BotLife"].Children.Add(titleBotLife);
                    spElements["BotLife"].Children.Add(tbLife);

                    // Bot speed
                    spElements["BotSpeed"] = new StackPanel();

                    Slider slSpeed = new Slider();
                    slSpeed.Width = 200;
                    slSpeed.TickPlacement = System.Windows.Controls.Primitives.TickPlacement.TopLeft;
                    slSpeed.TickFrequency = 4;
                    slSpeed.Minimum = 0;
                    slSpeed.Maximum = 50;

                    object botSpeed = m_game.WPFHandler("getElementInfo", new object[] { "bot_speed", GlobalVars.selectedElt.eltType, GlobalVars.selectedElt.eltId });
                    slSpeed.Value = (int)((float)botSpeed);

                    slSpeed.ValueChanged += (s, e) => PropertyValueChange(s, e, "bot_speed");

                    slSpeed.Margin = new Thickness(5, 0, 0, 0);

                    Label titleBotSpeed = new Label();
                    titleBotSpeed.Content = "Speed :";
                    titleBotSpeed.Target = slSpeed;

                    spElements["BotSpeed"].Children.Add(titleBotSpeed);
                    spElements["BotSpeed"].Children.Add(slSpeed);

                    // Bot range of attack
                    spElements["BotRangeOfAttack"] = new StackPanel();

                    Slider slRange = new Slider();
                    slRange.Width = 200;
                    slRange.TickPlacement = System.Windows.Controls.Primitives.TickPlacement.TopLeft;
                    slRange.TickFrequency = 100;
                    slRange.Minimum = 0;
                    slRange.Maximum = 1000;

                    object botRange = m_game.WPFHandler("getElementInfo", new object[] { "bot_rangeofattack", GlobalVars.selectedElt.eltType, GlobalVars.selectedElt.eltId });
                    slRange.Value = (int)((float)botRange);

                    slRange.ValueChanged += (s, e) => PropertyValueChange(s, e, "bot_rangeofattack");

                    slRange.Margin = new Thickness(5, 0, 0, 0);

                    Label titleRange = new Label();
                    titleRange.Content = "Range Of Attack :";
                    titleRange.Target = slRange;

                    spElements["BotRangeOfAttack"].Children.Add(titleRange);
                    spElements["BotRangeOfAttack"].Children.Add(slRange);

                    // Bot type
                    spElements["BotType"] = new StackPanel();

                    ListBox lbType = new ListBox();
                    lbType.Width = 150;

                    ListBoxItem lb1 = new ListBoxItem();
                    ListBoxItem lb2 = new ListBoxItem();
                    lb1.Content = "Friendly";
                    lb2.Content = "Enemy";

                    lbType.Items.Add(lb1);
                    lbType.Items.Add(lb2);

                    object botType = m_game.WPFHandler("getElementInfo", new object[] { "bot_type", GlobalVars.selectedElt.eltType, GlobalVars.selectedElt.eltId });
                    lbType.SelectedIndex = (int)(botType);

                    lbType.SelectionChanged += (s, e) => PropertySelectionChange(s, e, "bot_type");

                    lbType.Margin = new Thickness(5, 0, 0, 0);

                    Label titleType = new Label();
                    titleType.Content = "Type :";
                    titleType.Target = slRange;

                    spElements["BotType"].Children.Add(titleType);
                    spElements["BotType"].Children.Add(lbType);

                    // Is Aggressive
                    spElements["IsAggressive"] = new StackPanel();

                    CheckBox isAggressive = new CheckBox();
                    isAggressive.Width = 50;

                    object isaggressive = m_game.WPFHandler("getElementInfo", new object[] { "bot_isaggressive", GlobalVars.selectedElt.eltType, GlobalVars.selectedElt.eltId });
                    isAggressive.IsChecked = (bool)isaggressive;

                    isAggressive.Click += (s, e) => PropertyChecked(s, e, "bot_isaggressive");

                    isAggressive.Margin = new Thickness(5, 0, 0, 0);

                    Label titleAggr = new Label();
                    titleAggr.Content = "Attacks on sight :";
                    titleAggr.Target = isAggressive;

                    spElements["IsAggressive"].Children.Add(titleAggr);
                    spElements["IsAggressive"].Children.Add(isAggressive);
                }

                // Add elements to the main StackPanel
                foreach (KeyValuePair<string, StackPanel> pair in spElements)
                {
                    spElements[pair.Key].Name = "ppt_" + pair.Key;
                    Properties.Children.Add(pair.Value);
                }
            }
        }

        private void ColorChanged(object s, RoutedPropertyChangedEventArgs<Color> e, string propertyType)
        {
            if (propertyType == "lightcolor")
            {
                string newColor = e.NewValue.R.ToString("X") + e.NewValue.G.ToString("X") + e.NewValue.B.ToString("X");
                m_game.WPFHandler("setElementInfo", new object[] { propertyType, newColor, GlobalVars.selectedElt.eltType, GlobalVars.selectedElt.eltId });
                m_game.shouldUpdateOnce = true;
            }
        }

        private void PropertyChecked(object s, RoutedEventArgs e, string propertyType)
        {
            if (propertyType == "explodable" || propertyType == "bot_isaggressive")
                m_game.WPFHandler("setElementInfo", new object[] { propertyType, ((CheckBox)s).IsChecked, GlobalVars.selectedElt.eltType, GlobalVars.selectedElt.eltId });
        }

        private void PropertySelectionChange(object s, SelectionChangedEventArgs e, string propertyType)
        {
            if (propertyType == "bot_type")
                m_game.WPFHandler("setElementInfo", new object[] { propertyType, ((ListBox)s).SelectedIndex, GlobalVars.selectedElt.eltType, GlobalVars.selectedElt.eltId });
        }

        private void PropertyValueChange(object s, RoutedPropertyChangedEventArgs<double> e, string propertyType)
        {
            if (propertyType == "bot_speed" || propertyType == "bot_rangeofattack")
                m_game.WPFHandler("setElementInfo", new object[] { propertyType, ((Slider)s).Value, GlobalVars.selectedElt.eltType, GlobalVars.selectedElt.eltId });
        }

        private void PropertyChanged(object s, TextChangedEventArgs e, string propertyType)
        {
            if (propertyType == "pos" || propertyType == "rot" || propertyType == "scale")
            {
                List<string> vals = new List<string>();
                foreach (UIElement elt in (((FrameworkElement)s).Parent as StackPanel).Children)
                {
                    if (elt is TextBox)
                        vals.Add(((TextBox)elt).Text);
                }
                m_game.WPFHandler("setElementInfo", new object[] { propertyType, new object[] { vals[0], vals[1], vals[2] }, GlobalVars.selectedElt.eltType, GlobalVars.selectedElt.eltId });
            }
            else if (propertyType == "pickupname")
            {
                if ((bool)m_game.WPFHandler("setElementInfo", new object[] { propertyType, ((TextBox)s).Text, GlobalVars.selectedElt.eltType, GlobalVars.selectedElt.eltId }))
                {
                    foreach (TreeViewItem parentpu in GameComponentsList.Items)
                        if ((string)parentpu.Header == "Pick-Ups")
                            ((TreeViewItem)parentpu.Items[GlobalVars.selectedElt.eltId]).Header = (string)((TextBox)s).Text;
                }

            }
            else if (propertyType == "pickupbullets" || propertyType == "lightrange" || propertyType == "bot_life" || propertyType == "bot_name")
                m_game.WPFHandler("setElementInfo", new object[] { propertyType, ((TextBox)s).Text, GlobalVars.selectedElt.eltType, GlobalVars.selectedElt.eltId });

            m_game.shouldUpdateOnce = true;
        }
        #endregion

        #region "Windows Menu Helper"
        public void OnFragmentNavigation(FragmentNavigationEventArgs e)
        {
            GlobalVars.OnFragmentNavigation(e);
        }

        public void OnNavigatedTo(NavigationEventArgs e)
        {
            GlobalVars.OnNavigatedTo(e);
        }

        public void OnNavigatedFrom(NavigationEventArgs e)
        {
            GlobalVars.OnNavigatedFrom(e);
        }

        public void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            GlobalVars.OnNavigatingFrom(e);
        }
        #endregion
    }
}
