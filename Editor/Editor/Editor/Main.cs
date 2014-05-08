using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

using System.Diagnostics;

namespace Engine
{
    /// <summary>
    /// The main file, core of the program
    /// </summary>
    public class MainGameEngine : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        KeyboardState oldKeyboardState;
        MouseState oldMouseState;

        Display2D.CRenderCapture renderCapture;
        Display2D.CPostProcessor postProcessor;

        // WPF
        // Used to emulate XNA when embedded in WPF

        public WriteableBitmap em_WriteableBitmap { get; set; }
        private Point em_sizeViewport;
        private RenderTarget2D em_renderTarget2D;
        private byte[] em_bytes;
        public DispatcherTimer em_dispatcherTimer;
        private GameTime em_GameTime;
        private Stopwatch em_StopWatch;
        private TimeSpan em_LastTime;
        public bool isSoftwareEmbedded = false;
        public bool shouldNotUpdate = false;
        public bool shouldUpdateOnce = false;

        public MainGameEngine(bool launchedFromSoftware = false)
        {
            isSoftwareEmbedded = launchedFromSoftware;

            graphics = new GraphicsDeviceManager(this);
            //graphics.PreferMultiSampling = false; // Lags!!!
            Content.RootDirectory = "Content";

            graphics.PreferredBackBufferWidth = 1920;
            graphics.PreferredBackBufferHeight = 1080;
            //graphics.IsFullScreen = !launchedFromSoftware;
            graphics.IsFullScreen = false;
            Window.AllowUserResizing = true;

            graphics.SynchronizeWithVerticalRetrace = false;
            IsFixedTimeStep = false;


            // Icon
            if (System.IO.File.Exists("Icon.ico"))
                ((System.Windows.Forms.Form)System.Windows.Forms.Form.FromHandle(Window.Handle)).Icon = new System.Drawing.Icon("Icon.ico");

            // WPF
            if (isSoftwareEmbedded)
            {
                Window.ClientSizeChanged += new EventHandler<EventArgs>(Window_ClientSizeChanged);
                em_sizeViewport = new Point(graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);
                em_WriteableBitmap = new WriteableBitmap(em_sizeViewport.X, em_sizeViewport.Y, 96, 96, PixelFormats.Bgr565, null);
                em_bytes = new byte[em_sizeViewport.X * em_sizeViewport.Y * 2];

                em_LastTime = new TimeSpan();
                em_GameTime = new GameTime();
                em_dispatcherTimer = new DispatcherTimer();
                em_dispatcherTimer.Interval = TimeSpan.FromSeconds(1 / 60);
                em_dispatcherTimer.Tick += new EventHandler(GameLoop);

                this.Initialize();
                //this.LoadContent();
                em_dispatcherTimer.Start();
                em_StopWatch = new Stopwatch();
                em_StopWatch.Start();
            }
        }

        void Window_ClientSizeChanged(object sender, EventArgs e)
        {
            ChangeEmbeddedViewport(Window.ClientBounds.Width, Window.ClientBounds.Height);
        }

        public void ChangeEmbeddedViewport(int width, int height)
        {
            if (width % 2 != 0)
                width++;

            graphics.PreferredBackBufferWidth = width;
            graphics.PreferredBackBufferHeight = height;
            renderCapture.ChangeRenderTargetSize(width, height);
            
            if (isSoftwareEmbedded)
            {
                em_sizeViewport = new Point(graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);
                em_WriteableBitmap = new WriteableBitmap(em_sizeViewport.X, em_sizeViewport.Y, 96, 96, PixelFormats.Bgr565, null);
                em_bytes = new byte[em_sizeViewport.X * em_sizeViewport.Y * 2];
                em_renderTarget2D = new RenderTarget2D(GraphicsDevice, em_sizeViewport.X, em_sizeViewport.Y, true, SurfaceFormat.Bgr565, DepthFormat.Depth16);
                Display2D.C2DEffect.renderTarget = em_renderTarget2D;
                Display2D.C2DEffect.softwareViewport.Width = width;
                Display2D.C2DEffect.softwareViewport.Height = height;
            }
            
            //graphics.ApplyChanges(); // Seem to be crashing
        }

        protected override void Initialize()
        {
            if (isSoftwareEmbedded)
            {
                // Create the graphics device
                IGraphicsDeviceManager graphicsDeviceManager = this.Services.GetService(typeof(IGraphicsDeviceManager)) as IGraphicsDeviceManager;
                if (graphicsDeviceManager != null)
                    graphicsDeviceManager.CreateDevice();
                else
                    throw new Exception("Unable to retrieve GraphicsDeviceManager");

                // Width must a multiple of 2
                em_renderTarget2D = new RenderTarget2D(GraphicsDevice, em_sizeViewport.X, em_sizeViewport.Y, true, SurfaceFormat.Bgr565, DepthFormat.Depth16);
            }

            Game.CGameManagement.currentState = "CInGame";
            Game.CGameManagement.Initialize();

            SamplerState sState = new SamplerState();
            sState.Filter = TextureFilter.Linear;
            graphics.GraphicsDevice.SamplerStates[0] = sState;

            Display2D.C2DEffect.softwareViewport = GraphicsDevice.Viewport;

            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            renderCapture = new Display2D.CRenderCapture(GraphicsDevice);
            postProcessor = new Display2D.CPostProcessor(GraphicsDevice);

            Display2D.C2DEffect.LoadContent(Content, GraphicsDevice, spriteBatch, postProcessor, renderCapture);
            Display2D.C2DEffect.isSoftwareEmbedded = isSoftwareEmbedded;
            Display2D.C2DEffect.renderTarget = (isSoftwareEmbedded) ? em_renderTarget2D : renderCapture.renderTarget;

            Display3D.Particles.ParticlesManager.LoadContent(GraphicsDevice);
            Game.Settings.CGameSettings.LoadDatas(GraphicsDevice);
            Game.CConsole.LoadContent(Content, GraphicsDevice, spriteBatch, true, true/*false*/);
           // Game.CConsole._activationKeys = Game.Settings.CGameSettings._gameSettings.KeyMapping.Console;
            Game.CConsole._activationKeys = Keys.P;
            try
            {
                if(!isSoftwareEmbedded)
                    Game.CGameManagement.ChangeState("CInGame");
                Game.CGameManagement.LoadContent(Content, GraphicsDevice, spriteBatch, graphics);
            }
            catch (Exception e)
            {
                Game.CGameManagement.ChangeState("CError");
                Game.CGameManagement.SendParam("Error encountered\n\nCheck logs for more information");
                Game.CConsole.WriteLogs(e.ToString());
            }
            //Game.Script.CLuaVM.Initialize();
            //Game.Script.CLuaVM.LoadScript("Scripts/GuiManager.lua");
        }


        protected override void UnloadContent()
        {
            Game.CGameManagement.UnloadContent(Content);
        }

        protected override void Update(GameTime gameTime)
        {
            if (isSoftwareEmbedded || base.IsActive)
            {
                if (isSoftwareEmbedded)
                {
                    TimeSpan currentTime = em_StopWatch.Elapsed;
                    em_GameTime = new GameTime(currentTime, currentTime - em_LastTime);
                    em_LastTime = currentTime;
                }
                KeyboardState kbState = Keyboard.GetState();
                MouseState mouseState = Mouse.GetState();

                if (Game.Script.CLuaVM._settingEnableHighFreqCalls)
                    Game.Script.CLuaVM.CallEvent("framePulse");

                // Quit program when 'Escape' key is pressed
                if (kbState.IsKeyDown(Keys.Escape))
                    this.Exit();

                Game.CGameManagement.Update(gameTime, kbState, mouseState);

                Display2D.C2DEffect.Update(gameTime, kbState, mouseState);
                Game.CConsole.Update(kbState, gameTime);

                oldKeyboardState = kbState;
                oldMouseState = mouseState;

                base.Update(gameTime);
            }
        }
        
        protected override void Draw(GameTime gameTime)
        {
            // WPF
            if (isSoftwareEmbedded)
                GraphicsDevice.SetRenderTarget(em_renderTarget2D);

            // Capture the render
            renderCapture.Begin();
            GraphicsDevice.Clear(Microsoft.Xna.Framework.Color.Black);

            // Draw "All" the State
            Game.CGameManagement.Draw(spriteBatch, gameTime);

            // Draw the Console effect
            spriteBatch.Begin();
            Display2D.C2DEffect.Draw(gameTime);
            Game.CConsole.Draw(gameTime);
            spriteBatch.End();

            // End capturing
            renderCapture.End();

            // Draw the render via post processing
            postProcessor.Input = renderCapture.GetTexture();
            postProcessor.Draw(gameTime);

            base.Draw(gameTime);

            // WPF
            if (isSoftwareEmbedded)
            {
                GraphicsDevice.SetRenderTarget(null);

                em_renderTarget2D.GetData(em_bytes);
                em_WriteableBitmap.Lock();
                System.Runtime.InteropServices.Marshal.Copy(em_bytes, 0, em_WriteableBitmap.BackBuffer, em_bytes.Length);
                em_WriteableBitmap.AddDirtyRect(new System.Windows.Int32Rect(0, 0, em_sizeViewport.X, em_sizeViewport.Y));
                em_WriteableBitmap.Unlock();
            }
        }

        public int posx = 0;
        public int posy = 0;
        public object WPFHandler(string handle, object value)
        {
            return Game.CGameManagement.SendParam(new object[] { handle, value });
            /*switch (handle)
            {
                case "changeCamFreeze":
                case "moveCameraForward":
                case "click":
                case "selectObject":
                case "unselectObject":
                case "moveObject":
                case "changeTool":
                case "getElementInfo":
                case "setElementInfo":
                
                    
            }
            return true;*/
        }

        public void GameLoop(object sender, EventArgs e)
        {
            if (!shouldNotUpdate || shouldUpdateOnce)
            {
                this.Update(em_GameTime);
                this.Draw(em_GameTime);

                if (shouldUpdateOnce)
                    shouldUpdateOnce = false;
            }
        }

    }
}
