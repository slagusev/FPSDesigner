﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace Editor.GameStates
{
    class CInGame : Game.CGameState
    {
        #region "Singleton"

        // Singleton Code
        private static CInGame instance = null;
        private static readonly object myLock = new object();

        // Singelton Methods
        private CInGame() { }
        public static CInGame getInstance()
        {
            lock (myLock)
            {
                if (instance == null) instance = new CInGame();
                return instance;
            }
        }
        #endregion

        private Display3D.CModel model; // (TEST) One Model displayed
        private Display3D.CCamera cam; // (TEST) One camera instancied

        private Game.CCharacter _character; //Character : can shoot, etc..

        private Game.CConsole devConsole;
        private Game.Settings.CGameSettings gameSettings;

        private KeyboardState _oldKeyState;

        private bool isPlayerUnderwater = false;

        Display3D.CSkybox skybox;
        Display3D.CTerrain terrain;
        Display3D.CLensFlare lensFlare;
        Display3D.CWater water;

        Game.CWeapon weapon;
        List<Display3D.CModel> models = new List<Display3D.CModel>();
        GraphicsDevice _graphics;

        Display3D.CBillboards grass;

        public override void Initialize()
        {
            devConsole = Game.CConsole.getInstance();
            gameSettings = Game.Settings.CGameSettings.getInstance();

            _character = new Game.CCharacter();
            _character.Initialize();

            lensFlare = new Display3D.CLensFlare();

        }

        public override void loadContent(ContentManager content, SpriteBatch spriteBatch, GraphicsDevice graphics)
        {
            //Display 1 model : Building
            model = new Display3D.CModel(content.Load<Model>("3D//building001"), new Vector3(0, 53.4f, 0), new Vector3(0, -90f, 0), new Vector3(0.01f, 0.01f, 0.01f), graphics);

            models.Add(model);

            gameSettings.loadDatas(graphics);

            devConsole.LoadContent(content, graphics, spriteBatch, cam, true, false);
            devConsole._activationKeys = gameSettings._gameSettings.KeyMapping.Console;

            //Load content for Chara class
            _character.LoadContent(content, graphics);

            lensFlare = new Display3D.CLensFlare();
            lensFlare.LoadContent(content, graphics, spriteBatch, new Vector3(0.8434627f, -0.4053462f, -0.4539611f));

            skybox = new Display3D.CSkybox(content, graphics, content.Load<TextureCube>("Textures/Clouds"));

            terrain = new Display3D.CTerrain();
            terrain.LoadContent(content.Load<Texture2D>("Textures/Terrain/Heightmap"), 1.8f, 100, content.Load<Texture2D>("Textures/Terrain/terrain_grass"), 10, lensFlare.LightDirection, graphics, content);
            terrain.WeightMap = content.Load<Texture2D>("Textures/Terrain/weightMap");
            terrain.RTexture = content.Load<Texture2D>("Textures/Terrain/sand");
            terrain.GTexture = content.Load<Texture2D>("Textures/Terrain/rock");
            terrain.BTexture = content.Load<Texture2D>("Textures/Terrain/snow");
            terrain.DetailTexture = content.Load<Texture2D>("Textures/Terrain/noise_texture");

            //Load one cam : Main camera (for the moment)
            cam = new Display3D.CCamera(graphics, new Vector3(0, 400f, 0), new Vector3(0f, 0f, 0f), 0.1f, 10000.0f, false, terrain);
            cam._physicsMap._modelsList = models;


            model._lightDirection = lensFlare.LightDirection;

            water = new Display3D.CWater(content, graphics, new Vector3(0, 44.5f, 0), new Vector2(20 * 30), 0.9f, terrain);
            water.Objects.Add(skybox);
            water.Objects.Add(terrain);
            water.Objects.Add(model);

            _graphics = graphics;

            weapon = new Game.CWeapon();

            Model[] testmodel = new Model[] { content.Load<Model>("3D//building001") };
            object[][] testInfos = new object[][] {
                new object[] {
                0,
                100,
                100,
                100,
                100,
                true,
                10,
                0
                }
            };
            string[][] testSounds = new string[][] {
                new string[] {
                    "M4A1_SHOT", "GUN_SHOT", "GUN_SHOT"
                }
            };
            weapon.LoadContent(content, testmodel, testInfos, testSounds);


            /*Effect effect = content.Load<Effect>("Effects/ProjectedTexture");
            model.SetModelEffect(effect, true);
            Display3D.Materials.ProjectedTextureMaterial mat = new Display3D.Materials.ProjectedTextureMaterial(
                content.Load<Texture2D>("projected texture"), graphics);
            mat.ProjectorPosition = new Vector3(0, 105.5f, 0);
            mat.ProjectorTarget = new Vector3(0, 0, 0);
            mat.Scale = 2;
            model.Material = mat;

            renderer = new Display3D.Materials.PrelightingRenderer(graphics, content, terrain, true);
            renderer.Models = models;
            renderer.Camera = cam;
            renderer.Lights = new List<Display3D.Materials.PPPointLight>() {
                new Display3D.Materials.PPPointLight(new Vector3(10, 80f, 0), Color.Red * .85f, 100),
                new Display3D.Materials.PPPointLight(new Vector3(0, 100f, 10), Color.Blue * .85f, 100),
            };*/

            Game.Script.CLuaVM lua = new Game.Script.CLuaVM();
            lua.Initialize();
            lua.LoadDefaultFunctions();
            lua.LoadScript("test.lua");



            Random r = new Random();

            // Retrieve pixel grid from grass map
            Texture2D grassMap = content.Load<Texture2D>("Textures/Terrain/grassMap");
            Color[] grassPixels = new Color[grassMap.Width * grassMap.Height];
            grassMap.GetData<Color>(grassPixels);

            List<Vector3> grassPositions = new List<Vector3>();
            for (int i = 0; i < 400000; i++)
            {
                // Get X and Z coordinates from the random generator, between
                // [-(terrain width) / 2 * (cell size), (terrain width) / 2 * (cell size)]
                //float x = r.Next((int)(-256 * 1.7f), (int)(256 * 1.7f));
                //float z = r.Next((int)(-256 * 1.7f), (int)(256 * 1.7f));

                float x = r.Next(Convert.ToInt32(Math.Floor(-terrain.width/2 * terrain.cellSize+1)), Convert.ToInt32(Math.Floor(terrain.width/2 * terrain.cellSize-1)));
                float z = r.Next(Convert.ToInt32(Math.Floor(-terrain.length/2 * terrain.cellSize+1)), Convert.ToInt32(Math.Floor(terrain.length/2 * terrain.cellSize-1)));

                Vector2 pos = terrain.positionToTerrain(x, z);

                // Get corresponding coordinates in grass map
                int xCoord = (int)pos.X;
                int zCoord = (int)pos.Y;

                // Get value between 0 and 1 from grass map
                float texVal = grassPixels[zCoord * 512 + xCoord].R / 255f;

                // Retrieve height
                float steepness;
                float y = terrain.GetHeightAtPosition(x, z, out steepness);

                // Randomly place a billboard here based on pixel color in grass
                // map
                if ((int)((float)r.NextDouble() * texVal * 10) == 1)
                    grassPositions.Add(new Vector3(x+(r.Next(1,20)/15), y, z+(r.Next(1,20)/15)));
                else
                    i--;
            }

            grass = new Display3D.CBillboards(graphics, content, content.Load<Texture2D>("Textures/grass"), new Vector2(2), grassPositions.ToArray());

            grass.Mode = Display3D.CBillboards.BillboardMode.Spherical;
            grass.EnsureOcclusion = false;
            
        }

        public override void unloadContent(ContentManager content)
        {

        }

        public override void Update(GameTime gameTime, KeyboardState kbState, MouseState mouseState, MouseState oldMouseState)
        {
            //Update all chara actions
            _character.Update(mouseState, oldMouseState, kbState, weapon, gameTime, cam._view,cam._projection,cam._cameraPos);

            //Update camera - _charac.Run is a functions allows player to run, loook at the param
            cam.Update(gameTime, _character.Run(kbState, cam._physicsMap._velocity), isPlayerUnderwater, water.waterPosition.Y, kbState, mouseState, _oldKeyState);

            _oldKeyState = kbState;
            devConsole.Update(kbState, gameTime);
        }

        public override void Draw(SpriteBatch spritebatch, GameTime gameTime)
        {
            //renderer.Draw();
            Vector3 playerPos = cam._cameraPos;
            //playerPos.Y -= cam._playerHeight;

            if (isPlayerUnderwater != water.isPositionUnderWater(playerPos))
            {
                isPlayerUnderwater = !isPlayerUnderwater;
                water.isUnderWater = isPlayerUnderwater;
                terrain.isUnderWater = isPlayerUnderwater;
            }

            skybox.ColorIntensity = 0.5f;
            water.PreDraw(cam, gameTime);
            skybox.ColorIntensity = 1;

            skybox.Draw(cam._view, cam._projection, cam._cameraPos);

            terrain.Draw(cam._view, cam._projection, cam._cameraPos);

            for (int i = 0; i < models.Count; i++)
                if (cam.BoundingVolumeIsInView(models[i].BoundingSphere))
                    models[i].Draw(cam._view, cam._projection, cam._cameraPos);



            water.Draw(cam._view, cam._projection, cam._cameraPos);

            lensFlare.UpdateOcclusion(cam._view, cam._projection);
            lensFlare.Draw(gameTime);

            grass.Draw(gameTime, cam._view, cam._projection, cam._up, cam._right);

            _character.Draw(spritebatch, gameTime, cam._view, cam._projection);

            devConsole.Draw(gameTime);

            //renderer.DrawDebugBoxes(gameTime, cam._view, cam._projection);
        }

    }
}
