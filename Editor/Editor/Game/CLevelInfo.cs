﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;


namespace Engine.Game.LevelInfo
{
    class CLevelInfo
    {
        LevelData _levelData;
        CXMLManager _xmlManager;

        public CLevelInfo()
        {
            _xmlManager = new CXMLManager();
        }

        public LevelData loadLevelData(string levelFile)
        {
            _levelData = _xmlManager.deserializeClass<LevelData>(levelFile);
            return _levelData;
        }
    }

    public class LevelData
    {
        public Properties Properties { get; set; }
        public SpawnInfo SpawnInfo { get; set; }
        public Terrain Terrain { get; set; }
        public WaterList Water { get; set; }
        public Lights Lights { get; set; }
        public MapModels MapModels { get; set; }
        public GameFiles GameFiles { get; set; }
        public BotsList Bots { get; set; }
        public Weapons Weapons { get; set; }
    }

    #region "Node - SpawnInfo"
    public class SpawnInfo
    {
        public float NearClip { get; set; }
        public float FarClip { get; set; }
        public float WalkSpeed { get; set; }
        public float SprintSpeed { get; set; }
        public float AimSpeed { get; set; }
        public Coordinates SpawnPosition { get; set; }
        public Coordinates SpawnRotation { get; set; }
        public string HandTexture { get; set; }
    }
    #endregion

    #region "Node - Properties"

    // Properties
    public class Properties
    {
        public string Author { get; set; }
        public string LastEditionDate { get; set; }
        public string LevelName { get; set; }
    }
    #endregion

    #region "Node - MapTerrain"
    public class Terrain
    {
        public bool UseLensflare { get; set; }
        public bool UseTerrain { get; set; }
        public float CellSize { get; set; }
        public float Height { get; set; }
        public float TextureTiling { get; set; }
        public TerrainTextures TerrainTextures { get; set; }
    }

    public class TerrainTextures
    {
        public string HeightmapFile { get; set; }
        public string TextureFile { get; set; }
        public string RTexture { get; set; }
        public string GTexture { get; set; }
        public string BTexture { get; set; }
        public string BaseTexture { get; set; }
    }
    #endregion

    #region "Node - Water"
    public class WaterList
    {
        [XmlElement("Water")]
        public List<Water> Water { get; set; }
    }
    public class Water
    {
        public float SizeX { get; set; }
        public float SizeY { get; set; }
        public Coordinates DeepestPoint { get; set; }
        public float Alpha { get; set; }
        public Coordinates Coordinates { get; set; }
    }
    #endregion

    #region "Node - Lights"
    // Lights
    public class Lights
    {
        public Lights() { LightsList = new List<Light>(); }
        [XmlElement("Light")]
        public List<Light> LightsList { get; set; }
        public bool UseShadow { get; set; }
        public Coordinates ShadowLightPos { get; set; }
        public Coordinates ShadowLightTarget { get; set; }
    }

    public class Light
    {
        public Coordinates Position { get; set; }
        public string Color { get; set; }
        public float Attenuation { get; set; }

        [XmlIgnore]
        public Color Col { get { return Display3D.CLightsManager.GetColorFromHex(Color); } }
    }
    #endregion

    #region "Node - MapModels"
    // 3D Models
    public class MapModels
    {
        public MapModels() { Models = new List<MapModels_Model>(); }
        [XmlElement("MapModels_Model")]
        public List<MapModels_Model> Models { get; set; }
        [XmlElement("MapModels_Tree")]
        public List<MapModels_Tree> Trees { get; set; }
        [XmlElement("MapModels_Pickups")]
        public List<MapModels_Pickups> Pickups { get; set; }
    }

    // 3D Models - Pickups
    public class MapModels_Pickups
    {
        public string WeaponName { get; set; }
        public int WeaponBullets { get; set; }
        public Coordinates Position { get; set; }
        public Coordinates Rotation { get; set; }
        public Coordinates Scale { get; set; }
    }

    // 3D Models - Tree
    public class MapModels_Tree
    {
        public int Seed { get; set; }
        public string Profile { get; set; }
        public bool Wind { get; set; }
        public bool Branches { get; set; }
        public Coordinates Position { get; set; }
        public Coordinates Rotation { get; set; }
        public Coordinates Scale { get; set; }
    }

    // 3D Models - Model
    public class MapModels_Model
    {
        public string ModelFile { get; set; }
        public float Alpha { get; set; }
        public float SpecColor { get; set; }
        public bool Explodable { get; set; }
        public Coordinates Position { get; set; }
        public Coordinates Rotation { get; set; }
        public Coordinates Scale { get; set; }
        public MapModels_Textures Textures { get; set; }
        public MapModels_Textures BumpTextures { get; set; }
    }

    // 3D Models - Textures node
    [XmlRoot("Textures")]
    public class MapModels_Textures
    {
        public MapModels_Textures() { Texture = new List<MapModels_Texture>(); }
        [XmlElement("Texture")]
        public List<MapModels_Texture> Texture { get; set; }
    }

    // 3D Models - Texture
    public class MapModels_Texture
    {
        [XmlText]
        public string Texture { get; set; }
        [XmlAttribute]
        public string Mesh { get; set; }
    }
    #endregion

    #region "Node - Bots"
    public class BotsList
    {
        public BotsList() { Bots = new List<Bot>(); }
        [XmlElement("Bot")]
        public List<Bot> Bots { get; set; }
    }

    public class Bot
    {
        public Coordinates SpawnPosition { get; set; }
        public Coordinates SpawnRotation { get; set; }
        public string ModelName { get; set; }
        public MapModels_Textures ModelTexture { get; set; }
        public float Life { get; set; }
        public float Velocity { get; set; }
        public float RangeOfAttack { get; set; }
        public bool IsAggressive { get; set; }
        public string Name { get; set; }
        public int Type { get; set; } // 0: Friendly/1: Enemy
    }
    #endregion

    #region "Node - Coordinates"
    public class Coordinates
    {
        [XmlIgnore]
        public Vector3 Vector3 { get { return new Vector3(X, Y, Z); } set { X = value.X; Y = value.Y; Z = value.Z; } }
        [XmlIgnore]
        public Matrix RotMatrix { get { return Matrix.CreateRotationX(X) * Matrix.CreateRotationY(Y) * Matrix.CreateRotationZ(Z); } }

        public Coordinates(float cX, float cY, float cZ) { X = cX; Y = cY; Z = cZ; }
        public Coordinates(Vector3 coord) { X = coord.X; Y = coord.Y; Z = coord.Z; }
        public Coordinates() { }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    }
    #endregion

    #region "Node - GameFiles"
    public class GameFiles
    {
        [XmlElement("Texture")]
        public string[] Texture { get; set; }
        [XmlElement("Texture2D")]
        public string[] Texture2D { get; set; }
        [XmlElement("Texture3D")]
        public string[] Texture3D { get; set; }
        [XmlElement("Model")]
        public string[] Model { get; set; }
    }
    #endregion

    #region "Node - Weapons"
    // Weapons
    public class Weapons
    {
        public Weapons() { Weapon = new List<Weapon>(); }
        [XmlElement("Weapon")]
        public List<Weapon> Weapon { get; set; }
    }
    
    // Weapons
    public class Weapon
    {
        public string Model { get; set; }
        public string Texture { get; set; }
        public int Type { get; set; }
        public int MaxClip { get; set; }
        public bool IsAutomatic { get; set; }
        public float ShotsPerSecs { get; set; }
        public int Range { get; set; }
        //public Matrix Rotation { get; set; }
        public Coordinates Offset { get; set; }
        public Coordinates Rotation { get; set; }
        public float Scale { get; set; }
        public float Delay { get; set; }
        public float RecoilIntensity { get; set; }
        public float RecoilBackIntensity { get; set; }
        public float DamagesPerBullet { get; set; }
        public string Name { get; set; }
        public WeaponSound WeaponSound { get; set; }
        public WeaponAnim WeaponAnim { get; set; }
    }

    // WeaponSound
    public class WeaponSound
    {
        public string Shot { get; set; }
        public string DryShot { get; set; }
        public string Reload { get; set; }
    }

    // WeaponAnim
    public class WeaponAnim
    {
        public string Walk { get; set; }
        public string Attack { get; set; }
        public string Idle { get; set; }
        public string Reload { get; set; }
        public string Switch { get; set; }
        public string Aim { get; set; }
        public string AimShot { get; set; }
        
        public float WalkSpeed { get; set; }
        public float AttackSpeed { get; set; }
        public float IdleSpeed { get; set; }
        public float ReloadSpeed { get; set; }
        public float SwitchSpeed { get; set; }
        public float AimSpeed { get; set; }
        public float AimShotSpeed { get; set; }
    }
    #endregion

    
    // TODO: Add all the nodes to the example
    #region Examples
    /*
    Game.LevelInfo.LevelData dataToSerialize = new Game.LevelInfo.LevelData
    {
        Properties = new Game.LevelInfo.Properties
        {
            Author = "Author",
            levelName = "LevelName",
            lastEditionDate = "07/11/2013, 21:44"
        },
        MapTerrain = new Game.LevelInfo.MapTerrain
        {
            heightmapFile = "Folder/Heightmap.bmp",
            textureFile = "Folder/TexturesMap.bmp"
        },
        MapModels = new Game.LevelInfo.MapModels
        {
            MapModels_Model = new Game.LevelInfo.MapModels_Model
            {
                MapModels_Model_Info = new Game.LevelInfo.MapModels_Model_Info
                {
                    ModelID = "building.fbx"
                },
                MapModels_Model_Position = new Game.LevelInfo.MapModels_Model_Position
                {
                    X = 0.54f,
                    Y = 21.1f,
                    Z = 32.0f,
                }
            }
        },
        GameFiles = new Game.LevelInfo.GameFiles
        {
            Texture = new[] { "Content/Texture1.fbx", "Content/Texture2.fbx" },
            Texture2D = new[] { "Content/2D/Texture1.fbx", "Content/2D/Texture2.fbx" }
        },
    };
    */
    #endregion
}
