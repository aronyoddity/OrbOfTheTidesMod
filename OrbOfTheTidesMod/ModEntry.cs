using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System.Collections.Generic;

namespace OrbOfTheTidesMod
{
    public class ModEntry : Mod
    {
        private Vector2? teleportLocation;
        private string teleportMap;
        private Texture2D portalTexture;

        public override void Entry(IModHelper helper)
        {
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.Display.RenderedStep += Display_RenderedStep;

            Bootstrap();
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            portalTexture = Helper.ModContent.Load<Texture2D>("assets/portal.png");
            this.Monitor.Log("Portal texture loaded", LogLevel.Debug);
        }

        private void Bootstrap()
        {
            teleportLocation = null;
            teleportMap = null;
        }

        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            teleportLocation = null;
            teleportMap = null;
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            Farmer player = Game1.player;

            if (player.CurrentItem == null || e.Button != SButton.MouseRight)
                return;

            if (player.CurrentItem.QualifiedItemId == "(O)OrbOfTheTides")
            {
                this.Monitor.Log("Orb of the Tides detected", LogLevel.Debug);

                if (teleportLocation.HasValue && teleportMap != null)
                {
                    List<Response> responses = new List<Response>
                    {
                        new Response("Yes", "Yes"),
                        new Response("No", "No")
                    };
                    Game1.currentLocation.createQuestionDialogue(
                        "Would you like to use the Orb?",
                        responses.ToArray(),
                        OnQuestionAnswered
                    );
                }
                else
                {
                    SetTeleportLocation(player, e.Cursor.GrabTile);
                }
            }

            if (e.Button == SButton.G)
            {
                teleportLocation = null;
                teleportMap = null;
                Helper.Data.WriteSaveData("teleportLocation", (TeleportLocationData)null); // Reset saved location
                Game1.addHUDMessage(new HUDMessage("Teleport point reset!", HUDMessage.newQuest_type));
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
                Game1.addHUDMessage(new HUDMessage("Teleport location set!", HUDMessage.newQuest_type));
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
            if (answer == "Yes")
            {
                Game1.warpFarmer(teleportMap, (int)teleportLocation.Value.X, (int)teleportLocation.Value.Y, false);
                Game1.addHUDMessage(new HUDMessage("Teleported!", HUDMessage.newQuest_type));
                this.Monitor.Log($"Teleported to: {teleportMap} at {teleportLocation}", LogLevel.Info);
                Game1.playSound("wand");

                Game1.currentLocation.TemporarySprites.Add(new TemporaryAnimatedSprite(
                    textureName: "Mods/CapeStardew/Objects/Orb",
                    sourceRect: new Rectangle(0, 0, 16, 16),
                    animationInterval: 100f,
                    animationLength: 4,
                    numberOfLoops: 0,
                    position: teleportLocation.Value * Game1.tileSize,
                    flicker: false,
                    flipped: false,
                    layerDepth: 1f,
                    alphaFade: 0.01f,
                    color: Color.White,
                    scale: 1f,
                    scaleChange: 0f,
                    rotation: 0f,
                    rotationChange: 0f
                ));
            }
        }

       private void Display_RenderedStep(object sender, StardewModdingAPI.Events.RenderedStepEventArgs e)
{
    if (e.Step == StardewValley.Mods.RenderSteps.World_Sorted)
            {
                if (teleportLocation.HasValue && portalTexture != null)
                {
                    Vector2 drawPosition = teleportLocation.Value * Game1.tileSize - new Vector2(Game1.viewport.X, Game1.viewport.Y);
                    float layerDepth = (drawPosition.Y) / 10000f + 0.001f;

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
            }
        }

        private bool IsValidTeleportLocation(GameLocation location, Vector2 tile)
        {
            int x = (int)tile.X;
            int y = (int)tile.Y;

            bool isPassable = location.isTilePassable(new xTile.Dimensions.Location(x, y), Game1.viewport);
            bool hasImpassableProperty = location.doesTileHaveProperty(x, y, "Passable", "Back") == null;

            return isPassable && hasImpassableProperty;
        }
    }

    public class TeleportLocationData
    {
        public float X { get; set; }
        public float Y { get; set; }
        public string Map { get; set; }
    }
}
