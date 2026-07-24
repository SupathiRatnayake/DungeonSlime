using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Input;



namespace DungeonSlime;

public class Game1 : Core
{
    // Defines the slime animated sprite.
    private AnimatedSprite _slime;

    // Defines the bat animated sprite.
    private AnimatedSprite _bat;

    // Tracks the position of the slime.
    private Vector2 _slimePosition;

    // Speed multiplier of the slime.
    private const float MOVEMENT_SPEED = 5.0f;

    // Tracks the position of the bat.
    private Vector2 _batPosition;

    // tracks the velocity of the bat.
    private Vector2 _batVelocity;

    // Defines the tilemap to draw.
    private Tilemap _tilemap;

    // defines the bounds of the room that the slime and bat are contained within.
    private Rectangle _roomBounds;

    // The sound effects to play when the bat bouces off the edge of the screen.
    private SoundEffect _bounceSoundEffect;

    // The sound effect to play when slime eats a bat.
    private SoundEffect _collectSoundEffect;

    public Game1() : base("Dungeon Slime", 1280, 720, false)
    {

    }

    protected override void Initialize()
    {
        base.Initialize();

        Rectangle screenBounds = GraphicsDevice.PresentationParameters.Bounds;

        _roomBounds = new Rectangle(
            (int)_tilemap.TileWidth,
            (int)_tilemap.TileHeight,
            screenBounds.Width - (int)_tilemap.TileWidth * 2,
            screenBounds.Height - (int)_tilemap.TileHeight * 2
        );

        // Initial slime position will be the center of the tilemap.
        int centerRow = _tilemap.Rows / 2;
        int centerColumn = _tilemap.Columns / 2;
        _slimePosition = new Vector2(centerColumn * _tilemap.TileWidth, centerRow * _tilemap.TileHeight);

        // Initial bat position will be in the top left corner of the room
        _batPosition = new Vector2(_roomBounds.Left, _roomBounds.Top);

        // Assign the initial random velocity to the bat.
        AssignRandomBatVelocity();

    }

    private void AssignRandomBatVelocity()
    {
        // Generate a random angle.
        float angle = (float)(Random.Shared.NextDouble() * Math.PI * 2);

        // convert angle to a direction vector.
        float x = (float)Math.Cos(angle);
        float y = (float)Math.Sin(angle);
        Vector2 direction = new Vector2(x, y);

        // Multiply the direction vector by the movement speed.
        _batVelocity = direction * MOVEMENT_SPEED;
    }

    protected override void LoadContent()
    {
        // Create the texture atlas from the XML configuration file
        TextureAtlas atlas = TextureAtlas.FromFile(Content, "images/atlas-definition.xml");

        // Create the slime animated sprite from the atlas.
        _slime = atlas.CreateAnimatedSprite("slime-animation");
        _slime.Scale = new Vector2(4.0f, 4.0f);

        // Create the bat animated sprite from the atlas.
        _bat = atlas.CreateAnimatedSprite("bat-animation");
        _bat.Scale = new Vector2(4.0f, 4.0f);

        // create the tilemap from the XML configuration file.
        _tilemap = Tilemap.FromFile(Content, "images/tilemap-definition.xml");
        _tilemap.Scale = new Vector2(4.0f, 4.0f);

        // Load the bounce sound effect
        _bounceSoundEffect = Content.Load<SoundEffect>("audio/bounce");

        // Load the collect sound effect
        _collectSoundEffect = Content.Load<SoundEffect>("audio/collect");

        // Load the background theme music
        Song theme = Content.Load<Song>("audio/theme");

        // Ensure media player is not already pllaying on device, if so, stop it
        if (MediaPlayer.State == MediaState.Playing)
        {
            MediaPlayer.Stop();
        }

        MediaPlayer.Play(theme);

        // Set the theme music to repeat.
        MediaPlayer.IsRepeating = true;
    }

    protected override void Update(GameTime gameTime)
    {
        // Update the slime animated sprite.
        _slime.Update(gameTime);

        // Update the bat animated sprite.
        _bat.Update(gameTime);

        base.Update(gameTime);

        // check for keyboard input and handle it.
        CheckKeyboardInput();

        // check for gamepad input and handle it.
        CheckGamePadInput();

        // Create a bounding rectangle for the slime.
        Circle slimeBounds = new Circle(
            (int)(_slimePosition.X + _slime.Width * 0.5f),
            (int)(_slimePosition.Y + _slime.Height * 0.5f),
            (int)(_slime.Width * 0.5f)
        );

        // Use distance based check to ditermine if theslime is within the
        // bounds of the game screen, and if it is outside that screen edge,
        // move it back inside.
        if (slimeBounds.Left < _roomBounds.Left)
        {
            _slimePosition.X = _roomBounds.Left;
        }
        else if (slimeBounds.Right > _roomBounds.Right)
        {
            _slimePosition.X = _roomBounds.Right - _slime.Width;
        }

        if (slimeBounds.Top < _roomBounds.Top)
        {
            _slimePosition.Y = _roomBounds.Top;
        }
        else if (slimeBounds.Bottom > _roomBounds.Bottom)
        {
            _slimePosition.Y = _roomBounds.Bottom - _slime.Height;
        }

        // calculate the new position of the bat based on the velocity.
        Vector2 newBatPosition = _batPosition + _batVelocity;

        // Create a bounding rectangle for the bat.
        Circle batBounds = new Circle(
            (int)(_batPosition.X + _bat.Width * 0.5f),
            (int)(_batPosition.Y + _bat.Height * 0.5f),
            (int)(_bat.Width * 0.5f)
        );

        Vector2 normal = Vector2.Zero;

        // Use distance based checks to ditermine if the bat is within the
        // bounds of the game screen, and if it is outside that screen edge,
        // reflect it about the screen edge normal.
        if (batBounds.Left < _roomBounds.Left)
        {
            normal.X = Vector2.UnitX.X;
            newBatPosition.X = _roomBounds.Left;
        }
        else if (batBounds.Right > _roomBounds.Right)
        {
            normal.X = -Vector2.UnitX.X;
            newBatPosition.X = _roomBounds.Right - _bat.Width;
        }

        if (batBounds.Top < _roomBounds.Top)
        {
            normal.Y = Vector2.UnitY.Y;
            newBatPosition.Y = _roomBounds.Top;
        }
        else if (batBounds.Bottom > _roomBounds.Bottom)
        {
            normal.Y = -Vector2.UnitY.Y;
            newBatPosition.Y = _roomBounds.Bottom - _bat.Height;
        }

        // If nthe normal is anything but Velocity.Zero, this means the bat had
        // moved outside the screen edge so we should reflect it about the normal.
        if (normal != Vector2.Zero)
        {
            normal.Normalize();
            _batVelocity = Vector2.Reflect(_batVelocity, normal);

            // Play the bounce sound effect
            _bounceSoundEffect.Play();
        }

        _batPosition = newBatPosition;

        if (slimeBounds.Intersects(batBounds))
        {
            // Choose a random row and column based on the total number of each
            int column = Random.Shared.Next(1, _tilemap.Columns -1);
            int row = Random.Shared.Next(1, _tilemap.Rows -1);

            // Change the bat position by setting the x and y values equal to 
            // the column and row multiplied by the wiedth and height.
            _batPosition = new Vector2(column * _bat.Width, row * _bat.Height);

            // Assign a new random velocity to the bat.
            AssignRandomBatVelocity();

            // Play the collect sound effect
            _collectSoundEffect.Play();
        }
    }

    private void CheckKeyboardInput()
    {
        // If the space key is held down. the movement speed is increased by 1.5
        float speed = MOVEMENT_SPEED;
        if (Input.Keyboard.IsKeyDown(Keys.Space))
        {
            speed *= 1.5f;
        }

        // If the W or Up keys are dows, move the slime up of the screen.
        if (Input.Keyboard.IsKeyDown(Keys.W) || Input.Keyboard.IsKeyDown(Keys.Up))
        {
            _slimePosition.Y -= speed;
        }

        // If the S or Down keys are down, move the slime down of the screen.
        if (Input.Keyboard.IsKeyDown(Keys.S) || Input.Keyboard.IsKeyDown(Keys.Down))
        {
            _slimePosition.Y += speed;
        }

        // If the A or Left keys are down, move the slime left of the screen.
        if (Input.Keyboard.IsKeyDown(Keys.A) || Input.Keyboard.IsKeyDown(Keys.Left))
        {
            _slimePosition.X -= speed;
        }

        // If the D or Right keys are down, move the slime right of the screen.
        if (Input.Keyboard.IsKeyDown(Keys.D) || Input.Keyboard.IsKeyDown(Keys.Right))
        {
            _slimePosition.X += speed;
        }
    }

    private void CheckGamePadInput()
    {
        GamePadInfo gamePadOne = Input.GamePads[(int)PlayerIndex.One];

        // If the A button is held down, the movement speed is increased by 1.5
        // and the gamepas vibrates as feedback to the player.
        float speed = MOVEMENT_SPEED;
        if (gamePadOne.IsButtonDown(Buttons.A))
        {
            speed *= 1.5f;
            gamePadOne.SetVibration(1.0f, TimeSpan.FromSeconds(1));
        }
        else
        {
            gamePadOne.StopVibration();
        }

        // Check thumbstick first since it has a higher priority over which gamepad input is movement.
        // It has priority since the thumbstick values provide a more granular analog value that can be transformed for movement.
        if (gamePadOne.LeftThumbStick != Vector2.Zero)
        {
            _slimePosition.X += gamePadOne.LeftThumbStick.X * speed;
            _slimePosition.Y -= gamePadOne.LeftThumbStick.Y * speed; // Invert Y axis for movement
        }
        else
        {
            // If DPadUp is down, move the slime up on the screen.
            if (gamePadOne.IsButtonDown(Buttons.DPadUp))
            {
                _slimePosition.Y -= speed;
            }

            // If DPadDown is down, move the slime down on the screen.
            if (gamePadOne.IsButtonDown(Buttons.DPadDown))
            {
                _slimePosition.Y += speed;
            }

            // If DPadLeft is down, move the slime left on the screen.
            if (gamePadOne.IsButtonDown(Buttons.DPadLeft))
            {
                _slimePosition.X -= speed;
            }

            // If DPadRight is down, move the slime right on the screen.
            if (gamePadOne.IsButtonDown(Buttons.DPadRight))
            {
                _slimePosition.X += speed;
            } 
        }
    }



    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        // Begin the sprite batch to prepare for rendering.
        SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

        // Draw the tilemap.
        _tilemap.Draw(SpriteBatch);

        // Draw the slime sprite.
        _slime.Draw(SpriteBatch, _slimePosition);

        // Draw the bat sprite.
        _bat.Draw(SpriteBatch, _batPosition);

        // Always end the sprite batch when finished
        SpriteBatch.End();


        base.Draw(gameTime);
    }
}
