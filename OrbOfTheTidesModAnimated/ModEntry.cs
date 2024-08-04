using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System.Collections.Generic;
using xTile.Dimensions; // Import for Location

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
        private const double WarpDisplayDuration = 7000; // 7 seconds in milliseconds

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
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady || Game1.activeClickableMenu != null || Game1.dialogueUp)
                return;

            Farmer player = Game1.player;

            if (player.CurrentItem == null || player.CurrentItem.QualifiedItemId != "(O)OrbOfTheTides")
                return;

            if (e.Button == SButton.MouseRight)
            {
                if (teleportLocation.HasValue)
                {
                    List<Response> responses = new List<Response>
                    {
                        new Response("yes", Helper.Translation.Get("Yes_Translated")),
                        new Response("no", Helper.Translation.Get("No_Translated"))
                    };
                    Game1.currentLocation.createQuestionDialogue(
                        Helper.Translation.Get("ResetPortalQuestion_Translated"),
                        responses.ToArray(),
                        OnResetPortalQuestionAnswered
                    );
                }
                else
                {
                    SetTeleportLocation(player, e.Cursor.GrabTile);
                }
            }

            if (e.Button == SButton.MouseLeft)
            {
                if (teleportLocation.HasValue && teleportMap != null)
                {
                    List<Response> responses = new List<Response>
                    {
                        new Response("yes", Helper.Translation.Get("Yes_Translated")),
                        new Response("no", Helper.Translation.Get("No_Translated"))
                    };
                    Game1.currentLocation.createQuestionDialogue(
                        Helper.Translation.Get("OrbQuestion_Translated"),
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
                Game1.addHUDMessage(new HUDMessage(Helper.Translation.Get("ResetTeleport_Translated"), HUDMessage.newQuest_type));
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
                Game1.addHUDMessage(new HUDMessage(Helper.Translation.Get("SetTeleport_Translated"), HUDMessage.newQuest_type));
                this.Monitor.Log($"Teleport location set to: {teleportMap} at {teleportLocation}", LogLevel.Info);
            }
            else
            {
                Game1.addHUDMessage(new HUDMessage(Helper.Translation.Get("TeleportNotSet_Translated"), HUDMessage.error_type));
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
                Game1.addHUDMessage(new HUDMessage(Helper.Translation.Get("Teleported_Translated"), HUDMessage.newQuest_type));
                this.Monitor.Log($"Teleported to: {teleportMap} at {teleportLocation}", LogLevel.Info);
                Game1.playSound("wand");
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
            if (portalTexture != null)
            {
                Vector2 drawPosition = teleportLocation.Value * Game1.tileSize - new Vector2(Game1.viewport.X, Game1.viewport.Y);
                float layerDepth = drawPosition.Y / 10000f + 0.001f;

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

            // Inside the Display_RenderedStep method, in the OrbBig animation handling section

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
        Microsoft.Xna.Framework.Rectangle sourceRectangle = new Microsoft.Xna.Framework.Rectangle(frameWidth * orbBigFrame, 0, frameWidth, frameHeight);
        Vector2 orbBigPosition = Game1.player.Position - new Vector2(Game1.viewport.X, Game1.viewport.Y);
        orbBigPosition.X += 4f; // Offset by 4 pixels to the left
        orbBigPosition.Y -= 2f; // Offset by 2 pixels down

        e.SpriteBatch.Draw(
            orbBigTexture,
            orbBigPosition,
            sourceRectangle,
            Color.White,
            0f,
            new Vector2(frameWidth / 2, frameHeight / 2),
            4f, // Scale the sprite 4 times larger
            SpriteEffects.None,
            0.5f // Ensure it appears centered on the player
        );
    }
}

            // Handle portal effect animation
            if (portalEffectTexture != null)
            {
                portalEffectTimer += Game1.currentGameTime.ElapsedGameTime.Milliseconds;

                if (portalEffectTimer >= PortalEffectInterval)
                {
                    portalEffectTimer -= PortalEffectInterval;
                    portalEffectFrame = (portalEffectFrame + 1) % 4; // Update frame
                }

                int frameWidth = portalEffectTexture.Width / 4; // Adjust frame width
                int frameHeight = portalEffectTexture.Height;
                Microsoft.Xna.Framework.Rectangle sourceRectangle = new Microsoft.Xna.Framework.Rectangle(frameWidth * portalEffectFrame, 0, frameWidth, frameHeight);
                Vector2 portalEffectPosition = teleportLocation.Value * Game1.tileSize - new Vector2(Game1.viewport.X, Game1.viewport.Y);

                e.SpriteBatch.Draw(
                    portalEffectTexture,
                    portalEffectPosition,
                    sourceRectangle,
                    Color.White * 0.75f, // 75% transparency
                    0f,
                    Vector2.Zero,
                    4f,
                    SpriteEffects.None,
                    0.5f
                );
            }
        }
    }
}

        private bool IsValidTeleportLocation(GameLocation location, Vector2 tile)
        {
            if (location.isTilePassable(new Location((int)tile.X * Game1.tileSize, (int)tile.Y * Game1.tileSize), Game1.viewport))
                return true;
            return false;
        }

        private class TeleportLocationData
        {
            public float X { get; set; }
            public float Y { get; set; }
            public string Map { get; set; }
        }
    }
}
