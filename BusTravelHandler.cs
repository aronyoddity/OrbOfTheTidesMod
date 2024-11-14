using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using Microsoft.Xna.Framework;

namespace CapeStardewCode
{
    public class BusTravelHandler
    {
        private readonly IModHelper Helper;
        private const int TicketPrice = 500; // Set the ticket price here

        public BusTravelHandler(IModHelper helper)
        {
            this.Helper = helper;
            this.Helper.Events.Input.ButtonPressed += this.OnButtonPressed;
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady || Game1.activeClickableMenu != null)
                return;

            // Ensure player is at the BusStop and has clicked the ticket machine location
            if (Game1.currentLocation.Name.Contains("BusStop") && e.Button == SButton.MouseLeft || e.Button == SButton.ControllerX)
            {
                // Get the tile location of the cursor
                Vector2 cursorTile = e.Cursor.Tile;

                // Check if the cursor is over the ticket machine (e.g., tile coordinates (17, 10) to (17, 12))
                if (cursorTile.X == 17 && (cursorTile.Y == 10 || cursorTile.Y == 11 || cursorTile.Y == 12))
                {
                    // Prevent further loops by unsubscribing from the event temporarily
                    this.Helper.Events.Input.ButtonPressed -= this.OnButtonPressed;

                    // Check if the player has unlocked the CC vault or Joja vault
                    if ((Game1.player.mailReceived.Contains("ccVault") || Game1.player.mailReceived.Contains("JojaVault"))
                        && IsPamWorking()) // Check if Pam is available
                    {
                        // Add options for traveling
                        Response[] responses = {
                            new Response("CapeBusStop", $"Go to CapeBusStop ({TicketPrice}g)"),
                            new Response("Cancel", "Cancel")
                        };
                        Game1.currentLocation.createQuestionDialogue("Where would you like to go?", responses, this.TravelOptionSelected);
                    }
                    else
                    {
                        // Notify the player that the bus is out of service or Pam is unavailable
                        Game1.drawObjectDialogue("Pam is not available to drive the bus right now.");
                    }

                    // Re-subscribe after the interaction completes
                    this.Helper.Events.Input.ButtonPressed += this.OnButtonPressed;
                }
            }
        }



        private void TravelOptionSelected(Farmer who, string whichAnswer)
        {
            if (whichAnswer == "CapeBusStop")
            {
                // Check if player has enough gold
                if (Game1.player.Money >= TicketPrice)
                {
                    // Deduct the ticket price and warp the player
                    Game1.player.Money -= TicketPrice;
                    Game1.warpFarmer("Custom_CapeBusStop", 29, 25, 2); // Coordinates and facing direction
                }
                else
                {
                    // Notify the player they don’t have enough gold
                    Game1.drawObjectDialogue("You don't have enough gold to travel.");
                }
            }
            // No action needed for "Cancel", it will simply close the dialogue
        }

        private bool IsPamWorking()
        {
            int hour = Game1.timeOfDay / 100; // Get the current hour
            return hour >= 9 && hour < 17; // Pam works from 9 AM to 5 PM
        }
    }
}
