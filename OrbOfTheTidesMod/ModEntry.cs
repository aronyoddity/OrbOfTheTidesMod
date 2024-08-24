using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System.Collections.Generic;
using xTile.Dimensions;

namespace OrbOfTheTidesMod
{
    public class ModEntry : Mod
    {
        private Vector2? teleportLocation;
        private string teleportMap;
        private Texture2D portalTexture;
        private Texture2D orbBigTexture;
        private Texture2D portalEffectTexture;
        private int portalEffectFrame;
        private float portalEffectTimer;
        private int orbBigFrame;
        private float orbBigFrameTimer;
        private const float PortalEffectInterval = 100f; // 100ms per frame
        private const float OrbBigFrameInterval = 200f; // 200ms per frame
        private bool isWarping = false; // Flag to track if warping is active
        private double warpStartTime = 0; // Time when warp starts
        private const double WarpDisplayDuration = 2000; // 2 seconds in milliseconds
        private bool isModDisabled = false; // Flag to disable the mod temporarily

        public override void Entry(IModHelper helper)
        {
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.Display.RenderedStep += Display_RenderedStep;
            helper.Events.Player.Warped += OnPlayerWarped; // Subscribe to Warped event

            Bootstrap();
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            portalTexture = Helper.ModContent.Load<Texture2D>("assets/portal.png");
            orbBigTexture = Helper.ModContent.Load<Texture2D>("assets/OrbBig.png");
            portalEffectTexture = Helper.ModContent.Load<Texture2D>("assets/portaleffect.png");

            this.Monitor.Log("Textures loaded", LogLevel.Debug);
        }

        private void Bootstrap()
        {
            teleportLocation = null;
            teleportMap = null;
            portalEffectFrame = 0;
            portalEffectTimer = 0f;
            orbBigFrame = 0;
            orbBigFrameTimer = 0f;
            isWarping = false; // Reset the warping flag at the start of the day
            warpStartTime = 0;
            isModDisabled = false; // Reset the mod disable flag
        }

        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            teleportLocation = null;
            teleportMap = null;
            portalEffectFrame = 0;
            portalEffectTimer = 0f;
            orbBigFrame = 0;
            orbBigFrameTimer = 0f;
            isWarping = false; // Reset the warping flag at the start of the day
            warpStartTime = 0;
            isModDisabled = false; // Reset the mod disable flag
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsPlayerFree || Game1.activeClickableMenu != null || Game1.dialogueUp)
                return;

            Farmer player = Game1.player;

            if (player.CurrentItem == null || player.CurrentItem.QualifiedItemId != "(O)OrbOfTheTides")
                return;

            // Check if the player is interacting with an action tile or building
            if (isModDisabled)
                return;

            if (e.Button == SButton.MouseRight || e.Button == SButton.ControllerA)
            {
                if (teleportLocation.HasValue)
                {
                    List<Response> responses = new List<Response>
                    {
                        new Response("yes", "Yes"),
                        new Response("no", "No")
                    };
                    Game1.currentLocation.createQuestionDialogue(
                        "Do you want to reset the teleport location?",
                        responses.ToArray(),
                        OnResetPortalQuestionAnswered
                    );
                }
                else
                {
                    SetTeleportLocation(player, e.Cursor.Tile);
                }
            }

            if (e.Button == SButton.MouseLeft || e.Button == SButton.ControllerX)
            {
                if (teleportLocation.HasValue && teleportMap != null)
                {
                    List<Response> responses = new List<Response>
                    {
                        new Response("yes", "Yes"),
                        new Response("no", "No")
                    };
                    Game1.currentLocation.createQuestionDialogue(
                        "Do you want to use the Orb of the Tides?",
                        responses.ToArray(),
                        OnQuestionAnswered
                    );
                }
            }
        }

        private void OnResetPortalQuestionAnswered(Farmer who, string answer)
        {
            if (answer == "yes")
            {
                teleportLocation = null;
                teleportMap = null;
                Helper.Data.WriteSaveData("teleportLocation", (TeleportLocationData)null); // Reset saved location
                Game1.addHUDMessage(new HUDMessage("Teleport point reset", HUDMessage.newQuest_type));
                this.Monitor.Log("Teleport point reset", LogLevel.Info);
            }
        }

        private void SetTeleportLocation(Farmer player, Vector2 location)
        {
            GameLocation gameLocation = player.currentLocation;
            if (IsValidTeleportLocation(gameLocation, location))
            {
                teleportLocation = location;
                teleportMap = gameLocation.Name;
                var data = new TeleportLocationData { X = location.X, Y = location.Y, Map = teleportMap };
                Helper.Data.WriteSaveData("teleportLocation", data); // Save the location
                Game1.addHUDMessage(new HUDMessage("Teleport location set", HUDMessage.newQuest_type));
                this.Monitor.Log($"Teleport location set to: {teleportMap} at {teleportLocation}", LogLevel.Info);
            }
            else
            {
                Game1.addHUDMessage(new HUDMessage("Invalid teleport location", HUDMessage.error_type));
                this.Monitor.Log("Invalid teleport location", LogLevel.Warn);
            }
        }

        private void OnQuestionAnswered(Farmer who, string answer)
        {
            if (answer == "yes")
            {
                isWarping = true; // Set the warping flag to true
                warpStartTime = Game1.currentGameTime.TotalGameTime.TotalMilliseconds; // Record the start time
                Game1.warpFarmer(teleportMap, (int)teleportLocation.Value.X, (int)teleportLocation.Value.Y, false);
                Game1.addHUDMessage(new HUDMessage("Teleported", HUDMessage.newQuest_type));
                this.Monitor.Log($"Teleported to: {teleportMap} at {teleportLocation}", LogLevel.Info);
                Game1.playSound("portalActive");
            }
        }

        private void OnPlayerWarped(object sender, WarpedEventArgs e)
        {
            if (isWarping)
            {
                isWarping = false;
            }
        }

        private void Display_RenderedStep(object sender, RenderedStepEventArgs e)
        {
            if (e.Step == StardewValley.Mods.RenderSteps.World_Sorted)
            {
                if (teleportLocation.HasValue)
                {
                    // Draw the portal image
                    if (portalTexture != null && teleportLocation.HasValue && Game1.currentLocation.Name == teleportMap)
                    {
                        Vector2 drawPosition = teleportLocation.Value * Game1.tileSize - new Vector2(Game1.viewport.X, Game1.viewport.Y);
                        float layerDepth = drawPosition.Y / 10000f + 0.004f;

                        e.SpriteBatch.Draw(
                            portalTexture,
                            drawPosition,
                            null,
                            Color.White,
                            0f,
                            Vector2.Zero,
                            4f,
                            SpriteEffects.None,
                            layerDepth
                        );
                    }

                    // Draw the animated orbBigTexture if warping
                    if (isWarping && orbBigTexture != null)
                    {
                        double elapsed = Game1.currentGameTime.TotalGameTime.TotalMilliseconds - warpStartTime;
                        if (elapsed <= WarpDisplayDuration)
                        {
                            orbBigFrameTimer += Game1.currentGameTime.ElapsedGameTime.Milliseconds;

                            if (orbBigFrameTimer >= OrbBigFrameInterval)
                            {
                                orbBigFrameTimer -= OrbBigFrameInterval;
                                orbBigFrame = (orbBigFrame + 1) % 5; // Assuming 5 frames in the animation
                            }

                            int frameWidth = 64; // Frame width (64x64px)
                            int frameHeight = 64; // Frame height (64x64px)
                            var sourceRectangle = new Microsoft.Xna.Framework.Rectangle(frameWidth * orbBigFrame, 0, frameWidth, frameHeight);
                            Vector2 orbBigPosition = Game1.player.Position - new Vector2(Game1.viewport.X, Game1.viewport.Y);
                            orbBigPosition.X -= (frameWidth / 2f) * 4f; // Center the texture on the player
                            orbBigPosition.Y -= (frameHeight / 2f) * 4f; // Center the texture on the player

                            // Calculate fade effect
                            float fade = 1f;
                            if (elapsed < 500)
                                fade = (float)(elapsed / 500); // Fade in
                            else if (elapsed > WarpDisplayDuration - 500)
                                fade = (float)((WarpDisplayDuration - elapsed) / 500); // Fade out

                            e.SpriteBatch.Draw(
                                orbBigTexture,
                                orbBigPosition,
                                sourceRectangle,
                                Color.White * fade,
                                0f,
                                Vector2.Zero,
                                4f, // Scale the sprite 4 times larger
                                SpriteEffects.None,
                                0.89f // Above the player
                            );
                        }
                    }

                    // Draw the portal effect
if (portalEffectTexture != null && teleportLocation.HasValue && Game1.currentLocation.Name == teleportMap)
{
    // Update the animation timer for the portal effect
    portalEffectTimer += Game1.currentGameTime.ElapsedGameTime.Milliseconds;

    // Loop through animation frames if the timer reaches the interval
    if (portalEffectTimer >= PortalEffectInterval)
    {
        portalEffectTimer -= PortalEffectInterval;
        portalEffectFrame = (portalEffectFrame + 1) % 4; // Assuming 4 frames in the animation
    }

    // Define the frame size for the portal effect
    int frameWidth = 16;  // Width of each frame in the texture (16px)
    int frameHeight = 16; // Height of each frame in the texture (16px)

    // Calculate the source rectangle of the current frame in the texture
    var sourceRectangle = new Microsoft.Xna.Framework.Rectangle(
        frameWidth * portalEffectFrame, // X position of the frame in the texture
        0,                              // Y position (top) in the texture
        frameWidth,                     // Width of the frame
        frameHeight                     // Height of the frame
    );

    // Determine the position to draw the portal effect in the game world
    Vector2 drawPosition = teleportLocation.Value * Game1.tileSize - new Vector2(Game1.viewport.X, Game1.viewport.Y);

    // Set the layer depth to draw the portal effect above certain elements
    float layerDepth = drawPosition.Y / 10000f + 0.005f;

    // Draw the portal effect texture at the calculated position and size
    e.SpriteBatch.Draw(
        portalEffectTexture, // The texture to draw (portal effect)
        drawPosition,        // The position to draw at (on the screen)
        sourceRectangle,     // The portion of the texture to draw (current frame)
        Color.White,         // Color to tint the texture (none in this case)
        0f,                  // Rotation (none)
        Vector2.Zero,        // Origin (top-left corner)
        4f,                  // Scale (enlarge 4 times)
        SpriteEffects.None,  // Effects (none)
        layerDepth           // Layer depth (for drawing order)
    );
}
                }
            }
        }

        private bool IsValidTeleportLocation(GameLocation location, Vector2 tile)
        {
            return location.isTilePassable(new Location((int)tile.X, (int)tile.Y), Game1.viewport)
                && location.isTileLocationOpen(tile);
        }
    }

    public class TeleportLocationData
    {
        public float X { get; set; }
        public float Y { get; set; }
        public string Map { get; set; }
    }
}