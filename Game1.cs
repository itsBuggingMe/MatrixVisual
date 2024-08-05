using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualBasic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MatrixVisual;

public class GameRoot : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private const int M = 16;
    private const int K = 16;
    private const int N = 16;
    private Texture2D _onePixel;
    private MatrixTile[,] _a;
    private MatrixTile[,] _b;
    private MatrixTile[,] _c;
    public GameRoot()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        GenerateMatrix(ref _a, M, K, new Vector2(450, 350), Color.Red);
        GenerateMatrix(ref _b, K, N, new Vector2(850, 350), Color.Green);
        GenerateMatrix(ref _c, M, N, new Vector2(1250, 350), Color.Blue);

        //GenerateMatrix(ref _b, K, N, new Vector2(450, 350), Color.Green);
        //GenerateMatrix(ref _c, M, N, new Vector2(450, 400), Color.Blue);
        //GenerateMatrix(ref _a, M, K, new Vector2(450, 300), Color.Red);

        _graphics.PreferredBackBufferHeight = 1080;
        _graphics.PreferredBackBufferWidth = 1920;
        Setup();

        this.Window.Title = "Visualisation";
    }

    protected override void Initialize()
    {
        _onePixel = new Texture2D(_graphics.GraphicsDevice, 1, 1);
        _onePixel.SetData(new Color[] { Color.White });
        base.Initialize();
        _spriteBatch = new SpriteBatch(GraphicsDevice);
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        MouseState ms = Mouse.GetState();

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
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        _spriteBatch.Begin();

        foreach (var t in _a)
            DrawTile(t);
        foreach (var t in _b)
            DrawTile(t);
        foreach (var t in _c)
            DrawTile(t);

        _spriteBatch.End();
        base.Draw(gameTime);
    }

    private void DrawRect(Rectangle bounds, Color color)
    {
        _spriteBatch.Draw(_onePixel, bounds, color);
    }

    const int width = 16;
    private void DrawTile(MatrixTile tile)
    {
        if (tile.Selected)
            DrawRect(tile.Bounds, Color.White);
        else
            DrawRect(tile.Bounds, tile.Color);
    }

    private void GenerateMatrix(ref MatrixTile[,] tile, int x, int y, Vector2 poss, Color c)
    {
        tile = new MatrixTile[y, x];
        for (int i = 0; i < y; i++)
        {
            for (int j = 0; j < x; j++)
            {
                Rectangle r = new Rectangle(
                    (new Vector2(i, j) * (width) + poss).ToPoint(), new Point(width));

                tile[i, j] = new MatrixTile(0, r
                    , c);
            }
        }
    }

    public void Setup()
    {
        for (int i = 0; i < M; i++)
        {
            for (int j = 0; j < N; j++)
            {
                for (int k = 0; k < K; k++)
                {
                    MatrixTile left = _a[k, i];//MxK, i, k
                    MatrixTile right = _b[j, k];//KxN k, j
                    MatrixTile center = _c[j, i];

                    center.BorderedTiles.Add(left);
                    center.BorderedTiles.Add(right);

                    right.BorderedTiles.Add(left);
                    right.BorderedTiles.Add(center);

                    left.BorderedTiles.Add(center);
                    left.BorderedTiles.Add(right);
                }
            }
        }
    }
}

internal class MatrixTile(float value, Rectangle bounds, Color color, bool selected = false)
{
    public float Value = value;
    public Rectangle Bounds = bounds;
    public Color Color = color;
    public bool Selected = selected;
    public List<MatrixTile> BorderedTiles = [];
}
