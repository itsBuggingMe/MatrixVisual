using System.Collections.Generic;
using System;
using System.Linq;
using Microsoft.Toolkit.HighPerformance;
using Microsoft.VisualBasic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MatrixVisual;

public class GameRoot : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private const int M = 128;
    private const int K = 128;
    private const int N = 128;
    private Texture2D _onePixel;

    public GameRoot()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        //IsFixedTimeStep = false;
        //TargetElapsedTime = TimeSpan.FromMilliseconds(0.001f);
        //_graphics.SynchronizeWithVerticalRetrace = false;

        _graphics.HardwareModeSwitch = false;
        _graphics.ToggleFullScreen();

        Window.Title = "Visualisation";
        Visualiser.Instance = new Visualiser(M, K, N, new Vector2(100, 80),
            new Vector2(512 + 200, 80), new Vector2(1024 + 300, 80), 
            new Vector2(850, 700), width);
        Visualiser.Instance.Init();



        _multiplicationHistory = Visualiser.Instance.GetItems().Reverse().ToArray();
        foreach (var tile in _multiplicationHistory[_multiplicationHistoryIndex % _multiplicationHistory.Length])
            tile.Tile.LerpValue = 1;
    }

    protected override void Initialize()
    {
        _onePixel = new Texture2D(_graphics.GraphicsDevice, 1, 1);
        _onePixel.SetData(new Color[] { Color.White });
        base.Initialize();
        _spriteBatch = new SpriteBatch(GraphicsDevice);
    }

    private MouseState _pms;
    private MatmulItem[][] _multiplicationHistory;
    private int _multiplicationHistoryIndex = 0;

    protected override void Update(GameTime gameTime)
    {
        if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        MouseState ms = Mouse.GetState();
        int delta = ms.ScrollWheelValue - _pms.ScrollWheelValue;
        if (delta != 0)
            Move(delta > 0);
        if (ms.LeftButton == ButtonState.Pressed)
            Move(true);
        if (ms.RightButton == ButtonState.Pressed)
            Move(false);
        if (Keyboard.GetState().IsKeyDown(Keys.Space))
            foreach (var tileList in _multiplicationHistory)
                foreach (var tile in tileList)
                {
                    tile.Tile.LerpValue = 0;
                    tile.Tile.AltColor = Color.White;
                }

        void Move(bool dir)
        {
            for(int i = 0; i < 1; i++)
            {
                Visualiser.Instance.Update();

                _multiplicationHistoryIndex += dir ? 1 : -1;

                if (_multiplicationHistoryIndex < 0)
                    _multiplicationHistoryIndex = 0;
                if (_multiplicationHistoryIndex >= _multiplicationHistory.Length)
                    _multiplicationHistoryIndex = _multiplicationHistory.Length - 1;

                foreach (var tile in _multiplicationHistory[_multiplicationHistoryIndex])
                {
                    tile.Tile.LerpValue = 1;
                    tile.Tile.AltColor = tile.Color;
                }
            }
        }

        _pms = ms;

        /*
        foreach (var t in _a)
            t.Selected = false;
        foreach (var t in _b)
            t.Selected = false;
        foreach (var t in _c)
            t.Selected = false;
        
        foreach (var t in _a)
            UpdateTile(t);
        foreach (var t in _b)
            UpdateTile(t);
        foreach (var t in _c)
            UpdateTile(t);
        
        void UpdateTile(MatrixTile mt)
        {
            bool selected = mt.Bounds.Contains(ms.Position);
        
            if (selected)
            {
                mt.Selected = selected;
                foreach (var bt in mt.BorderedTiles)
                {
                    bt.Selected = true;
                }
            }
        }*/

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        _spriteBatch.Begin();
        Visualiser.Instance.Draw(_spriteBatch, _onePixel);
        _spriteBatch.End();
        base.Draw(gameTime);
    }

    const int width = 4;
}

internal class MatrixTile(float value, Rectangle bounds, Color color)
{
    public float Value = value;
    public Rectangle Bounds = bounds;
    public Color Color = color;
    public Color AltColor = Color.White;
    public float LerpValue;
    public Color DrawColor => Color.Lerp(Color, AltColor, LerpValue);
    public void Update()
    {
        //LerpValue *= 0.995f;
        LerpValue *= 0.8f;
    }
}
