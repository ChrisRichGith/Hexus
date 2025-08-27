using Hexus.Managers;
using Hexus.Models;
using Hexus.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Linq;

namespace Hexus;

public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private SpriteFont _font;

    private GridManager _gridManager;
    private GameState _gameState;
    private Vector2 _gridScreenOffset;

    private MouseState _currentMouseState;
    private MouseState _previousMouseState;
    private float _aiTurnTimer;

    private const int GridWidth = 10;
    private const int GridHeight = 10;
    private const float HexSize = 30f;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        _graphics.PreferredBackBufferWidth = 800;
        _graphics.PreferredBackBufferHeight = 600;
        _graphics.ApplyChanges();

        _gridManager = new GridManager(GridWidth, GridHeight, HexSize);
        _gridManager.CreateGrid();
        _gridManager.AddInitialTiles();
        _gameState = new GameState();

        var gridPixelWidth = GridWidth * HexSize * MathF.Sqrt(3);
        var gridPixelHeight = GridHeight * HexSize * 1.5f;
        _gridScreenOffset = new Vector2((_graphics.PreferredBackBufferWidth - gridPixelWidth) / 3, (_graphics.PreferredBackBufferHeight - gridPixelHeight) / 2);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _font = Content.Load<SpriteFont>("font");
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        if (_gameState.GameOver)
        {
            base.Update(gameTime);
            return;
        }

        _previousMouseState = _currentMouseState;
        _currentMouseState = Mouse.GetState();

        if (_gameState.CurrentTurn == Player.Human)
        {
            HandleMouseInput();
        }
        else if (_gameState.CurrentTurn == Player.AI)
        {
            _aiTurnTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_aiTurnTimer <= 0)
            {
                ExecuteAiTurn();
            }
        }

        base.Update(gameTime);
    }

    private void HandleMouseInput()
    {
        if (_currentMouseState.LeftButton != ButtonState.Released || _previousMouseState.LeftButton != ButtonState.Pressed) return;

        var clickedHex = PixelToHex(new Vector2(_currentMouseState.X, _currentMouseState.Y));
        if (!_gridManager.AllHexes.Contains(clickedHex)) return;

        if (_gameState.SelectedTile != null)
        {
            // A tile is selected, check for action
            var neighbors = _gameState.SelectedTile.Position.GetNeighbors().ToList();
            if (neighbors.Contains(clickedHex))
            {
                bool actionTaken = false;
                if (_gridManager.Tiles.TryGetValue(clickedHex, out var targetTile))
                {
                    // Attack
                    if (targetTile.Class.Owner == Player.AI)
                    {
                        targetTile.TakeDamage(_gameState.SelectedTile.Class.BaseDamage);
                        if (!targetTile.IsAlive) _gridManager.Tiles.Remove(clickedHex);
                        actionTaken = true;
                    }
                }
                else
                {
                    // Move
                    _gridManager.Tiles.Remove(_gameState.SelectedTile.Position);
                    _gameState.SelectedTile.Position = clickedHex;
                    _gridManager.Tiles.Add(clickedHex, _gameState.SelectedTile);
                    actionTaken = true;
                }

                if (actionTaken)
                {
                    _gameState.SelectedTile = null;
                    EndTurn();
                }
            }
            else
            {
                _gameState.SelectedTile = null; // Deselect if clicking somewhere else
            }
        }
        else
        {
            // No tile is selected, try to select one
            if (_gridManager.Tiles.TryGetValue(clickedHex, out var tile) && tile.Class.Owner == Player.Human)
            {
                _gameState.SelectedTile = tile;
            }
        }
    }

    private void EndTurn()
    {
        CheckWinConditions();
        if (_gameState.GameOver) return;

        _gameState.CurrentTurn = _gameState.CurrentTurn == Player.Human ? Player.AI : Player.Human;
        if (_gameState.CurrentTurn == Player.AI)
        {
            _aiTurnTimer = 0.5f; // AI will "think" for 0.5 seconds
        }
    }

    private void ExecuteAiTurn()
    {
        var aiTiles = _gridManager.Tiles.Values.Where(t => t.Class.Owner == Player.AI).ToList();
        if (!aiTiles.Any())
        {
            EndTurn();
            return;
        }

        var aiTile = aiTiles[Random.Shared.Next(aiTiles.Count)];
        var neighbors = aiTile.Position.GetNeighbors().ToList();
        var attackableTargets = neighbors.Where(n => _gridManager.Tiles.ContainsKey(n) && _gridManager.Tiles[n].Class.Owner == Player.Human).ToList();

        if (attackableTargets.Any())
        {
            var targetHex = attackableTargets.First();
            var targetTile = _gridManager.Tiles[targetHex];
            targetTile.TakeDamage(aiTile.Class.BaseDamage);
            if (!targetTile.IsAlive) _gridManager.Tiles.Remove(targetHex);
        }
        else
        {
            var playerTiles = _gridManager.Tiles.Values.Where(t => t.Class.Owner == Player.Human).Select(t=>t.Position).ToList();
            var emptyControlPoints = _gridManager.ControlPoints.Where(cp => !_gridManager.Tiles.ContainsKey(cp)).ToList();
            var targets = playerTiles.Concat(emptyControlPoints).ToList();

            if (targets.Any())
            {
                var closestTarget = targets.OrderBy(t => Hex.Distance(aiTile.Position, t)).First();
                var pathToTarget = neighbors.Where(n => !_gridManager.Tiles.ContainsKey(n))
                                            .OrderBy(n => Hex.Distance(n, closestTarget))
                                            .FirstOrDefault();
                if(pathToTarget != default)
                {
                    _gridManager.Tiles.Remove(aiTile.Position);
                    aiTile.Position = pathToTarget;
                    _gridManager.Tiles.Add(pathToTarget, aiTile);
                }
            }
        }

        EndTurn();
    }

    private void CheckWinConditions()
    {
        var playerTiles = _gridManager.Tiles.Values.Count(t => t.Class.Owner == Player.Human);
        var aiTiles = _gridManager.Tiles.Values.Count(t => t.Class.Owner == Player.AI);

        if (playerTiles == 0) { _gameState.GameOver = true; _gameState.Winner = Player.AI; return; }
        if (aiTiles == 0) { _gameState.GameOver = true; _gameState.Winner = Player.Human; return; }

        var playerControlledPoints = _gridManager.ControlPoints.Count(cp => _gridManager.Tiles.ContainsKey(cp) && _gridManager.Tiles[cp].Class.Owner == Player.Human);
        var aiControlledPoints = _gridManager.ControlPoints.Count(cp => _gridManager.Tiles.ContainsKey(cp) && _gridManager.Tiles[cp].Class.Owner == Player.AI);

        if (playerControlledPoints == _gridManager.ControlPoints.Count) { _gameState.GameOver = true; _gameState.Winner = Player.Human; }
        if (aiControlledPoints == _gridManager.ControlPoints.Count) { _gameState.GameOver = true; _gameState.Winner = Player.AI; }
    }

    #region Coordinate Conversion and Drawing
    private Vector2 HexToPixel(Hex hex) { /* ... */ return new Vector2(HexSize*(MathF.Sqrt(3)*hex.Q+MathF.Sqrt(3)/2f*hex.R), HexSize*(3f/2f*hex.R)); }
    private Vector2 GetIsoPixel(Hex hex) { var p=HexToPixel(hex); return new Vector2((p.X-p.Y)*0.866f, (p.X+p.Y)*0.5f) + _gridScreenOffset; }
    private Hex PixelToHex(Vector2 p) { var ip=p-_gridScreenOffset; var cX=(ip.X/0.866f+ip.Y/0.5f)/2; var cY=(ip.Y/0.5f-ip.X/0.866f)/2; var qf=(MathF.Sqrt(3)/3f*cX-1f/3f*cY)/HexSize; var rf=(2f/3f*cY)/HexSize; return HexRound(qf,rf); }
    private Hex HexRound(float q_f,float r_f){var s_f=-q_f-r_f;var q=MathF.Round(q_f);var r=MathF.Round(r_f);var s=MathF.Round(s_f);var qd=MathF.Abs(q-q_f);var rd=MathF.Abs(r-r_f);var sd=MathF.Abs(s-s_f);if(qd>rd&&qd>sd)q=-r-s;else if(rd>sd)r=-q-s;return new Hex((int)q,(int)r);}
    #endregion

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(40, 40, 40));
        _spriteBatch.Begin();

        foreach (var hex in _gridManager.AllHexes)
        {
            var center = GetIsoPixel(hex);
            var isControlPoint = _gridManager.ControlPoints.Contains(hex);

            if (isControlPoint) PrimitiveDrawer.DrawHexOutline(_spriteBatch, center, HexSize, Color.Gold, 3);

            if (_gridManager.Tiles.TryGetValue(hex, out var tile))
            {
                PrimitiveDrawer.DrawHexOutline(_spriteBatch, center, HexSize - 1, tile.Class.Color, (int)HexSize);
                var hp = (float)tile.Health / tile.MaxHealth;
                var hpRect = new Rectangle((int)(center.X-HexSize*0.75f), (int)(center.Y+HexSize*0.7f), (int)(HexSize*1.5f*hp), 5);
                PrimitiveDrawer.DrawFilledRectangle(_spriteBatch, hpRect, Color.Lerp(Color.Red, Color.Green, hp));
            }

            PrimitiveDrawer.DrawHexOutline(_spriteBatch, center, HexSize, Color.White, 1);
        }

        if (_gameState.SelectedTile != null)
        {
            var selectedHex = _gameState.SelectedTile.Position;
            var center = GetIsoPixel(selectedHex);
            PrimitiveDrawer.DrawHexOutline(_spriteBatch, center, HexSize, Color.Yellow, 3);

            foreach (var neighbor in selectedHex.GetNeighbors())
            {
                if (!_gridManager.AllHexes.Contains(neighbor)) continue;
                var neighborCenter = GetIsoPixel(neighbor);
                if (_gridManager.Tiles.TryGetValue(neighbor, out var tile) && tile.Class.Owner == Player.AI)
                {
                    PrimitiveDrawer.DrawHexOutline(_spriteBatch, neighborCenter, HexSize, Color.Red, 2); // Attackable
                }
                else if (!_gridManager.Tiles.ContainsKey(neighbor))
                {
                    PrimitiveDrawer.DrawHexOutline(_spriteBatch, neighborCenter, HexSize, Color.LawnGreen, 1); // Movable
                }
            }
        }

        if (_gameState.GameOver)
        {
            var darkerOverlay = new Rectangle(0, 0, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
            PrimitiveDrawer.DrawFilledRectangle(_spriteBatch, darkerOverlay, new Color(0, 0, 0, 150));
            var msg = $"{_gameState.Winner} Wins!";
            var msgSize = _font.MeasureString(msg);
            var msgPos = new Vector2(_graphics.PreferredBackBufferWidth / 2f - msgSize.X / 2, _graphics.PreferredBackBufferHeight / 2f - msgSize.Y / 2);
            _spriteBatch.DrawString(_font, msg, msgPos, Color.White);
        }

        _spriteBatch.End();
        base.Draw(gameTime);
    }
}
